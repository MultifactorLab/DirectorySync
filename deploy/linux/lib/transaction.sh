# shellcheck shell=bash
# Upgrade transaction with automatic rollback.

transaction_dir() {
  printf '%s/transactions' "$STATE_DIR"
}

begin_transaction() {
  TRANSACTION_ACTIVE=1
  PREVIOUS_CURRENT_TARGET=""
  SERVICE_WAS_ACTIVE=0
  ROLLBACK_ATTEMPTED=0

  if [[ -L "${INSTALL_PREFIX}/current" ]]; then
    PREVIOUS_CURRENT_TARGET="$(readlink -f "${INSTALL_PREFIX}/current" 2>/dev/null || readlink "${INSTALL_PREFIX}/current")"
  elif [[ -d "${INSTALL_PREFIX}/current" ]]; then
    PREVIOUS_CURRENT_TARGET="$(readlink -f "${INSTALL_PREFIX}/current")"
  fi

  if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    SERVICE_WAS_ACTIVE=1
  fi

  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "begin_transaction previous=${PREVIOUS_CURRENT_TARGET:-none}"
    return 0
  fi

  mkdir -p "$(transaction_dir)"
  local tx_file
  tx_file="$(transaction_dir)/current.json.tmp"
  {
    printf '{\n'
    printf '  "plan_id": "%s",\n' "$PLAN_ID"
    printf '  "target_release": "%s",\n' "$(release_target_dir)"
    printf '  "previous_current": "%s",\n' "${PREVIOUS_CURRENT_TARGET:-}"
    printf '  "service_was_active": %s,\n' "$([[ "$SERVICE_WAS_ACTIVE" == "1" ]] && echo true || echo false)"
    printf '  "started_at": "%s"\n' "$(_log_timestamp)"
    printf '}\n'
  } >"$tx_file"
  mv -f "$tx_file" "$(transaction_dir)/current.json"
  log INFO "begin_transaction previous=${PREVIOUS_CURRENT_TARGET:-none}"
}

activate_release() {
  local release_dir
  release_dir="$(release_target_dir)"

  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "activate_release dir=${release_dir}"
    return 0
  fi

  if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    log INFO "activate_release stopping service"
    systemctl stop "$SERVICE_NAME"
  fi

  mkdir -p "${INSTALL_PREFIX}/releases"
  ln -sfn "$release_dir" "${INSTALL_PREFIX}/current.tmp"
  mv -Tf "${INSTALL_PREFIX}/current.tmp" "${INSTALL_PREFIX}/current"
  link_appsettings_config
  log INFO "activate_release current=${INSTALL_PREFIX}/current"
}

rollback_transaction() {
  ROLLBACK_ATTEMPTED=1
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "rollback_transaction"
    return 0
  fi
  log ERROR "rollback_transaction starting"

  if [[ -n "$PREVIOUS_CURRENT_TARGET" && -e "$PREVIOUS_CURRENT_TARGET" ]]; then
    ln -sfn "$PREVIOUS_CURRENT_TARGET" "${INSTALL_PREFIX}/current.tmp"
    mv -Tf "${INSTALL_PREFIX}/current.tmp" "${INSTALL_PREFIX}/current"
    link_appsettings_config
    log INFO "rollback_transaction restored current=${PREVIOUS_CURRENT_TARGET}"
  else
    log ERROR "rollback_transaction no previous current target to restore"
  fi

  if [[ "$SERVICE_WAS_ACTIVE" == "1" ]]; then
    systemctl daemon-reload 2>/dev/null || true
    systemctl restart "$SERVICE_NAME" 2>/dev/null || systemctl start "$SERVICE_NAME" 2>/dev/null || true
  fi

  rm -f "$(transaction_dir)/current.json"
  TRANSACTION_ACTIVE=0
}

commit_transaction() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "commit_transaction"
    TRANSACTION_ACTIVE=0
    return 0
  fi
  rm -f "$(transaction_dir)/current.json"
  TRANSACTION_ACTIVE=0
  log INFO "commit_transaction ok"
}
