# shellcheck shell=bash
# Human-readable and JSON logging with secret redaction.

init_log_dest() {
  mkdir -p "$LOG_DIR" 2>/dev/null || true
  touch "$LOG_FILE" 2>/dev/null || true
  if [[ "$LOG_FORMAT" == "json" || "$LOG_FORMAT" == "both" ]]; then
    touch "$JSON_LOG_FILE" 2>/dev/null || true
  fi
}

is_secret_key() {
  local key="$1"
  local pattern
  for pattern in "${SECRET_KEY_PATTERNS[@]}"; do
    if [[ "$key" == *"${pattern}"* ]]; then
      return 0
    fi
  done
  return 1
}

redact_message() {
  local msg="$1"
  local redacted="$msg"
  # Redact key=value patterns for known secret env names
  redacted="$(printf '%s' "$redacted" | sed -E \
    's/(DIRECTORYSYNC_(LDAP__PASSWORD|MULTIFACTOR__SECRET|MULTIFACTOR__KEY))=[^[:space:]]*/\1=[REDACTED]/g')"
  redacted="$(printf '%s' "$redacted" | sed -E \
    's/(password|secret|token)=[^[:space:]&]*/\1=[REDACTED]/gi')"
  printf '%s' "$redacted"
}

_log_timestamp() {
  date -Is 2>/dev/null || date '+%Y-%m-%dT%H:%M:%S%z'
}

log() {
  local level="$1"
  local message="$2"
  local safe
  safe="$(redact_message "$message")"
  local line
  line="$(printf '%s [%s] %s' "$(_log_timestamp)" "$level" "$safe")"
  if [[ "$LOG_FORMAT" != "json" ]]; then
    if [[ -w "$LOG_FILE" ]] 2>/dev/null; then
      printf '%s\n' "$line" | tee -a "$LOG_FILE"
    else
      printf '%s\n' "$line" >&2
    fi
  fi
  log_event "$level" "log" "message=${safe}"
}

log_event() {
  local level="$1"
  local event="$2"
  shift 2
  local payload="$*"
  payload="$(redact_message "$payload")"
  if [[ "$LOG_FORMAT" == "json" || "$LOG_FORMAT" == "both" ]]; then
    local ts plan extra
    ts="$(_log_timestamp)"
    plan="${PLAN_ID:-}"
    extra=""
    [[ -n "$payload" ]] && extra=", ${payload}"
    if [[ -w "$JSON_LOG_FILE" ]] 2>/dev/null; then
      printf '{"ts":"%s","level":"%s","event":"%s","installer":"%s","plan":"%s"%s}\n' \
        "$ts" "$level" "$event" "$INSTALLER_VERSION" "$plan" "$extra" >>"$JSON_LOG_FILE"
    fi
  fi
}

log_progress() {
  local index="$1"
  local total="$2"
  local step="$3"
  local status="$4"
  log INFO "progress step=${index}/${total} name=${step} status=${status}"
}

log_dry_run() {
  log INFO "dry-run action=$*"
}
