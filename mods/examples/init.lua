-- VoxelBlock example mod pack entrypoint.
-- Kept simple and runtime-safe: returns tables/functions only.

local M = {}

M.name = "VoxelBlock Example Mods"
M.version = "0.1.0"

function M.describe()
  return "Example Lua scripts for RPG-like events and HD-2D style lighting hooks."
end

return M

