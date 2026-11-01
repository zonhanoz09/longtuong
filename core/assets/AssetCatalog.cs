using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

/// <summary>Convention-based PNG loading. Loose files do not need Godot import.</summary>
public static class AssetCatalog
{
    private sealed record Entry(long Modified, long Length, Texture2D? Texture);
    private static readonly Dictionary<string, Entry> Cache = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Warnings = new(StringComparer.Ordinal);
    private static string? _testDirectory;

    public static string LooseDirectory => _testDirectory ?? (OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath("res://assets")
        : Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "assets"));

    public static Texture2D? Portrait(string id) => LoadCombatant(id, "portrait.png");
    public static Texture2D? Body(string id) => LoadCombatant(id, "body.png");

    private static Texture2D? LoadCombatant(string id, string file)
    {
        // Resolve known IDs only; display names and filesystem paths are never IDs.
        var group = HeroDatabase.Heroes.Any(h => h.Id == id) ? "heroes"
            : EnemyDatabase.Find(id) != null ? "enemies" : null;
        if (group == null) return null;
        var relative = $"{group}/{id}/{file}";
        var loose = LoadLoose(Path.Combine(LooseDirectory, group, id, file));
        if (loose != null) return loose;
        // Packed textures use Godot's importer/remap handling, not System.IO.
        // In the editor, deleted source PNGs must not resurrect from stale .import files.
        if (_testDirectory != null || OS.HasFeature("editor")) return null;
        string resource = "res://assets/" + relative;
        return ResourceLoader.Exists(resource) ? ResourceLoader.Load<Texture2D>(resource) : null;
    }

    private static Texture2D? LoadLoose(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) { Cache.Remove(path); return null; }
            long modified = info.LastWriteTimeUtc.Ticks, length = info.Length;
            Cache.TryGetValue(path, out var old);
            if (old != null && old.Modified == modified && old.Length == length) return old.Texture;
            Texture2D? texture = null;
            // Validate PNG canvas before decoding. Export art should normally be 512².
            if (length <= 32 * 1024 * 1024 && length >= 33)
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length >= 33 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                    && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
                {
                    uint w = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
                    uint h = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
                    if (w is > 0 and <= 4096 && h is > 0 and <= 4096)
                    {
                        using var image = new Image();
                        if (image.LoadPngFromBuffer(bytes) == Error.Ok && !image.IsEmpty())
                            texture = ImageTexture.CreateFromImage(image);
                    }
                }
            }
            if (texture == null)
                WarnOnce(path + modified + length, $"Asset PNG không hợp lệ (tối đa 4096×4096, 32 MB): {path}");
            // A broken replacement keeps the most recent good image for this process.
            texture ??= old?.Texture;
            Cache[path] = new Entry(modified, length, texture);
            return texture;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WarnOnce(path + ex.GetType().Name, $"Không đọc được asset {path}: {ex.Message}");
            return Cache.TryGetValue(path, out var old) ? old.Texture : null;
        }
    }

    private static void WarnOnce(string key, string message)
    {
        if (Warnings.Add(key)) GD.PushWarning(message);
    }

    // Tests generate temporary PNGs; never overwrite an artist's real files.
    internal static void UseTestDirectory(string? directory)
    {
        if (!GameManager.IsTestRun) throw new InvalidOperationException("Test asset root is only available in test scenes.");
        Cache.Clear();
        Warnings.Clear();
        _testDirectory = directory;
    }
}
