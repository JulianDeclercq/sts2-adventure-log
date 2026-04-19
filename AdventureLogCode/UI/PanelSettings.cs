using System.Text.Json;
using Godot;

namespace AdventureLog.AdventureLogCode.UI;

internal static class PanelSettings
{
    private const string Dir = "user://AdventureLog";
    private const string Path = "user://AdventureLog/settings.json";

    public record Data(float OffsetLeft, float OffsetRight, float OffsetTop, float OffsetBottom);

    public static Data? Load()
    {
        if (!Godot.FileAccess.FileExists(Path)) return null;
        try
        {
            using var f = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Read);
            return f is null ? null : JsonSerializer.Deserialize<Data>(f.GetAsText());
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[AdventureLog] Load settings failed: {e.Message}");
            return null;
        }
    }

    public static void Save(Data data)
    {
        try
        {
            if (!DirAccess.DirExistsAbsolute(Dir))
                DirAccess.MakeDirRecursiveAbsolute(Dir);
            using var f = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);
            if (f is null) return;
            f.StoreString(JsonSerializer.Serialize(data));
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[AdventureLog] Save settings failed: {e.Message}");
        }
    }
}
