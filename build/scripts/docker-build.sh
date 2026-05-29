#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "${SCRIPT_DIR}/common.sh"

TAG=""
PUSH=0
PLATFORMS="${DOCKER_PLATFORMS:-linux/amd64,linux/arm64}"
BUILDER="${DOCKER_BUILDX_BUILDER:-}"
TAGS_FILE=""
NO_BINFMT=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)
      TAG="${2:-}"
      shift 2
      ;;
    --push)
      PUSH=1
      shift
      ;;
    --platforms)
      PLATFORMS="${2:-}"
      shift 2
      ;;
    --builder)
      BUILDER="${2:-}"
      shift 2
      ;;
    --tags-file)
      TAGS_FILE="${2:-}"
      shift 2
      ;;
    --no-binfmt)
      NO_BINFMT=1
      shift
      ;;
    *)
      die "unknown argument: $1"
      ;;
  esac
done

[[ -n "$TAG" ]] || die "docker-build requires --tag <image:tag>"

require_command docker
require_command python3

ensure_dir "$ARTIFACTS_DOCKER"

declare -A tag_seen=()
declare -a unique_tags=()

add_tag() {
  local t="${1//$'\r'/}"
  [[ -z "$t" ]] && return 0
  [[ -n "${tag_seen[$t]:-}" ]] && return 0
  tag_seen["$t"]=1
  unique_tags+=("$t")
}

add_tag "$TAG"
if [[ -n "$TAGS_FILE" && -f "$TAGS_FILE" ]]; then
  while IFS= read -r line || [[ -n "$line" ]]; do
    add_tag "$line"
  done <"$TAGS_FILE"
fi

is_multi_platform=0
if [[ "$PLATFORMS" == *","* ]]; then
  is_multi_platform=1
fi

if [[ "$is_multi_platform" == "1" && "$PUSH" != "1" ]]; then
  die "multi-platform builds require --push (buildx cannot reliably --load manifest lists)"
fi

requires_binfmt=0
if [[ "$PLATFORMS" == *"linux/arm64"* || "$is_multi_platform" == "1" ]]; then
  requires_binfmt=1
fi

build_args=(
  -f "$DOCKERFILE"
  --target runtime
  --platform "$PLATFORMS"
)

for t in "${unique_tags[@]}"; do
  build_args+=(-t "$t")
done

buildx_metadata="${ARTIFACTS_DOCKER}/buildx-metadata.json"
uses_buildx=0

if docker buildx version >/dev/null 2>&1; then
  uses_buildx=1
  build_args+=(--metadata-file "$buildx_metadata")
fi

if [[ "$PUSH" == "1" ]]; then
  build_args+=(--push)
else
  build_args+=(--load)
fi

if [[ "$uses_buildx" == "1" ]]; then
  if [[ "$requires_binfmt" == "1" && "$NO_BINFMT" != "1" ]]; then
    log "docker-build installing binfmt handlers for cross-platform emulation"
    docker run --rm --privileged tonistiigi/binfmt --install all
  fi
  if [[ -n "$BUILDER" ]]; then
    docker buildx use "$BUILDER"
  elif ! docker buildx inspect >/dev/null 2>&1; then
    docker buildx create --name directorysync --driver docker-container --use >/dev/null
  fi
  docker buildx inspect --bootstrap >/dev/null
  log "docker-build buildx platforms=${PLATFORMS} tags=${#unique_tags[@]} push=${PUSH}"
  docker buildx build "${build_args[@]}" "$REPO_ROOT"
else
  if [[ "$PUSH" == "1" ]]; then
    die "docker buildx is required for --push"
  fi
  if [[ "$PLATFORMS" != *","* ]]; then
    log "docker-build docker build platform=${PLATFORMS} tag=${TAG}"
    docker build -f "$DOCKERFILE" --target runtime -t "$TAG" "$REPO_ROOT"
  else
    die "multiple --platforms require docker buildx"
  fi
fi

digest=""
if [[ -f "$buildx_metadata" ]]; then
  digest="$(python3 -c "import json; print(json.load(open('${buildx_metadata}')).get('containerimage.digest','') or '')" 2>/dev/null || true)"
fi

if [[ -z "$digest" && "$PUSH" == "1" && "$uses_buildx" == "1" ]]; then
  digest="$(docker buildx imagetools inspect "$TAG" --format '{{json .}}' 2>/dev/null | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('manifest',{}).get('digest','') or d.get('digest',''))" 2>/dev/null || true)"
fi

primary_tag="${unique_tags[0]}"
image_name="${primary_tag%%:*}"

printf '%s\n' "${unique_tags[@]}" >"${ARTIFACTS_DOCKER}/image-tags.txt"

export DOCKER_META_PRIMARY="$primary_tag"
export DOCKER_META_IMAGE="$image_name"
export DOCKER_META_DIGEST="${digest:-}"
export DOCKER_META_TAGS_FILE="${ARTIFACTS_DOCKER}/image-tags.txt"
export DOCKER_META_JSON="${ARTIFACTS_DOCKER}/image-metadata.json"

python3 - <<'PY'
import json
import os
from pathlib import Path

tags_path = Path(os.environ["DOCKER_META_TAGS_FILE"])
metadata_path = Path(os.environ["DOCKER_META_JSON"])

tags = [line.strip() for line in tags_path.read_text(encoding="utf-8").splitlines() if line.strip()]
if not tags:
    tags = [os.environ["DOCKER_META_PRIMARY"]]

metadata = {
    "image": os.environ["DOCKER_META_IMAGE"],
    "primaryTag": os.environ["DOCKER_META_PRIMARY"],
    "tags": tags,
    "digest": os.environ.get("DOCKER_META_DIGEST") or None,
}
metadata_path.write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
PY

log "docker-build wrote=${ARTIFACTS_DOCKER}/image-tags.txt"
log "docker-build wrote=${ARTIFACTS_DOCKER}/image-metadata.json digest=${digest:-<none>}"
log "docker-build complete"
