using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace AdventureLog.AdventureLogCode.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame.LaunchMainMenu))]
public static class SkipSplashPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref bool skipLogo) => skipLogo = true;
}
