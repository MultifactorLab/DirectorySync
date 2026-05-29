#!/usr/bin/env bash
# Shared path constants and helpers for build scripts.
set -euo pipefail

_COMMON_SH_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$(CDPATH= cd -- "${_COMMON_SH_DIR}/.." && pwd)"
REPO_ROOT="$(CDPATH= cd -- "${BUILD_DIR}/.." && pwd)"

export REPO_ROOT BUILD_DIR

SOLUTION="${REPO_ROOT}/src/DirectorySync.sln"
SOLUTION_LINUX_FILTER="${REPO_ROOT}/src/DirectorySync.Linux.slnf"
CONSOLE_PROJECT="${REPO_ROOT}/src/hosts/DirectorySync.Host.Console/DirectorySync.Host.Console.csproj"
WINDOWS_SERVICE_PROJECT="${REPO_ROOT}/src/hosts/DirectorySync.Host.WindowsService/DirectorySync.Host.WindowsService.csproj"
NUGET_CONFIG_DEFAULT="${REPO_ROOT}/nuget.config"
NUGET_CONFIG_LOCAL="${REPO_ROOT}/nuget.config.local"
NUGET_CONFIG="$NUGET_CONFIG_DEFAULT"
LINUX_DEPLOY="${REPO_ROOT}/deploy/linux"
DOCKERFILE="${REPO_ROOT}/deploy/docker/Dockerfile"
WIX_PROJECT="${REPO_ROOT}/deploy/windows/DirectorySync.Installer/DirectorySync.Installer.wixproj"

ARTIFACTS_ROOT="${REPO_ROOT}/artifacts"
ARTIFACTS_PUBLISH="${ARTIFACTS_ROOT}/publish"
ARTIFACTS_PACKAGES="${ARTIFACTS_ROOT}/packages"
ARTIFACTS_LINUX_PACKAGES="${ARTIFACTS_PACKAGES}/linux"
ARTIFACTS_WINDOWS_PACKAGES="${ARTIFACTS_PACKAGES}/windows"
ARTIFACTS_DOCKER="${ARTIFACTS_ROOT}/docker"
RELEASE_MANIFEST="${ARTIFACTS_PACKAGES}/release-manifest.json"
RELEASES_ROOT="${REPO_ROOT}/releases"

BUILD_CONFIGURATION="${BUILD_CONFIGURATION:-Release}"
DOTNET_WARN_AS_MESSAGE="${DOTNET_WARN_AS_MESSAGE:--warnasmessage:*}"

log() {
  printf '[build] %s\n' "$*" >&2
}

# WiX/MSI and WindowsService host require Windows; use solution filter on Linux/macOS CI and local dev.
should_skip_windows_build() {
  if [[ "${SKIP_WINDOWS_BUILD:-}" == "1" ]]; then
    return 0
  fi
  case "$(uname -s 2>/dev/null || echo unknown)" in
    Linux|Darwin) return 0 ;;
    *) return 1 ;;
  esac
}

resolve_build_solution() {
  if should_skip_windows_build && [[ -f "$SOLUTION_LINUX_FILTER" ]]; then
    printf '%s' "$SOLUTION_LINUX_FILTER"
    return 0
  fi
  printf '%s' "$SOLUTION"
}

die() {
  log "ERROR: $*"
  exit 1
}

require_command() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || die "required command not found: ${cmd}"
}

ensure_dir() {
  mkdir -p "$1"
}

resolve_version() {
  if [[ -n "${VERSION:-}" ]]; then
    printf '%s' "$VERSION"
    return 0
  fi
  if [[ -n "${CI_COMMIT_TAG:-}" ]]; then
    printf '%s' "${CI_COMMIT_TAG#v}"
    return 0
  fi
  if [[ -n "${GITHUB_REF_NAME:-}" ]]; then
    printf '%s' "${GITHUB_REF_NAME#v}"
    return 0
  fi
  printf '%s' "0.0.0-dev"
}

resolve_git_short_sha() {
  if command -v git >/dev/null 2>&1; then
    git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || true
  fi
}

resolve_git_commit() {
  if command -v git >/dev/null 2>&1; then
    git -C "$REPO_ROOT" rev-parse HEAD 2>/dev/null || true
  fi
}

read_installer_version() {
  local constants="${LINUX_DEPLOY}/lib/constants.sh"
  [[ -f "$constants" ]] || die "installer constants missing: ${constants}"
  sed -n 's/^INSTALLER_VERSION="\(.*\)"/\1/p' "$constants" | head -n1
}

file_sha256() {
  sha256sum "$1" | awk '{print $1}'
}

file_size_bytes() {
  local path="$1"
  if stat -c%s "$path" >/dev/null 2>&1; then
    stat -c%s "$path"
  else
    wc -c <"$path" | tr -d '[:space:]'
  fi
}

offline_release_dir() {
  local version="$1"
  printf '%s/v%s' "$RELEASES_ROOT" "$version"
}

# AssemblyVersion/FileVersion require numeric major.minor.build.revision (no prerelease labels).
normalize_assembly_version() {
  local version="$1"
  if [[ "$version" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+) ]]; then
    printf '%s.%s.%s.0' "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}" "${BASH_REMATCH[3]}"
    return 0
  fi
  if [[ "$version" =~ ^([0-9]+)\.([0-9]+) ]]; then
    printf '%s.%s.0.0' "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}"
    return 0
  fi
  printf '%s' '0.0.0.0'
}

# Populates DOTNET_VERSION_MSBUILD_ARGS for dotnet build/publish.
dotnet_version_msbuild_args() {
  local version assembly_version sha informational
  version="$(resolve_version)"
  assembly_version="$(normalize_assembly_version "$version")"
  sha="$(resolve_git_short_sha)"
  informational="$version"
  if [[ -n "$sha" ]]; then
    informational="${version}+${sha}"
  fi
  DOTNET_VERSION_MSBUILD_ARGS=(
    "/p:Version=${version}"
    "/p:PackageVersion=${version}"
    "/p:AssemblyVersion=${assembly_version}"
    "/p:FileVersion=${assembly_version}"
    "/p:InformationalVersion=${informational}"
  )
}

linux_tarball_name() {
  local version="$1"
  local rid="$2"
  printf 'directorysync_%s_%s.tar.gz' "$version" "$rid"
}

linux_offline_kit_name() {
  local version="$1"
  local rid="$2"
  printf 'directorysync_%s_%s-offline.tar.gz' "$version" "$rid"
}

publish_output_dir() {
  local rid="$1"
  printf '%s/%s' "$ARTIFACTS_PUBLISH" "$rid"
}

resolve_nuget_config() {
  local config="$NUGET_CONFIG_DEFAULT"

  if [[ "${USE_LOCAL_NUGET:-}" == "1" ]]; then
    [[ -f "$NUGET_CONFIG_LOCAL" ]] || die "USE_LOCAL_NUGET=1 requires ${NUGET_CONFIG_LOCAL}"
    config="$NUGET_CONFIG_LOCAL"
    log "nuget config=${config} (USE_LOCAL_NUGET=1)"
  fi

  NUGET_CONFIG="$config"
  export NUGET_CONFIG
}

resolve_nuget_config
