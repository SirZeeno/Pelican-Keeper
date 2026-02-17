#!/bin/bash\r
set -euo pipefail

if [[ "${AUTO_UPDATE:-0}" == "1" ]]; then
  bash "./Update Scripts/Pelican/update.sh"
fi

bash "./Update Scripts/Pelican/update_config.sh"

export PK_DISABLE_SELF_UPDATE=1
chmod +x ./"Pelican Keeper" || true
exec ./"Pelican Keeper"