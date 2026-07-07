#!/bin/sh
set -eu

if [ "$#" -ne 2 ]; then
  echo 'Usage: package-deps.sh <game-config> <output-zip>' >&2
  exit 1
fi

GAME_CONFIG=$1
OUTPUT_ZIP=$2
# shellcheck disable=SC1090
. "$GAME_CONFIG"

STEAM_LIBRARY_DIR=${STEAM_LIBRARY_DIR:-"$HOME/.local/share/Steam/steamapps"}
GAME_ROOT="$STEAM_LIBRARY_DIR/common/$INSTALL_DIR"

if [ ! -d "$GAME_ROOT" ]; then
  echo "Game root not found: $GAME_ROOT" >&2
  exit 1
fi

STAGE_DIR=$(mktemp -d)
trap 'rm -rf "$STAGE_DIR"' EXIT INT TERM

printf '%s\n' "$DEPENDENCY_PATHS" | while IFS= read -r relative_path; do
  [ -n "$relative_path" ] || continue
  SOURCE_PATH="$GAME_ROOT/$relative_path"
  TARGET_PATH="$STAGE_DIR/$relative_path"

  if [ ! -f "$SOURCE_PATH" ]; then
    echo "Missing dependency: $SOURCE_PATH" >&2
    exit 1
  fi

  mkdir -p "$(dirname "$TARGET_PATH")"
  cp "$SOURCE_PATH" "$TARGET_PATH"
done

mkdir -p "$(dirname "$OUTPUT_ZIP")"
rm -f "$OUTPUT_ZIP"

(
  cd "$STAGE_DIR"
  zip -qr "$OUTPUT_ZIP" .
)

printf '%s\n' "$OUTPUT_ZIP"
