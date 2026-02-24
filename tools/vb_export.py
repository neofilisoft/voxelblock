#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
from pathlib import Path

from vb_common import ensure_dir, project_paths, write_json


def safe_copy_tree(src: Path, dst: Path) -> None:
    if not src.exists():
        return
    ensure_dir(dst.parent)
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)


def export_project(build_name: str) -> int:
    paths = project_paths()
    out_dir = ensure_dir(paths.exports / build_name)

    safe_copy_tree(paths.scenes, out_dir / "Scenes")
    safe_copy_tree(paths.assets_processed, out_dir / "Assets" / "Processed")
    safe_copy_tree(paths.mods, out_dir / "mods")

    manifest = {
        "schema_version": 1,
        "build_name": build_name,
        "includes": [
            "Scenes",
            "Assets/Processed",
            "mods",
        ],
        "exe_found": False,
    }
    write_json(out_dir / "export_manifest.json", manifest)
    print(f"Export package created: {out_dir}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="VoxelBlock standalone export helper (Python)")
    parser.add_argument("build_name", nargs="?", default="Build1")
    args = parser.parse_args()
    return export_project(args.build_name)


if __name__ == "__main__":
    raise SystemExit(main())

