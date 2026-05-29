#!/usr/bin/env bash
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

[[ -n "$RID" ]] || die "publish-linux requires --rid <runtime-id>"

require_command dotnet

version="$(resolve_version)"
out="$(publish_output_dir "$RID")"
dotnet_version_msbuild_args

log "publish-linux rid=${RID} version=${version} output=${out}"
ensure_dir "$out"

dotnet publish "$CONSOLE_PROJECT" \
  -c "$BUILD_CONFIGURATION" \
  -r "$RID" \
  --self-contained true \
  -o "$out" \
  --configfile "$NUGET_CONFIG" \
  "${DOTNET_VERSION_MSBUILD_ARGS[@]}" \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:DebugType=None \
  /p:DebugSymbols=false

log "publish-linux complete: ${out}"
