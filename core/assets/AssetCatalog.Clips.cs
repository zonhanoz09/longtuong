using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LongTuong;

public static partial class AssetCatalog
{
    private sealed record ClipEntry(string Signature, FrameClip? Clip);
    private static readonly Dictionary<string, ClipEntry> ClipCache = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Poses = new() { "idle", "attack", "ultimate", "hit", "death", "victory" };
    private static readonly HashSet<string> Effects = new() { "normal", "ultimate", "projectile", "cast", "aura" };

    public static FrameClip? Clip(string id, string name) => Poses.Contains(name) ? LoadClip(id, "animations", name) : null;
    public static FrameClip? Effect(string id, string name) => Effects.Contains(name) ? LoadClip(id, "effects", name) : null;

    private static string? CombatantDirectory(string id) => HeroDatabase.Heroes.Any(h => h.Id == id) ? "heroes"
        : EnemyDatabase.Find(id) != null ? "enemies" : null;

    private static FrameClip? LoadClip(string id, string type, string name)
    {
        string? group = CombatantDirectory(id);
        if (group == null) return null;
        return LoadDirectory(Path.Combine(LooseDirectory, group, id), type, name);
    }

    public static FrameClip? SharedEffect(string name) => name is "hit" or "heal" or "projectile"
        ? LoadDirectory(Path.Combine(LooseDirectory, "vfx"), "shared", name) : null;

    private static FrameClip? LoadDirectory(string heroDir, string type, string name)
    {
        string directory = Path.Combine(heroDir, type, name);
        ClipCache.TryGetValue(directory, out var previous);
        try
        {
            var files = Directory.Exists(directory) ? Directory.GetFiles(directory)
                .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
            if (files.Length == 0) { ClipCache.Remove(directory); return null; }
            string config = Path.Combine(heroDir, "animation.json");
            string signature = string.Join('|', files.Select(p => { var f = new FileInfo(p); return $"{f.Name}:{f.Length}:{f.LastWriteTimeUtc.Ticks}"; }))
                + (File.Exists(config) ? File.GetLastWriteTimeUtc(config).Ticks + ":" + new FileInfo(config).Length : "");
            if (previous?.Signature == signature) return previous.Clip;
            FrameClip? loaded = null;
            var sheet = SheetOptions(config, type, name);
            // An artist can replace a shipped atlas by simply dropping numbered PNGs.
            if (files.Any(p => Path.GetFileName(p)=="000.png"))
            {
                files=files.Where(p=>Path.GetFileName(p)!=(sheet?.File??"atlas.png")).ToArray();
                sheet=null;
            }
            if (sheet != null)
            {
                string sheetPath = Path.Combine(directory, sheet.File);
                if (files.Length == 1 && files[0] == sheetPath)
                {
                    var size = ReadCanvas(sheetPath);
                    if (size.X >= sheet.Columns && size.Y >= sheet.Rows && (long)size.X * size.Y <= 16 * 1024 * 1024)
                    {
                        var texture = LoadLoose(sheetPath, keepLastGood: false);
                        if (texture != null)
                        {
                            float width = (float)size.X / sheet.Columns, height = (float)size.Y / sheet.Rows;
                            var regions = sheet.Regions ?? Enumerable.Range(0, sheet.Count)
                                .Select(i => new Rect2(i % sheet.Columns * width, i / sheet.Columns * height, width, height)).ToArray();
                            bool validRegions = regions.All(r => r.Position.X >= 0 && r.Position.Y >= 0 && r.Size.X > 0 && r.Size.Y > 0
                                && r.End.X <= size.X && r.End.Y <= size.Y
                                && (sheet.Canvas == null || (r.Size.X <= sheet.Canvas.Value.X && r.Size.Y <= sheet.Canvas.Value.Y)));
                            if (sheet.Canvas != null)
                                validRegions &= regions.Select((r,i)=>(sheet.Offsets?[i]??Vector2.Zero)+r.Size)
                                    .All(end=>end.X<=sheet.Canvas.Value.X&&end.Y<=sheet.Canvas.Value.Y);
                            if (sheet.Polygons != null)
                                validRegions &= sheet.Polygons.Select((polygon, i) => polygon == null || (polygon.All(p => p.X>=regions[i].Position.X && p.Y>=regions[i].Position.Y && p.X<=regions[i].End.X && p.Y<=regions[i].End.Y)
                                    && Geometry2D.TriangulatePolygon(polygon).Length>=3)).All(valid=>valid);
                            var frames = regions.Select((region, i) => (Texture2D)new AtlasTexture
                            {
                                Atlas = texture, Region = region, FilterClip = true,
                                Margin = sheet.Canvas == null ? default : new Rect2(sheet.Offsets?[i] ?? Vector2.Zero, sheet.Canvas.Value - region.Size),
                            });
                            var options = Options(config, type, name, sheet.Count);
                            if (validRegions && sheet.Count / options.Fps <= 10)
                                loaded = new FrameClip(frames, options.Fps, name is "idle" or "aura", options.Event,
                                    sheet.Pivots ?? Enumerable.Repeat(new Vector2(0.5f, type == "animations" ? 460f/512f : 0.5f), sheet.Count), sheet.Scale, sheet.Polygons);
                        }
                    }
                }
            }
            else if (files.Length <= 120 && files.Select((p, i) => Path.GetFileName(p) == $"{i:000}.png").All(v => v))
            {
                var sizes = files.Select(ReadCanvas).ToArray();
                bool canvasValid = sizes.All(size => size.X > 0 && size == sizes[0])
                    && sizes.Sum(size => (long)size.X * size.Y) <= 16 * 1024 * 1024;
                var frames = canvasValid ? files.Select(p => LoadLoose(p, keepLastGood: false)).ToArray() : Array.Empty<Texture2D?>();
                if (canvasValid && frames.All(f => f != null))
                {
                    var options = Options(config, type, name, files.Length);
                    if (files.Length / options.Fps <= 10)
                        loaded = new FrameClip(frames.Select(f => f!), options.Fps, name is "idle" or "aura", options.Event, type == "animations" ? null : Enumerable.Repeat(new Vector2(0.5f, 0.5f), files.Length));
                }
            }
            if (loaded == null) WarnOnce(directory + signature, $"Clip không hợp lệ: {directory}. Cần frame liên tục hoặc sprite sheet hợp lệ, cùng canvas, tối đa 120 frame/10 giây/16 triệu pixel.");
            loaded ??= previous?.Clip;
            ClipCache[directory] = new ClipEntry(signature, loaded);
            return loaded;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WarnOnce(directory + ex.Message, $"Không đọc được clip {directory}: {ex.Message}");
            return previous?.Clip;
        }
    }

    private sealed record SheetSpec(string File, int Columns, int Rows, int Count, Vector2[]? Pivots, Rect2[]? Regions, Vector2? Canvas, Vector2[]? Offsets, float Scale, Vector2[]?[]? Polygons);
    private static SheetSpec? SheetOptions(string path, string type, string name)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var doc = ReadConfig(path);
            string section = type == "animations" ? "clips" : "effects";
            if (doc == null || !doc.RootElement.TryGetProperty(section, out var entries) || !entries.TryGetProperty(name, out var entry)
                || !entry.TryGetProperty("sheet", out var file)) return null;
            string filename = file.GetString() ?? "";
            int columns = entry.GetProperty("columns").GetInt32(), rows = entry.GetProperty("rows").GetInt32();
            int count = entry.GetProperty("frames").GetInt32();
            if (Path.GetFileName(filename) != filename || filename.Contains('\\') || filename is "" or "." or ".." || !filename.EndsWith(".png", StringComparison.Ordinal)
                || columns < 1 || columns > 16 || rows < 1 || rows > 16 || count < 1 || count > 120 || count > columns * rows)
                throw new FormatException("sheet layout");
            Vector2[]? pivots = null;
            if (entry.TryGetProperty("pivots", out var points))
            {
                if (points.GetArrayLength() != count) throw new FormatException("pivots count");
                pivots = points.EnumerateArray().Select(p =>
                {
                    if (p.GetArrayLength() != 2) throw new FormatException("pivot");
                    var v = new Vector2(p[0].GetSingle(), p[1].GetSingle());
                    if (!v.IsFinite() || v.X < 0 || v.X > 1 || v.Y < 0 || v.Y > 1) throw new FormatException("pivot");
                    return v;
                }).ToArray();
            }
            Rect2[]? regions = null;
            if (entry.TryGetProperty("regions", out var boxes))
            {
                if (boxes.GetArrayLength() != count) throw new FormatException("regions count");
                regions = boxes.EnumerateArray().Select(box =>
                {
                    if (box.GetArrayLength() != 4) throw new FormatException("region");
                    var r = new Rect2(box[0].GetSingle(), box[1].GetSingle(), box[2].GetSingle(), box[3].GetSingle());
                    if (!r.Position.IsFinite() || !r.Size.IsFinite()) throw new FormatException("region");
                    return r;
                }).ToArray();
            }
            Vector2? canvas = null;
            Vector2[]? offsets = null;
            if (entry.TryGetProperty("canvas", out var canvasSize))
            {
                if (canvasSize.GetArrayLength() != 2) throw new FormatException("canvas");
                canvas = new Vector2(canvasSize[0].GetSingle(), canvasSize[1].GetSingle());
                if (!canvas.Value.IsFinite() || canvas.Value.X < 1 || canvas.Value.Y < 1 || canvas.Value.X > 4096 || canvas.Value.Y > 4096)
                    throw new FormatException("canvas");
                if (entry.TryGetProperty("offsets", out var pads))
                {
                    if (pads.GetArrayLength() != count) throw new FormatException("offsets count");
                    offsets = pads.EnumerateArray().Select(pad =>
                    {
                        if (pad.GetArrayLength() != 2) throw new FormatException("offset");
                        var p = new Vector2(pad[0].GetSingle(), pad[1].GetSingle());
                        if (!p.IsFinite() || p.X < 0 || p.Y < 0) throw new FormatException("offset");
                        return p;
                    }).ToArray();
                    if (regions != null && regions.Select((r, i) => offsets[i] + r.Size).Any(end => end.X > canvas.Value.X || end.Y > canvas.Value.Y))
                        throw new FormatException("offset bounds");
                }
            }
            float scale = entry.TryGetProperty("display_scale", out var displayScale) ? displayScale.GetSingle() : 1;
            if (!float.IsFinite(scale) || scale < 0.25f || scale > 2) throw new FormatException("display scale");
            Vector2[]?[]? polygons = null;
            if (entry.TryGetProperty("source_polygons", out var shapes))
            {
                if (shapes.GetArrayLength()!=count) throw new FormatException("source polygons count");
                polygons = shapes.EnumerateArray().Select(shape =>
                {
                    if (shape.ValueKind==JsonValueKind.Null) return null;
                    if (shape.GetArrayLength()<3 || shape.GetArrayLength()>64) throw new FormatException("source polygon");
                    return shape.EnumerateArray().Select(point =>
                    {
                        if(point.GetArrayLength()!=2) throw new FormatException("source polygon point");
                        var p=new Vector2(point[0].GetSingle(),point[1].GetSingle());
                        if(!p.IsFinite()) throw new FormatException("source polygon point");
                        return p;
                    }).ToArray();
                }).ToArray();
            }
            return new SheetSpec(filename, columns, rows, count, pivots, regions, canvas, offsets, scale, polygons);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or InvalidOperationException or OverflowException or KeyNotFoundException)
        {
            WarnOnce(path + FileStamp(path) + name + "sheet", $"Sprite sheet sai, dùng fallback: {path} ({name})");
            return null;
        }
    }

    private static Vector2I ReadCanvas(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        if (stream.ReadAtLeast(header, 24, throwOnEndOfStream: false) < 24
            || !header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || !header.Slice(12, 4).SequenceEqual("IHDR"u8)) return Vector2I.Zero;
        uint width = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(header.Slice(16, 4));
        uint height = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(header.Slice(20, 4));
        return width is > 0 and <= 4096 && height is > 0 and <= 4096 ? new Vector2I((int)width, (int)height) : Vector2I.Zero;
    }

    private static (double Fps, int Event) Options(string path, string type, string name, int count)
    {
        var fallback = (Fps: type == "animations" && name == "idle" ? 8.0 : 12.0, Event: count / 2);
        if (!File.Exists(path)) return fallback;
        try
        {
            using var json = ReadConfig(path);
            if (json == null) return fallback;
            string section = type == "animations" ? "clips" : "effects";
            if (!json.RootElement.TryGetProperty(section, out var clips) || !clips.TryGetProperty(name, out var clip)) return fallback;
            double fps = clip.TryGetProperty("fps", out var value) ? value.GetDouble() : fallback.Fps;
            int frame = clip.TryGetProperty("event_frame", out value) ? value.GetInt32() : fallback.Event;
            if (!double.IsFinite(fps) || fps < 1 || fps > 60 || frame < 0 || frame >= count) throw new FormatException("FPS/event_frame");
            return (fps, frame);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or OverflowException or IOException or UnauthorizedAccessException)
        {
            WarnOnce(path + FileStamp(path) + name, $"Cấu hình clip sai, dùng mặc định: {path} ({name})");
            return fallback;
        }
    }

    public static Vector2 Anchor(string id, string name)
    {
        Vector2 fallback = name == "cast" ? new Vector2(0.65f, 0.55f) : new Vector2(0.5f, 0.55f);
        string? group = CombatantDirectory(id);
        if (group == null || (name != "cast" && name != "hit")) return fallback;
        string path = Path.Combine(LooseDirectory, group, id, "animation.json");
        if (!File.Exists(path)) return fallback;
        try
        {
            using var json = ReadConfig(path);
            if (json == null || !json.RootElement.TryGetProperty("anchors", out var anchors) || !anchors.TryGetProperty(name, out var point)) return fallback;
            if (point.GetArrayLength() != 2) throw new FormatException("anchor");
            var result = new Vector2(point[0].GetSingle(), point[1].GetSingle());
            if (!result.IsFinite() || result.X < 0 || result.X > 1 || result.Y < 0 || result.Y > 1) throw new FormatException("anchor");
            return result;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or OverflowException or IOException or UnauthorizedAccessException)
        {
            WarnOnce(path + FileStamp(path) + name, $"Anchor sai, dùng mặc định: {path} ({name})");
            return fallback;
        }
    }

    private static string FileStamp(string path)
    {
        try { var f = new FileInfo(path); return $"{f.LastWriteTimeUtc.Ticks}:{f.Length}"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return "unreadable"; }
    }
    private static JsonDocument? ReadConfig(string path)
    {
        if (new FileInfo(path).Length > 65536) throw new FormatException("animation.json too large");
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        try
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("version", out var version) || version.GetInt32() != 1)
                throw new FormatException("animation.json version");
            return doc;
        }
        catch { doc.Dispose(); throw; }
    }
}
