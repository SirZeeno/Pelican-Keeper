#!/bin/bash
set -euo pipefail
cd /mnt/server

REPO="SirZeeno/Pelican-Keeper"
ASSET_SUFFIX="linux-x64.zip"   # matches your asset name ending
STATE_FILE="/mnt/server/.pk_installed_tag"
TMPDIR="$(mktemp -d)"
trap 'rm -rf "$TMPDIR"' EXIT

# Fetch latest release JSON (no jq)
release_json="$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest")"

latest_tag="$(
  echo "$release_json" |
  grep -oE '"tag_name":\s*"[^"]+"' |
  head -n 1 |
  cut -d'"' -f4
)"

download_url="$(
  echo "$release_json" |
  grep -oE '"browser_download_url":\s*"[^"]+"' |
  cut -d'"' -f4 |
  grep "${ASSET_SUFFIX}$" |
  head -n 1
)"

if [[ -z "${latest_tag:-}" || -z "${download_url:-}" ]]; then
  echo "Error: Could not determine latest tag or asset URL (suffix: ${ASSET_SUFFIX})." >&2
  exit 1
fi

installed_tag=""
if [[ -f "$STATE_FILE" ]]; then
  installed_tag="$(cat "$STATE_FILE" || true)"
fi

if [[ "$installed_tag" == "$latest_tag" ]]; then
  echo "Pelican Keeper is up to date (tag: $latest_tag)."
  exit 0
fi

echo "Update available: installed='$installed_tag' -> latest='$latest_tag'"
echo "Downloading: $download_url"

zip_path="${TMPDIR}/release.zip"
curl -fL --retry 3 --retry-delay 1 -o "$zip_path" "$download_url"

# Extract into a staging folder so we don't partially overwrite on failure
stage_dir="${TMPDIR}/stage"
mkdir -p "$stage_dir"
unzip -o "$zip_path" -d "$stage_dir" >/dev/null

# Figure out where the binary is inside the zip (handles either flat or nested folder zips)
new_bin="$(find "$stage_dir" -maxdepth 3 -type f -name "Pelican Keeper" -print -quit || true)"
if [[ -z "${new_bin:-}" ]]; then
  echo "Error: Could not find 'Pelican Keeper' inside the downloaded zip." >&2
  exit 1
fi

# Copy files into place.
# NOTE: this overwrites app files, but avoids touching common persistent files if you exclude them.
# If you have config/db folders, keep them in /mnt/server/data or similar so they won't collide.
rsync -a --delete \
  --exclude ".pk_installed_tag" \
  --exclude "config.json" \
  --exclude "secrets.json" \
  --exclude "data/" \
  --exclude "logs/" \
  "$stage_dir"/ /mnt/server/

chmod +x /mnt/server/"Pelican Keeper" || true

# Mark installed tag AFTER successful install
echo "$latest_tag" > "$STATE_FILE"

echo "Updated to $latest_tag."