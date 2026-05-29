#!/usr/bin/env bash
set -euo pipefail

cert_dir="${1:-/tmp/build-certs}"

if ! compgen -G "${cert_dir}/*" >/dev/null 2>&1; then
  echo "No build CA certificates in ${cert_dir}; skipping trust store update."
  exit 0
fi

for cert in "${cert_dir}"/*; do
  [[ -f "$cert" ]] || continue
  base="$(basename "$cert")"
  case "$base" in
    *.crt|*.pem) dest="$base" ;;
    *.cer) dest="${base%.cer}.crt" ;;
    *) dest="${base}.crt" ;;
  esac
  cp "$cert" "/usr/local/share/ca-certificates/${dest}"
done

update-ca-certificates
