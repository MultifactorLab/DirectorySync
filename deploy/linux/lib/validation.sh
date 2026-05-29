# shellcheck shell=bash
# Post-install validation and health checks.

validate_ldaps() {
  local ldap_url="${DIRECTORYSYNC_LDAP__PATH:-}"
  ldap_url="${ldap_url:-$(read_env_value DIRECTORYSYNC_LDAP__PATH)}"
  [[ "$ldap_url" == ldaps://* ]] || return 0

  local host port
  host="$(printf '%s' "$ldap_url" | sed -E 's#^ldaps://([^/:]+).*#\1#')"
  port="$(printf '%s' "$ldap_url" | sed -nE 's#^ldaps://[^/:]+:([0-9]+).*#\1#p')"
  port="${port:-636}"

  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "validate_ldaps host=${host} port=${port}"
    return 0
  fi

  local ldaps_log
  ldaps_log="$(mktemp /tmp/directorysync-ldaps-check.XXXXXX)"
  chmod 0600 "$ldaps_log"
  if ! timeout 10 openssl s_client -connect "${host}:${port}" -servername "$host" -verify_return_error \
    </dev/null >"$ldaps_log" 2>&1; then
    fail_with_hint "LDAPS TLS validation failed" "inspect ${ldaps_log} and CA certificates"
  fi
  rm -f "$ldaps_log"
  log INFO "validate_ldaps ok host=${host} port=${port}"
}

validate_installation() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "validate_installation"
    return 0
  fi
  local binary="${INSTALL_PREFIX}/current/DirectorySync.Host.Console"
  [[ -x "$binary" ]] || fail "installed binary is missing or not executable: ${binary}"

  systemctl cat "$SERVICE_NAME" >/dev/null || fail "systemd unit is not installed"

  if systemctl is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
    systemctl is-active --quiet "$SERVICE_NAME" || fail "service is not active after install"
  fi

  as_service_user test -w "$LOG_DIR" \
    || fail "service user cannot write to ${LOG_DIR}"

  local config_file="${CONFIG_DIR}/directorysync.env"
  if [[ -f "$config_file" ]]; then
    as_service_user test -r "$config_file" \
      || fail "service user cannot read ${config_file}"
  fi

  validate_ldap_runtime_ready
  validate_ldaps
  log INFO "validate_installation ok"
}

deploy_with_validation() {
  begin_transaction
  activate_release
  apply_staged_config
  enable_start_service
  validate_installation
  commit_transaction
}
