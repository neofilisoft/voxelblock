# Physics Backends (ThirdParty)

This folder is intentionally empty by default.

Project users can add the backend they want later, for example:

```text
thirdparty/
\\- physics/
   |- jolt/
   |- physx/
   \\- havok/
```

Recommended backend layout:

```text
<backend-name>/
|- include/
|- lib/
|- bin/        # optional runtime DLLs
|- src/        # optional if vendoring source
|- licenses/
\\- README.md
```

Integration rule:

- Engine/game code should use a VoxelBlock physics abstraction (`IPhysicsBackend`) instead of calling backend SDK APIs directly everywhere.
