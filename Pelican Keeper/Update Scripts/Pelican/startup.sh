#!/bin/bash
set -euo pipefail
cd /mnt/server

if [[ "${AUTO_UPDATE:-0}" == "1" ]]; then
  bash ./update.sh
fi

export PK_DISABLE_SELF_UPDATE=1
chmod +x ./"Pelican Keeper" || true
exec ./"Pelican Keeper"