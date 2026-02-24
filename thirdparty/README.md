# VoxelBlock ThirdParty

This folder is reserved for vendor SDKs/dependencies used by games built on VoxelBlock Engine.

Current convention:

- `thirdparty/physics/`
  - Physics backends and SDKs (for example `jolt`, `physx`, `havok`)

Notes:

- Keep vendor SDKs isolated from engine/game source code.
- Prefer adapter layers in `include/physics` and `src/physics/backends`.
- Some SDKs have license/redistribution restrictions. Do not commit restricted binaries unless allowed.

