#!/usr/bin/env bash
set -euo pipefail
pgrep -u "$(id -u)" -f "DirectorySync.Host.Console.dll" >/dev/null
