#!/usr/bin/env python3
import argparse
import pathlib
import sys

from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload, MediaIoBaseDownload


def build_service(credentials_path: str):
    scopes = ["https://www.googleapis.com/auth/drive"]
    credentials = service_account.Credentials.from_service_account_file(credentials_path, scopes=scopes)
    return build("drive", "v3", credentials=credentials)


def upload(service, file_path: pathlib.Path, folder_id: str, target_name: str):
    query = (
        f"name = '{target_name}' and '{folder_id}' in parents and trashed = false"
    )
    existing = service.files().list(q=query, fields="files(id,name)").execute().get("files", [])
    media = MediaFileUpload(str(file_path), resumable=True)

    metadata = {"name": target_name, "parents": [folder_id]}
    if existing:
      file_id = existing[0]["id"]
      service.files().update(fileId=file_id, media_body=media).execute()
      print(file_id)
      return

    created = service.files().create(body=metadata, media_body=media, fields="id").execute()
    print(created["id"])


def download(service, file_id: str, output_path: pathlib.Path):
    request = service.files().get_media(fileId=file_id)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("wb") as handle:
        downloader = MediaIoBaseDownload(handle, request)
        done = False
        while not done:
            _, done = downloader.next_chunk()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["upload", "download"])
    parser.add_argument("--credentials", required=True)
    parser.add_argument("--file", required=True)
    parser.add_argument("--folder-id")
    parser.add_argument("--file-id")
    parser.add_argument("--name")
    args = parser.parse_args()

    service = build_service(args.credentials)
    file_path = pathlib.Path(args.file)

    if args.mode == "upload":
        if not args.folder_id:
            parser.error("--folder-id is required for upload")
        target_name = args.name or file_path.name
        upload(service, file_path, args.folder_id, target_name)
        return 0

    if not args.file_id:
        parser.error("--file-id is required for download")
    download(service, args.file_id, file_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
