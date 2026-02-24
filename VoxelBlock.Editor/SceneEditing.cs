using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace VoxelBlock.Editor
{
    public sealed class SceneEditorState : INotifyPropertyChanged
    {
        private readonly SceneCellModel[,] _cells;
        private string _sceneName = "scene1";
        private bool _eraseMode;
        private int _layerY = 20;
        private string _selectedBlockName = "stone";
        private byte _selectedR = 180;
        private byte _selectedG = 180;
        private byte _selectedB = 180;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? SceneChanged;
        public event Action<ScenePaintOp>? CellPainted;

        public int Columns { get; }
        public int Rows { get; }

        public string SceneName
        {
            get => _sceneName;
            set => Set(ref _sceneName, string.IsNullOrWhiteSpace(value) ? "scene1" : value.Trim());
        }

        public bool EraseMode
        {
            get => _eraseMode;
            set
            {
                if (Set(ref _eraseMode, value))
                    OnPropertyChanged(nameof(ActiveToolLabel));
            }
        }

        public int LayerY
        {
            get => _layerY;
            set
            {
                if (Set(ref _layerY, Math.Clamp(value, 0, 255)))
                    OnPropertyChanged(nameof(LayerYText));
            }
        }

        public string LayerYText
        {
            get => _layerY.ToString();
            set
            {
                if (!int.TryParse(value, out int parsed)) return;
                LayerY = parsed;
            }
        }

        public string SelectedBlockName => _selectedBlockName;
        public byte SelectedR => _selectedR;
        public byte SelectedG => _selectedG;
        public byte SelectedB => _selectedB;

        public string ActiveToolLabel => EraseMode ? "Erase" : $"Paint: {_selectedBlockName}";

        public SceneEditorState(int columns = 24, int rows = 24)
        {
            Columns = Math.Max(1, columns);
            Rows = Math.Max(1, rows);
            _cells = new SceneCellModel[Rows, Columns];
            for (int z = 0; z < Rows; z++)
            for (int x = 0; x < Columns; x++)
                _cells[z, x] = new SceneCellModel(x, z);
        }

        public SceneCellModel GetCell(int x, int z) => _cells[z, x];

        public void SetSelectedBlock(string? blockName, byte r, byte g, byte b)
        {
            _selectedBlockName = string.IsNullOrWhiteSpace(blockName) ? "stone" : blockName.Trim();
            _selectedR = r;
            _selectedG = g;
            _selectedB = b;
            OnPropertyChanged(nameof(SelectedBlockName));
            OnPropertyChanged(nameof(SelectedR));
            OnPropertyChanged(nameof(SelectedG));
            OnPropertyChanged(nameof(SelectedB));
            OnPropertyChanged(nameof(ActiveToolLabel));
        }

        public void ToggleEraseMode() => EraseMode = !EraseMode;

        public bool PaintAt(int x, int z)
        {
            if (x < 0 || z < 0 || x >= Columns || z >= Rows) return false;
            var cell = _cells[z, x];

            if (EraseMode)
            {
                if (cell.IsEmpty) return false;
                cell.SetEmpty();
                SceneChanged?.Invoke();
                CellPainted?.Invoke(new ScenePaintOp(x, z, null));
                return true;
            }

            if (cell.BlockName == _selectedBlockName &&
                cell.R == _selectedR && cell.G == _selectedG && cell.B == _selectedB)
                return false;

            cell.SetBlock(_selectedBlockName, _selectedR, _selectedG, _selectedB);
            SceneChanged?.Invoke();
            CellPainted?.Invoke(new ScenePaintOp(x, z, _selectedBlockName));
            return true;
        }

        public void Clear()
        {
            bool changed = false;
            for (int z = 0; z < Rows; z++)
            for (int x = 0; x < Columns; x++)
            {
                if (_cells[z, x].IsEmpty) continue;
                _cells[z, x].SetEmpty();
                changed = true;
            }

            if (changed)
                SceneChanged?.Invoke();
        }

        public SceneDocument ToDocument()
        {
            var doc = new SceneDocument
            {
                Name = SceneName,
                LayerY = LayerY,
                Columns = Columns,
                Rows = Rows,
            };

            for (int z = 0; z < Rows; z++)
            for (int x = 0; x < Columns; x++)
            {
                var c = _cells[z, x];
                if (c.IsEmpty) continue;
                doc.Blocks.Add(new SceneBlockPlacement
                {
                    X = x,
                    Z = z,
                    Block = c.BlockName,
                    R = c.R,
                    G = c.G,
                    B = c.B,
                });
            }

            return doc;
        }

        public void LoadDocument(SceneDocument doc)
        {
            Clear();

            SceneName = string.IsNullOrWhiteSpace(doc.Name) ? SceneName : doc.Name;
            LayerY = doc.LayerY;
            foreach (var b in doc.Blocks)
            {
                if (b.X < 0 || b.Z < 0 || b.X >= Columns || b.Z >= Rows) continue;
                _cells[b.Z, b.X].SetBlock(b.Block, b.R, b.G, b.B);
            }

            SceneChanged?.Invoke();
        }

        public string GetScenePath(string projectRoot)
        {
            var safe = string.Concat(SceneName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(safe)) safe = "scene1";
            return Path.Combine(projectRoot, "Scenes", safe + ".json");
        }

        public (bool ok, string message) SaveToProject(string projectRoot)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(projectRoot, "Scenes"));
                var path = GetScenePath(projectRoot);
                var json = JsonSerializer.Serialize(ToDocument(), SceneJson.Options);
                File.WriteAllText(path, json);
                return (true, path);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string message) LoadFromProject(string projectRoot)
        {
            try
            {
                var path = GetScenePath(projectRoot);
                if (!File.Exists(path)) return (false, $"Scene file not found: {path}");
                var json = File.ReadAllText(path);
                var doc = JsonSerializer.Deserialize<SceneDocument>(json, SceneJson.Options);
                if (doc is null) return (false, "Scene JSON is empty or invalid.");
                if (doc.Columns != Columns || doc.Rows != Rows)
                {
                    // Keep editor grid size stable; ignore mismatch and clamp placements.
                }
                LoadDocument(doc);
                return (true, path);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string prop = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(prop);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string prop = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public sealed class SceneCellModel
    {
        public int X { get; }
        public int Z { get; }
        public string BlockName { get; private set; } = "";
        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }
        public bool IsEmpty => string.IsNullOrEmpty(BlockName);

        public SceneCellModel(int x, int z)
        {
            X = x;
            Z = z;
        }

        public void SetBlock(string name, byte r, byte g, byte b)
        {
            BlockName = name ?? "";
            R = r; G = g; B = b;
        }

        public void SetEmpty()
        {
            BlockName = "";
            R = G = B = 0;
        }
    }

    public readonly record struct ScenePaintOp(int X, int Z, string? BlockName);

    public sealed class SceneDocument
    {
        public string Name { get; set; } = "scene1";
        public int LayerY { get; set; } = 20;
        public int Columns { get; set; } = 24;
        public int Rows { get; set; } = 24;
        public List<SceneBlockPlacement> Blocks { get; set; } = new();
    }

    public sealed class SceneBlockPlacement
    {
        public int X { get; set; }
        public int Z { get; set; }
        public string Block { get; set; } = "";
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    internal static class SceneJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
