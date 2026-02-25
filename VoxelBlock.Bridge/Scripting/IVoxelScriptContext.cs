using System;

namespace VoxelBlock.Bridge.Scripting
{
    public interface IVoxelScriptContext
    {
        bool IsEngineReady { get; }
        VoxelBlockEngine? Engine { get; }

        bool PlaceVoxel(int x, int y, int z, string blockType);
        bool DestroyVoxel(int x, int y, int z);
        (bool ok, string? error) ExecLua(string code);

        void Log(string message);
    }

    public sealed class VoxelScriptContext : IVoxelScriptContext
    {
        private readonly Func<VoxelBlockEngine?> _engineAccessor;
        private readonly Action<string>? _logger;

        public VoxelScriptContext(Func<VoxelBlockEngine?> engineAccessor, Action<string>? logger = null)
        {
            _engineAccessor = engineAccessor ?? throw new ArgumentNullException(nameof(engineAccessor));
            _logger = logger;
        }

        public bool IsEngineReady => Engine?.IsValid == true;
        public VoxelBlockEngine? Engine => _engineAccessor();

        public bool PlaceVoxel(int x, int y, int z, string blockType)
        {
            var engine = Engine;
            return engine is not null && engine.IsValid && engine.PlaceVoxel(x, y, z, blockType);
        }

        public bool DestroyVoxel(int x, int y, int z)
        {
            var engine = Engine;
            return engine is not null && engine.IsValid && engine.DestroyVoxel(x, y, z);
        }

        public (bool ok, string? error) ExecLua(string code)
        {
            var engine = Engine;
            if (engine is null || !engine.IsValid) return (false, "Engine not ready");
            return engine.ExecLua(code);
        }

        public void Log(string message) => _logger?.Invoke(message);
    }
}

