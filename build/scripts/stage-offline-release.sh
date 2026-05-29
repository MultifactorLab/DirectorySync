#!/usr/bin/env bash
# Copy built packages into releases/v<version>/ for local offline distribution.
set -euo pipefail

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "${SCRIPT_DIR}/common.sh"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      VERSION="${2:-}"
      shift 2
      ;;
    *)
      die "unknown argument: $1"
      ;;
  esac
done

version="$(resolve_version)"
dest="$(offline_release_dir "$version")"
linux_dest="${dest}/linux"
windows_dest="${dest}/windows"

[[ -d "$ARTIFACTS_LINUX_PACKAGES" ]] || die "linux packages missing (run package-linux first): ${ARTIFACTS_LINUX_PACKAGES}"

ensure_dir "$linux_dest"
ensure_dir "$windows_dest"

shopt -s nullglob
tarballs=("${ARTIFACTS_LINUX_PACKAGES}"/directorysync_*.tar.gz)
shopt -u nullglob
[[ ${#tarballs[@]} -gt 0 ]] || die "no linux tarballs in ${ARTIFACTS_LINUX_PACKAGES}"

cp -f "${tarballs[@]}" "$linux_dest/"

for asset in install.sh uninstall.sh checksums.txt release-manifest.json; do
  if [[ -f "${ARTIFACTS_LINUX_PACKAGES}/${asset}" ]]; then
    cp -f "${ARTIFACTS_LINUX_PACKAGES}/${asset}" "$linux_dest/${asset}"
  fi
done

if [[ -d "${ARTIFACTS_LINUX_PACKAGES}/lib" ]]; then
  rm -rf "${linux_dest}/lib"
  cp -a "${ARTIFACTS_LINUX_PACKAGES}/lib" "${linux_dest}/lib"
fi

if [[ -f "${ARTIFACTS_LINUX_PACKAGES}/checksums.txt" ]]; then
  cp -f "${ARTIFACTS_LINUX_PACKAGES}/checksums.txt" "${dest}/checksums.txt"
fi

if [[ -f "$RELEASE_MANIFEST" ]]; then
  cp -f "$RELEASE_MANIFEST" "${dest}/release-manifest.json"
else
  bash "${SCRIPT_DIR}/generate-release-manifest.sh" --version "$version"
  cp -f "$RELEASE_MANIFEST" "${dest}/release-manifest.json"
fi

shopt -s nullglob
msi_files=("${ARTIFACTS_WINDOWS_PACKAGES}"/DirectorySync-*.msi)
shopt -u nullglob
if [[ ${#msi_files[@]} -gt 0 ]]; then
  cp -f "${msi_files[@]}" "$windows_dest/"
fi

log "stage-offline-release complete dir=${dest}"
