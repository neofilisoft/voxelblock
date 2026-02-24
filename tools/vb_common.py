from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import json


@dataclass(frozen=True)
class ProjectPaths:
    root: Path
    assets_raw: Path
    assets_processed: Path
    scenes: Path
    mods: Path
    exports: Path


def project_paths(root: Path | None = None) -> ProjectPaths:
    root = (root or Path(__file__).resolve().parents[1]).resolve()
    return ProjectPaths(
        root=root,
        assets_raw=root / "Assets" / "Raw",
        assets_processed=root / "Assets" / "Processed",
        scenes=root / "Scenes",
        mods=root / "mods",
        exports=root / "Exports",
    )


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def write_json(path: Path, payload: object) -> None:
    ensure_dir(path.parent)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

