# shellcheck shell=bash
# Release download, verification, and safe extraction.

detect_platform() {
  case "$(uname -m)" in
    x86_64|amd64) RID="linux-x64" ;;
    aarch64|arm64) RID="linux-arm64" ;;
    *) fail "unsupported architecture: $(uname -m)" ;;
  esac
  log INFO "detect_platform rid=${RID}"
}

resolve_release_tag() {
  if [[ "$RELEASE_TAG" == "latest" ]]; then
    RELEASE_TAG="$(
      curl -fsSL \
        -H "Accept: application/vnd.github+json" \
        "https://api.github.com/repos/${RELEASE_REPO}/releases/latest" \
        | sed -n 's/.*"tag_name": *"\([^"]*\)".*/\1/p' \
        | head -n1
    )"
  fi
  [[ -n "$RELEASE_TAG" ]] || fail "failed to resolve release version"
  RELEASE_VERSION="${RELEASE_TAG#v}"
  compute_plan_id
  log INFO "resolve_release_tag tag=${RELEASE_TAG}"
}

parse_tarball_version() {
  local base
  base="$(basename "$TARBALL_PATH")"
  if [[ "$base" =~ ^directorysync_([^_]+)_linux-(x64|arm64)\.tar\.gz$ ]]; then
    RELEASE_TAG="v${BASH_REMATCH[1]}"
    RELEASE_VERSION="${BASH_REMATCH[1]}"
    compute_plan_id
  elif [[ -z "${RELEASE_VERSION:-}" ]]; then
    RELEASE_VERSION="local"
    RELEASE_TAG="local"
  fi
}

prepare_work_dir() {
  WORK_DIR="$(mktemp -d /tmp/directorysync-install.XXXXXX)"
  chmod 0700 "$WORK_DIR"
  log INFO "prepare_work_dir dir=${WORK_DIR}"
}

download_release() {
  if [[ "$SKIP_DOWNLOAD" == "1" ]]; then
    log INFO "download_release skipped"
    return 0
  fi
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "download_release tag=${RELEASE_TAG} rid=${RID}"
    ASSET="directorysync_${RELEASE_VERSION}_${RID}.tar.gz"
    return 0
  fi
  resolve_release_tag
  ASSET="directorysync_${RELEASE_VERSION}_${RID}.tar.gz"
  local base_url="https://github.com/${RELEASE_REPO}/releases/download/${RELEASE_TAG}"
  curl -fL --retry 3 -o "${WORK_DIR}/${ASSET}" "${base_url}/${ASSET}"
  curl -fL --retry 3 -o "${WORK_DIR}/checksums.txt" "${base_url}/checksums.txt"
  download_release_manifest_optional "${base_url}"
  log INFO "download_release asset=${ASSET}"
}

download_release_manifest_optional() {
  local base_url="${1:-}"
  [[ -n "$base_url" ]] || return 0
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "download_release_manifest_optional"
    return 0
  fi
  if curl -fL --retry 3 -o "${WORK_DIR}/release-manifest.json" "${base_url}/release-manifest.json" 2>/dev/null; then
    log INFO "download_release_manifest_optional ok"
  fi
}

stage_offline_tarball() {
  [[ -n "$TARBALL_PATH" ]] || return 0
  [[ -f "$TARBALL_PATH" ]] || fail "tarball not found: ${TARBALL_PATH}"
  parse_tarball_version
  ASSET="$(basename "$TARBALL_PATH")"
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "stage_offline_tarball asset=${ASSET}"
    SKIP_DOWNLOAD=1
    return 0
  fi
  cp -f "$TARBALL_PATH" "${WORK_DIR}/${ASSET}"
  local checksums_dir
  checksums_dir="$(dirname "$TARBALL_PATH")"
  if [[ -n "$CHECKSUM_FILE" && -f "$CHECKSUM_FILE" ]]; then
    cp -f "$CHECKSUM_FILE" "${WORK_DIR}/checksums.txt"
  elif [[ -f "${checksums_dir}/checksums.txt" ]]; then
    cp -f "${checksums_dir}/checksums.txt" "${WORK_DIR}/checksums.txt"
  fi
  if [[ -f "${checksums_dir}/release-manifest.json" ]]; then
    cp -f "${checksums_dir}/release-manifest.json" "${WORK_DIR}/release-manifest.json"
    log INFO "stage_offline_tarball manifest=release-manifest.json"
  fi
  SKIP_DOWNLOAD=1
  log INFO "stage_offline_tarball asset=${ASSET}"
}

verify_archive_checksum() {
  [[ -n "$ASSET" ]] || fail "release asset name is not set"
  [[ -f "${WORK_DIR}/${ASSET}" ]] || fail "release archive missing: ${ASSET}"
  [[ -f "${WORK_DIR}/checksums.txt" ]] || return 1
  (
    cd "$WORK_DIR"
    grep -F "  ${ASSET}" checksums.txt | sha256sum -c -
  )
}

verify_archive_gpg() {
  [[ "$GPG_VERIFY" != "1" ]] && return 0
  command -v gpg >/dev/null 2>&1 || fail "gpg required for signature verification"
  [[ -n "$SIGNATURE_FILE" && -f "$SIGNATURE_FILE" ]] || fail "--signature-file required with GPG verify"
  local gpg_args=()
  if [[ -n "$GPG_KEYRING" ]]; then
    gpg_args+=(--keyring "$GPG_KEYRING")
  fi
  gpg "${gpg_args[@]}" --verify "$SIGNATURE_FILE" "${WORK_DIR}/${ASSET}"
  log INFO "verify_archive_gpg ok"
}

verify_archive() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "verify_archive asset=${ASSET}"
    return 0
  fi
  if verify_archive_checksum; then
    verify_archive_gpg
    log INFO "verify_archive ok asset=${ASSET}"
    return 0
  fi
  if [[ "$ALLOW_UNVERIFIED" == "1" ]]; then
    log INFO "verify_archive skipped reason=allow_unverified"
    return 0
  fi
  fail_with_hint "checksum verification failed or checksums.txt missing" \
    "provide checksums.txt, --checksum-file, or pass --allow-unverified with --unattended"
}

validate_tar_members() {
  local archive="$1"
  local member
  while IFS= read -r member; do
    [[ -z "$member" ]] && continue
    if [[ "$member" == /* ]]; then
      fail "unsafe tar member (absolute path): ${member}"
    fi
    if [[ "$member" =~ (^|/)\.\.(/|$) ]]; then
      fail "unsafe tar member (path traversal): ${member}"
    fi
  done < <(tar -tzf "$archive")
}

release_target_dir() {
  printf '%s/releases/%s' "$INSTALL_PREFIX" "${RELEASE_VERSION}"
}

extract_release() {
  local release_dir
  release_dir="$(release_target_dir)"
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "extract_release dir=${release_dir}"
    return 0
  fi
  mkdir -p "$release_dir"

  if [[ -n "$LOCAL_SOURCE_DIR" ]]; then
    cp -a "${LOCAL_SOURCE_DIR}/." "$release_dir/"
    find "$release_dir" -maxdepth 1 -type f \( -name 'install.sh' -o -name 'uninstall.sh' \) -delete
    find "$release_dir" -maxdepth 1 -type f -name 'directorysync.service.template' -delete
    rm -rf "${release_dir}/templates"
    RELEASE_VERSION="${RELEASE_VERSION:-local}"
    RELEASE_TAG="${RELEASE_TAG:-local}"
    log INFO "extract_release source=local dir=${release_dir}"
    return 0
  fi

  [[ -f "${WORK_DIR}/${ASSET}" ]] || fail "release archive missing for extract"
  validate_tar_members "${WORK_DIR}/${ASSET}"
  local staging="${WORK_DIR}/extract-staging"
  rm -rf "$staging"
  mkdir -p "$staging"
  tar -xzf "${WORK_DIR}/${ASSET}" -C "$staging"
  cp -a "${staging}/." "$release_dir/"
  rm -rf "$staging"
  log INFO "extract_release dir=${release_dir}"
}

repair_release_runtime_dirs() {
  local release_dir="$1"
  getent passwd directorysync >/dev/null 2>&1 || return 0
  local sub
  for sub in data logs; do
    if [[ -d "${release_dir}/${sub}" ]]; then
      chown -R directorysync:directorysync "${release_dir}/${sub}"
    fi
  done
}

set_release_payload_ownership() {
  local release_dir="$1"
  chown root:root "$release_dir"
  find "$release_dir" -mindepth 1 \( -name data -o -name logs \) -prune -o -exec chown root:root {} +
  repair_release_runtime_dirs "$release_dir"
}

validate_release_binary() {
  local release_dir binary
  release_dir="$(release_target_dir)"
  binary="${release_dir}/DirectorySync.Host.Console"
  [[ -x "$binary" || -f "$binary" ]] || fail "binary not found: ${binary}"
  if [[ "$DRY_RUN" != "1" ]]; then
    chmod 0755 "$binary"
    set_release_payload_ownership "$release_dir"
  fi
  log INFO "validate_release_binary ok path=${binary}"
}

detect_local_bundle() {
  if [[ -n "$TARBALL_PATH" ]]; then
    return 0
  fi
  if [[ -f "${SCRIPT_DIR}/DirectorySync.Host.Console" ]]; then
    LOCAL_SOURCE_DIR="$SCRIPT_DIR"
    SKIP_DOWNLOAD=1
    RELEASE_VERSION="${RELEASE_VERSION:-local}"
    RELEASE_TAG="${RELEASE_TAG:-local}"
    log INFO "detect_local_bundle dir=${LOCAL_SOURCE_DIR}"
  fi
}

cleanup_work_dir() {
  if [[ -n "$WORK_DIR" && -d "$WORK_DIR" ]]; then
    rm -rf "$WORK_DIR"
  fi
}
