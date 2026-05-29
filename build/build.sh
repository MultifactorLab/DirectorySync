#!/usr/bin/env bash
# DirectorySync build orchestration entrypoint.
set -euo pipefail

BUILD_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS_DIR="${BUILD_DIR}/scripts"

usage() {
  cat <<'EOF'
Usage: build/build.sh <command> [options]

Commands:
  restore
  build
  test
  publish-linux --rid <rid> [--version <version>]
  package-linux --rid <rid> [--version <version>] [--no-stage-release-assets]
  stage-linux-release                   copy install/uninstall scripts and refresh checksums.txt
  package-linux-offline [--rid <rid>] [--version <version>]   offline install .tar.gz per RID
  generate-release-manifest [--version <version>]
  stage-offline-release [--version <version>]   copy packages to releases/v<version>/
  package-msi [--version <version>]   (delegates to package-windows-msi.ps1 via pwsh when available)
  docker-build --tag <tag> [--push] [--platforms <list>] [--builder <name>] [--tags-file <path>] [--no-binfmt]
  clean
  all [--version <version>]             restore, build, test, linux-x64/arm64 publish+package

Global options:
  --local-nuget                         Use nuget.config.local (sets USE_LOCAL_NUGET=1)

Environment:
  VERSION, CI_COMMIT_TAG, GITHUB_REF_NAME  Version resolution for packaging
  BUILD_CONFIGURATION                      Default: Release
  USE_LOCAL_NUGET                          1 = nuget.config.local; 0/empty = nuget.config
EOF
}

run_script() {
  local script="$1"
  shift
  bash "${SCRIPTS_DIR}/${script}" "$@"
}

require_dotnet() {
  command -v dotnet >/dev/null 2>&1 || {
    echo "[build] ERROR: dotnet CLI not found" >&2
    exit 1
  }
}

cmd_restore() {
  require_dotnet
  # shellcheck source=scripts/common.sh
  source "${SCRIPTS_DIR}/common.sh"
  local solution
  solution="$(resolve_build_solution)"
  log "restore solution=${solution}"
  dotnet restore "$solution" --configfile "$NUGET_CONFIG"
}

cmd_build() {
  require_dotnet
  source "${SCRIPTS_DIR}/common.sh"
  local solution
  solution="$(resolve_build_solution)"
  dotnet_version_msbuild_args
  log "build configuration=${BUILD_CONFIGURATION} version=$(resolve_version) solution=${solution}"
  dotnet build "$solution" -c "$BUILD_CONFIGURATION" --no-restore "$DOTNET_WARN_AS_MESSAGE" "${DOTNET_VERSION_MSBUILD_ARGS[@]}"
}

cmd_test() {
  require_dotnet
  source "${SCRIPTS_DIR}/common.sh"
  local solution
  solution="$(resolve_build_solution)"
  log "test configuration=${BUILD_CONFIGURATION} solution=${solution}"
  dotnet test "$solution" -c "$BUILD_CONFIGURATION" --no-build --verbosity normal "$DOTNET_WARN_AS_MESSAGE"
}

cmd_clean() {
  source "${SCRIPTS_DIR}/common.sh"
  log "clean artifacts=${ARTIFACTS_ROOT}"
  rm -rf "$ARTIFACTS_ROOT"
}

cmd_stage_linux_release() {
  source "${SCRIPTS_DIR}/common.sh"
  require_command sha256sum
  ensure_dir "$ARTIFACTS_LINUX_PACKAGES"
  cp -f "${LINUX_DEPLOY}/install.sh" "${ARTIFACTS_LINUX_PACKAGES}/install.sh"
  cp -f "${LINUX_DEPLOY}/uninstall.sh" "${ARTIFACTS_LINUX_PACKAGES}/uninstall.sh"
  chmod 0755 "${ARTIFACTS_LINUX_PACKAGES}/install.sh" "${ARTIFACTS_LINUX_PACKAGES}/uninstall.sh"
  rm -rf "${ARTIFACTS_LINUX_PACKAGES}/lib"
  cp -a "${LINUX_DEPLOY}/lib" "${ARTIFACTS_LINUX_PACKAGES}/lib"
  (
    cd "$ARTIFACTS_LINUX_PACKAGES"
    local tarballs=()
    shopt -s nullglob
    for tarball in directorysync_*.tar.gz; do
      [[ "$tarball" == *-offline.tar.gz ]] && continue
      tarballs+=("$tarball")
    done
    shopt -u nullglob
    [[ ${#tarballs[@]} -gt 0 ]] || die "no linux tarballs found in ${ARTIFACTS_LINUX_PACKAGES}"
    sha256sum "${tarballs[@]}" >checksums.txt
  )
  run_script generate-release-manifest.sh
  log "stage-linux-release complete dir=${ARTIFACTS_LINUX_PACKAGES}"
}

cmd_all() {
  local version=""
  local version_args=()
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --version)
        version="${2:-}"
        export VERSION="$version"
        version_args=(--version "$version")
        shift 2
        ;;
      *)
        usage
        exit 1
        ;;
    esac
  done

  cmd_restore
  cmd_build
  cmd_test

  local rid
  for rid in linux-x64 linux-arm64; do
    run_script publish-linux.sh --rid "$rid" "${version_args[@]}"
    if [[ "$rid" == "linux-arm64" ]]; then
      run_script package-linux.sh --rid "$rid" "${version_args[@]}"
    else
      run_script package-linux.sh --rid "$rid" "${version_args[@]}" --no-stage-release-assets
    fi
  done
}

cmd_package_msi() {
  if command -v pwsh >/dev/null 2>&1; then
    pwsh -NoProfile -File "${SCRIPTS_DIR}/package-windows-msi.ps1" "$@"
  elif command -v powershell >/dev/null 2>&1; then
    powershell -NoProfile -File "${SCRIPTS_DIR}/package-windows-msi.ps1" "$@"
  else
    echo "[build] ERROR: package-msi requires PowerShell" >&2
    exit 1
  fi
}

main() {
  local command=""
  local -a command_args=()
  local arg
  for arg in "$@"; do
    if [[ "$arg" == "--local-nuget" ]]; then
      export USE_LOCAL_NUGET=1
      continue
    fi
    if [[ -z "$command" ]]; then
      command="$arg"
    else
      command_args+=("$arg")
    fi
  done

  if [[ -z "$command" ]]; then
    usage
    exit 1
  fi

  case "$command" in
    restore) cmd_restore ;;
    build) cmd_build ;;
    test) cmd_test ;;
    publish-linux) run_script publish-linux.sh "${command_args[@]}" ;;
    package-linux) run_script package-linux.sh "${command_args[@]}" ;;
    stage-linux-release) cmd_stage_linux_release ;;
    package-linux-offline) run_script package-linux-offline.sh "${command_args[@]}" ;;
    generate-release-manifest) run_script generate-release-manifest.sh "${command_args[@]}" ;;
    stage-offline-release) run_script stage-offline-release.sh "${command_args[@]}" ;;
    package-msi) cmd_package_msi "${command_args[@]}" ;;
    docker-build) run_script docker-build.sh "${command_args[@]}" ;;
    clean) cmd_clean ;;
    all) cmd_all "${command_args[@]}" ;;
    -h|--help|help) usage ;;
    *)
      echo "[build] ERROR: unknown command: ${command}" >&2
      usage
      exit 1
      ;;
  esac
}

main "$@"
