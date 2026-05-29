# shellcheck shell=bash
# Preflight checks (root, systemd, disk, network, dependencies).

check_command() {
  command -v "$1" >/dev/null 2>&1 || fail "required command not found: $1"
}

check_systemd_running() {
  local state
  state="$(systemctl is-system-running 2>/dev/null || true)"
  case "$state" in
    running|degraded) return 0 ;;
    "") return 0 ;;
    *)
      fail "systemd is not running (state=${state})"
      ;;
  esac
}

check_free_space_kb() {
  local path="$1"
  local min_kb="$2"
  local free_kb
  free_kb="$(df -Pk "$path" 2>/dev/null | awk 'NR==2 {print $4}')"
  [[ "${free_kb:-0}" -ge "$min_kb" ]] || fail "not enough free space under ${path} (need ${min_kb}KB)"
}

check_network() {
  if [[ "$SKIP_DOWNLOAD" == "1" || "$UPDATE_CERTS" == "1" ]]; then
    return 0
  fi
  if [[ "$DRY_RUN" == "1" ]]; then
    return 0
  fi
  curl -fsSL --max-time 15 -o /dev/null "https://github.com" \
    || fail "cannot reach https://github.com (use --tarball for offline install)"
}

check_optional_health_port() {
  local port="${DIRECTORYSYNC_HEALTH_PORT:-}"
  [[ -z "$port" ]] && return 0
  if command -v ss >/dev/null 2>&1; then
    ss -ltn | grep -q ":${port} " && fail "port ${port} is already in use"
  elif command -v netstat >/dev/null 2>&1; then
    netstat -ltn | grep -q ":${port} " && fail "port ${port} is already in use"
  fi
}

preflight() {
  [[ "$SKIP_CHECKS" == "1" ]] && return 0
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "preflight"
    return 0
  fi

  [[ "$(id -u)" -eq 0 ]] || fail "install.sh must run as root"
  [[ "$(uname -s)" == "Linux" ]] || fail "only Linux is supported"

  load_os_release
  log INFO "preflight os=${OS_NAME:-Linux} version=${OS_VERSION_ID:-unknown}"

  check_distro_support
  ensure_dependencies
  prepare_ldap_runtime

  check_command systemctl
  check_systemd_running

  local opt_parent var_parent
  opt_parent="$(dirname "$INSTALL_PREFIX")"
  var_parent="$(dirname "$DATA_DIR")"
  check_free_space_kb "$opt_parent" 262144
  check_free_space_kb "$var_parent" 102400
  check_network
  check_optional_health_port
}
