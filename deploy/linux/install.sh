#!/usr/bin/env bash
# DirectorySync Linux installer v2 — modular, idempotent, restartable.
# Run as root. Requires deploy/linux/lib/ beside this script (or bundled release layout).
set -Eeuo pipefail

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
LIB_DIR="${SCRIPT_DIR}/lib"

if [[ ! -d "$LIB_DIR" ]]; then
  echo "install.sh: missing lib directory: ${LIB_DIR}" >&2
  exit 1
fi

# shellcheck source=lib/constants.sh
source "${LIB_DIR}/constants.sh"
# shellcheck source=lib/logging.sh
source "${LIB_DIR}/logging.sh"
# shellcheck source=lib/errors.sh
source "${LIB_DIR}/errors.sh"
# shellcheck source=lib/state.sh
source "${LIB_DIR}/state.sh"
# shellcheck source=lib/args.sh
source "${LIB_DIR}/args.sh"
# shellcheck source=lib/distro.sh
source "${LIB_DIR}/distro.sh"
# shellcheck source=lib/ldap-runtime.sh
source "${LIB_DIR}/ldap-runtime.sh"
# shellcheck source=lib/artifacts.sh
source "${LIB_DIR}/artifacts.sh"
# shellcheck source=lib/transaction.sh
source "${LIB_DIR}/transaction.sh"
# shellcheck source=lib/config.sh
source "${LIB_DIR}/config.sh"
# shellcheck source=lib/certs.sh
source "${LIB_DIR}/certs.sh"
# shellcheck source=lib/systemd.sh
source "${LIB_DIR}/systemd.sh"
# shellcheck source=lib/validation.sh
source "${LIB_DIR}/validation.sh"
# shellcheck source=lib/ui.sh
source "${LIB_DIR}/ui.sh"
# shellcheck source=lib/preflight.sh
source "${LIB_DIR}/preflight.sh"
# shellcheck source=lib/main.sh
source "${LIB_DIR}/main.sh"

main "$@"
