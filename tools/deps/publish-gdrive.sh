#!/bin/sh
set -eu

if [ "$#" -ne 4 ]; then
  echo 'Usage: publish-gdrive.sh <zip-path> <credentials-json> <folder-id> <target-name>' >&2
  exit 1
fi

ZIP_PATH=$1
CREDENTIALS_JSON=$2
FOLDER_ID=$3
TARGET_NAME=$4

if [ ! -f "$ZIP_PATH" ]; then
  echo "Archive not found: $ZIP_PATH" >&2
  exit 1
fi

python3 "$(dirname "$0")/gdrive-transfer.py" \
  upload \
  --credentials "$CREDENTIALS_JSON" \
  --file "$ZIP_PATH" \
  --folder-id "$FOLDER_ID" \
  --name "$TARGET_NAME"
