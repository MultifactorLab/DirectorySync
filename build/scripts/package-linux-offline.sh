#!/usr/bin/env bash
# Package per-RID offline install kits: install scripts, lib/, app tarball, checksums.
set -euo pipefail

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "${SCRIPT_DIR}/common.sh"

RID=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --rid)
      RID="${2:-}"
      shift 2
      ;;
    --version)
      VERSION="${2:-}"
      shift 2
      ;;
    *)
      die "unknown argument: $1"
      ;;
  esac
done

require_command tar
require_command sha256sum

version="$(resolve_version)"
[[ -d "$ARTIFACTS_LINUX_PACKAGES" ]] || die "linux packages missing (run stage-linux-release first): ${ARTIFACTS_LINUX_PACKAGES}"

for asset in install.sh uninstall.sh release-manifest.json; do
  [[ -f "${ARTIFACTS_LINUX_PACKAGES}/${asset}" ]] || die "missing ${asset} (run stage-linux-release first)"
done
[[ -d "${ARTIFACTS_LINUX_PACKAGES}/lib" ]] || die "missing lib/ (run stage-linux-release first)"

package_rid() {
  local rid="$1"
  local app_name staging tarball_path kit_path kit_name
  app_name="$(linux_tarball_name "$version" "$rid")"
  tarball_path="${ARTIFACTS_LINUX_PACKAGES}/${app_name}"
  [[ -f "$tarball_path" ]] || die "app tarball missing for ${rid}: ${tarball_path}"

  kit_name="$(linux_offline_kit_name "$version" "$rid")"
  kit_path="${ARTIFACTS_LINUX_PACKAGES}/${kit_name}"
  staging="$(mktemp -d)"

  cp -f "${ARTIFACTS_LINUX_PACKAGES}/install.sh" "${staging}/install.sh"
  cp -f "${ARTIFACTS_LINUX_PACKAGES}/uninstall.sh" "${staging}/uninstall.sh"
  cp -f "${ARTIFACTS_LINUX_PACKAGES}/release-manifest.json" "${staging}/release-manifest.json"
  rm -rf "${staging}/lib"
  cp -a "${ARTIFACTS_LINUX_PACKAGES}/lib" "${staging}/lib"
  cp -f "$tarball_path" "${staging}/${app_name}"
  chmod 0755 "${staging}/install.sh" "${staging}/uninstall.sh"
  (
    cd "$staging"
    sha256sum "${app_name}" >checksums.txt
  )

  tar -czf "$kit_path" -C "$staging" .
  rm -rf "$staging"
  log "package-linux-offline wrote=${kit_path}"
}

if [[ -n "$RID" ]]; then
  package_rid "$RID"
else
  packaged=0
  for rid in linux-x64 linux-arm64; do
    app_name="$(linux_tarball_name "$version" "$rid")"
    if [[ -f "${ARTIFACTS_LINUX_PACKAGES}/${app_name}" ]]; then
      package_rid "$rid"
      packaged=1
    fi
  done
  [[ "$packaged" == "1" ]] || die "no app tarballs found in ${ARTIFACTS_LINUX_PACKAGES}"
fi

log "package-linux-offline complete dir=${ARTIFACTS_LINUX_PACKAGES}"
