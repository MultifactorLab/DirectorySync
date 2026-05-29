#!/usr/bin/env bash
# Build release-manifest.json from packaged artifacts and docker metadata.
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

require_command python3

version="$(resolve_version)"
commit="$(resolve_git_commit)"
commit_short="$(resolve_git_short_sha)"
installer_version="$(read_installer_version)"

ensure_dir "$ARTIFACTS_PACKAGES"

export MANIFEST_VERSION="$version"
export MANIFEST_COMMIT="${commit:-}"
export MANIFEST_COMMIT_SHORT="${commit_short:-}"
export MANIFEST_INSTALLER_VERSION="$installer_version"
export MANIFEST_LINUX_DIR="$ARTIFACTS_LINUX_PACKAGES"
export MANIFEST_WINDOWS_DIR="$ARTIFACTS_WINDOWS_PACKAGES"
export MANIFEST_DOCKER_DIR="$ARTIFACTS_DOCKER"
export MANIFEST_DOCKER_RELEASE="${REPO_ROOT}/deploy/docker/release"
export MANIFEST_OUTPUT="$RELEASE_MANIFEST"

python3 - <<'PY'
import json
import os
import re
from pathlib import Path

version = os.environ["MANIFEST_VERSION"]
linux_dir = Path(os.environ["MANIFEST_LINUX_DIR"])
windows_dir = Path(os.environ["MANIFEST_WINDOWS_DIR"])
docker_dir = Path(os.environ["MANIFEST_DOCKER_DIR"])
docker_release_dir = Path(os.environ["MANIFEST_DOCKER_RELEASE"])
output = Path(os.environ["MANIFEST_OUTPUT"])

tarball_re = re.compile(r"^directorysync_(?P<version>[^_]+)_(?P<rid>linux-(?:x64|arm64))\.tar\.gz$")


def file_entry(path: Path) -> dict:
    data = path.read_bytes()
    import hashlib

    digest = hashlib.sha256(data).hexdigest()
    return {
        "fileName": path.name,
        "sha256": digest,
        "sizeBytes": path.stat().st_size,
    }


linux_assets = []
if linux_dir.is_dir():
    for path in sorted(linux_dir.glob("directorysync_*.tar.gz")):
        match = tarball_re.match(path.name)
        if not match:
            continue
        entry = file_entry(path)
        entry["rid"] = match.group("rid")
        entry["version"] = match.group("version")
        linux_assets.append(entry)

windows_asset = None
if windows_dir.is_dir():
    msi_files = sorted(windows_dir.glob("DirectorySync-*.msi"), key=lambda p: p.stat().st_mtime, reverse=True)
    if msi_files:
        windows_asset = file_entry(msi_files[0])

docker_meta = None
metadata_candidates = [
    docker_dir / "image-metadata.json",
    docker_release_dir / "image-metadata.json",
]
for metadata_path in metadata_candidates:
    if metadata_path.is_file():
        docker_meta = json.loads(metadata_path.read_text(encoding="utf-8"))
        break

if docker_meta is None:
    tags_candidates = [
        docker_dir / "image-tags.txt",
        docker_release_dir / "image-tags.txt",
    ]
    for tags_path in tags_candidates:
        if not tags_path.is_file():
            continue
        tags = [line.strip() for line in tags_path.read_text(encoding="utf-8").splitlines() if line.strip()]
        if tags:
            docker_meta = {
                "image": tags[0].split(":")[0] if ":" in tags[0] else tags[0],
                "primaryTag": tags[0],
                "tags": tags,
                "digest": None,
            }
            break

manifest = {
    "schemaVersion": 1,
    "version": version,
    "gitCommit": os.environ.get("MANIFEST_COMMIT") or None,
    "gitCommitShort": os.environ.get("MANIFEST_COMMIT_SHORT") or None,
    "installerVersion": os.environ["MANIFEST_INSTALLER_VERSION"],
    "linux": linux_assets,
    "windows": windows_asset,
    "docker": docker_meta,
}

output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(output)
PY

log "generate-release-manifest wrote=${RELEASE_MANIFEST}"

if [[ -d "$ARTIFACTS_LINUX_PACKAGES" ]]; then
  cp -f "$RELEASE_MANIFEST" "${ARTIFACTS_LINUX_PACKAGES}/release-manifest.json"
  log "generate-release-manifest staged=${ARTIFACTS_LINUX_PACKAGES}/release-manifest.json"
fi
