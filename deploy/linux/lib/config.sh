# shellcheck shell=bash
# Configuration loading, precedence, staging, and rendering.

STAGED_CONFIG_DIR=""

validate_secret_file() {
  local path="$1"
  [[ -f "$path" ]] || fail "secret file not found: ${path}"
  local perm owner
  perm="$(stat -c '%a' "$path" 2>/dev/null || echo "")"
  owner="$(stat -c '%u' "$path" 2>/dev/null || echo "")"
  [[ "$owner" == "0" ]] || fail "secret file must be owned by root: ${path}"
  [[ "$perm" == "600" || "$perm" == "400" ]] || \
    log INFO "warning secret file permissions=${perm} path=${path} (recommended 0600)"
}

load_env_file() {
  local path="$1"
  [[ -f "$path" ]] || fail "env file not found: ${path}"
  local line key value loaded_count=0 skipped_empty_count=0
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%%#*}"
    line="${line#"${line%%[![:space:]]*}"}"
    line="${line%"${line##*[![:space:]]}"}"
    [[ -z "$line" ]] && continue
    [[ "$line" =~ ^[A-Za-z_][A-Za-z0-9_]*= ]] || continue
    key="${line%%=*}"
    value="${line#*=}"
    value="${value%$'\r'}"
    if [[ "$value" =~ ^\".*\"$ ]]; then
      value="${value:1:${#value}-2}"
    elif [[ "$value" =~ ^\'.*\'$ ]]; then
      value="${value:1:${#value}-2}"
    fi
    if [[ -n "${value//[[:space:]]/}" ]]; then
      # shellcheck disable=SC2163
      export "${key}=${value}"
      loaded_count=$((loaded_count + 1))
    else
      skipped_empty_count=$((skipped_empty_count + 1))
    fi
  done < "$path"
  log INFO "load_env_file path=${path} exported=${loaded_count} skipped_empty=${skipped_empty_count}"
}

load_secret_files() {
  if [[ -n "$SECRETS_FILE" ]]; then
    validate_secret_file "$SECRETS_FILE"
    load_env_file "$SECRETS_FILE"
  fi
}

load_config_context() {
  export_config_defaults
  load_secret_files
  if [[ -n "$ENV_FILE_PATH" ]]; then
    load_env_file "$ENV_FILE_PATH"
  fi
  log INFO "load_config_context mode=${CONFIG_MODE}"
}

export_config_defaults() {
  export DIRECTORYSYNC_HOST__SHUTDOWNTIMEOUT="${DIRECTORYSYNC_HOST__SHUTDOWNTIMEOUT:-00:02:00}"
  export DIRECTORYSYNC_STORAGE__DIRECTORY="${DIRECTORYSYNC_STORAGE__DIRECTORY:-${DATA_DIR}/data}"
  export DIRECTORYSYNC_STORAGE__LITEDBFILENAME="${DIRECTORYSYNC_STORAGE__LITEDBFILENAME:-storage.db}"
  export DIRECTORYSYNC_LOGGING__FILE__PATH="${DIRECTORYSYNC_LOGGING__FILE__PATH:-${LOG_DIR}/log-.txt}"
  export DIRECTORYSYNC_LDAP__PATH="${DIRECTORYSYNC_LDAP__PATH:-}"
  export DIRECTORYSYNC_LDAP__USERNAME="${DIRECTORYSYNC_LDAP__USERNAME:-}"
  export DIRECTORYSYNC_LDAP__PASSWORD="${DIRECTORYSYNC_LDAP__PASSWORD:-}"
  export DIRECTORYSYNC_MULTIFACTOR__URL="${DIRECTORYSYNC_MULTIFACTOR__URL:-}"
  export DIRECTORYSYNC_MULTIFACTOR__KEY="${DIRECTORYSYNC_MULTIFACTOR__KEY:-}"
  export DIRECTORYSYNC_MULTIFACTOR__SECRET="${DIRECTORYSYNC_MULTIFACTOR__SECRET:-}"
  export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Production}"
  export DIRECTORYSYNC_RUNTIME_MODE="${DIRECTORYSYNC_RUNTIME_MODE:-LinuxSystemd}"
}

link_appsettings_config() { :; }

runtime_env_template_path() {
  local candidate
  for candidate in \
    "${SCRIPT_DIR}/templates/directorysync.env.template" \
    "${SCRIPT_DIR}/directorysync.env.template"; do
    if [[ -f "$candidate" ]]; then
      printf '%s' "$candidate"
      return 0
    fi
  done
  return 1
}

stage_default_runtime_env() {
  export_config_defaults
  local dest="${STAGED_CONFIG_DIR}/directorysync.env"
  local template_path tmp
  if template_path="$(runtime_env_template_path)"; then
    tmp="${dest}.tmp"
    envsubst <"$template_path" >"$tmp"
    install -o root -g directorysync -m 0640 "$tmp" "$dest"
    rm -f "$tmp"
    return 0
  fi
  tmp="${dest}.tmp"
  {
    printf 'DIRECTORYSYNC_HOST__SHUTDOWNTIMEOUT=%s\n' "$DIRECTORYSYNC_HOST__SHUTDOWNTIMEOUT"
    printf 'DIRECTORYSYNC_STORAGE__DIRECTORY=%s\n' "$DIRECTORYSYNC_STORAGE__DIRECTORY"
    printf 'DIRECTORYSYNC_STORAGE__LITEDBFILENAME=%s\n' "$DIRECTORYSYNC_STORAGE__LITEDBFILENAME"
    printf 'DIRECTORYSYNC_LOGGING__FILE__PATH=%s\n' "$DIRECTORYSYNC_LOGGING__FILE__PATH"
    printf 'DOTNET_ENVIRONMENT=%s\n' "$DOTNET_ENVIRONMENT"
    printf 'DIRECTORYSYNC_RUNTIME_MODE=%s\n' "$DIRECTORYSYNC_RUNTIME_MODE"
  } >"$tmp"
  install -o root -g directorysync -m 0640 "$tmp" "$dest"
  rm -f "$tmp"
}

maybe_migrate_legacy_release_storage() {
  if [[ "$DRY_RUN" == "1" ]]; then
    return 0
  fi
  local release_dir legacy_db target_dir target_db
  release_dir="$(release_target_dir)"
  legacy_db="${release_dir}/data/storage.db"
  target_dir="${DATA_DIR}/data"
  target_db="${target_dir}/storage.db"

  install -d -o directorysync -g directorysync -m 0750 "$target_dir"

  if [[ ! -f "$legacy_db" ]]; then
    return 0
  fi
  if [[ ! -f "$target_db" ]]; then
    log INFO "migrate legacy storage from=${legacy_db} to=${target_db}"
    cp -a "$legacy_db" "$target_db"
    chown directorysync:directorysync "$target_db"
  fi
  repair_release_runtime_dirs "$release_dir"
}

stage_config() {
  STAGED_CONFIG_DIR="${WORK_DIR}/staged-config"
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "stage_config dir=${STAGED_CONFIG_DIR}"
    return 0
  fi
  rm -rf "$STAGED_CONFIG_DIR"
  mkdir -p "$STAGED_CONFIG_DIR"
  mkdir -p "$CONFIG_DIR"

  if [[ "$CONFIG_MODE" != "render" && ( -z "$CONFIG_FILE" || ! -f "$CONFIG_FILE" ) ]]; then
    fail "--config-file required for ${CONFIG_MODE} mode"
  fi
  if [[ -n "$ENV_FILE_PATH" ]]; then
    [[ -f "$ENV_FILE_PATH" ]] || fail "env file not found: ${ENV_FILE_PATH}"
    install -o root -g directorysync -m 0640 "$ENV_FILE_PATH" "${STAGED_CONFIG_DIR}/directorysync.env"
  elif [[ -f "${CONFIG_DIR}/directorysync.env" ]]; then
    install -o root -g directorysync -m 0640 \
      "${CONFIG_DIR}/directorysync.env" "${STAGED_CONFIG_DIR}/directorysync.env"
  else
    stage_default_runtime_env
  fi

  write_install_config_metadata
  log INFO "stage_config ok dir=${STAGED_CONFIG_DIR}"
}

write_install_config_metadata() {
  local meta="${STAGED_CONFIG_DIR}/install-config.json"
  local tmp="${meta}.tmp"
  {
    printf '{\n'
    printf '  "installer_version": "%s",\n' "$INSTALLER_VERSION"
    printf '  "config_mode": "%s",\n' "$CONFIG_MODE"
    printf '  "release_version": "%s",\n' "${RELEASE_VERSION:-}"
    printf '  "plan_id": "%s",\n' "$PLAN_ID"
    printf '  "staged_at": "%s"\n' "$(_log_timestamp)"
    printf '}\n'
  } >"$tmp"
  install -o root -g root -m 0644 "$tmp" "$meta"
  rm -f "$tmp"
}

apply_staged_config() {
  [[ -n "$STAGED_CONFIG_DIR" && -d "$STAGED_CONFIG_DIR" ]] || return 0
  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "apply_staged_config"
    return 0
  fi
  maybe_migrate_legacy_release_storage
  if [[ -f "${STAGED_CONFIG_DIR}/directorysync.env" ]]; then
    install -o root -g directorysync -m 0640 \
      "${STAGED_CONFIG_DIR}/directorysync.env" "${CONFIG_DIR}/directorysync.env"
  fi
  if [[ -f "${STAGED_CONFIG_DIR}/appsettings.json" ]]; then
    install -o root -g directorysync -m 0640 \
      "${STAGED_CONFIG_DIR}/appsettings.json" "${CONFIG_DIR}/appsettings.json"
  fi
  if [[ -f "${STAGED_CONFIG_DIR}/install-config.json" ]]; then
    install -o root -g root -m 0644 \
      "${STAGED_CONFIG_DIR}/install-config.json" "${CONFIG_DIR}/install-config.json"
  fi
  rm -f "${CONFIG_DIR}/appsettings.json"
  link_appsettings_config
  log INFO "apply_staged_config ok"
}

render_config() {
  load_config_context
  stage_config
}

validate_config() {
  [[ "$VALIDATE_CONFIG_MODE" == "skip" ]] && return 0
  local ldap_path="${DIRECTORYSYNC_LDAP__PATH:-}"
  local mf_url="${DIRECTORYSYNC_MULTIFACTOR__URL:-}"
  ldap_path="${ldap_path:-$(read_env_value DIRECTORYSYNC_LDAP__PATH)}"
  mf_url="${mf_url:-$(read_env_value DIRECTORYSYNC_MULTIFACTOR__URL)}"
  local -a missing=()
  [[ -z "$ldap_path" ]] && missing+=("DIRECTORYSYNC_LDAP__PATH")
  [[ -z "$mf_url" ]] && missing+=("DIRECTORYSYNC_MULTIFACTOR__URL")
  if [[ ${#missing[@]} -eq 0 ]]; then
    log INFO "validate_config ok"
    return 0
  fi
  local msg="missing recommended config: ${missing[*]}"
  case "$VALIDATE_CONFIG_MODE" in
    strict) fail "$msg" ;;
    warn) log INFO "validate_config warn ${msg}" ;;
  esac
}

read_env_value() {
  local key="$1"
  printf '%s' "${!key:-}"
}
