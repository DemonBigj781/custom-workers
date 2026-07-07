#!/bin/sh
set -eu

STEAMCMD_ROOT=${STEAMCMD_ROOT:-"$HOME/.cache/steamcmd"}
STEAMCMD_BIN="$STEAMCMD_ROOT/steamcmd.sh"

if [ -x "$STEAMCMD_BIN" ]; then
  printf '%s\n' "$STEAMCMD_BIN"
  exit 0
fi

mkdir -p "$STEAMCMD_ROOT"
ARCHIVE_PATH="$STEAMCMD_ROOT/steamcmd_linux.tar.gz"

curl -fsSL 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz' -o "$ARCHIVE_PATH"
tar -xzf "$ARCHIVE_PATH" -C "$STEAMCMD_ROOT"
rm -f "$ARCHIVE_PATH"

if [ ! -x "$STEAMCMD_BIN" ]; then
  echo 'Failed to install steamcmd.' >&2
  exit 1
fi

printf '%s\n' "$STEAMCMD_BIN"
