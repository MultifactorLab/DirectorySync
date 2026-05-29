# shellcheck shell=bash
# Error handling with actionable hints.

LAST_ERROR_HINT=""

fail() {
  log ERROR "$1"
  trap - ERR
  exit 1
}

fail_with_hint() {
  local message="$1"
  local hint="$2"
  LAST_ERROR_HINT="$hint"
  log ERROR "${message} hint=${hint}"
  if [[ -n "${STATE_DIR:-}" ]]; then
    log ERROR "state_dir=${STATE_DIR} log_file=${LOG_FILE}"
  fi
  if [[ "$ROLLBACK_ATTEMPTED" == "1" ]]; then
    log ERROR "rollback=attempted"
  fi
  trap - ERR
  exit 1
}

on_err() {
  local line="${1:-?}"
  local cmd="${2:-?}"
  local safe_cmd
  safe_cmd="$(redact_message "$cmd")"
  log ERROR "failed at line=${line} command=${safe_cmd}"
  if [[ "$TRANSACTION_ACTIVE" == "1" ]]; then
    rollback_transaction || true
  fi
  if [[ -n "$LAST_ERROR_HINT" ]]; then
    log ERROR "recovery_hint=${LAST_ERROR_HINT}"
  else
    log ERROR "recovery_hint=check ${LOG_FILE} and re-run with --force or --reset if needed"
  fi
  trap - ERR
  exit 1
}
