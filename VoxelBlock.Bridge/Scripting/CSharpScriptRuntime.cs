using System;
using System.Collections.Generic;

namespace VoxelBlock.Bridge.Scripting
{
    public sealed class CSharpScriptRuntime
    {
        private readonly IVoxelScriptContext _context;
        private readonly Dictionary<string, Func<VoxelScriptBehaviour>> _factories = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<VoxelScriptBehaviour> _instances = new();

        public CSharpScriptRuntime(IVoxelScriptContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<string> RegisteredScriptNames => _factories.Keys;
        public int ActiveScriptCount => _instances.Count;

        public void RegisterFactory(string name, Func<VoxelScriptBehaviour> factory)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Script name is required", nameof(name));
            _factories[name.Trim()] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void Register<T>(string? name = null) where T : VoxelScriptBehaviour, new()
        {
            var key = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name!;
            RegisterFactory(key, static () => new T());
        }

        public bool TryAttach(string name, out VoxelScriptBehaviour? instance, out string? error)
        {
            instance = null;
            error = null;

            if (!_factories.TryGetValue(name, out var factory))
            {
                error = $"Unknown C# script '{name}'";
                return false;
            }

            try
            {
                var script = factory();
                if (script is null)
                {
                    error = $"Factory for '{name}' returned null";
                    return false;
                }

                script.ScriptName = name;
                script.Context = _context;
                script.OnAwake();
                _instances.Add(script);
                instance = script;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Attach failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            for (int i = 0; i < _instances.Count; i++)
            {
                var script = _instances[i];
                if (!script.Enabled) continue;

                try
                {
                    if (!script.Started)
                    {
                        script.OnStart();
                        script.Started = true;
                    }
                    script.OnUpdate(dt);
                }
                catch (Exception ex)
                {
                    _context.Log($"[C#:{script.ScriptName}] Runtime error: {ex.GetType().Name}: {ex.Message}");
                    script.Enabled = false;
                }
            }
        }

        public void DispatchEngineEvent(string name, string json)
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                var script = _instances[i];
                if (!script.Enabled) continue;

                try
                {
                    script.OnEngineEvent(name, json);
                }
                catch (Exception ex)
                {
                    _context.Log($"[C#:{script.ScriptName}] Event error: {ex.GetType().Name}: {ex.Message}");
                    script.Enabled = false;
                }
            }
        }

        public void Reset()
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                try { _instances[i].OnDestroy(); }
                catch (Exception ex)
                {
                    _context.Log($"[C#:{_instances[i].ScriptName}] Destroy error: {ex.GetType().Name}: {ex.Message}");
                }
            }
            _instances.Clear();
        }
    }
}
