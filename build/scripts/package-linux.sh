#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "${SCRIPT_DIR}/common.sh"

RID=""
STAGE_RELEASE_ASSETS=1
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
    --no-stage-release-assets)
      STAGE_RELEASE_ASSETS=0
      shift
      ;;
    *)
      die "unknown argument: $1"
      ;;
  esac
done

[[ -n "$RID" ]] || die "package-linux requires --rid <runtime-id>"
require_command tar
require_command sha256sum

version="$(resolve_version)"
publish_dir="$(publish_output_dir "$RID")"
[[ -d "$publish_dir" ]] || die "publish output missing (run publish-linux first): ${publish_dir}"

ensure_dir "$ARTIFACTS_LINUX_PACKAGES"

staging="$(mktemp -d)"
trap 'rm -rf "$staging"' EXIT

cp -a "${publish_dir}/." "${staging}/"

tarball="$(linux_tarball_name "$version" "$RID")"
tarball_path="${ARTIFACTS_LINUX_PACKAGES}/${tarball}"

tar -C "$staging" -czf "$tarball_path" .
log "package-linux wrote=${tarball_path}"

checksums="${ARTIFACTS_LINUX_PACKAGES}/checksums.txt"
hash="$(sha256sum "$tarball_path" | awk '{print $1}')"
line="${hash}  ${tarball}"
if [[ -f "$checksums" ]]; then
  grep -Fv "  ${tarball}" "$checksums" >"${checksums}.tmp" || true
else
  : >"${checksums}.tmp"
fi
printf '%s\n' "$line" >>"${checksums}.tmp"
mv "${checksums}.tmp" "$checksums"
log "package-linux updated=${checksums}"

if [[ "$STAGE_RELEASE_ASSETS" == "1" ]]; then
  cp -f "${LINUX_DEPLOY}/install.sh" "${ARTIFACTS_LINUX_PACKAGES}/install.sh"
  cp -f "${LINUX_DEPLOY}/uninstall.sh" "${ARTIFACTS_LINUX_PACKAGES}/uninstall.sh"
  chmod 0755 "${ARTIFACTS_LINUX_PACKAGES}/install.sh" "${ARTIFACTS_LINUX_PACKAGES}/uninstall.sh"
  rm -rf "${ARTIFACTS_LINUX_PACKAGES}/lib"
  cp -a "${LINUX_DEPLOY}/lib" "${ARTIFACTS_LINUX_PACKAGES}/lib"
  log "package-linux staged release scripts and lib/ in ${ARTIFACTS_LINUX_PACKAGES}"
fi

log "package-linux complete"
