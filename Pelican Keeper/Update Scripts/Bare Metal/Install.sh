#!/bin/bash

apt update
apt install -y curl unzip

mkdir -p /mnt/server
cd /mnt/server

set -euo pipefail

REPO="SirZeeno/Pelican-Keeper"

# The asset name we want (exact match, per your example)
ASSET_PATTERN="linux-x64.zip"

# Where to put extracted files
INSTALL_DIR="${INSTALL_DIR:-./}"

# Requirements
for cmd in curl unzip; do
  command -v "$cmd" >/dev/null 2>&1 || {
    echo "Error: '$cmd' is required but not installed." >&2
    exit 1
  }
done

tmpdir="$(mktemp -d)"
cleanup() { rm -rf "$tmpdir"; }
trap cleanup EXIT

# Extract the browser_download_url for the asset that ends with linux-x64.zip (no jq/python)
download_url=$(
  curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" |
  grep -oE '"browser_download_url":\s*"[^"]+' |
  cut -d'"' -f4 |
  grep "$ASSET_PATTERN" |
  head -n 1
)

if [[ -z "${download_url:-}" ]]; then
  echo "Error: Could not find an asset ending with '${ASSET_NAME_SUFFIX}' in the latest release." >&2
  echo "Tip: Check the release assets and confirm the filename matches, e.g. Pelican-Keeper-vX.Y.Z-${ASSET_NAME_SUFFIX}" >&2
  exit 1
fi

zip_name="$(basename "$download_url")"
zip_path="${tmpdir}/${zip_name}"

echo "Downloading: $download_url"
curl -fL --retry 3 --retry-delay 1 -o "$zip_path" "$download_url"

echo "Extracting to: $INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
unzip -o "$zip_path" -d "$INSTALL_DIR" >/dev/null

echo "Deleting zip: $zip_name"
rm -f "$zip_path"

## install end
echo "-----------------------------------------"
echo "Installation completed..."
echo "-----------------------------------------"