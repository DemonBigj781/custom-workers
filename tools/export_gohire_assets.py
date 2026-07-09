#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import io
import os
import re
from pathlib import Path

import UnityPy


DEFAULT_TERMS = (
    "hire",
    "worker",
    "phone",
    "controller",
    "highlight",
    "cursor",
    "crosshair",
)


def sanitize(name: str) -> str:
    cleaned = re.sub(r"[^0-9A-Za-z]+", "_", name).strip("_")
    if not cleaned:
        cleaned = "unnamed"
    if cleaned[0].isdigit():
        cleaned = f"asset_{cleaned}"
    return cleaned.lower()


def cs_class_name(stem: str) -> str:
    return "".join(part.capitalize() for part in stem.split("_")) + "Base64Data"


def export_assets(data_root: Path, output_root: Path, terms: tuple[str, ...]) -> list[Path]:
    output_root.mkdir(parents=True, exist_ok=True)
    generated: list[Path] = []
    lowered_terms = tuple(term.lower() for term in terms)
    written_stems: set[str] = set()
    for asset_path in sorted(data_root.glob("*.assets")):
        env = UnityPy.load(str(asset_path))
        for obj in env.objects:
            if obj.type.name not in {"Sprite", "Texture2D"}:
                continue

            data = obj.read()
            name = getattr(data, "m_Name", "") or ""
            lowered = name.lower()
            if not any(term in lowered for term in lowered_terms):
                continue

            image = data.image
            if image is None:
                continue

            buffer = io.BytesIO()
            image.save(buffer, format="PNG")
            payload = base64.b64encode(buffer.getvalue()).decode("ascii")
            stem = sanitize(name)
            if stem in written_stems:
                continue

            class_name = cs_class_name(stem)
            file_path = output_root / f"{stem}base64enum.cs"
            file_path.write_text(
                "namespace CustomWorkers;\n\n"
                f"internal static class {class_name}\n"
                "{\n"
                f"    internal const string Value = @\"{payload}\";\n"
                "}\n",
                encoding="utf-8",
            )
            written_stems.add(stem)
            generated.append(file_path)
            print(f"generated {file_path.name} from {asset_path.name}:{name}")

    return generated


def main() -> int:
    parser = argparse.ArgumentParser(description="Export Go Hire related Unity sprites/textures into enum/images base64 files.")
    parser.add_argument("data_root", type=Path)
    parser.add_argument("output_root", type=Path)
    parser.add_argument("--term", action="append", default=[])
    args = parser.parse_args()

    terms = tuple(args.term) if args.term else DEFAULT_TERMS
    generated = export_assets(args.data_root, args.output_root, terms)
    print(f"generated_count={len(generated)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
