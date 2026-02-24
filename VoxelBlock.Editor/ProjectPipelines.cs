using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VoxelBlock.Editor
{
    public static class ProjectFolders
    {
        public static string Scenes(string root) => Path.Combine(root, "Scenes");
        public static string AssetsRaw(string root) => Path.Combine(root, "Assets", "Raw");
        public static string AssetsProcessed(string root) => Path.Combine(root, "Assets", "Processed");
        public static string Exports(string root) => Path.Combine(root, "Exports");

        public static void EnsureAll(string root)
        {
            Directory.CreateDirectory(Scenes(root));
            Directory.CreateDirectory(AssetsRaw(root));
            Directory.CreateDirectory(AssetsProcessed(root));
            Directory.CreateDirectory(Exports(root));
        }
    }

    public sealed class AssetPipelineService
    {
        public AssetPipelineReport Process(string projectRoot)
        {
            ProjectFolders.EnsureAll(projectRoot);

            var rawRoot = ProjectFolders.AssetsRaw(projectRoot);
            var outRoot = ProjectFolders.AssetsProcessed(projectRoot);
            var report = new AssetPipelineReport();
            var manifest = new AssetManifest();

            foreach (var file in Directory.EnumerateFiles(rawRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".png" or ".wav" or ".fbx"))
                    continue;

                try
                {
                    var kind = Classify(ext);
                    var rel = Path.GetRelativePath(rawRoot, file);
                    var subdir = kind switch
                    {
                        AssetKind.Texture => "textures",
                        AssetKind.Audio => "audio",
                        AssetKind.Model => "models",
                        _ => "misc"
                    };

                    Validate(file, kind);

                    var outDir = Path.Combine(outRoot, subdir, Path.GetDirectoryName(rel) ?? "");
                    Directory.CreateDirectory(outDir);
                    var outPath = Path.Combine(outDir, Path.GetFileName(file));

                    bool changed = !File.Exists(outPath) || File.GetLastWriteTimeUtc(outPath) < File.GetLastWriteTimeUtc(file);
                    File.Copy(file, outPath, true);

                    var record = new AssetManifestItem
                    {
                        Source = Path.GetRelativePath(projectRoot, file),
                        Output = Path.GetRelativePath(projectRoot, outPath),
                        Kind = kind.ToString().ToLowerInvariant(),
                        Extension = ext,
                        ByteSize = new FileInfo(file).Length,
                        Sha256 = ComputeSha256(file),
                        ImportedUtc = DateTime.UtcNow,
                        Status = ext == ".fbx" ? "staged" : "ready",
                        Notes = ext == ".fbx" ? "FBX staged. Runtime conversion/importer backend still required for mesh optimization." : "",
                    };
                    manifest.Items.Add(record);
                    report.Items.Add(new AssetPipelineItemView(record.Source, record.Kind, record.Status, record.Output));
                    if (changed) report.ImportedCount++;
                    else report.SkippedCount++;
                }
                catch (Exception ex)
                {
                    report.ErrorCount++;
                    report.Errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }

            var manifestPath = Path.Combine(outRoot, "asset_manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, SceneJson.Options));
            report.ManifestPath = manifestPath;
            return report;
        }

        private static AssetKind Classify(string ext) => ext switch
        {
            ".png" => AssetKind.Texture,
            ".wav" => AssetKind.Audio,
            ".fbx" => AssetKind.Model,
            _ => AssetKind.Unknown
        };

        private static void Validate(string path, AssetKind kind)
        {
            using var fs = File.OpenRead(path);
            switch (kind)
            {
                case AssetKind.Texture:
                    Span<byte> png = stackalloc byte[8];
                    if (fs.Read(png) != 8) throw new InvalidDataException("PNG file too short.");
                    byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
                    for (int i = 0; i < 8; i++)
                        if (png[i] != sig[i]) throw new InvalidDataException("Invalid PNG signature.");
                    break;

                case AssetKind.Audio:
                    Span<byte> wav = stackalloc byte[12];
                    if (fs.Read(wav) != 12) throw new InvalidDataException("WAV file too short.");
                    if (Encoding.ASCII.GetString(wav.Slice(0, 4)) != "RIFF" ||
                        Encoding.ASCII.GetString(wav.Slice(8, 4)) != "WAVE")
                        throw new InvalidDataException("Invalid WAV header.");
                    break;

                case AssetKind.Model:
                    Span<byte> hdr = stackalloc byte[27];
                    int n = fs.Read(hdr);
                    if (n < 16) throw new InvalidDataException("FBX file too short.");
                    var prefix = Encoding.ASCII.GetString(hdr.Slice(0, n).ToArray());
                    if (!prefix.Contains("FBX", StringComparison.OrdinalIgnoreCase) &&
                        !prefix.StartsWith("Kaydara FBX Binary", StringComparison.Ordinal))
                        throw new InvalidDataException("Unrecognized FBX header.");
                    break;
            }
        }

        private static string ComputeSha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(fs));
        }
    }

    public sealed class StandaloneExportService
    {
        public ExportReport Export(string projectRoot, string exportName)
        {
            ProjectFolders.EnsureAll(projectRoot);

            var safeName = string.Concat((exportName ?? "Build").Where(ch => !Path.GetInvalidFileNameChars().Contains(ch))).Trim();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Build";

            var outDir = Path.Combine(ProjectFolders.Exports(projectRoot), safeName);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var report = new ExportReport { ExportDirectory = outDir };

            CopyIfExists(ProjectFolders.Scenes(projectRoot), Path.Combine(outDir, "Scenes"), report);
            CopyIfExists(ProjectFolders.AssetsProcessed(projectRoot), Path.Combine(outDir, "Assets"), report);
            CopyIfExists(Path.Combine(projectRoot, "mods"), Path.Combine(outDir, "mods"), report);

            string? exe = FindCandidateExe(projectRoot);
            if (exe is not null)
            {
                var exeOut = Path.Combine(outDir, Path.GetFileName(exe));
                File.Copy(exe, exeOut, true);
                report.ExecutablePath = exeOut;
                report.HasPlayableExe = true;
            }
            else
            {
                report.HasPlayableExe = false;
                report.Warnings.Add("No game executable found. Build native game target first, then export again.");
            }

            var runtimeDll = Directory.EnumerateFiles(projectRoot, "voxelblock*.dll", SearchOption.AllDirectories)
                .FirstOrDefault(p => !p.Contains("\\Exports\\", StringComparison.OrdinalIgnoreCase));
            if (runtimeDll is not null)
            {
                File.Copy(runtimeDll, Path.Combine(outDir, Path.GetFileName(runtimeDll)), true);
                report.CopiedFiles++;
            }

            File.WriteAllText(Path.Combine(outDir, "export_manifest.json"),
                JsonSerializer.Serialize(report, SceneJson.Options));

            if (!report.HasPlayableExe)
            {
                File.WriteAllText(Path.Combine(outDir, "README-EXPORT.txt"),
                    "Export package created, but no playable .exe was found.\r\n" +
                    "Build voxelblock_game first, then run Export again.\r\n");
            }

            return report;
        }

        private static void CopyIfExists(string src, string dst, ExportReport report)
        {
            if (!Directory.Exists(src)) return;
            DirectoryCopy(src, dst, true, report);
        }

        private static void DirectoryCopy(string src, string dst, bool recursive, ExportReport report)
        {
            var dir = new DirectoryInfo(src);
            if (!dir.Exists) return;
            Directory.CreateDirectory(dst);
            foreach (var file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(dst, file.Name), true);
                report.CopiedFiles++;
            }

            if (!recursive) return;
            foreach (var sub in dir.GetDirectories())
                DirectoryCopy(sub.FullName, Path.Combine(dst, sub.Name), true, report);
        }

        private static string? FindCandidateExe(string projectRoot)
        {
            string[] preferred =
            {
                Path.Combine(projectRoot, "build", "voxelblock_game.exe"),
                Path.Combine(projectRoot, "build-native", "voxelblock_game.exe"),
                Path.Combine(projectRoot, "bin", "voxelblock_game.exe"),
            };
            foreach (var p in preferred)
                if (File.Exists(p)) return p;

            return Directory.EnumerateFiles(projectRoot, "*.exe", SearchOption.AllDirectories)
                .Where(p => p.Contains("voxelblock", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Contains("game", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Contains("\\Exports\\", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }
    }

    public enum AssetKind
    {
        Unknown,
        Texture,
        Audio,
        Model,
    }

    public sealed class AssetPipelineReport
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public string ManifestPath { get; set; } = "";
        public List<string> Errors { get; } = new();
        public List<AssetPipelineItemView> Items { get; } = new();
    }

    public sealed record AssetPipelineItemView(string Source, string Kind, string Status, string Output);

    public sealed class AssetManifest
    {
        public List<AssetManifestItem> Items { get; set; } = new();
    }

    public sealed class AssetManifestItem
    {
        public string Source { get; set; } = "";
        public string Output { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Extension { get; set; } = "";
        public long ByteSize { get; set; }
        public string Sha256 { get; set; } = "";
        public DateTime ImportedUtc { get; set; }
        public string Status { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public sealed class ExportReport
    {
        public string ExportDirectory { get; set; } = "";
        public bool HasPlayableExe { get; set; }
        public string? ExecutablePath { get; set; }
        public int CopiedFiles { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
