using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace CombatLog.CombatLogCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "CombatLog";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        GD.Print($"[{ModId}] Combat Log mod loaded.");
    }
}
