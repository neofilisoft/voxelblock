#!/usr/bin/env python3
from __future__ import annotations

import argparse
from hashlib import sha1
from pathlib import Path
from typing import Iterable

from vb_common import ensure_dir, project_paths, write_json


SUPPORTED = {".png": "texture", ".wav": "audio", ".fbx": "mesh"}


def iter_assets(root: Path) -> Iterable[Path]:
    if not root.exists():
        return []
    return (p for p in root.rglob("*") if p.is_file() and p.suffix.lower() in SUPPORTED)


def file_sha1(path: Path) -> str:
    h = sha1()
    with path.open("rb") as f:
        while True:
            chunk = f.read(65536)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()


def process_assets(dry_run: bool = False) -> int:
    paths = project_paths()
    ensure_dir(paths.assets_processed)

    items = []
    for src in sorted(iter_assets(paths.assets_raw), key=lambda p: str(p).lower()):
        rel = src.relative_to(paths.assets_raw)
        dst = paths.assets_processed / rel
        kind = SUPPORTED[src.suffix.lower()]
        items.append(
            {
                "kind": kind,
                "source": str(src),
                "relative_path": str(rel).replace("\\", "/"),
                "size_bytes": src.stat().st_size,
                "sha1": file_sha1(src),
                "staged_path": str(dst),
            }
        )
        if not dry_run:
            ensure_dir(dst.parent)
            dst.write_bytes(src.read_bytes())

    manifest = {
        "schema_version": 1,
        "asset_count": len(items),
        "items": items,
    }
    if not dry_run:
        write_json(paths.assets_processed / "asset_manifest.json", manifest)

    print(f"Processed {len(items)} assets ({'dry-run' if dry_run else 'written'}).")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="VoxelBlock asset processing helper (Python)")
    parser.add_argument("--dry-run", action="store_true", help="Scan and print without copying files")
    args = parser.parse_args()
    return process_assets(dry_run=args.dry_run)


if __name__ == "__main__":
    raise SystemExit(main())

