#!/bin/sh
set -eu

if [ "$#" -ne 4 ]; then
  echo 'Usage: publish-github.sh <zip-path> <repo> <tag> <asset-name>' >&2
  exit 1
fi

ZIP_PATH=$1
REPO=$2
TAG=$3
ASSET_NAME=$4

if [ ! -f "$ZIP_PATH" ]; then
  echo "Archive not found: $ZIP_PATH" >&2
  exit 1
fi

if gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
  gh release upload "$TAG" "$ZIP_PATH#$ASSET_NAME" --repo "$REPO" --clobber
else
  gh release create "$TAG" "$ZIP_PATH#$ASSET_NAME" --repo "$REPO" --title "$TAG" --notes 'Private install-derived build dependencies.'
fi
