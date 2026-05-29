# shellcheck shell=bash
# CLI parsing; rejects direct secret-value flags.

reject_secret_cli_flag() {
  local flag="$1"
  local forbidden
  for forbidden in "${FORBIDDEN_SECRET_CLI_FLAGS[@]}"; do
    if [[ "$flag" == "$forbidden" ]] || [[ "$flag" == "${forbidden}"* ]]; then
      fail "secret values must not be passed via CLI (use --secrets-file or environment variables): ${flag}"
    fi
  done
  case "$flag" in
    --*password|--*password=*|--*secret|--*secret=*|--*token|--*token=*)
      if [[ "$flag" != *-file ]] && [[ "$flag" != *-file=* ]]; then
        fail "secret values must not be passed via CLI (use --secrets-file or environment variables): ${flag}"
      fi
      ;;
  esac
}

usage() {
  cat <<EOF
Usage: install.sh [OPTIONS]

DirectorySync Linux installer v${INSTALLER_VERSION} (systemd).

Options:
  --version TAG               GitHub release tag (default: latest)
  --repo OWNER/REPO           GitHub repository
  --install-prefix PATH       Application root (default: /opt/directorysync)
  --config-dir PATH           Configuration directory (default: /etc/directorysync)
  --cert-dir PATH             LDAPS CA certificates source directory
  --certs-app-only            Install certs to app dir only (no system trust store)
  --update-certs              Update certificates only
  --config-file PATH          Pre-install appsettings.json (with --config-mode)
  --config-mode MODE          render | copy | merge (default: render)
  --env-file-path PATH        Runtime key=value environment file to install
  --config-service-path PATH  systemd unit template (e.g. directorysync.service.template)
  --secrets-file PATH         Pre-install secrets file (root-only, mode 0600)
  --validate-config MODE      strict | warn | skip (default: warn)
  --tarball PATH              Offline install from release .tar.gz
  --checksum-file PATH        checksums.txt for offline verify
  --allow-unverified          Allow install without checksum verification
  --gpg-keyring PATH          GPG keyring for optional signature verify
  --signature-file PATH       Detached signature for tarball
  --install-deps              Install missing distro packages (unattended)
  --no-install-deps           Only report missing dependencies (default)
  --dry-run                   Show planned actions without changes
  --yes                       Auto-confirm (non-interactive)
  --unattended                Non-interactive mode
  --log-format FMT            text | json | both
  --reset                     Clear state for current install plan
  --reset-all-state           Clear all install state under ${STATE_DIR}
  --force                     Re-run completed steps
  --skip-checks               Skip preflight checks
  --skip-distro-check         Skip unsupported-distro failure
  -h, --help                  Show this help

Secrets must use files or environment variables — never pass secret values on the CLI.
EOF
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    reject_secret_cli_flag "$1"
    case "$1" in
      --version)
        RELEASE_TAG="${2:?--version requires a value}"
        shift 2
        ;;
      --repo)
        RELEASE_REPO="${2:?--repo requires OWNER/REPO}"
        shift 2
        ;;
      --install-prefix)
        INSTALL_PREFIX="${2:?--install-prefix requires a path}"
        shift 2
        ;;
      --config-dir)
        CONFIG_DIR="${2:?--config-dir requires a path}"
        CERTS_DIR="${CONFIG_DIR}/certs"
        shift 2
        ;;
      --cert-dir)
        USER_CERT_DIR="${2:?--cert-dir requires a path}"
        shift 2
        ;;
      --config-file)
        CONFIG_FILE="${2:?--config-file requires a path}"
        shift 2
        ;;
      --config-mode)
        CONFIG_MODE="${2:?--config-mode requires render|copy|merge}"
        shift 2
        ;;
      --env-file-path)
        ENV_FILE_PATH="${2:?--env-file-path requires a path}"
        shift 2
        ;;
      --config-service-path)
        CONFIG_SERVICE_PATH="${2:?--config-service-path requires a path}"
        shift 2
        ;;
      --secrets-file)
        SECRETS_FILE="${2:?--secrets-file requires a path}"
        shift 2
        ;;
      --validate-config)
        VALIDATE_CONFIG_MODE="${2:?--validate-config requires strict|warn|skip}"
        shift 2
        ;;
      --tarball)
        TARBALL_PATH="${2:?--tarball requires a path}"
        shift 2
        ;;
      --checksum-file)
        CHECKSUM_FILE="${2:?--checksum-file requires a path}"
        shift 2
        ;;
      --gpg-keyring)
        GPG_KEYRING="${2:?--gpg-keyring requires a path}"
        GPG_VERIFY=1
        shift 2
        ;;
      --signature-file)
        SIGNATURE_FILE="${2:?--signature-file requires a path}"
        GPG_VERIFY=1
        shift 2
        ;;
      --log-format)
        LOG_FORMAT="${2:?--log-format requires text|json|both}"
        shift 2
        ;;
      --update-certs) UPDATE_CERTS=1; shift ;;
      --reset) RESET=1; shift ;;
      --reset-all-state) RESET_ALL_STATE=1; RESET=1; shift ;;
      --force) FORCE=1; shift ;;
      --skip-checks) SKIP_CHECKS=1; shift ;;
      --skip-distro-check) SKIP_DISTRO_CHECK=1; shift ;;
      --unattended) UNATTENDED=1; shift ;;
      --yes) ASSUME_YES=1; UNATTENDED=1; shift ;;
      --dry-run) DRY_RUN=1; shift ;;
      --allow-unverified) ALLOW_UNVERIFIED=1; shift ;;
      --install-deps) INSTALL_DEPS=1; shift ;;
      --no-install-deps) NO_INSTALL_DEPS=1; shift ;;
      --certs-app-only) CERTS_APP_ONLY=1; shift ;;
      -h|--help) usage; exit 0 ;;
      *)
        fail "unknown option: $1 (use --help)"
        ;;
    esac
  done

  case "$CONFIG_MODE" in
    render|copy|merge) ;;
    *) fail "invalid --config-mode: ${CONFIG_MODE} (use render|copy|merge)" ;;
  esac
  case "$VALIDATE_CONFIG_MODE" in
    strict|warn|skip) ;;
    *) fail "invalid --validate-config: ${VALIDATE_CONFIG_MODE}" ;;
  esac
  case "$LOG_FORMAT" in
    text|json|both) ;;
    *) fail "invalid --log-format: ${LOG_FORMAT}" ;;
  esac
}
