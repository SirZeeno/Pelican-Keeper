#!/bin/bash\r
set -euo pipefail

if [[ "${AUTO_UPDATE:-0}" == "1" ]]; then
  bash "./Update_Scripts/Pelican/update.sh"
fi

bash "./Update_Scripts/Pelican/update_config.sh"

chmod +x ./"Pelican Keeper" || true
exec ./"Pelican Keeper"