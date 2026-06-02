# shellcheck shell=bash
# systemd unit installation and service lifecycle.

create_user() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "create_user"
    return 0
  fi
  getent group directorysync >/dev/null || groupadd --system directorysync
  getent passwd directorysync >/dev/null || \
    useradd --system --gid directorysync --no-create-home --shell /usr/sbin/nologin directorysync

  install -d -o root -g directorysync -m 0750 "$CONFIG_DIR"
  install -d -o directorysync -g directorysync -m 0750 "${DATA_DIR}/data"
  install -d -o directorysync -g directorysync -m 0750 "$LOG_DIR"
  install -d -o root -g directorysync -m 0750 "$CERTS_DIR" 2>/dev/null || true
  if [[ -n "${RELEASE_VERSION:-}" ]]; then
    repair_release_runtime_dirs "$(release_target_dir)"
  fi
  log INFO "create_user ok"
}

install_systemd() {
  local unit_tmp="/etc/systemd/system/directorysync.service.tmp"
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "install_systemd"
    return 0
  fi
  render_systemd_unit "$unit_tmp"
  install -o root -g root -m 0644 \
    "$unit_tmp" \
    /etc/systemd/system/directorysync.service
  rm -f "$unit_tmp"
  systemctl daemon-reload
  systemctl enable "$SERVICE_NAME"
  log INFO "install_systemd enabled=${SERVICE_NAME}"
}

render_systemd_unit() {
  local output="$1"
  if [[ -n "$CONFIG_SERVICE_PATH" ]]; then
    [[ -f "$CONFIG_SERVICE_PATH" ]] || fail "systemd service template not found: ${CONFIG_SERVICE_PATH}"
    render_service_template "$CONFIG_SERVICE_PATH" "$output"
    return 0
  fi
  render_default_systemd_unit "$output"
}

service_template_add_if_not_empty() {
  local name="$1"
  case "$name" in
    ENVIRONMENT_FILE) [[ -n "$ENV_FILE_PATH" ]] ;;
    *) return 1 ;;
  esac
}

substitute_service_template_placeholders() {
  local line="$1"
  line="${line//\{\{INSTALL_PREFIX\}\}/${INSTALL_PREFIX}}"
  line="${line//\{\{CONFIG_DIR\}\}/${CONFIG_DIR}}"
  line="${line//\{\{DATA_DIR\}\}/${DATA_DIR}}"
  line="${line//\{\{LOG_DIR\}\}/${LOG_DIR}}"
  printf '%s' "$line"
}

render_service_template() {
  local template="$1"
  local output="$2"
  local line block_var="" block_mode=""
  : >"$output"
  while IFS= read -r line || [[ -n "$line" ]]; do
    if [[ "$line" =~ ^\{\{AddIfNotEmpty:([A-Za-z0-9_]+)\}\}$ ]]; then
      block_var="${BASH_REMATCH[1]}"
      if service_template_add_if_not_empty "$block_var"; then
        block_mode="include"
      else
        block_mode="skip"
      fi
      continue
    fi
    if [[ "$line" == '{{/AddIfNotEmpty}}' ]]; then
      block_var=""
      block_mode=""
      continue
    fi
    if [[ "$block_mode" == "skip" ]]; then
      continue
    fi
    substitute_service_template_placeholders "$line" >>"$output"
    printf '\n' >>"$output"
  done <"$template"
}

render_default_systemd_unit() {
  local output="$1"
  {
    cat <<EOF
[Unit]
Description=Multifactor Directory Sync
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=directorysync
Group=directorysync
WorkingDirectory=${INSTALL_PREFIX}/current
ExecStart=${INSTALL_PREFIX}/current/DirectorySync.Host.Console
EOF
    if [[ -n "$ENV_FILE_PATH" ]]; then
      printf 'EnvironmentFile=%s/directorysync.env\n' "$CONFIG_DIR"
    fi
    cat <<EOF
Restart=on-failure
RestartSec=10
TimeoutStopSec=130
KillSignal=SIGTERM
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full
ReadWritePaths=${DATA_DIR} ${LOG_DIR} ${CONFIG_DIR}
ProtectHome=true
PrivateDevices=true
ProtectKernelTunables=true
ProtectControlGroups=true
RestrictSUIDSGID=true
LockPersonality=true
SystemCallArchitectures=native

[Install]
WantedBy=multi-user.target
EOF
  } >"$output"
}

enable_start_service() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "enable_start_service"
    return 0
  fi
  systemctl restart "$SERVICE_NAME"
  systemctl is-active --quiet "$SERVICE_NAME" || fail_with_hint "service failed to start" \
    "check journalctl -u ${SERVICE_NAME}; rollback may restore previous release"
}

restart_service() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "restart_service"
    return 0
  fi
  if systemctl is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
    systemctl restart "$SERVICE_NAME"
    systemctl is-active --quiet "$SERVICE_NAME" || fail "service failed to restart after certificate update"
    log INFO "restart_service ok"
  else
    log INFO "restart_service skipped reason=not_enabled"
  fi
}

setup_logging() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "setup_logging"
    return 0
  fi
  mkdir -p "$LOG_DIR"
  touch "$LOG_FILE"
  chmod 0640 "$LOG_FILE"
  if [[ "$LOG_FORMAT" == "json" || "$LOG_FORMAT" == "both" ]]; then
    touch "$JSON_LOG_FILE"
    chmod 0640 "$JSON_LOG_FILE"
  fi
  if [[ "$(stat -c%s "$LOG_FILE" 2>/dev/null || echo 0)" -gt 10485760 ]]; then
    mv "$LOG_FILE" "${LOG_FILE}.$(date +%Y%m%d%H%M%S)"
    touch "$LOG_FILE"
    chmod 0640 "$LOG_FILE"
  fi
  cat >/etc/logrotate.d/directorysync-installer <<'EOF'
/var/log/directorysync/install.log /var/log/directorysync/install.jsonl {
  size 10M
  rotate 5
  compress
  missingok
  notifempty
  copytruncate
}
EOF
  log INFO "setup_logging path=${LOG_FILE}"
}

as_service_user() {
  if command -v runuser >/dev/null 2>&1; then
    runuser -u directorysync -- "$@"
  else
    su -s /bin/sh directorysync -c "$(printf '%q ' "$@")"
  fi
}
