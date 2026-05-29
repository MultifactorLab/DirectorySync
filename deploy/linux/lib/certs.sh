# shellcheck shell=bash
# Certificate installation with staging and optional app-only mode.

reject_private_keys() {
  local dir="$1"
  local key
  shopt -s nullglob
  for key in "$dir"/*.key "$dir"/*/*.key; do
    [[ -e "$key" ]] || continue
    fail "private key files are not supported (found ${key})"
  done
  shopt -u nullglob
}

reject_private_key_content() {
  local file="$1"
  if grep -q 'BEGIN .*PRIVATE KEY' "$file" 2>/dev/null; then
    fail "private key content detected in ${file}"
  fi
}

normalize_cert_to_crt() {
  local src="$1"
  local dest="$2"
  reject_private_key_content "$src"
  if openssl x509 -in "$src" -noout >/dev/null 2>&1; then
    openssl x509 -in "$src" -out "$dest" 2>/dev/null || cp -f "$src" "$dest"
  else
    fail "invalid certificate: ${src}"
  fi
}

install_certificates() {
  [[ -n "$USER_CERT_DIR" ]] || return 0
  [[ -d "$USER_CERT_DIR" ]] || fail "certificate directory not found: ${USER_CERT_DIR}"

  if [[ "$DRY_RUN" == "1" ]]; then
    log_dry_run "install_certificates from=${USER_CERT_DIR}"
    return 0
  fi

  reject_private_keys "$USER_CERT_DIR"
  mkdir -p "$CERTS_DIR"

  local staging="${WORK_DIR}/cert-staging"
  rm -rf "$staging"
  mkdir -p "$staging"

  local cert copied=0
  shopt -s nullglob
  for cert in "$USER_CERT_DIR"/*.{crt,pem,cer,CRT,PEM,CER}; do
    [[ -e "$cert" ]] || continue
    local base dest
    base="$(basename "$cert")"
    dest="${staging}/${base%.*}.crt"
    normalize_cert_to_crt "$cert" "$dest"
    copied=1
  done
  shopt -u nullglob
  [[ "$copied" == "1" ]] || fail "no certificates found in ${USER_CERT_DIR}"

  local backup="${WORK_DIR}/cert-backup"
  rm -rf "$backup"
  if [[ -d "$CERTS_DIR" ]] && [[ -n "$(ls -A "$CERTS_DIR" 2>/dev/null || true)" ]]; then
    cp -a "$CERTS_DIR" "$backup"
  fi

  find "$CERTS_DIR" -maxdepth 1 -type f -name '*.crt' -delete
  local staged
  for staged in "$staging"/*.crt; do
    [[ -f "$staged" ]] || continue
    install -o root -g directorysync -m 0640 "$staged" "${CERTS_DIR}/$(basename "$staged")"
  done

  if [[ "$CERTS_APP_ONLY" == "1" ]]; then
    log INFO "install_certificates app_only count=$(find "$CERTS_DIR" -type f | wc -l | tr -d ' ')"
    return 0
  fi

  if command -v update-ca-certificates >/dev/null 2>&1; then
    mkdir -p /usr/local/share/ca-certificates/directorysync
    rm -f /usr/local/share/ca-certificates/directorysync/*
    local installed
    for installed in "$CERTS_DIR"/*; do
      [[ -f "$installed" ]] || continue
      cp -f "$installed" "/usr/local/share/ca-certificates/directorysync/$(basename "$installed")"
    done
    update-ca-certificates
  elif command -v update-ca-trust >/dev/null 2>&1; then
    mkdir -p /etc/pki/ca-trust/source/anchors
    cp -f "$CERTS_DIR"/* /etc/pki/ca-trust/source/anchors/ 2>/dev/null || true
    update-ca-trust extract
  else
    if [[ -d "$backup" ]]; then
      rm -rf "$CERTS_DIR"
      cp -a "$backup" "$CERTS_DIR"
    fi
    fail "no supported CA trust updater (use --certs-app-only or install ca-certificates)"
  fi
  log INFO "install_certificates count=$(find "$CERTS_DIR" -type f | wc -l | tr -d ' ')"
}
