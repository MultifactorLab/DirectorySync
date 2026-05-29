#!/usr/bin/env bash
# DirectorySync Linux uninstaller — stops systemd service; optional data/config purge.
set -Eeuo pipefail

SERVICE_NAME="directorysync.service"
INSTALL_PREFIX="${INSTALL_PREFIX:-/opt/directorysync}"
CONFIG_DIR="${CONFIG_DIR:-/etc/directorysync}"
DATA_DIR="/var/lib/directorysync"
STATE_DIR="${DATA_DIR}/install-state"
LOG_DIR="/var/log/directorysync"
CERTS_DIR="${CONFIG_DIR}/certs"

PURGE=0
PURGE_APP=0
PURGE_CONFIG=0
PURGE_DATA=0
PURGE_LOGS=0
PURGE_SYSTEM_CERTS=0
PURGE_LDAP_COMPAT=0
REMOVE_USER=0
UNATTENDED=0
DRY_RUN=0

usage() {
  cat <<'EOF'
Usage: uninstall.sh [OPTIONS]

Stop and remove the DirectorySync systemd service. By default, application
files, configuration, data, and the service account are preserved.

Options:
  --install-prefix PATH   Application root (default: /opt/directorysync)
  --config-dir PATH       Configuration directory (default: /etc/directorysync)
  --purge                 Remove app, config, data, logs, install state, and user
  --purge-app             Remove application releases under INSTALL_PREFIX
  --purge-config          Remove CONFIG_DIR (including certs)
  --purge-data            Remove /var/lib/directorysync
  --purge-logs            Remove /var/log/directorysync
  --purge-system-certs    Remove CA entries installed to the system trust store
  --purge-ldap-compat     Remove installer-managed LDAP compat symlinks (Ubuntu 24.04)
  --remove-user           Remove directorysync user and group (with --purge or alone)
  --unattended            Non-interactive mode
  --yes                   Same as --unattended
  --dry-run               Show planned actions without making changes
  -h, --help              Show this help

Environment:
  INSTALL_PREFIX, CONFIG_DIR
EOF
}

log() {
  printf '%s [%s] %s\n' "$(date -Is)" "$1" "${2:-}"
}

fail() {
  log ERROR "$1"
  exit 1
}

run_or_echo() {
  if [[ "$DRY_RUN" == "1" ]]; then
    log DRY-RUN "$*"
    return 0
  fi
  "$@"
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --install-prefix)
        INSTALL_PREFIX="${2:?--install-prefix requires a path}"
        shift 2
        ;;
      --config-dir)
        CONFIG_DIR="${2:?--config-dir requires a path}"
        CERTS_DIR="${CONFIG_DIR}/certs"
        shift 2
        ;;
      --purge)
        PURGE=1
        PURGE_APP=1
        PURGE_CONFIG=1
        PURGE_DATA=1
        PURGE_LOGS=1
        PURGE_LDAP_COMPAT=1
        REMOVE_USER=1
        shift
        ;;
      --purge-app) PURGE_APP=1; shift ;;
      --purge-config) PURGE_CONFIG=1; shift ;;
      --purge-data) PURGE_DATA=1; shift ;;
      --purge-logs) PURGE_LOGS=1; shift ;;
      --purge-system-certs) PURGE_SYSTEM_CERTS=1; shift ;;
      --purge-ldap-compat) PURGE_LDAP_COMPAT=1; shift ;;
      --remove-user) REMOVE_USER=1; shift ;;
      --unattended|--yes) UNATTENDED=1; shift ;;
      --dry-run) DRY_RUN=1; shift ;;
      -h|--help) usage; exit 0 ;;
      *) fail "unknown option: $1 (use --help)" ;;
    esac
  done
}

confirm_unattended() {
  if [[ "$UNATTENDED" == "1" ]]; then
    return 0
  fi
  if [[ ! -t 0 ]]; then
    return 0
  fi
  printf 'Proceed with DirectorySync uninstall? [y/N] ' >&2
  local answer
  read -r answer
  case "$answer" in
    y|Y|yes|YES) ;;
    *) fail "uninstall cancelled" ;;
  esac
}

confirm_purge() {
  if [[ "$PURGE_APP$PURGE_CONFIG$PURGE_DATA$PURGE_LOGS$REMOVE_USER$PURGE_SYSTEM_CERTS$PURGE_LDAP_COMPAT" == "0000000" ]]; then
    return 0
  fi
  if [[ "$UNATTENDED" == "1" ]]; then
    return 0
  fi
  if [[ ! -t 0 ]]; then
    fail "destructive uninstall requires --unattended or --yes when stdin is not a TTY"
  fi
  printf 'This will permanently delete selected paths. Continue? [y/N] ' >&2
  local answer
  read -r answer
  case "$answer" in
    y|Y|yes|YES) ;;
    *) fail "uninstall cancelled" ;;
  esac
}

remove_systemd_service() {
  if command -v systemctl >/dev/null 2>&1; then
    if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
      run_or_echo systemctl stop "$SERVICE_NAME"
    fi
    if systemctl is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
      run_or_echo systemctl disable "$SERVICE_NAME"
    fi
  fi
  if [[ -f /etc/systemd/system/directorysync.service ]]; then
    run_or_echo rm -f /etc/systemd/system/directorysync.service
  fi
  if command -v systemctl >/dev/null 2>&1; then
    run_or_echo systemctl daemon-reload
  fi
  log INFO "systemd service removed"
}

remove_installer_artifacts() {
  run_or_echo rm -f /etc/logrotate.d/directorysync-installer
  if [[ -d "$STATE_DIR" ]]; then
    run_or_echo rm -rf "$STATE_DIR"
    log INFO "install state removed path=${STATE_DIR}"
  fi
}

remove_system_certificates() {
  if [[ "$PURGE_SYSTEM_CERTS" != "1" ]]; then
    return 0
  fi
  if [[ -d /usr/local/share/ca-certificates/directorysync ]]; then
    run_or_echo rm -rf /usr/local/share/ca-certificates/directorysync
    if command -v update-ca-certificates >/dev/null 2>&1; then
      run_or_echo update-ca-certificates
    fi
    log INFO "removed Debian/Ubuntu system CA directory"
  fi
  if [[ -d "$CERTS_DIR" ]]; then
    local cert removed=0
    shopt -s nullglob
    for cert in "$CERTS_DIR"/*.crt; do
      local anchor="/etc/pki/ca-trust/source/anchors/$(basename "$cert")"
      if [[ -f "$anchor" ]]; then
        run_or_echo rm -f "$anchor"
        removed=1
      fi
    done
    shopt -u nullglob
    if [[ "$removed" == "1" ]] && command -v update-ca-trust >/dev/null 2>&1; then
      run_or_echo update-ca-trust extract
    fi
    log INFO "removed RHEL-compatible system CA anchors"
  fi
}

remove_ldap_compat_symlinks() {
  if [[ "$PURGE_LDAP_COMPAT" != "1" ]]; then
    return 0
  fi

  local -a dirs=(
    /usr/lib/x86_64-linux-gnu
    /usr/lib/aarch64-linux-gnu
    /lib/x86_64-linux-gnu
    /lib/aarch64-linux-gnu
    /usr/lib64
    /usr/lib
    /lib64
    /lib
  )
  local dir link target removed=0
  for dir in "${dirs[@]}"; do
    for link in "${dir}/libldap-2.5.so.0" "${dir}/liblber-2.5.so.0"; do
      [[ -L "$link" ]] || continue
      target="$(readlink -f "$link" 2>/dev/null || true)"
      case "$(basename "$target")" in
        libldap.so.2|libldap-2.6.so.0|liblber.so.2|liblber-2.6.so.0)
          run_or_echo rm -f "$link"
          removed=1
          log INFO "removed ldap compat symlink path=${link} target=${target}"
          ;;
        *)
          log INFO "skip ldap compat symlink path=${link} target=${target:-unknown}"
          ;;
      esac
    done
  done

  if [[ "$removed" == "1" ]] && command -v ldconfig >/dev/null 2>&1; then
    run_or_echo ldconfig
    log INFO "ldconfig refreshed after ldap compat symlink cleanup"
  fi
}

remove_service_account() {
  if [[ "$REMOVE_USER" != "1" ]]; then
    return 0
  fi
  if id directorysync >/dev/null 2>&1; then
    run_or_echo userdel directorysync
  fi
  if getent group directorysync >/dev/null 2>&1; then
    run_or_echo groupdel directorysync
  fi
  log INFO "service account removed"
}

purge_paths() {
  if [[ "$PURGE_APP" == "1" && -e "$INSTALL_PREFIX" ]]; then
    run_or_echo rm -rf "$INSTALL_PREFIX"
    log INFO "removed application path=${INSTALL_PREFIX}"
  fi
  if [[ "$PURGE_CONFIG" == "1" && -e "$CONFIG_DIR" ]]; then
    run_or_echo rm -rf "$CONFIG_DIR"
    log INFO "removed configuration path=${CONFIG_DIR}"
  fi
  if [[ "$PURGE_DATA" == "1" && -e "$DATA_DIR" ]]; then
    run_or_echo rm -rf "$DATA_DIR"
    log INFO "removed data path=${DATA_DIR}"
  fi
  if [[ "$PURGE_LOGS" == "1" && -e "$LOG_DIR" ]]; then
    run_or_echo rm -rf "$LOG_DIR"
    log INFO "removed logs path=${LOG_DIR}"
  fi
}

print_summary() {
  log INFO "uninstall complete dry_run=${DRY_RUN}"
  if [[ "$PURGE_APP$PURGE_CONFIG$PURGE_DATA$PURGE_LOGS" == "0000" ]]; then
    log INFO "preserved paths: ${INSTALL_PREFIX}, ${CONFIG_DIR}, ${DATA_DIR}, ${LOG_DIR}"
    log INFO "to remove everything: sudo bash uninstall.sh --purge --unattended"
  fi
}

main() {
  parse_args "$@"
  [[ "$(id -u)" -eq 0 ]] || fail "uninstall.sh must run as root"
  [[ "$(uname -s)" == "Linux" ]] || fail "only Linux is supported"

  confirm_unattended
  confirm_purge

  remove_systemd_service
  remove_installer_artifacts
  remove_system_certificates
  remove_ldap_compat_symlinks
  purge_paths
  remove_service_account
  print_summary
}

main "$@"
