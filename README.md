# VoxelBlock Engine v0.8

A Minecraft-inspired voxel engine built with [Ursina](https://www.ursinaengine.org/).

```bash
g++
```

## Build (Windows)
# Requirements
.NET SDK 8
CMake 3.22
Visual Studio 2022 Build Tools
Ninja (Optional)

## Block Types

| # | Block |
|---|-------|
| 1 | Grass |
| 2 | Dirt |
| 3 | Stone |
| 4 | Wood |
| 5 | Leaves |
| 6 | Sand |

## Project Structure

```
VoxelBlock/
├─ CMakeLists.txt                          # Native build (C++ engine/modules)
├─ VoxelBlock.sln                          # .NET solution (Editor + Bridge)
│
├─ thirdparty/     
│  ├─ README.md
│  └─ physics/     
│     └─ README.md
│
├─ include/      
│  ├─ engine.hpp
│  ├─ api/
│  │  └─ voxelblock_capi.h        
│  ├─ renderer/
│  │  ├─ camera.hpp
│  │  ├─ mesh.hpp
│  │  ├─ renderer.hpp
│  │  ├─ shader.hpp
│  │  └─ window.hpp
│  ├─ scripting/
│  │  └─ lua_runtime.hpp
│  └─ ecs/    
│     ├─ entity.hpp
│     ├─ components.hpp
│     ├─ registry.hpp
│     └─ systems.hpp
│
├─ src/  
│  ├─ engine.cpp
│  ├─ api/
│  │  └─ voxelblock_capi.cpp
│  ├─ renderer/
│  │  ├─ camera.cpp
│  │  ├─ mesh.cpp
│  │  ├─ renderer.cpp
│  │  ├─ shader.cpp
│  │  └─ window.cpp
│  └─ scripting/
│     └─ lua_runtime.cpp
│
├─ bindings/
│  └─ python/
│     └─ voxelblock_py.cpp 
│
├─ VoxelBlock.Bridge/ 
│  ├─ VoxelBlock.Bridge.csproj
│  ├─ VoxelBlockNative.cs
│  └─ VoxelBlockEngine.cs
│
├─ VoxelBlock.Editor/  
│  ├─ VoxelBlock.Editor.csproj
│  ├─ Program.cs
│  ├─ App.axaml
│  ├─ App.axaml.cs
│  ├─ MainWindow.axaml
│  ├─ MainWindow.axaml.cs
│  ├─ Controls.cs
│  ├─ SceneEditing.cs   
│  ├─ ProjectPipelines.cs    
│  └─ OpenGlViewport.cs              
│
├─ mods/                               
   └─ mystuff.lua                               

```

## Adding Mods

Drop any `.lua` file into the `mods/` folder. The following API is exposed to Lua:

```lua
print("hello from lua")
place_block(x, y, z, "stone")   -- place a block at world coords
```

Requires `lupa` (`pip install lupa`). If not installed, mods are silently skipped.

## Adding Textures

Place `.png` files in `assets/textures/`. To use a custom texture on a block,
update `BLOCK_COLORS` in `core/voxel.py` and pass the texture name to `Voxel`.
