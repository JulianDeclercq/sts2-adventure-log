#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace AdventureLog.AdventureLogCode.Patches;

// Dev-only: skips the studio logos on launch. Mutates game startup, which technically
// violates affects_gameplay: false — so it must not ship in Release builds.
[HarmonyPatch(typeof(NGame), nameof(NGame.LaunchMainMenu))]
public static class SkipSplashPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref bool skipLogo) => skipLogo = true;
}
#endif
