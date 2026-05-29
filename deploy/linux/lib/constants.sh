# shellcheck shell=bash
# Shared constants and layout paths for DirectorySync Linux installer.

INSTALLER_VERSION="2.0.0"

APP_NAME="directorysync"
SERVICE_NAME="directorysync.service"

INSTALL_PREFIX="${INSTALL_PREFIX:-/opt/directorysync}"
CONFIG_DIR="${CONFIG_DIR:-/etc/directorysync}"
DATA_DIR="/var/lib/directorysync"
STATE_DIR="${DATA_DIR}/install-state"
CERTS_DIR="${CONFIG_DIR}/certs"
LOG_DIR="/var/log/directorysync"
LOG_FILE="${LOG_DIR}/install.log"
JSON_LOG_FILE="${LOG_DIR}/install.jsonl"

# GitHub release tag (never use bare VERSION — collides with /etc/os-release).
RELEASE_TAG="latest"
RELEASE_REPO="${RELEASE_REPO:-MultifactorLab/DirectorySync}"

RELEASE_VERSION=""
RID=""
ASSET=""

USER_CERT_DIR=""
TARBALL_PATH=""
LOCAL_SOURCE_DIR=""
WORK_DIR=""

FORCE=0
RESET=0
RESET_ALL_STATE=0
SKIP_CHECKS=0
UNATTENDED=0
ASSUME_YES=0
DRY_RUN=0
UPDATE_CERTS=0
SKIP_DOWNLOAD=0
ALLOW_UNVERIFIED=0
INSTALL_DEPS=0
NO_INSTALL_DEPS=0
CERTS_APP_ONLY=0
SKIP_DISTRO_CHECK=0

LOG_FORMAT="text" # text | json | both

CONFIG_FILE=""
CONFIG_MODE="render" # render | copy | merge
ENV_FILE_PATH=""
CONFIG_SERVICE_PATH=""
SECRETS_FILE=""
VALIDATE_CONFIG_MODE="warn" # strict | warn | skip

CHECKSUM_FILE=""
GPG_KEYRING=""
SIGNATURE_FILE=""

GPG_VERIFY=0

# Transaction / rollback
TRANSACTION_ACTIVE=0
PREVIOUS_CURRENT_TARGET=""
SERVICE_WAS_ACTIVE=0
ROLLBACK_ATTEMPTED=0

OS_NAME=""
OS_ID=""
OS_VERSION_ID=""
DISTRO_PROVIDER=""

# Secret key patterns (for redaction and CLI rejection)
readonly -a SECRET_KEY_PATTERNS=(
  'PASSWORD'
  'SECRET'
  'TOKEN'
  'MULTIFACTOR__KEY'
)

readonly -a FORBIDDEN_SECRET_CLI_FLAGS=(
  '--ldap-password'
  '--multifactor-secret'
  '--ldap-password='
  '--multifactor-secret='
)
