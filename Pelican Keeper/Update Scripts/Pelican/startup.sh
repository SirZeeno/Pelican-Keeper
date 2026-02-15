#!/bin/bash\r
set -euo pipefail

if [[ "${AutoUpdate:-0}" == "1" ]]; then
  bash ./"Update Scripts"/Pelican/update.sh
fi

export PK_DISABLE_SELF_UPDATE=1
chmod +x ./"Pelican Keeper" || true
exec ./"Pelican Keeper"