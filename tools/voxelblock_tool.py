#!/usr/bin/env python3
"""
VoxelBlock tooling entry point (starter template).

This script is intentionally lightweight and does not require engine bindings yet.
Extend commands here for scene/asset/export automation.
"""

from __future__ import annotations

import argparse
from pathlib import Path
import sys


def project_root() -> Path:
    return Path(__file__).resolve().parents[1]


def cmd_info(_: argparse.Namespace) -> int:
    root = project_root()
    print(f"VoxelBlock tools root: {root}")
    print(f"Has docs: {(root / 'docs').exists()}")
    print(f"Has editor: {(root / 'VoxelBlock.Editor').exists()}")
    print(f"Has bindings: {(root / 'bindings').exists()}")
    return 0


def cmd_validate_layout(_: argparse.Namespace) -> int:
    root = project_root()
    required = [
        "docs",
        "include",
        "src",
        "VoxelBlock.Editor",
        "VoxelBlock.Bridge",
        "bindings",
    ]
    missing = [p for p in required if not (root / p).exists()]
    if missing:
        print("Missing paths:")
        for item in missing:
            print(f"- {item}")
        return 1
    print("Project layout looks OK.")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="voxelblock-tool")
    sub = parser.add_subparsers(dest="command", required=True)

    p_info = sub.add_parser("info", help="Show basic project/tooling info")
    p_info.set_defaults(func=cmd_info)

    p_validate = sub.add_parser("validate-layout", help="Check required project folders")
    p_validate.set_defaults(func=cmd_validate_layout)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
