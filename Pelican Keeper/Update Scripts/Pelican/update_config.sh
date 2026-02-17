#!/bin/bash\r
set -euo pipefail

CONFIG="Config.json"

# -----------------------------
# Helpers
# -----------------------------

# Trim leading/trailing whitespace
trim() {
  local s="$1"
  # remove leading
  s="${s#"${s%%[![:space:]]*}"}"
  # remove trailing
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

# Escape a string for safe inclusion inside JSON quotes
json_escape() {
  local s="$1"
  s="${s//\\/\\\\}"
  s="${s//\"/\\\"}"
  s="${s//$'\n'/\\n}"
  s="${s//$'\r'/\\r}"
  s="${s//$'\t'/\\t}"
  printf '%s' "$s"
}

# Convert "item1, item2, item3" -> ["item1","item2","item3"]
# Rules:
# - splits on commas
# - trims whitespace around items
# - drops empty items
csv_to_json_array() {
  local csv="${1:-}"
  csv="$(trim "$csv")"

  # empty => []
  if [[ -z "$csv" ]]; then
    printf '[]'
    return 0
  fi

  # Split
  local IFS=',' parts=()
  read -r -a parts <<< "$csv"

  local out="[" first=1
  local item esc
  for item in "${parts[@]}"; do
    item="$(trim "$item")"
    [[ -z "$item" ]] && continue
    esc="$(json_escape "$item")"
    if [[ $first -eq 1 ]]; then
      out+="\"$esc\""
      first=0
    else
      out+=",\"$esc\""
    fi
  done
  out+="]"
  printf '%s' "$out"
}

# Convert various boolean-ish inputs into JSON true/false
# Accepts: 1/0, true/false, yes/no, on/off (any case)
bool_to_json() {
  local v="${1:-}"
  v="$(trim "$v")"
  v="${v,,}" # lowercase

  case "$v" in
    1|true|yes|on)  printf 'true' ;;
    0|false|no|off|"") printf 'false' ;;
    *)
      # Unknown -> false (or change to 'false' + log if you prefer)
      printf 'false'
      ;;
  esac
}

# Number with default
num_or_default() {
  local v="${1:-}" def="${2:-0}"
  v="$(trim "$v")"
  if [[ "$v" =~ ^-?[0-9]+$ ]]; then
    printf '%s' "$v"
  else
    printf '%s' "$def"
  fi
}

# String JSON value (quoted + escaped). If empty, use empty string.
str_json() {
  local v="${1:-}"
  v="$(json_escape "$v")"
  printf '"%s"' "$v"
}

# -----------------------------
# Read env vars (Pelican)
# Use these env var names in the Egg variables:
# INTERNAL_IP_STRUCTURE, MESSAGE_FORMAT, MESSAGE_SORTING, MESSAGE_SORTING_DIRECTION, etc.
# Arrays should be CSV strings like: item1, item2, item3
# -----------------------------

INTERNAL_IP_STRUCTURE="${INTERNAL_IP_STRUCTURE:-192.168.*.*}"
MESSAGE_FORMAT="${MESSAGE_FORMAT:-Consolidated}"
MESSAGE_SORTING="${MESSAGE_SORTING:-Name}"
MESSAGE_SORTING_DIRECTION="${MESSAGE_SORTING_DIRECTION:-Ascending}"

IGNORE_OFFLINE_SERVERS="${IGNORE_OFFLINE_SERVERS:-0}"
IGNORE_INTERNAL_SERVERS="${IGNORE_INTERNAL_SERVERS:-0}"
IGNORE_SERVERS_WITHOUT_ALLOCATIONS="${IGNORE_SERVERS_WITHOUT_ALLOCATIONS:-1}"

SERVERS_TO_IGNORE_CSV="${SERVERS_TO_IGNORE:-UUIDS HERE}"

JOINABLE_IP_DISPLAY="${JOINABLE_IP_DISPLAY:-1}"
PLAYER_COUNT_DISPLAY="${PLAYER_COUNT_DISPLAY:-1}"
SERVERS_TO_MONITOR_CSV="${SERVERS_TO_MONITOR:-UUIDS HERE}"

AUTOMATIC_SHUTDOWN="${AUTOMATIC_SHUTDOWN:-0}"
SERVERS_TO_AUTO_SHUTDOWN_CSV="${SERVERS_TO_AUTO_SHUTDOWN:-UUIDS HERE}"
EMPTY_SERVER_TIMEOUT="${EMPTY_SERVER_TIMEOUT:-00:01:00}"

ALLOW_USER_SERVER_STARTUP="${ALLOW_USER_SERVER_STARTUP:-1}"
ALLOW_SERVER_STARTUP_CSV="${ALLOW_SERVER_STARTUP:-UUIDS HERE}"
USERS_ALLOWED_TO_START_SERVERS_CSV="${USERS_ALLOWED_TO_START_SERVERS:-USERIDS HERE}"

ALLOW_USER_SERVER_STOPPING="${ALLOW_USER_SERVER_STOPPING:-1}"
ALLOW_SERVER_STOPPING_CSV="${ALLOW_SERVER_STOPPING:-UUIDS HERE}"
USERS_ALLOWED_TO_STOP_SERVERS_CSV="${USERS_ALLOWED_TO_STOP_SERVERS:-USERIDS HERE}"

CONTINUES_MARKDOWN_READ="${CONTINUES_MARKDOWN_READ:-0}"
CONTINUES_GAMES_TO_MONITOR_READ="${CONTINUES_GAMES_TO_MONITOR_READ:-0}"
MARKDOWN_UPDATE_INTERVAL="${MARKDOWN_UPDATE_INTERVAL:-30}"
SERVER_UPDATE_INTERVAL="${SERVER_UPDATE_INTERVAL:-10}"

LIMIT_SERVER_COUNT="${LIMIT_SERVER_COUNT:-0}"
MAX_SERVER_COUNT="${MAX_SERVER_COUNT:-10}"
SERVERS_TO_DISPLAY_CSV="${SERVERS_TO_DISPLAY:-UUIDS HERE}"

DEBUG="${DEBUG:-0}"
OUTPUT_MODE="${OUTPUT_MODE:-None}"
DRY_RUN="${DRY_RUN:-0}"
AUTO_UPDATE="${AUTO_UPDATE:-0}"

# -----------------------------
# Build JSON arrays from CSV strings
# -----------------------------
SERVERS_TO_IGNORE_JSON="$(csv_to_json_array "$SERVERS_TO_IGNORE_CSV")"
SERVERS_TO_MONITOR_JSON="$(csv_to_json_array "$SERVERS_TO_MONITOR_CSV")"
SERVERS_TO_AUTO_SHUTDOWN_JSON="$(csv_to_json_array "$SERVERS_TO_AUTO_SHUTDOWN_CSV")"
ALLOW_SERVER_STARTUP_JSON="$(csv_to_json_array "$ALLOW_SERVER_STARTUP_CSV")"
USERS_ALLOWED_TO_START_SERVERS_JSON="$(csv_to_json_array "$USERS_ALLOWED_TO_START_SERVERS_CSV")"
ALLOW_SERVER_STOPPING_JSON="$(csv_to_json_array "$ALLOW_SERVER_STOPPING_CSV")"
USERS_ALLOWED_TO_STOP_SERVERS_JSON="$(csv_to_json_array "$USERS_ALLOWED_TO_STOP_SERVERS_CSV")"
SERVERS_TO_DISPLAY_JSON="$(csv_to_json_array "$SERVERS_TO_DISPLAY_CSV")"

# -----------------------------
# Write Config.json (overwrite)
# -----------------------------
cat > "$CONFIG" <<EOF
{
  "InternalIpStructure": $(str_json "$INTERNAL_IP_STRUCTURE"),
  "MessageFormat": $(str_json "$MESSAGE_FORMAT"),
  "MessageSorting": $(str_json "$MESSAGE_SORTING"),
  "MessageSortingDirection": $(str_json "$MESSAGE_SORTING_DIRECTION"),
  "IgnoreOfflineServers": $(bool_to_json "$IGNORE_OFFLINE_SERVERS"),
  "IgnoreInternalServers": $(bool_to_json "$IGNORE_INTERNAL_SERVERS"),
  "IgnoreServersWithoutAllocations": $(bool_to_json "$IGNORE_SERVERS_WITHOUT_ALLOCATIONS"),
  "ServersToIgnore": $SERVERS_TO_IGNORE_JSON,

  "JoinableIpDisplay": $(bool_to_json "$JOINABLE_IP_DISPLAY"),
  "PlayerCountDisplay": $(bool_to_json "$PLAYER_COUNT_DISPLAY"),
  "ServersToMonitor": $SERVERS_TO_MONITOR_JSON,

  "AutomaticShutdown": $(bool_to_json "$AUTOMATIC_SHUTDOWN"),
  "ServersToAutoShutdown": $SERVERS_TO_AUTO_SHUTDOWN_JSON,
  "EmptyServerTimeout": $(str_json "$EMPTY_SERVER_TIMEOUT"),
  "AllowUserServerStartup": $(bool_to_json "$ALLOW_USER_SERVER_STARTUP"),
  "AllowServerStartup": $ALLOW_SERVER_STARTUP_JSON,
  "UsersAllowedToStartServers": $USERS_ALLOWED_TO_START_SERVERS_JSON,
  "AllowUserServerStopping": $(bool_to_json "$ALLOW_USER_SERVER_STOPPING"),
  "AllowServerStopping": $ALLOW_SERVER_STOPPING_JSON,
  "UsersAllowedToStopServers": $USERS_ALLOWED_TO_STOP_SERVERS_JSON,

  "ContinuesMarkdownRead": $(bool_to_json "$CONTINUES_MARKDOWN_READ"),
  "ContinuesGamesToMonitorRead": $(bool_to_json "$CONTINUES_GAMES_TO_MONITOR_READ"),
  "MarkdownUpdateInterval": $(num_or_default "$MARKDOWN_UPDATE_INTERVAL" 30),
  "ServerUpdateInterval": $(num_or_default "$SERVER_UPDATE_INTERVAL" 10),

  "LimitServerCount": $(bool_to_json "$LIMIT_SERVER_COUNT"),
  "MaxServerCount": $(num_or_default "$MAX_SERVER_COUNT" 10),
  "ServersToDisplay": $SERVERS_TO_DISPLAY_JSON,

  "Debug": $(bool_to_json "$DEBUG"),
  "OutputMode": $(str_json "$OUTPUT_MODE"),
  "DryRun": $(bool_to_json "$DRY_RUN"),
  "AutoUpdate": $(bool_to_json "$AUTO_UPDATE")
}
EOF

echo "Config.json generated successfully."