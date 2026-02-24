// VoxelBlock.Editor â€” MainWindow.axaml.cs + MainViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using VoxelBlock.Bridge;

namespace VoxelBlock.Editor
{

    public partial class MainWindow : Window
    {
        private MainViewModel _vm = null!;

        public MainWindow()
        {
            InitializeComponent();
            _vm         = new MainViewModel();
            DataContext = _vm;
            _vm.Init();
        }

        private void InitializeComponent()
            => Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        protected override void OnClosed(EventArgs e)
        {
            _vm.Dispose();
            base.OnClosed(e);
        }
    }

    // MainViewModel

    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private VoxelBlockEngine? _engine;
        private DispatcherTimer?  _ticker;
        private DispatcherTimer?  _assetTicker;
        private bool              _engineReady; // FIX â‘¬
        private bool              _assetPipelineBusy;
        private long              _assetFingerprint;
        private readonly StringBuilder _luaLog = new();
        private readonly AssetPipelineService _assetPipeline = new();
        private readonly StandaloneExportService _exportService = new();

        private string _statusText          = "Initialisingâ€¦";
        private string _statsText           = "";
        private string _worldName           = "world1";
        private string _seedText            = "0";
        private string _chunkCountText      = "0";      
        private string _blockFilter         = "";
        private string _luaInput            = "";
        private string _luaOutput           = "";
        private string _selectedBlockName    = "";       
        private string _selectedBlockHardness = "1.0"; 
        private string _assetPipelineStatus = "Idle";
        private string _exportStatus = "Idle";
        private string _exportBuildName = "Build1";
        private BlockViewModel? _selectedBlock;
        private StatsViewModel  _stats = new();
        private long            _engineHandle;

        public string StatusText   { get => _statusText;   set => Set(ref _statusText,   value); }
        public string StatsText    { get => _statsText;    set => Set(ref _statsText,    value); }
        public string WorldName    { get => _worldName;    set => Set(ref _worldName,    value); }
        public string SeedText     { get => _seedText;     set => Set(ref _seedText,     value); }
        public string ChunkCountText { get => _chunkCountText; set => Set(ref _chunkCountText, value); } 
        public string BlockFilter  { get => _blockFilter;
                                     set { Set(ref _blockFilter, value); RefreshBlockList(); } }
        public string LuaInput     { get => _luaInput;     set => Set(ref _luaInput,     value); }
        public string LuaOutput    { get => _luaOutput;    set => Set(ref _luaOutput,    value); }

        // expose as string so AXAML TextBox/LabeledField can bind directly
        public string SelectedBlockName     { get => _selectedBlockName;     set => Set(ref _selectedBlockName,     value); }
        public string SelectedBlockHardness { get => _selectedBlockHardness; set => Set(ref _selectedBlockHardness, value); }
        public string AssetPipelineStatus   { get => _assetPipelineStatus;   set => Set(ref _assetPipelineStatus, value); }
        public string ExportStatus          { get => _exportStatus;          set => Set(ref _exportStatus, value); }
        public string ExportBuildName       { get => _exportBuildName;       set => Set(ref _exportBuildName, value); }
        public string ProjectRootPath       { get; } = Directory.GetCurrentDirectory();

        public long EngineHandle => _engineHandle;
        public SceneEditorState SceneEditor { get; } = new(24, 24);

        public BlockViewModel? SelectedBlock
        {
            get => _selectedBlock;
            set
            {
                Set(ref _selectedBlock, value);
                SelectedBlockName     = value?.Name     ?? "";
                SelectedBlockHardness = value?.Hardness ?? "1.0"; // already string
                if (value is not null)
                    SceneEditor.SetSelectedBlock(value.Name, value.R, value.G, value.B);
            }
        }

        public StatsViewModel Stats { get => _stats; set => Set(ref _stats, value); }

        public ObservableCollection<BlockViewModel> AllBlocks      { get; } = new();
        public ObservableCollection<BlockViewModel> FilteredBlocks { get; } = new();
        public ObservableCollection<InventoryItem>  InventoryItems { get; } = new();
        public ObservableCollection<AssetPipelineItemView> AssetItems { get; } = new();

        public ICommand NewWorldCommand       { get; }
        public ICommand OpenWorldCommand      { get; }
        public ICommand SaveCommand           { get; }
        public ICommand ExitCommand           { get; }
        public ICommand PlayCommand           { get; }
        public ICommand StopCommand           { get; }
        public ICommand RegenerateCommand     { get; }
        public ICommand RunLuaCommand         { get; }
        public ICommand ReloadModsCommand     { get; }
        public ICommand WorldSettingsCommand  { get; }
        public ICommand ShowBlocksCommand     { get; }
        public ICommand ShowLuaConsoleCommand { get; }
        public ICommand ShowStatsCommand      { get; }
        public ICommand SaveSceneCommand      { get; }
        public ICommand LoadSceneCommand      { get; }
        public ICommand ClearSceneCommand     { get; }
        public ICommand ProcessAssetsCommand  { get; }
        public ICommand ExportStandaloneCommand { get; }

        public MainViewModel()
        {
            NewWorldCommand       = new RelayCommand(_newWorld);
            OpenWorldCommand      = new RelayCommand(() => StatusText = "Open â€” file dialog (Phase 4)");
            SaveCommand           = new RelayCommand(_save);
            ExitCommand           = new RelayCommand(() => Environment.Exit(0));
            PlayCommand           = new RelayCommand(() => StatusText = "â–¶ Play mode");
            StopCommand           = new RelayCommand(() => StatusText = "â¹ Stopped");
            RegenerateCommand     = new RelayCommand(_regenerate);
            RunLuaCommand         = new RelayCommand(_runLua);
            ReloadModsCommand     = new RelayCommand(_reloadMods);
            WorldSettingsCommand  = new RelayCommand(() => StatusText = "World Settings");
            ShowBlocksCommand     = new RelayCommand(() => StatusText = "Block Registry");
            ShowLuaConsoleCommand = new RelayCommand(() => StatusText = "Lua Console");
            ShowStatsCommand      = new RelayCommand(() => StatusText = "Render Stats");
            SaveSceneCommand      = new RelayCommand(_saveScene);
            LoadSceneCommand      = new RelayCommand(_loadScene);
            ClearSceneCommand     = new RelayCommand(_clearScene);
            ProcessAssetsCommand  = new RelayCommand(_processAssetsNow);
            ExportStandaloneCommand = new RelayCommand(_exportStandalone);

            SceneEditor.CellPainted += _onSceneCellPainted;
            ProjectFolders.EnsureAll(ProjectRootPath);
            AssetPipelineStatus = $"Watching {Path.Combine(ProjectRootPath, "Assets", "Raw")}";
            _startAssetWatcher();
            _processAssetsNow();
        }

        public void Init()
        {
            try
            {
                _engine = new VoxelBlockEngine();
                _engine.OnEngineEvent += _onEngineEvent;

                bool ok = _engine.InitHeadless(1280, 720);
                if (!ok)
                {
                    _seedFallbackBlockPaletteIfNeeded();
                    StatusText = "âš  Engine init failed â€” native library missing?";
                    
                    return;
                }

                _engineReady  = true;
                _engineHandle = _engine.Handle;
                OnPropertyChanged(nameof(EngineHandle));

                _refreshBlockRegistry();

                _engine.AddItem("grass", 64);
                _engine.AddItem("stone", 64);
                _engine.AddItem("wood",  32);
                _engine.AddItem("dirt",  64);
                _refreshInventory();
                if (AllBlocks.Count > 0)
                    SelectedBlock = AllBlocks[0];

                _startAssetWatcher();
                _processAssetsNow();

                _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16.67) };
                _ticker.Tick += (_, _) => _frame();
                _ticker.Start();

                SeedText   = _engine.WorldSeed.ToString();
                StatusText = $"âœ… '{WorldName}' ready â€” seed {_engine.WorldSeed}";
            }
            catch (Exception ex)
            {
                StatusText = $"âŒ {ex.GetType().Name}: {ex.Message}";
            }
        }

        private float _statTimer;

        private void _frame()
        {
            if (!_engineReady || _engine is null || !_engine.IsValid) return;

            try { _engine.Tick(1f / 60f); }
            catch (Exception ex)
            {
                StatusText   = $"âš  Tick error: {ex.Message}";
                _engineReady = false;
                _ticker?.Stop();
                return;
            }

            _statTimer += 1f / 60f;
            if (_statTimer < 0.5f) return;
            _statTimer = 0f;

            var s = _engine.GetStats();
            Stats = new StatsViewModel
            {
                ChunksDrawnText = s.ChunksDrawn.ToString(),
                DrawCallsText   = s.DrawCalls.ToString(),
                TrianglesText   = s.Triangles.ToString("N0"),
                FrameMs         = s.FrameMs,
            };

            int chunks = _engine.ChunkCount;
            ChunkCountText = chunks.ToString(); 
            StatsText = $"Chunks: {chunks} | {s.FrameMs:F1} ms | ~{(s.FrameMs > 0 ? 1000f / s.FrameMs : 0):F0} FPS";
        }

        private void _newWorld()
        {
            if (!_engineReady || _engine is null) return;
            WorldName = "world_new";
            _engine.LoadWorld(WorldName);
            _refreshBlockRegistry();
            StatusText = $"New world '{WorldName}'";
        }

        private void _save()
        {
            if (!_engineReady || _engine is null) return;
            bool ok = _engine.SaveWorld(WorldName);
            var scene = SceneEditor.SaveToProject(ProjectRootPath);
            StatusText = ok
                ? (scene.ok ? $"Saved world + scene '{SceneEditor.SceneName}'" : $"World saved; scene save failed: {scene.message}")
                : "Save failed";
        }

        private void _regenerate()
        {
            if (!_engineReady || _engine is null) return;
            _engine.LoadWorld(WorldName);
            ChunkCountText = _engine.ChunkCount.ToString();
            SeedText       = _engine.WorldSeed.ToString();
            StatusText     = $"Regenerated â€” seed {_engine.WorldSeed}";
        }

        private void _runLua()
        {
            if (!_engineReady || _engine is null) return;
            if (string.IsNullOrWhiteSpace(LuaInput)) return;
            var (ok, err) = _engine.ExecLua(LuaInput);
            _luaLog.AppendLine($"> {LuaInput}");
            if (!ok) _luaLog.AppendLine($"  ERROR: {err}");
            LuaOutput = _luaLog.ToString();
            LuaInput  = "";
        }

        private void _reloadMods()
        {
            if (!_engineReady || _engine is null) return;
            int n = _engine.LoadMods("mods");
            _refreshBlockRegistry();
            StatusText = $"ðŸ”„ {n} mod(s) loaded";
        }

        private void _saveScene()
        {
            var result = SceneEditor.SaveToProject(ProjectRootPath);
            StatusText = result.ok ? $"Scene saved: {result.message}" : $"Scene save failed: {result.message}";
        }

        private void _loadScene()
        {
            var result = SceneEditor.LoadFromProject(ProjectRootPath);
            if (!result.ok)
            {
                StatusText = $"Scene load failed: {result.message}";
                return;
            }

            _syncSceneToEngine();
            StatusText = $"Scene loaded: {result.message}";
        }

        private void _clearScene()
        {
            SceneEditor.Clear();
            _syncSceneToEngine();
            StatusText = $"Scene '{SceneEditor.SceneName}' cleared";
        }

        private void _processAssetsNow()
        {
            if (_assetPipelineBusy) return;
            _assetPipelineBusy = true;
            try
            {
                var report = _assetPipeline.Process(ProjectRootPath);
                AssetItems.Clear();
                foreach (var item in report.Items)
                    AssetItems.Add(item);

                AssetPipelineStatus = $"Assets: imported {report.ImportedCount}, skipped {report.SkippedCount}, errors {report.ErrorCount}";
                if (report.ErrorCount > 0 && report.Errors.Count > 0)
                    StatusText = $"Asset pipeline warning: {report.Errors[0]}";
            }
            catch (Exception ex)
            {
                AssetPipelineStatus = $"Asset pipeline error: {ex.Message}";
                StatusText = AssetPipelineStatus;
            }
            finally
            {
                _assetFingerprint = _computeAssetFingerprint();
                _assetPipelineBusy = false;
            }
        }

        private void _exportStandalone()
        {
            var report = _exportService.Export(ProjectRootPath, ExportBuildName);
            ExportStatus = report.HasPlayableExe
                ? $"Exported playable package: {report.ExportDirectory}"
                : $"Export package created (no exe yet): {report.ExportDirectory}";
            if (report.Warnings.Count > 0)
                ExportStatus += $" | {report.Warnings[0]}";
            StatusText = ExportStatus;
        }

        private void _seedFallbackBlockPaletteIfNeeded()
        {
            if (AllBlocks.Count > 0) return;
            AllBlocks.Add(new BlockViewModel(new BlockInfo { Name = "grass", R = 80, G = 170, B = 80 }));
            AllBlocks.Add(new BlockViewModel(new BlockInfo { Name = "stone", R = 130, G = 130, B = 130 }));
            AllBlocks.Add(new BlockViewModel(new BlockInfo { Name = "dirt", R = 120, G = 78, B = 45 }));
            AllBlocks.Add(new BlockViewModel(new BlockInfo { Name = "wood", R = 156, G = 109, B = 65 }));
            RefreshBlockList();
            if (FilteredBlocks.Count > 0)
                SelectedBlock = FilteredBlocks[0];
        }

        private void _refreshBlockRegistry()
        {
            AllBlocks.Clear();
            if (_engine is null) return;
            foreach (var bi in _engine.GetAllBlocks())
                AllBlocks.Add(new BlockViewModel(bi));
            RefreshBlockList();
        }

        public void RefreshBlockList()
        {
            FilteredBlocks.Clear();
            string f = _blockFilter.ToLowerInvariant();
            foreach (var b in AllBlocks)
                if (string.IsNullOrEmpty(f) ||
                    b.Name.Contains(f, StringComparison.OrdinalIgnoreCase))
                    FilteredBlocks.Add(b);
        }

        private void _refreshInventory()
        {
            InventoryItems.Clear();
            if (_engine is null) return;
            foreach (var b in _engine.GetAllBlocks())
            {
                int c = _engine.ItemCount(b.Name);
                if (c > 0)
                    InventoryItems.Add(new InventoryItem(b.Name, c));
            }
        }

        private void _onEngineEvent(string name, string json)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _luaLog.AppendLine($"[{name}]");
                LuaOutput = _luaLog.ToString();
                if (name is "on_place" or "on_destroy") _refreshInventory();
            });
        }

        private void _onSceneCellPainted(ScenePaintOp op)
        {
            if (!_engineReady || _engine is null || !_engine.IsValid) return;
            try
            {
                if (op.BlockName is null)
                    _engine.DestroyVoxel(op.X, SceneEditor.LayerY, op.Z);
                else
                    _engine.PlaceVoxel(op.X, SceneEditor.LayerY, op.Z, op.BlockName);
            }
            catch (Exception ex)
            {
                StatusText = $"Scene runtime sync error: {ex.Message}";
            }
        }

        private void _syncSceneToEngine()
        {
            if (!_engineReady || _engine is null || !_engine.IsValid) return;
            try
            {
                for (int z = 0; z < SceneEditor.Rows; z++)
                for (int x = 0; x < SceneEditor.Columns; x++)
                {
                    var cell = SceneEditor.GetCell(x, z);
                    if (cell.IsEmpty)
                        _engine.DestroyVoxel(x, SceneEditor.LayerY, z);
                    else
                        _engine.PlaceVoxel(x, SceneEditor.LayerY, z, cell.BlockName);
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Scene sync failed: {ex.Message}";
            }
        }

        private void _startAssetWatcher()
        {
            if (_assetTicker is not null) return;
            _assetFingerprint = _computeAssetFingerprint();
            _assetTicker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _assetTicker.Tick += (_, _) =>
            {
                if (_assetPipelineBusy) return;
                long fp = _computeAssetFingerprint();
                if (fp == _assetFingerprint) return;
                _processAssetsNow();
            };
            _assetTicker.Start();
        }

        private long _computeAssetFingerprint()
        {
            try
            {
                string raw = ProjectFolders.AssetsRaw(ProjectRootPath);
                if (!Directory.Exists(raw)) return 0;
                long acc = 17;
                foreach (var file in Directory.EnumerateFiles(raw, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext is not ".png" and not ".wav" and not ".fbx") continue;
                    var fi = new FileInfo(file);
                    unchecked
                    {
                        acc = (acc * 31) + fi.Length;
                        acc = (acc * 31) + fi.LastWriteTimeUtc.Ticks;
                        acc = (acc * 31) + fi.FullName.GetHashCode();
                    }
                }
                return acc;
            }
            catch
            {
                return 0;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Set<T>(ref T field, T value, [CallerMemberName] string prop = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string prop = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        
        public void Dispose()
        {
            _ticker?.Stop();
            _assetTicker?.Stop();
            _engineReady = false;
            try { _engine?.Quit(); } catch { /* ignore */ }
            try { _engine?.Dispose(); } catch { /* ignore */ }
        }
    }

    // Sub-ViewModels

    public class BlockViewModel
    {
        public string  Name     { get; }
        public string  Hardness { get; } = "1.0"; 
        public IBrush  ColorBrush { get; }
        public byte    R, G, B;

        public BlockViewModel(BlockInfo bi)
        {
            Name      = bi.Name;
            R = bi.R; G = bi.G; B = bi.B;
            ColorBrush = new SolidColorBrush(new Color(255, R, G, B));
        }
    }

    public class InventoryItem
    {
        public string Name      { get; }
        public string CountText { get; }

        public InventoryItem(string name, int count)
        {
            Name      = name;
            CountText = count.ToString();
        }
    }

    public sealed class StatsViewModel : INotifyPropertyChanged
    {
        private string _chunksDrawnText = "0";
        private string _drawCallsText   = "0";
        private string _trianglesText   = "0";
        private float  _frameMs;

        public string ChunksDrawnText { get => _chunksDrawnText; set => Set(ref _chunksDrawnText, value); }
        public string DrawCallsText   { get => _drawCallsText;   set => Set(ref _drawCallsText,   value); }
        public string TrianglesText   { get => _trianglesText;   set => Set(ref _trianglesText,   value); }

        public float FrameMs
        {
            get => _frameMs;
            set
            {
                Set(ref _frameMs, value);
                OnPropertyChanged(nameof(FrameMsFormatted));
                OnPropertyChanged(nameof(FpsFormatted));
            }
        }

        public string FrameMsFormatted => $"{_frameMs:F1} ms";
        public string FpsFormatted     => _frameMs > 0 ? $"{1000f / _frameMs:F0} fps" : "â€“";

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Set<T>(ref T f, T v, [CallerMemberName] string p = "")
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p)); return true;
        }
        private void OnPropertyChanged([CallerMemberName] string p = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action _action;
        public RelayCommand(Action action) => _action = action;
        public bool CanExecute(object? _)  => true;
        public void Execute(object? _)     => _action.Invoke();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
