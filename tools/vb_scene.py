#!/usr/bin/env python3
from __future__ import annotations

import argparse
from dataclasses import asdict, dataclass
from pathlib import Path

from vb_common import project_paths, write_json


@dataclass
class Block:
    x: int
    y: int
    z: int
    block: str


def build_grid(width: int, height: int, layer_y: int, block_name: str) -> list[Block]:
    blocks: list[Block] = []
    for z in range(height):
        for x in range(width):
            blocks.append(Block(x=x, y=layer_y, z=z, block=block_name))
    return blocks


def write_scene(scene_name: str, width: int, height: int, layer_y: int, block_name: str) -> Path:
    paths = project_paths()
    scene = {
        "schema_version": 1,
        "scene_name": scene_name,
        "blocks": [asdict(b) for b in build_grid(width, height, layer_y, block_name)],
    }
    out_path = paths.scenes / f"{scene_name}.json"
    write_json(out_path, scene)
    return out_path


def main() -> int:
    parser = argparse.ArgumentParser(description="VoxelBlock scene utility (Python)")
    parser.add_argument("scene_name")
    parser.add_argument("--width", type=int, default=16)
    parser.add_argument("--height", type=int, default=16)
    parser.add_argument("--layer-y", type=int, default=0)
    parser.add_argument("--block", default="stone")
    args = parser.parse_args()

    if args.width <= 0 or args.height <= 0:
        raise SystemExit("width/height must be > 0")

    path = write_scene(args.scene_name, args.width, args.height, args.layer_y, args.block)
    print(f"Scene written: {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

