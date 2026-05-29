# shellcheck shell=bash
# Release-aware install state and step orchestration.

PLAN_ID=""

compute_plan_id() {
  local target="${RELEASE_VERSION:-}"
  [[ -z "$target" ]] && target="${RELEASE_TAG:-unknown}"
  local mode="install"
  [[ "$UPDATE_CERTS" == "1" ]] && mode="update-certs"
  local fingerprint
  fingerprint="$(printf '%s|%s|%s|%s' "$target" "$INSTALL_PREFIX" "$CONFIG_DIR" "$mode" | sha256sum | awk '{print $1}')"
  PLAN_ID="${target}-${fingerprint:0:12}"
}

step_marker_path() {
  local step="$1"
  printf '%s/plans/%s/%s.done' "$STATE_DIR" "$PLAN_ID" "$step"
}

mark_done() {
  local step="$1"
  local marker
  marker="$(step_marker_path "$step")"
  mkdir -p "$(dirname "$marker")"
  {
    printf 'step=%s\n' "$step"
    printf 'plan=%s\n' "$PLAN_ID"
    printf 'release=%s\n' "${RELEASE_VERSION:-}"
    printf 'asset=%s\n' "${ASSET:-}"
    printf 'completed_at=%s\n' "$(_log_timestamp)"
  } >"${marker}.tmp"
  mv -f "${marker}.tmp" "$marker"
}

is_done() {
  local step="$1"
  [[ -f "$(step_marker_path "$step")" ]]
}

reset_install_state() {
  if [[ "$RESET_ALL_STATE" == "1" ]]; then
    rm -rf "$STATE_DIR"
    log INFO "reset all install state"
    return 0
  fi
  if [[ -n "$PLAN_ID" ]]; then
    rm -rf "${STATE_DIR}/plans/${PLAN_ID}"
    log INFO "reset plan state plan=${PLAN_ID}"
  fi
}

run_step() {
  local name="$1"
  shift
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "step=${name}"
    return 0
  fi
  if is_done "$name" && [[ "$FORCE" != "1" ]]; then
    log INFO "skip step=${name} reason=already_done plan=${PLAN_ID}"
    return 0
  fi
  log INFO "start step=${name} plan=${PLAN_ID}"
  "$@"
  mark_done "$name"
  log INFO "done step=${name}"
}

run_step_always() {
  local name="$1"
  shift
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "step=${name}"
    return 0
  fi
  log INFO "start step=${name}"
  "$@"
  log INFO "done step=${name}"
}

write_state_json() {
  mkdir -p "$STATE_DIR"
  local state_file="${STATE_DIR}/state.json"
  local tmp="${state_file}.tmp"
  {
    printf '{\n'
    printf '  "installer_version": "%s",\n' "$INSTALLER_VERSION"
    printf '  "plan_id": "%s",\n' "$PLAN_ID"
    printf '  "release_version": "%s",\n' "${RELEASE_VERSION:-}"
    printf '  "release_tag": "%s",\n' "${RELEASE_TAG:-}"
    printf '  "install_prefix": "%s",\n' "$INSTALL_PREFIX"
    printf '  "updated_at": "%s"\n' "$(_log_timestamp)"
    printf '}\n'
  } >"$tmp"
  install -o root -g root -m 0644 "$tmp" "$state_file"
  rm -f "$tmp"
}
