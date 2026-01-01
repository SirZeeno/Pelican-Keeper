#!/usr/bin/env bash
set -euo pipefail

ZIP_PATH="$1"     # path to downloaded zip
APP_DIR="$2"      # directory where the bot lives
PID="$3"          # PID of running bot
APP_NAME="$4"     # executable name, e.g. "Pelican-Keeper"

echo "Updater: waiting for process $PID to exit..."

# Wait until the process is gone
while kill -0 "$PID" 2>/dev/null; do
    sleep 1
done

echo "Updater: process $PID exited, applying update..."

# Extract archive
unzip -o "$ZIP_PATH" -d "$APP_DIR"

# Deletes the zip
rm -f "$ZIP_PATH" || true

echo "Updater: starting new version..."

cd "$APP_DIR"
chmod +x "./$APP_NAME"
"./$APP_NAME" &

echo "Updater: done."
