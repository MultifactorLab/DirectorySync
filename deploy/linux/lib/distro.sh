# shellcheck shell=bash
# Distro detection and optional package installation.

# Commands required by install.sh itself (checked via command -v).
readonly INSTALLER_REQUIRED_COMMANDS=(
  systemctl curl tar sha256sum openssl envsubst getent useradd install timeout ldconfig
)

load_os_release() {
  OS_NAME=""
  OS_ID=""
  OS_VERSION_ID=""
  if [[ ! -f /etc/os-release ]]; then
    return 0
  fi
  # Parse without sourcing (avoids clobbering RELEASE_TAG).
  local line key value
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%$'\r'}"
    [[ "$line" =~ ^[A-Za-z_][A-Za-z0-9_]*= ]] || continue
    key="${line%%=*}"
    value="${line#*=}"
    value="${value#\"}"
    value="${value%\"}"
    case "$key" in
      NAME) OS_NAME="$value" ;;
      ID) OS_ID="$value" ;;
      VERSION_ID) OS_VERSION_ID="$value" ;;
    esac
  done < /etc/os-release
}

detect_distro_provider() {
  load_os_release
  case "${OS_ID,,}" in
    ubuntu|debian|raspbian) DISTRO_PROVIDER="apt" ;;
    rhel|centos|rocky|almalinux|ol|amzn) DISTRO_PROVIDER="dnf" ;;
    fedora) DISTRO_PROVIDER="dnf" ;;
    opensuse*|sles|sled) DISTRO_PROVIDER="zypper" ;;
    *)
      DISTRO_PROVIDER="unknown"
      ;;
  esac
  log INFO "distro provider=${DISTRO_PROVIDER} os=${OS_NAME:-Linux} version=${OS_VERSION_ID:-unknown}"
}

# Maps installer commands to distro packages (best-effort).
package_for_command() {
  local cmd="$1"
  case "$DISTRO_PROVIDER" in
    apt)
      case "$cmd" in
        curl) printf '%s' 'curl' ;;
        envsubst) printf '%s' 'gettext-base' ;;
        sha256sum|timeout) printf '%s' 'coreutils' ;;
        openssl) printf '%s' 'openssl' ;;
        gpg) printf '%s' 'gnupg' ;;
        systemctl) printf '%s' 'systemd' ;;
        getent) printf '%s' 'libc-bin' ;;
        useradd) printf '%s' 'passwd' ;;
        install) printf '%s' 'coreutils' ;;
        ldconfig) printf '%s' 'libc-bin' ;;
        *) printf '%s' "$cmd" ;;
      esac
      ;;
    dnf|yum)
      case "$cmd" in
        envsubst) printf '%s' 'gettext' ;;
        systemctl) printf '%s' 'systemd' ;;
        getent) printf '%s' 'glibc-common' ;;
        useradd) printf '%s' 'shadow-utils' ;;
        install) printf '%s' 'coreutils' ;;
        sha256sum) printf '%s' 'coreutils' ;;
        timeout) printf '%s' 'coreutils' ;;
        *) printf '%s' "$cmd" ;;
      esac
      ;;
    zypper)
      case "$cmd" in
        envsubst) printf '%s' 'gettext-tools' ;;
        systemctl) printf '%s' 'systemd' ;;
        getent|useradd) printf '%s' 'shadow' ;;
        install) printf '%s' 'coreutils' ;;
        sha256sum|timeout) printf '%s' 'coreutils' ;;
        *) printf '%s' "$cmd" ;;
      esac
      ;;
    *)
      printf '%s' "$cmd"
      ;;
  esac
}

apt_package_candidate() {
  local pkg="$1"
  local candidate
  candidate="$(apt-cache policy "$pkg" 2>/dev/null | awk '/Candidate:/ {print $2; exit}')"
  [[ -n "$candidate" && "$candidate" != "(none)" ]]
}

apt_pick_package() {
  local pkg candidate
  for pkg in "$@"; do
    if apt_package_candidate "$pkg"; then
      printf '%s' "$pkg"
      return 0
    fi
  done
  return 1
}

# Runtime packages for DirectorySync LDAP/TLS (aligned with deploy/docker/Dockerfile runtime stage).
# Checked via package manager, not command -v.
apt_runtime_package_list() {
  local ldap sasl
  printf '%s\n' ca-certificates libldap-common libsasl2-2
  if ldap="$(apt_ldap_package)"; then
      printf '%s\n' "$ldap"
  else
    log INFO "warning no libldap package candidate for ${OS_ID:-unknown} ${OS_VERSION_ID:-unknown}"
  fi
  if sasl="$(apt_pick_package libsasl2-modules libsasl2-modules-2)"; then
    printf '%s\n' "$sasl"
  fi
}

runtime_packages_for_provider() {
  case "$DISTRO_PROVIDER" in
    apt)
      apt_runtime_package_list
      ;;
    dnf|yum)
      if [[ "${OS_ID,,}" =~ (rhel|centos) && "$OS_VERSION_ID" =~ ^7 ]]; then
        printf '%s\n' ca-certificates openldap cyrus-sasl cyrus-sasl-lib
      else
        printf '%s\n' ca-certificates openldap openldap-libs cyrus-sasl cyrus-sasl-lib
      fi
      ;;
    zypper)
      printf '%s\n' ca-certificates libldap2 libldap-common libsasl2
      ;;
  esac
}

is_package_installed() {
  local pkg="$1"
  case "$DISTRO_PROVIDER" in
    apt)
      dpkg-query -W -f='${Status}' "$pkg" 2>/dev/null | grep -q 'install ok installed'
      ;;
    dnf|yum|zypper)
      rpm -q "$pkg" >/dev/null 2>&1
      ;;
    *)
      return 1
      ;;
  esac
}

# select the appropriate libldap package depending on the distribution and version.
apt_ldap_package() {
  load_os_release
  case "$(printf '%s:%s' "${OS_ID:-}" "${OS_VERSION_ID:-}")" in
    ubuntu:22.04)
      apt_pick_package libldap-2.5-0
      ;;
    ubuntu:24.04)
      # Noble ships OpenLDAP 2.6 (libldap2); 2.5-name compat symlinks are created in ldap-runtime.sh
      apt_pick_package libldap2 libldap-2.6-0 libldap-2.5-0
      ;;
    *)
      # Unsupported for native install; do not offer legacy 2.4 packages here.
      return 1
      ;;
  esac
}

# Optional: verify native LDAP libs are visible to dynamic linker (post-install sanity).
check_native_ldap_libs() {
  if command -v ldconfig >/dev/null 2>&1; then
      ldconfig -p 2>/dev/null | grep -q 'libldap' && return 0
  fi
  local libdirs=(
    /usr/lib/x86_64-linux-gnu
    /usr/lib/aarch64-linux-gnu
    /usr/lib64
    /usr/lib
    /lib64
    /lib
  )
  for dir in "${libdirs[@]}"; do
    if [[ -f "${dir}/libldap-2.5.so.0" || -f "${dir}/libldap.so.2" || -f "${dir}/libldap.so" ]]; then
      return 0
    fi
  done
  return 1
}

collect_missing_command_packages() {
  local -a pkgs=()
  local cmd pkg
  for cmd in "${INSTALLER_REQUIRED_COMMANDS[@]}"; do
    if command -v "$cmd" >/dev/null 2>&1; then
      continue
    fi
    pkg="$(package_for_command "$cmd")"
    pkgs+=("$pkg")
    printf 'Missing command: %s (package: %s)\n' "$cmd" "$pkg" >&2
  done
  if [[ ${#pkgs[@]} -gt 0 ]]; then
    printf '%s\n' "${pkgs[@]}"
  fi
}

collect_missing_runtime_packages() {
  local -a pkgs=()
  local pkg
  while IFS= read -r pkg; do
    [[ -z "$pkg" ]] && continue
    if is_package_installed "$pkg"; then
      continue
    fi
    pkgs+=("$pkg")
    printf 'Missing runtime package: %s\n' "$pkg" >&2
  done < <(runtime_packages_for_provider)
  if [[ ${#pkgs[@]} -gt 0 ]]; then
    printf '%s\n' "${pkgs[@]}"
  fi
}

dedupe_packages() {
  local -a input=("$@")
  local -A seen=()
  local -a out=()
  local pkg
  for pkg in "${input[@]}"; do
    [[ -n "$pkg" ]] || continue
    if [[ -n "${seen[$pkg]:-}" ]]; then
      continue
    fi
    seen[$pkg]=1
    out+=("$pkg")
  done
  if [[ ${#out[@]} -gt 0 ]]; then
    printf '%s\n' "${out[@]}"
  fi
}

list_missing_dependencies() {
  local -a cmd_pkgs=()
  local -a rt_pkgs=()
  local missing=0

  mapfile -t cmd_pkgs < <(collect_missing_command_packages || true)
  mapfile -t rt_pkgs < <(collect_missing_runtime_packages || true)

  if [[ ${#cmd_pkgs[@]} -gt 0 || ${#rt_pkgs[@]} -gt 0 ]]; then
    missing=1
    log INFO "missing installer commands=${#cmd_pkgs[@]} runtime_packages=${#rt_pkgs[@]}"
  fi

  [[ "$missing" -eq 0 ]]
}

confirm_dependency_install() {
  if [[ "$INSTALL_DEPS" != "1" ]]; then
    return 1
  fi
  if [[ "$UNATTENDED" == "1" || "$ASSUME_YES" == "1" ]]; then
    return 0
  fi
  if [[ ! -t 0 ]]; then
    return 1
  fi
  printf 'Install missing dependencies via %s? [y/N] ' "$DISTRO_PROVIDER" >&2
  local answer
  read -r answer
  case "$answer" in
    y|Y|yes|YES) return 0 ;;
    *) return 1 ;;
  esac
}

install_apt_packages() {
  local -a pkgs=("$@")
  local log_file
  log_file="${WORK_DIR:-/tmp}/directorysync-apt-install.log"
  : >"$log_file"

  log INFO "apt-get update (log=${log_file})"
  if ! apt-get update >>"$log_file" 2>&1; then
    fail_with_hint "apt-get update failed (offline host or broken apt sources?)" \
      "inspect ${log_file}; install packages manually without --install-deps"
  fi

  local -a to_install=()
  local pkg
  for pkg in "${pkgs[@]}"; do
    if apt_package_candidate "$pkg"; then
      to_install+=("$pkg")
    else
      log INFO "warning skipping apt package with no candidate: ${pkg}"
    fi
  done

  if [[ ${#to_install[@]} -eq 0 ]]; then
    fail_with_hint "no installable apt packages from: ${pkgs[*]}" "inspect ${log_file}"
  fi

  log INFO "apt-get install packages=${to_install[*]}"
  if ! DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    "${to_install[@]}" >>"$log_file" 2>&1; then
    local tail
    tail="$(tail -n 25 "$log_file" 2>/dev/null | tr '\n' '; ')"
    fail_with_hint "apt-get install failed for: ${to_install[*]}" "apt log: ${tail}"
  fi
}

install_dependencies() {
  local -a cmd_pkgs=()
  local -a rt_pkgs=()
  local -a pkgs=()
  mapfile -t cmd_pkgs < <(collect_missing_command_packages || true)
  mapfile -t rt_pkgs < <(collect_missing_runtime_packages || true)
  mapfile -t pkgs < <(dedupe_packages "${cmd_pkgs[@]}" "${rt_pkgs[@]}" || true)

  [[ ${#pkgs[@]} -gt 0 ]] || return 0

  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "install-deps packages=${pkgs[*]} provider=${DISTRO_PROVIDER}"
    return 0
  fi
  if ! confirm_dependency_install; then
    fail_with_hint "missing dependencies: ${pkgs[*]}" "install packages manually or re-run with --install-deps --unattended"
  fi
  case "$DISTRO_PROVIDER" in
    apt)
      install_apt_packages "${pkgs[@]}"
      ;;
    dnf)
      dnf install -y "${pkgs[@]}" || fail_with_hint "dnf install failed" "packages=${pkgs[*]}"
      ;;
    yum)
      yum install -y "${pkgs[@]}" || fail_with_hint "yum install failed" "packages=${pkgs[*]}"
      ;;
    zypper)
      zypper --non-interactive install -y "${pkgs[@]}" \
        || fail_with_hint "zypper install failed" "packages=${pkgs[*]}"
      ;;
    *)
      fail "cannot install dependencies: unsupported provider ${DISTRO_PROVIDER}"
      ;;
  esac
  log INFO "install_dependencies ok packages=${pkgs[*]}"

  if ! check_native_ldap_libs; then
    log INFO "warning native libldap not found in ldconfig after package install (self-contained publish may still work)"
  fi
}

check_distro_support() {
  detect_distro_provider
  if [[ "$SKIP_DISTRO_CHECK" == "1" ]]; then
    log INFO "skip distro check"
    return 0
  fi
  if [[ "$DISTRO_PROVIDER" == "unknown" ]]; then
    fail_with_hint "unsupported or unknown distro (ID=${OS_ID:-})" \
      "native install supports Ubuntu 22.04/24.04; use Docker for other OS"
  fi
  # Native systemd install is limited to supported Ubuntu LTS versions.
  if is_native_ldap_supported_os 2>/dev/null; then
    return 0
  fi
  load_os_release
  case "$(printf '%s:%s' "${OS_ID:-}" "${OS_VERSION_ID:-}")" in
    ubuntu:20.04|debian:11)
      fail_with_hint \
        "native Linux install is not supported on ${OS_NAME:-${OS_ID}} ${OS_VERSION_ID}" \
        "use Docker deployment for this OS"
      ;;
    alpine*)
      fail_with_hint "Alpine Linux is not supported for native install" \
        "use Ubuntu-based Docker image"
      ;;
    ubuntu:*|debian:*)
      fail_with_hint \
        "unsupported Ubuntu/Debian version for native install: ${OS_VERSION_ID:-unknown}" \
        "supported native OS: Ubuntu 22.04 or 24.04; otherwise use Docker"
      ;;
    *)
      if [[ "${OS_ID,,}" == alpine* ]]; then
        fail_with_hint "Alpine Linux is not supported for native install" \
          "use Ubuntu-based Docker image"
      fi
      fail_with_hint \
        "unsupported native Linux distro/version: ID=${OS_ID:-unknown} VERSION_ID=${OS_VERSION_ID:-unknown}" \
        "supported native OS: Ubuntu 22.04 or 24.04; otherwise use Docker"
      ;;
  esac
}

ensure_dependencies() {
  if list_missing_dependencies; then
    return 0
  fi
  if [[ "$NO_INSTALL_DEPS" == "1" ]]; then
    fail_with_hint "missing required commands or runtime packages" "install dependencies manually or use --install-deps"
  fi
  if [[ "$INSTALL_DEPS" == "1" ]]; then
    install_dependencies
    list_missing_dependencies || fail "dependencies still missing after install attempt"
    return 0
  fi
  fail_with_hint "missing required commands or runtime packages" "install manually or re-run with --install-deps --unattended"
}
