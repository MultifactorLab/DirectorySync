#!/usr/bin/env bash
# Unit tests for DirectorySync Linux installer modules (no root required).
set -euo pipefail

TESTS_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
LINUX_DIR="$(CDPATH= cd -- "${TESTS_DIR}/.." && pwd)"
LIB_DIR="${LINUX_DIR}/lib"

PASS=0
FAIL=0

assert_eq() {
  local expected="$1"
  local actual="$2"
  local name="$3"
  if [[ "$expected" == "$actual" ]]; then
    PASS=$((PASS + 1))
    printf 'PASS %s\n' "$name"
  else
    FAIL=$((FAIL + 1))
    printf 'FAIL %s: expected [%s] got [%s]\n' "$name" "$expected" "$actual" >&2
  fi
}

assert_fail() {
  local name="$1"
  shift
  if ("$@") >/dev/null 2>&1; then
    FAIL=$((FAIL + 1))
    printf 'FAIL %s: expected failure\n' "$name" >&2
  else
    PASS=$((PASS + 1))
    printf 'PASS %s\n' "$name"
  fi
}

# Minimal harness: source modules without running main (once per process).
source_libs() {
  if [[ -n "${_LIBS_SOURCED:-}" ]]; then
    return 0
  fi
  _LIBS_SOURCED=1
  SCRIPT_DIR="${LINUX_DIR}"
  # shellcheck source=../lib/constants.sh
  source "${LIB_DIR}/constants.sh"
  # shellcheck source=../lib/logging.sh
  source "${LIB_DIR}/logging.sh"
  # shellcheck source=../lib/errors.sh
  source "${LIB_DIR}/errors.sh"
  # shellcheck source=../lib/state.sh
  source "${LIB_DIR}/state.sh"
  # shellcheck source=../lib/args.sh
  source "${LIB_DIR}/args.sh"
  # shellcheck source=../lib/distro.sh
  source "${LIB_DIR}/distro.sh"
}

source_libs_ldap() {
  source_libs
  if [[ -n "${_LIBS_LDAP_SOURCED:-}" ]]; then
    return 0
  fi
  _LIBS_LDAP_SOURCED=1
  # shellcheck source=../lib/ldap-runtime.sh
  source "${LIB_DIR}/ldap-runtime.sh"
}

test_redact_message() {
  source_libs
  local out
  out="$(redact_message 'DIRECTORYSYNC_LDAP__PASSWORD=secret123')"
  assert_eq 'DIRECTORYSYNC_LDAP__PASSWORD=[REDACTED]' "$out" 'redact_message password'
}

test_reject_secret_cli() {
  source_libs
  assert_fail 'reject --ldap-password' bash -c 'reject_secret_cli_flag --ldap-password'
  assert_fail 'reject --multifactor-secret=foo' bash -c 'reject_secret_cli_flag --multifactor-secret=foo'
}

test_plan_id_stable() {
  source_libs
  RELEASE_VERSION="1.2.3"
  RELEASE_TAG="v1.2.3"
  INSTALL_PREFIX="/opt/directorysync"
  CONFIG_DIR="/etc/directorysync"
  UPDATE_CERTS=0
  compute_plan_id
  local id1="$PLAN_ID"
  compute_plan_id
  assert_eq "$id1" "$PLAN_ID" 'plan_id stable'
}

test_os_release_no_clobber() {
  source_libs
  RELEASE_TAG="v9.9.9"
  load_os_release
  assert_eq "v9.9.9" "$RELEASE_TAG" 'RELEASE_TAG not clobbered by os-release'
}

test_runtime_packages_apt() {
  source_libs
  DISTRO_PROVIDER="apt"
  local pkgs
  pkgs="$(runtime_packages_for_provider | tr '\n' ' ')"
  [[ "$pkgs" == *ca-certificates* ]] || { printf 'FAIL runtime apt missing ca-certificates\n' >&2; exit 1; }
  [[ "$pkgs" == *libsasl2-2* ]] || { printf 'FAIL runtime apt missing libsasl2\n' >&2; exit 1; }
  PASS=$((PASS + 1))
  printf 'PASS runtime_packages_apt\n'
}

test_apt_getent_maps_libc_bin() {
  source_libs
  DISTRO_PROVIDER="apt"
  assert_eq "libc-bin" "$(package_for_command getent)" 'getent maps to libc-bin on apt'
}

test_package_names_not_commands() {
  source_libs
  local missing
  missing="$(command -v libldap-2.5-0 2>/dev/null || true)"
  assert_eq "" "$missing" 'libldap package is not a command'
}

test_release_ownership_preserves_runtime_dirs() {
  source_libs
  # shellcheck source=../lib/artifacts.sh
  source "${LIB_DIR}/artifacts.sh"
  local tmp release_dir owner expected
  tmp="$(mktemp -d)"
  release_dir="${tmp}/rel"
  mkdir -p "${release_dir}/data" "${release_dir}/logs"
  printf 'x' >"${release_dir}/data/storage.db"
  printf 'y' >"${release_dir}/logs/install.log"
  owner="$(id -u):$(id -g)"
  chown -R "$owner" "${release_dir}/data" "${release_dir}/logs"
  touch "${release_dir}/DirectorySync.Host.Console"
  chmod 0755 "${release_dir}/DirectorySync.Host.Console"
  if getent passwd directorysync >/dev/null 2>&1; then
    expected="$(getent passwd directorysync | awk -F: '{print $3":"$4}')"
  else
    expected="$owner"
  fi
  set_release_payload_ownership "$release_dir"
  assert_eq "$expected" "$(stat -c '%u:%g' "${release_dir}/data/storage.db")" \
    'release data owner preserved for service user'
  assert_eq "$expected" "$(stat -c '%u:%g' "${release_dir}/logs/install.log")" \
    'release logs owner preserved for service user'
  assert_eq '0:0' "$(stat -c '%u:%g' "${release_dir}/DirectorySync.Host.Console")" \
    'release binary owned by root'
  rm -rf "$tmp"
}

test_validate_tar_members() {
  source_libs
  # shellcheck source=../lib/artifacts.sh
  source "${LIB_DIR}/artifacts.sh"
  WORK_DIR="$(mktemp -d)"
  local archive="${WORK_DIR}/bad.tar.gz"
  if ! printf '../evil.txt\n' | tar -czf "$archive" -T - 2>/dev/null; then
    printf 'SKIP validate_tar_members (tar -T ../ path not supported on this host)\n'
    rm -rf "$WORK_DIR"
    return 0
  fi
  assert_fail 'reject traversal' validate_tar_members "$archive"
  rm -rf "$WORK_DIR"
}

test_render_service_template_add_if_not_empty() {
  source_libs
  SCRIPT_DIR="${LINUX_DIR}"
  INSTALL_PREFIX="/opt/directorysync"
  CONFIG_DIR="/etc/directorysync"
  # shellcheck source=../lib/systemd.sh
  source "${LIB_DIR}/systemd.sh"
  local tmp template out
  tmp="$(mktemp -d)"
  template="${tmp}/directorysync.service.template"
  cat >"$template" <<'EOF'
ExecStart={{INSTALL_PREFIX}}/current/DirectorySync.Host.Console
{{AddIfNotEmpty:ENVIRONMENT_FILE}}
EnvironmentFile={{CONFIG_DIR}}/directorysync.env
{{/AddIfNotEmpty}}
EOF
  ENV_FILE_PATH=""
  out="${tmp}/without-env.service"
  render_service_template "$template" "$out"
  if grep -q 'EnvironmentFile=' "$out"; then
    FAIL=$((FAIL + 1))
    printf 'FAIL render_service_template skip empty ENVIRONMENT_FILE\n' >&2
  else
    PASS=$((PASS + 1))
    printf 'PASS render_service_template skip empty ENVIRONMENT_FILE\n'
  fi

  ENV_FILE_PATH="${tmp}/directorysync.env"
  out="${tmp}/with-env.service"
  render_service_template "$template" "$out"
  assert_eq 'EnvironmentFile=/etc/directorysync/directorysync.env' \
    "$(grep '^EnvironmentFile=' "$out" | head -n1)" \
    'render_service_template include ENVIRONMENT_FILE'
  rm -rf "$tmp"
}

test_native_ldap_supported_matrix() {
  source_libs_ldap
  OS_ID=ubuntu OS_VERSION_ID=22.04
  is_native_ldap_supported_os || { FAIL=$((FAIL + 1)); printf 'FAIL native ldap supported ubuntu 22.04\n' >&2; return; }
  PASS=$((PASS + 1))
  printf 'PASS native ldap supported ubuntu 22.04\n'

  OS_ID=ubuntu OS_VERSION_ID=24.04
  is_native_ldap_supported_os || { FAIL=$((FAIL + 1)); printf 'FAIL native ldap supported ubuntu 24.04\n' >&2; return; }
  PASS=$((PASS + 1))
  printf 'PASS native ldap supported ubuntu 24.04\n'

  OS_ID=ubuntu OS_VERSION_ID=20.04
  is_native_ldap_supported_os && { FAIL=$((FAIL + 1)); printf 'FAIL native ldap unsupported ubuntu 20.04\n' >&2; return; }
  PASS=$((PASS + 1))
  printf 'PASS native ldap unsupported ubuntu 20.04\n'
}

_ldap_test_harness_source() {
  printf '%s\n' \
    "source '${LIB_DIR}/constants.sh'" \
    "source '${LIB_DIR}/logging.sh'" \
    "source '${LIB_DIR}/errors.sh'" \
    "source '${LIB_DIR}/ldap-runtime.sh'"
}

test_assert_native_ldap_rejects_unsupported() {
  source_libs_ldap
  local harness
  harness="$(_ldap_test_harness_source)"
  assert_fail 'reject ubuntu 20.04' bash -c "${harness}
    OS_ID=ubuntu OS_VERSION_ID=20.04 OS_NAME=Ubuntu
    assert_native_ldap_supported_os
  "
  assert_fail 'reject debian 11' bash -c "${harness}
    OS_ID=debian OS_VERSION_ID=11 OS_NAME=Debian
    assert_native_ldap_supported_os
  "
  assert_fail 'reject alpine' bash -c "${harness}
    OS_ID=alpine OS_VERSION_ID=3.19 OS_NAME=Alpine
    assert_native_ldap_supported_os
  "
}

test_compat_symlink_idempotent() {
  source_libs_ldap
  local tmp target
  tmp="$(mktemp -d)"
  target="${tmp}/libldap-2.6.so.0"
  touch "$target"
  LDAP_COMPAT_CREATED_PATHS=()
  create_compat_symlink_if_missing "$target" "libldap-2.5.so.0"
  [[ -e "${tmp}/libldap-2.5.so.0" ]] || {
    FAIL=$((FAIL + 1))
    printf 'FAIL compat symlink not created\n' >&2
    rm -rf "$tmp"
    return
  }
  if [[ -L "${tmp}/libldap-2.5.so.0" ]]; then
    create_compat_symlink_if_missing "$target" "libldap-2.5.so.0"
    PASS=$((PASS + 1))
    printf 'PASS compat symlink idempotent\n'
  else
    printf 'SKIP compat symlink idempotent (host ln -s did not create a symlink)\n'
  fi
  rm -rf "$tmp"
}

test_compat_symlink_rejects_wrong_target() {
  source_libs_ldap
  local tmp target26 wrong
  tmp="$(mktemp -d)"
  target26="${tmp}/libldap-2.6.so.0"
  wrong="${tmp}/wrong.so"
  touch "$target26" "$wrong"
  ln -s "$wrong" "${tmp}/libldap-2.5.so.0"
  assert_fail 'reject wrong compat symlink' bash -c "
    $(_ldap_test_harness_source)
    LDAP_COMPAT_CREATED_PATHS=()
    create_compat_symlink_if_missing '${target26}' 'libldap-2.5.so.0'
  "
  rm -rf "$tmp"
}

test_native_ldap_os_key() {
  source_libs_ldap
  OS_ID=ubuntu OS_VERSION_ID=24.04
  assert_eq 'ubuntu:24.04' "$(native_ldap_os_key)" 'native_ldap_os_key'
}

test_resolve_first_available_path_fallback() {
  source_libs_ldap
  local target
  target="$(mktemp)"
  resolve_ldconfig_path() {
    local soname="$1"
    if [[ "$soname" == "libldap.so.2" ]]; then
      printf '%s' "$target"
      return 0
    fi
    return 1
  }
  assert_eq "$target" "$(resolve_first_available_path 'libldap-2.6.so.0' 'libldap.so.2')" \
    'resolve_first_available_path fallback to libldap.so.2'
  rm -f "$target"
}

test_compat_symlink_accepts_canonical_match() {
  source_libs_ldap
  local tmp real target_alias
  tmp="$(mktemp -d)"
  real="${tmp}/libldap-2.6.so.0.real"
  target_alias="${tmp}/libldap.so.2"
  touch "$real"
  if ! ln -s "$real" "$target_alias" 2>/dev/null; then
    printf 'SKIP canonical symlink test (symlink creation not supported on this host)\n'
    rm -rf "$tmp"
    return 0
  fi
  if [[ ! -L "$target_alias" ]]; then
    printf 'SKIP canonical symlink test (alias is not a symlink on this host)\n'
    rm -rf "$tmp"
    return 0
  fi
  if ! ln -s "$real" "${tmp}/libldap-2.5.so.0" 2>/dev/null; then
    printf 'SKIP canonical symlink test (compat symlink creation not supported on this host)\n'
    rm -rf "$tmp"
    return 0
  fi
  if [[ ! -L "${tmp}/libldap-2.5.so.0" ]]; then
    printf 'SKIP canonical symlink test (compat path is not a symlink on this host)\n'
    rm -rf "$tmp"
    return 0
  fi
  create_compat_symlink_if_missing "$target_alias" "libldap-2.5.so.0"
  PASS=$((PASS + 1))
  printf 'PASS compat symlink accepts canonical match\n'
  rm -rf "$tmp"
}

run_shellcheck() {
  if ! command -v shellcheck >/dev/null 2>&1; then
    printf 'SKIP shellcheck (not installed)\n'
    return 0
  fi
  shellcheck -x "${LINUX_DIR}/install.sh" "${LIB_DIR}"/*.sh
  printf 'PASS shellcheck\n'
  PASS=$((PASS + 1))
}

test_redact_message
test_reject_secret_cli
test_plan_id_stable
test_os_release_no_clobber
test_runtime_packages_apt
test_apt_getent_maps_libc_bin
test_package_names_not_commands
test_validate_tar_members
test_release_ownership_preserves_runtime_dirs
test_render_service_template_add_if_not_empty
test_native_ldap_os_key
test_resolve_first_available_path_fallback
test_compat_symlink_accepts_canonical_match
test_native_ldap_supported_matrix
test_assert_native_ldap_rejects_unsupported
test_compat_symlink_idempotent
test_compat_symlink_rejects_wrong_target
run_shellcheck || FAIL=$((FAIL + 1))

printf '\nResults: %s passed, %s failed\n' "$PASS" "$FAIL"
[[ "$FAIL" -eq 0 ]]
