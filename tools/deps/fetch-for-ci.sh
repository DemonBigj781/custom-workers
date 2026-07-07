#!/bin/sh
set -eu

if [ "$#" -ne 1 ]; then
  echo 'Usage: fetch-for-ci.sh <output-root>' >&2
  exit 1
fi

OUTPUT_ROOT=$1
SCRIPT_DIR=$(CDPATH= cd -- "$(dirname "$0")" && pwd)
BACKENDS=${PRIVATE_DEPS_BACKENDS:-github,drive}
TMP_DIR=$(mktemp -d)
trap 'rm -rf "$TMP_DIR"' EXIT INT TERM

download_from_github() {
  [ -n "${PRIVATE_DEPS_REPO:-}" ] || return 1
  [ -n "${PRIVATE_DEPS_TAG:-}" ] || return 1
  [ -n "${GH_TOKEN:-}" ] || return 1

  gh release download "$PRIVATE_DEPS_TAG" \
    --repo "$PRIVATE_DEPS_REPO" \
    --pattern '*.zip' \
    --dir "$TMP_DIR"
}

download_from_drive() {
  [ -n "${GOOGLE_DRIVE_FILE_ID:-}" ] || return 1
  [ -n "${GOOGLE_SERVICE_ACCOUNT_JSON_B64:-}" ] || return 1

  CREDENTIALS_JSON="$TMP_DIR/service-account.json"
  printf '%s' "$GOOGLE_SERVICE_ACCOUNT_JSON_B64" | base64 --decode > "$CREDENTIALS_JSON"
  python3 "$SCRIPT_DIR/gdrive-transfer.py" \
    download \
    --credentials "$CREDENTIALS_JSON" \
    --file "$TMP_DIR/private-install.zip" \
    --file-id "$GOOGLE_DRIVE_FILE_ID"
}

cleanup_output() {
  rm -rf "$OUTPUT_ROOT"
  mkdir -p "$OUTPUT_ROOT"
}

extract_first_zip() {
  ZIP_PATH=$(find "$TMP_DIR" -maxdepth 1 -name '*.zip' | head -n 1)
  if [ -z "$ZIP_PATH" ]; then
    return 1
  fi

  cleanup_output
  unzip -q "$ZIP_PATH" -d "$OUTPUT_ROOT"
}

IFS=,
for backend in $BACKENDS; do
  case "$backend" in
    github)
      if download_from_github && extract_first_zip; then
        printf '%s\n' "$OUTPUT_ROOT"
        exit 0
      fi
      ;;
    drive)
      if download_from_drive && extract_first_zip; then
        printf '%s\n' "$OUTPUT_ROOT"
        exit 0
      fi
      ;;
    *)
      echo "Unsupported dependency backend: $backend" >&2
      exit 1
      ;;
  esac
done
unset IFS

echo 'Failed to restore private dependency bundle from all configured backends.' >&2
exit 1
