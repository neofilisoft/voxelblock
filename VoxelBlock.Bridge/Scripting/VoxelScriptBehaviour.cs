using System;

namespace VoxelBlock.Bridge.Scripting
{
    public abstract class VoxelScriptBehaviour
    {
        public string ScriptName { get; internal set; } = "";
        public Guid InstanceId { get; } = Guid.NewGuid();
        public IVoxelScriptContext Context { get; internal set; } = null!;
        public bool Enabled { get; set; } = true;

        internal bool Started { get; set; }

        public virtual void OnAwake() { }
        public virtual void OnStart() { }
        public virtual void OnUpdate(float dt) { }
        public virtual void OnEngineEvent(string name, string json) { }
        public virtual void OnDestroy() { }

        protected void Log(string message)
            => Context?.Log($"[C#:{(string.IsNullOrWhiteSpace(ScriptName) ? GetType().Name : ScriptName)}] {message}");
    }
}

