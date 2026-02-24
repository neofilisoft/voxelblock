# VoxelBlock

VoxelBlock is a hybrid tooling project with:
- `C++` native engine modules (renderer/runtime/physics/material/scene scaffolds)
- `C#` Avalonia editor (`VoxelBlock.Editor`)
- `Lua` modding scripts (`mods/`)
- `Python` tooling scripts (`tools/`)

Current direction:
- `Tier A` Lightweight 2D pipeline (pixel-art / RPG-like workflows)
- `Tier B` Unified pipeline for 3D + HD-2D / 2.5D

## Build (Windows)

### Requirements
- `.NET SDK 8`
- `CMake 3.22`
- `Visual Studio 2022 Build Tools` (Desktop development with C++)
- `Ninja` (optional)

Notes:
- Prefer `Visual Studio 2022` generator for CMake on Windows.
- `MSYS/MinGW + CMake` may be slow or hang during configure on some machines.

## Build Editor EXE (Recommended)

Debug build:

```powershell
dotnet build VoxelBlock.Editor\VoxelBlock.Editor.csproj -c Debug
```

Publish Release EXE:

```powershell
dotnet publish VoxelBlock.Editor\VoxelBlock.Editor.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false
```

Output:
- `VoxelBlock.Editor\bin\Release\net8.0\win-x64\publish\VoxelBlock.Editor.exe`

## Build Native C++ Targets

Configure (MSVC / Visual Studio generator):

```powershell
cmake -S . -B build-msvc -G "Visual Studio 17 2022" -A x64 `
  -DVB_BUILD_RENDERER=OFF `
  -DVB_BUILD_CAPI=OFF `
  -DVB_BUILD_PYTHON=OFF `
  -DVB_BUILD_TESTS=OFF `
  -DVB_BUILD_GAME=OFF
```

Build the native scaffold target:

```powershell
cmake --build build-msvc --config Release --target voxelblock_native_framework
```

Notes:
- `voxelblock_native` is a native C++ target for validating new modules (`physics/material/scene/runtime`) without requiring the full legacy core checkout.
- Some targets intentionally `skip` in CMake if required files are missing (placeholder targets are used to keep configure/build behavior explicit).

## Project Structure

```text
VoxelBlock/
├─ CMakeLists.txt
├─ VoxelBlock.sln
├─ README.md
├─ .gitattributes
│
├─ thirdparty/                        # External SDK/backend drop-ins (e.g. physics)
│  ├─ README.md
│  └─ physics/
│     └─ README.md
│
├─ include/                           # Native public headers (C++)
│  ├─ engine.hpp
│  ├─ api/
│  ├─ ecs/
│  ├─ material/                       # Material / PBR-ready scaffolds
│  ├─ physics/                        # Physics abstraction + null backend
│  ├─ renderer/
│  ├─ runtime/                        # 2D/3D/post/ECS runtime pipeline scaffolds
│  ├─ scene/                          # Scene document / serializer scaffolds
│  └─ scripting/
│
├─ src/                               # Native sources (C++)
│  ├─ engine.cpp
│  ├─ api/
│  ├─ material/
│  ├─ physics/
│  │  └─ backends/
│  ├─ renderer/
│  ├─ runtime/
│  ├─ scene/
│  └─ scripting/
│
├─ bindings/
│  └─ python/
│     └─ voxelblock_py.cpp            # C++ binding layer for Python (pybind11)
│
├─ VoxelBlock.Bridge/                 # C# native bridge (P/Invoke)
│  ├─ VoxelBlock.Bridge.csproj
│  ├─ VoxelBlockNative.cs
│  └─ VoxelBlockEngine.cs
│
├─ VoxelBlock.Editor/                 # Avalonia Editor
│  ├─ VoxelBlock.Editor.csproj
│  ├─ Program.cs
│  ├─ App.axaml
│  ├─ App.axaml.cs
│  ├─ MainWindow.axaml
│  ├─ MainWindow.axaml.cs
│  ├─ Controls.cs
│  ├─ SceneEditing.cs
│  └─ ProjectPipelines.cs
│
├─ mods/                              # Lua mods / examples
│  ├─ mystuff.lua
│  └─ examples/
│     ├─ init.lua
│     ├─ rpg_events.lua
│     ├─ hd2d_lighting.lua
│     └─ pbr_lite_2d.lua
│
├─ tools/                             # Python tooling / automation
│  ├─ README.md
│  ├─ voxelblock_tool.py
│  ├─ vb_common.py
│  ├─ vb_assets.py
│  ├─ vb_scene.py
│  └─ vb_export.py
│
├─ experiments/                       # Non-production / legacy prototypes
│  ├─ README.md
│  └─ avalonia/
│     └─ OpenGlViewport.cs
│
└─ docs/
   ├─ API_CSharp_Quickstart_TH.md
   ├─ API_CSharp_Quickstart_EN.md
   ├─ EDITOR_WORKFLOW_TH.md
   ├─ EDITOR_WORKFLOW_EN.md
   ├─ PIPELINE_TIERS_MATRIX_TH.md
   └─ PIPELINE_TIERS_MATRIX_EN.md
```

## Lua Mods

Drop `.lua` files into `mods/` (or `mods/examples/` as references).

Examples included:
- RPG event hooks
- HD-2D lighting preset helpers
- PBR-lite 2D lit-sprite material registration examples

Notes:
- Lua in this repo is intended for runtime scripting / modding.
- It is **not** the old Python `lupa` flow.

## Python Tools

`tools/` contains lightweight Python scripts for automation and content workflows.

Examples:

```powershell
python tools\voxelblock_tool.py info
python tools\voxelblock_tool.py validate-layout
python tools\vb_scene.py demo_scene --width 32 --height 32 --block stone
python tools\vb_assets.py --dry-run
python tools\vb_export.py Build1
```

## Docs

See `docs/` for:
- C# API quickstart (TH/EN)
- Editor workflow (TH/EN)
- Tier A / Tier B pipeline architecture matrix (TH/EN)

## Troubleshooting

### CMake configure is very slow or hangs (Windows/MSYS)

Use Visual Studio generator instead of MSYS/MinGW:

```powershell
cmake -S . -B build-msvc -G "Visual Studio 17 2022" -A x64
```

### NuGet warning `NU1900`

This is usually a vulnerability-feed/network issue and does not necessarily block `dotnet build`.
