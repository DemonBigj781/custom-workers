#!/bin/sh
set -eu

if [ "$#" -ne 1 ]; then
  echo 'Usage: acquire-deps.sh <game-key>' >&2
  exit 1
fi

GAME_KEY=$1
SCRIPT_DIR=$(CDPATH= cd -- "$(dirname "$0")" && pwd)
GAME_CONFIG="$SCRIPT_DIR/games/$GAME_KEY.conf"

if [ ! -f "$GAME_CONFIG" ]; then
  echo "Unknown game key: $GAME_KEY" >&2
  exit 1
fi

# shellcheck disable=SC1090
. "$GAME_CONFIG"

ARCHIVE_OUTPUT=${ARCHIVE_OUTPUT:-"$PWD/$ARCHIVE_NAME"}
INSTALL_ROOT=$("$SCRIPT_DIR/steam-watchdog.sh" "$GAME_CONFIG")
"$SCRIPT_DIR/package-deps.sh" "$GAME_CONFIG" "$ARCHIVE_OUTPUT" >/dev/null

if [ -n "${PRIVATE_DEPS_REPO:-}" ] && [ -n "${PRIVATE_DEPS_TAG:-}" ]; then
  "$SCRIPT_DIR/publish-github.sh" "$ARCHIVE_OUTPUT" "$PRIVATE_DEPS_REPO" "$PRIVATE_DEPS_TAG" "$(basename "$ARCHIVE_OUTPUT")"
fi

if [ -n "${GOOGLE_SERVICE_ACCOUNT_JSON:-}" ] && [ -n "${GOOGLE_DRIVE_FOLDER_ID:-}" ]; then
  "$SCRIPT_DIR/publish-gdrive.sh" "$ARCHIVE_OUTPUT" "$GOOGLE_SERVICE_ACCOUNT_JSON" "$GOOGLE_DRIVE_FOLDER_ID" "$(basename "$ARCHIVE_OUTPUT")"
fi

printf '%s\n' "Steam-ready install: $INSTALL_ROOT"
printf '%s\n' "Dependency archive: $ARCHIVE_OUTPUT"
