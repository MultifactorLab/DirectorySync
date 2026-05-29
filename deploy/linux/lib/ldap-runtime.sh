# shellcheck shell=bash
# LDAP native runtime compatibility for System.DirectoryServices.Protocols on Linux.

readonly LDAP_LIB_SEARCH_DIRS=(
  /usr/lib/x86_64-linux-gnu
  /usr/lib/aarch64-linux-gnu
  /lib/x86_64-linux-gnu
  /lib/aarch64-linux-gnu
  /usr/lib64
  /usr/lib
  /lib64
  /lib
)

# Tracks symlinks created in the current ensure_ubuntu_2404_ldap_compat_symlinks run (rollback).
LDAP_COMPAT_CREATED_PATHS=()

native_ldap_os_key() {
  printf '%s:%s' "${OS_ID:-}" "${OS_VERSION_ID:-}"
}

is_native_ldap_supported_os() {
  case "$(native_ldap_os_key)" in
    ubuntu:22.04|ubuntu:24.04) return 0 ;;
    *) return 1 ;;
  esac
}

assert_native_ldap_supported_os() {
  load_os_release
  case "$(native_ldap_os_key)" in
    ubuntu:22.04|ubuntu:24.04)
      log INFO "ldap runtime native support ok os=${OS_ID} version=${OS_VERSION_ID}"
      return 0
      ;;
    ubuntu:20.04|debian:11)
      fail_with_hint \
        "native Linux install is not supported on ${OS_NAME:-${OS_ID}} ${OS_VERSION_ID}" \
        "use Docker deployment for this OS"
      ;;
    alpine*)
      fail_with_hint "Alpine Linux is not supported for native install" \
        "use Ubuntu-based Docker image (deploy/docker/Dockerfile)"
      ;;
    *)
      if [[ "${OS_ID,,}" == alpine* ]]; then
        fail_with_hint "Alpine Linux is not supported for native install" \
          "use Ubuntu-based Docker image (deploy/docker/Dockerfile)"
      fi
      fail_with_hint \
        "unsupported native Linux distro/version: ID=${OS_ID:-unknown} VERSION_ID=${OS_VERSION_ID:-unknown}" \
        "supported native OS: Ubuntu 22.04 or Ubuntu 24.04; otherwise use Docker"
      ;;
  esac
}

resolve_ldconfig_path() {
  local soname="$1"
  local path=""

  if command -v ldconfig >/dev/null 2>&1; then
    path="$(ldconfig -p 2>/dev/null | awk -v name="$soname" '
      $1 == name {
        for (i = 1; i <= NF; i++) {
          if ($i ~ /^\//) { print $i; exit }
        }
      }
    ')"
    if [[ -n "$path" && -e "$path" ]]; then
      printf '%s' "$path"
      return 0
    fi
  fi

  local dir
  for dir in "${LDAP_LIB_SEARCH_DIRS[@]}"; do
    if [[ -e "${dir}/${soname}" ]]; then
      printf '%s' "${dir}/${soname}"
      return 0
    fi
  done
  return 1
}

resolve_first_available_path() {
  local soname path
  for soname in "$@"; do
    path="$(resolve_ldconfig_path "$soname" 2>/dev/null || true)"
    if [[ -n "$path" && -e "$path" ]]; then
      printf '%s' "$path"
      return 0
    fi
  done
  return 1
}

canonical_path_or_self() {
  local path="$1"
  readlink -f "$path" 2>/dev/null || printf '%s' "$path"
}

require_linker_entry_or_file() {
  local soname="$1"
  local path
  path="$(resolve_ldconfig_path "$soname" 2>/dev/null || true)"
  if [[ -n "$path" && -e "$path" ]]; then
    log INFO "ldap runtime ok soname=${soname} path=${path}"
    return 0
  fi
  fail_with_hint "LDAP runtime library not found: ${soname}" \
    "run ldconfig -p | grep ${soname}; on Ubuntu 24.04 ensure OpenLDAP 2.6 packages and compat symlinks"
}

require_ldap_25_native() {
  require_linker_entry_or_file 'libldap-2.5.so.0'
  require_linker_entry_or_file 'liblber-2.5.so.0'
  log INFO "ldap runtime ubuntu 22.04 native libldap 2.5 ok"
}

validate_ldap_runtime_ready() {
  require_linker_entry_or_file 'libldap-2.5.so.0'
  require_linker_entry_or_file 'liblber-2.5.so.0'
}

_ldap_compat_rollback_created() {
  local p
  if [[ ${#LDAP_COMPAT_CREATED_PATHS[@]} -eq 0 ]]; then
    return 0
  fi
  log ERROR "ldap compat rollback removing ${#LDAP_COMPAT_CREATED_PATHS[@]} symlink(s) created this run"
  for p in "${LDAP_COMPAT_CREATED_PATHS[@]}"; do
    if [[ -L "$p" ]]; then
      rm -f "$p" || true
      log INFO "ldap compat rollback removed ${p}"
    fi
  done
  LDAP_COMPAT_CREATED_PATHS=()
}

create_compat_symlink_if_missing() {
  local target="$1"
  local compat_name="$2"
  local dir compat_path existing canonical_existing canonical_target

  [[ -f "$target" || -L "$target" ]] || fail "LDAP compat target is not a file: ${target}"

  dir="$(dirname "$target")"
  compat_path="${dir}/${compat_name}"
  canonical_target="$(canonical_path_or_self "$target")"

  if [[ -e "$compat_path" || -L "$compat_path" ]]; then
    if [[ -L "$compat_path" ]]; then
      existing="$(readlink -f "$compat_path" 2>/dev/null || true)"
      canonical_existing="$(canonical_path_or_self "$compat_path")"
      if [[ "$existing" == "$target" || "$canonical_existing" == "$canonical_target" ]]; then
        log INFO "ldap compat symlink exists path=${compat_path} target=${existing:-unknown}"
        return 0
      fi
    fi
    fail_with_hint \
      "LDAP compat path already exists and is not managed by installer: ${compat_path}" \
      "inspect existing file/symlink; installer will not overwrite it"
  fi

  if ! ln -s "$target" "$compat_path"; then
    fail_with_hint "failed to create LDAP compat symlink ${compat_path}" \
      "check permissions on ${dir}"
  fi
  LDAP_COMPAT_CREATED_PATHS+=("$compat_path")
  log INFO "created LDAP compat symlink ${compat_path} -> ${target}"
}

run_ldconfig_refresh() {
  command -v ldconfig >/dev/null 2>&1 \
    || fail_with_hint "ldconfig is required for LDAP runtime setup" "install libc-bin or binutils"

  if ! ldconfig 2>/dev/null; then
    fail_with_hint "ldconfig failed" "inspect LDAP library paths under /usr/lib"
  fi
  log INFO "ldap runtime ldconfig refresh ok"
}

ensure_ubuntu_2404_ldap_compat_symlinks() {
  local ldap26 lber26

  LDAP_COMPAT_CREATED_PATHS=()

  # Noble can expose either explicit 2.6 SONAMEs or generic ABI SONAMEs from libldap2.
  ldap26="$(resolve_first_available_path 'libldap-2.6.so.0' 'libldap.so.2' 2>/dev/null || true)"
  lber26="$(resolve_first_available_path 'liblber-2.6.so.0' 'liblber.so.2' 2>/dev/null || true)"

  [[ -n "$ldap26" && -e "$ldap26" ]] \
    || fail_with_hint "libldap-2.6.so.0 not found" \
      "install Ubuntu 24.04 OpenLDAP runtime packages (e.g. libldap2) with --install-deps; check ldconfig -p for libldap-2.6.so.0 or libldap.so.2"
  [[ -n "$lber26" && -e "$lber26" ]] \
    || fail_with_hint "liblber-2.6.so.0 not found" \
      "install Ubuntu 24.04 OpenLDAP runtime packages with --install-deps; check ldconfig -p for liblber-2.6.so.0 or liblber.so.2"

  log INFO "ldap runtime ubuntu 24.04 targets ldap26=${ldap26} lber26=${lber26}"

  if ! create_compat_symlink_if_missing "$ldap26" "libldap-2.5.so.0"; then
    _ldap_compat_rollback_created
    fail "LDAP compat symlink setup failed for libldap-2.5.so.0"
  fi
  if ! create_compat_symlink_if_missing "$lber26" "liblber-2.5.so.0"; then
    _ldap_compat_rollback_created
    fail "LDAP compat symlink setup failed for liblber-2.5.so.0"
  fi

  if ! run_ldconfig_refresh; then
    _ldap_compat_rollback_created
    fail "ldconfig refresh failed after LDAP compat symlink setup"
  fi

  validate_ldap_runtime_ready
  log INFO "ldap runtime ubuntu 24.04 compat symlinks ok"
}

prepare_ldap_runtime() {
  if [[ "$SKIP_CHECKS" == "1" ]]; then
    return 0
  fi
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "prepare_ldap_runtime os=${OS_ID:-unknown} version=${OS_VERSION_ID:-unknown}"
    return 0
  fi

  load_os_release
  assert_native_ldap_supported_os

  case "$(native_ldap_os_key)" in
    ubuntu:22.04)
      require_ldap_25_native
      ;;
    ubuntu:24.04)
      ensure_ubuntu_2404_ldap_compat_symlinks
      ;;
  esac
}
