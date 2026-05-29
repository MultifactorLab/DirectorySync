#!/usr/bin/env bash
set -Eeuo pipefail

if compgen -G "/certs/*" >/dev/null 2>&1; then
  for cert in /certs/*; do
    [[ -f "$cert" ]] || continue
    openssl x509 -in "$cert" -noout >/dev/null 2>&1 || {
      echo "Invalid certificate mounted at $cert" >&2
      exit 1
    }
  done
fi

exec dotnet DirectorySync.Host.Console.dll
