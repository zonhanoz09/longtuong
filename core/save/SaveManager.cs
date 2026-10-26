using System.IO;
using System.Text.Json;

namespace LongTuong;

/// <summary>Pure C# local save/load (JSON). No Godot dependency, so it is unit-testable.</summary>
public static class SaveManager
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void Save(string path, SaveData data)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(data, Options));
    }

    public static SaveData Load(string path)
    {
        if (!File.Exists(path)) return new SaveData();
        return JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path), Options) ?? new SaveData();
    }
}
