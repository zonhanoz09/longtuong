using System.IO;
using System.Text.Json;

namespace LongTuong;

/// <summary>Pure C# local save/load (JSON). No Godot dependency, so it is unit-testable.
/// Phase 2.38: writes are atomic (temp file, then replace) and keep the
/// previous good file as "{path}.bak". A corrupt file falls back to the
/// backup; if both are unreadable the corrupt file is kept as
/// "{path}.corrupt" and an InvalidDataException is thrown instead of
/// silently returning an empty save that would overwrite the player's data.</summary>
public static class SaveManager
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void Save(string path, SaveData data) =>
        WriteAtomic(path, JsonSerializer.Serialize(data, Options));

    public static SaveData Load(string path)
    {
        if (!File.Exists(path) && !File.Exists(path + ".bak")) return new SaveData();
        return ReadWithBackup<SaveData>(path) ?? new SaveData();
    }

    public static void WriteAtomic(string path, string content)
    {
        var full = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = full + ".tmp";
        File.WriteAllText(tmp, content);
        if (File.Exists(full)) File.Replace(tmp, full, full + ".bak");
        else File.Move(tmp, full);
    }

    /// Reads JSON from path, falling back to path.bak. Returns default
    /// only when neither file exists; throws when files exist but none parse.
    public static T? ReadWithBackup<T>(string path)
    {
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try { return JsonSerializer.Deserialize<T>(File.ReadAllText(candidate), Options); }
            catch (JsonException) { }
        }
        if (!File.Exists(path) && !File.Exists(path + ".bak")) return default;
        if (File.Exists(path)) File.Copy(path, path + ".corrupt", true);
        throw new InvalidDataException($"File dữ liệu bị hỏng: {path} (đã giữ bản {path}.corrupt)");
    }
}
