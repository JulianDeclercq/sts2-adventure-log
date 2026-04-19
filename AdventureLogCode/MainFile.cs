using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AdventureLog.AdventureLogCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "AdventureLog";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        GD.Print($"[{ModId}] Adventure Log mod loaded.");
    }
}
