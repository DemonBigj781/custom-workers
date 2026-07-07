#!/bin/sh
set -eu

if [ "$#" -ne 1 ]; then
  echo 'Usage: steam-watchdog.sh <game-config>' >&2
  exit 1
fi

GAME_CONFIG=$1
# shellcheck disable=SC1090
. "$GAME_CONFIG"

STEAM_LIBRARY_DIR=${STEAM_LIBRARY_DIR:-"$HOME/.local/share/Steam/steamapps"}
TIMEOUT_SECONDS=${STEAM_READY_TIMEOUT_SECONDS:-600}
POLL_SECONDS=${STEAM_READY_POLL_SECONDS:-10}
AUTO_DOWNLOAD=${STEAMCMD_DOWNLOAD:-0}
APP_MANIFEST="$STEAM_LIBRARY_DIR/appmanifest_${STEAM_APP_ID}.acf"
INSTALL_ROOT="$STEAM_LIBRARY_DIR/common/$INSTALL_DIR"

is_ready() {
  [ -f "$APP_MANIFEST" ] && [ -d "$INSTALL_ROOT" ]
}

download_with_steamcmd() {
  STEAMCMD_BIN=$("$(dirname "$0")/install-steamcmd.sh")
  STEAMCMD_USER=${STEAM_USERNAME:-}
  STEAMCMD_PASSWORD=${STEAM_PASSWORD:-}
  STEAM_GUARD_CODE=${STEAM_GUARD_CODE:-}

  if [ -n "${STEAM_GUARD_CODE_FILE:-}" ] && [ -f "$STEAM_GUARD_CODE_FILE" ]; then
    STEAM_GUARD_CODE=$(tr -d '\r\n' < "$STEAM_GUARD_CODE_FILE")
  fi

  if [ -z "$STEAMCMD_USER" ] || [ -z "$STEAMCMD_PASSWORD" ]; then
    echo 'STEAMCMD_DOWNLOAD=1 requires STEAM_USERNAME and STEAM_PASSWORD.' >&2
    return 1
  fi

  mkdir -p "$STEAM_LIBRARY_DIR/common"

  set +e
  if [ -n "$STEAM_GUARD_CODE" ]; then
    "$STEAMCMD_BIN" \
      +force_install_dir "$INSTALL_ROOT" \
      +set_steam_guard_code "$STEAM_GUARD_CODE" \
      +login "$STEAMCMD_USER" "$STEAMCMD_PASSWORD" \
      +app_update "$STEAM_APP_ID" validate \
      +quit
  else
    "$STEAMCMD_BIN" \
      +force_install_dir "$INSTALL_ROOT" \
      +login "$STEAMCMD_USER" "$STEAMCMD_PASSWORD" \
      +app_update "$STEAM_APP_ID" validate \
      +quit
  fi
  STATUS=$?
  set -e
  return "$STATUS"
}

START_TIME=$(date +%s)
while :; do
  if is_ready; then
    printf '%s\n' "$INSTALL_ROOT"
    exit 0
  fi

  NOW=$(date +%s)
  if [ $((NOW - START_TIME)) -ge "$TIMEOUT_SECONDS" ]; then
    echo "Timed out waiting for Steam to become ready for $GAME_NAME." >&2
    echo "Expected install root: $INSTALL_ROOT" >&2
    exit 1
  fi

  if [ "$AUTO_DOWNLOAD" = '1' ]; then
    if download_with_steamcmd; then
      continue
    fi
    echo 'steamcmd did not complete successfully yet; waiting for Steam Guard or retry window.' >&2
  else
    echo "Waiting for Steam-ready install at $INSTALL_ROOT" >&2
  fi

  sleep "$POLL_SECONDS"
done
