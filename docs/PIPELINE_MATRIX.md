# VoxelBlock Pipeline Tiers Matrix (Tier A / Tier B)

This document defines a practical engine direction for supporting:
- pure 2D games (pixel art / RPG maker-like)
- HD-2D / 2.5D games
- full 3D games

Core idea:
- `Tier A` = Lightweight 2D Pipeline
- `Tier B` = Scalable Unified Pipeline (2D + 3D + HD-2D/2.5D)

## Tier Goals

### Tier A: 2D Pipeline (Lightweight)
- Best for pure 2D games
- Optimized for performance and fast iteration
- Lower runtime cost (great for indie projects and mid-range hardware)

### Tier B: Unified Pipeline (Scalable)
- Best for 3D and HD-2D/2.5D games
- Shared rendering/material/post-processing stack
- Higher visual scalability and flexibility

## Feature Matrix

| Area | Tier A: 2D Lightweight | Tier B: Unified Scalable |
|---|---|---|
| Target Games | Pure 2D, pixel RPG, platformers | 3D, HD-2D, 2.5D, hybrids |
| Render Mode | Specialized 2D renderer | Shared 2D + 3D pipeline |
| Camera | Orthographic, pixel-perfect | Ortho + Perspective, shared camera framework |
| Batching | Static + dynamic sprite batching | Sprite batching + 3D batching/instancing |
| Tilemap | Chunk batching, collision layers | Tier A features + depth-aware layers |
| Lighting | Canvas/2D lights, optional normal maps | 2D + 3D lights with shared post FX |
| Materials | Unlit / simple lit 2D | Scalable material system, PBR-ready |
| Post-processing | Lightweight stack | Shared post stack |
| Physics | Simple collision or backend abstraction | Physics backend abstraction (Jolt/PhysX/etc.) |
| ECS | Full ECS for 2D entities | Shared ECS across 2D/3D |
| Asset Pipeline | png/wav/fbx staging + 2D workflows | Tier A + 3D mesh/material workflows |
| Editor | 2D scene/tile tools | Unified editor with mode-aware tools |
| Runtime UI | Pixel UI/HUD | Pixel UI + hybrid UI variants |
| Export | Standalone export for 2D games | Standalone export for 2D/3D/HD-2D |

## Renderer Design Guidelines

### Tier A (2D)
- `SpriteBatcher` is the core:
  - `StaticBatch`: tilemaps/backgrounds/static props
  - `DynamicBatch`: actors/VFX/dynamic sprites
- Sort by:
  - texture / atlas
  - material
  - blend mode
  - layer / draw order
- Main performance targets:
  - low draw calls
  - fewer state changes
  - minimal per-frame allocations

### Tier B (Unified)
- Prefer pass-based rendering / render graph style architecture
- Clear passes:
  - 2D opaque/lit
  - 3D opaque
  - transparent
  - UI
  - post-processing
- Reuse one material abstraction whenever possible
- HD-2D support:
  - 3D environments + 2D characters + shared post FX

## PBR Strategy (3D + HD-2D)

Tier B should support a `PBR-ready` material system:
- BaseColor (Albedo)
- Normal
- Metallic
- Roughness
- AO (optional)
- Emissive

For 2D/HD-2D:
- Provide `Lit Sprite Material` (normal map + emissive)
- Reuse post-processing with 3D (bloom, grading, tone mapping)

## API / Tooling Implications

### Tier A API Focus
- Scene2D / Tilemap APIs
- Sprite animation APIs
- 2D lighting APIs
- Save/load game-state APIs

### Tier B API Focus
- Material APIs
- Render pass/layer APIs
- 3D + hybrid scene APIs
- Physics backend integration APIs

Shared across both tiers:
- ECS
- Asset pipeline
- Export pipeline
- Lua scripting
- Python tooling

## Suggested Milestone Roadmap

### M1: Tier A Core (Playable 2D)
- Sprite batching (static + dynamic)
- Tilemap chunk rendering
- 2D camera + pixel-perfect mode
- Scene save/load (2D map entities)
- Basic 2D canvas lighting
- Lightweight post FX

### M2: Tier A Tooling (Production-ready 2D)
- Tile editor tools (paint/fill/erase/layers)
- Basic animation editor
- Tileset/atlas pipeline
- RPG-focused runtime UI widgets
- Export validation/polish

### M3: Tier B Foundation (Unified)
- Material abstraction
- Shared post-processing stack
- 3D scene integration with ECS
- Hybrid render ordering (2D + 3D)
- Physics abstraction (`none/jolt/...`)

### M4: Tier B HD-2D / 2.5D
- 3D environment + 2D character pipeline
- Lit sprites + depth-aware FX
- PBR for 3D assets
- Hybrid editor previews/tools

## Bug / Runtime Checklist (Every Iteration)

- Batching:
  - No excessive per-frame allocations
  - Vertex/index buffer updates do not overflow
- Lighting:
  - Invalid light parameters do not crash rendering
  - Safe fallback when shaders/normal maps are missing
- Post FX:
  - Correct pass ordering (2D/3D/UI)
  - Feature toggles do not break rendering
- ECS:
  - Stale entity handles are rejected
  - Create/destroy loops do not invalidate iteration unexpectedly
- Export:
  - Scenes/assets are fully included
  - Build runs from the exported folder

