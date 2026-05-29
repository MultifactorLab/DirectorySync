# shellcheck shell=bash
# Installer orchestration.

run_full_install() {
  local -a steps=(
    preflight
    detect_platform
    setup_logging
    download_release
    verify_archive
    extract_release
    validate_release_binary
    create_user
    load_config_context
    stage_config
    validate_config
    install_certificates
    install_systemd
    deploy_with_validation
  )
  local total="${#steps[@]}"
  local i=0 step
  for step in "${steps[@]}"; do
    i=$((i + 1))
    log_progress "$i" "$total" "$step" "pending"
    run_step "$step" "$step"
    log_progress "$i" "$total" "$step" "done"
  done
}

run_cert_update() {
  SKIP_DOWNLOAD=1
  local -a cert_steps=(
    setup_logging
    preflight
    create_user
    install_certificates
    restart_service
    validate_installation
  )
  local step
  for step in "${cert_steps[@]}"; do
    run_step_always "$step" "$step"
  done
}

main() {
  parse_args "$@"
  init_log_dest
  trap 'on_err "${LINENO}" "${BASH_COMMAND}"' ERR
  trap cleanup_work_dir EXIT

  detect_local_bundle

  if [[ "$UPDATE_CERTS" != "1" ]]; then
    prepare_work_dir
    stage_offline_tarball
  fi

  if [[ -n "$TARBALL_PATH" || -n "$LOCAL_SOURCE_DIR" ]]; then
    parse_tarball_version 2>/dev/null || true
  fi

  if [[ -z "${RELEASE_VERSION:-}" && "$RELEASE_TAG" != "latest" ]]; then
    RELEASE_VERSION="${RELEASE_TAG#v}"
  fi

  compute_plan_id

  if [[ "$RESET" == "1" ]]; then
    reset_install_state
  fi

  confirm_unattended

  if [[ "$UPDATE_CERTS" == "1" ]]; then
    if [[ -z "$USER_CERT_DIR" ]]; then
      fail "--update-certs requires --cert-dir"
    fi
    prepare_work_dir
    run_cert_update
    write_state_json
    log INFO "certificate update completed"
    write_dry_run_summary
    return 0
  fi

  if [[ "$DRY_RUN" == "1" ]]; then
    RELEASE_VERSION="${RELEASE_VERSION:-dry-run}"
    compute_plan_id
    detect_platform
    print_install_plan
    run_full_install
    write_dry_run_summary
    log INFO "dry-run completed plan=${PLAN_ID}"
    return 0
  fi

  run_full_install
  write_state_json
  log INFO "installation completed version=${RELEASE_VERSION:-unknown} plan=${PLAN_ID}"
}
