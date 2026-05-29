# shellcheck shell=bash
# Interactive confirmation, dry-run plan output.

require_non_interactive_consent() {
  if [[ "$UNATTENDED" == "1" || "$ASSUME_YES" == "1" ]]; then
    return 0
  fi
  if [[ -t 0 ]]; then
    return 0
  fi
  fail_with_hint "non-interactive stdin requires --unattended or --yes" \
    "re-run with --unattended for automation"
}

print_install_plan() {
  log INFO "install_plan installer=${INSTALLER_VERSION} plan=${PLAN_ID}"
  log INFO "install_plan release=${RELEASE_VERSION:-unknown} tag=${RELEASE_TAG} rid=${RID:-pending}"
  log INFO "install_plan prefix=${INSTALL_PREFIX} config=${CONFIG_DIR}"
  log INFO "install_plan dry_run=${DRY_RUN} unattended=${UNATTENDED}"
  if [[ -n "$CONFIG_FILE" ]]; then
    log INFO "install_plan config_file=${CONFIG_FILE} mode=${CONFIG_MODE}"
  fi
}

confirm_unattended() {
  require_non_interactive_consent
  if [[ "$UNATTENDED" == "1" || "$ASSUME_YES" == "1" ]]; then
    return 0
  fi
  if [[ ! -t 0 ]]; then
    return 0
  fi
  print_install_plan
  printf 'Proceed with DirectorySync installation? [y/N] ' >&2
  local answer
  read -r answer
  case "$answer" in
    y|Y|yes|YES) ;;
    *) fail "installation cancelled" ;;
  esac
}

write_dry_run_summary() {
  if [[ "$DRY_RUN" != "1" ]]; then
    return 0
  fi
  local summary="${WORK_DIR:-/tmp}/install-plan.json"
  if [[ -z "$WORK_DIR" ]]; then
    summary="/tmp/directorysync-install-plan.json"
  fi
  {
    printf '{\n'
    printf '  "dry_run": true,\n'
    printf '  "installer_version": "%s",\n' "$INSTALLER_VERSION"
    printf '  "plan_id": "%s",\n' "$PLAN_ID"
    printf '  "release_version": "%s"\n' "${RELEASE_VERSION:-}"
    printf '}\n'
  } >"$summary" 2>/dev/null || true
  log INFO "dry_run_summary path=${summary}"
}
