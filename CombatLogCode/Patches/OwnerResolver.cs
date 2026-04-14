using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace CombatLog.CombatLogCode.Patches;

public static class OwnerResolver
{
    public static void Resolve(ulong? netId, out string name, out bool isLocal)
    {
        name = "";
        isLocal = true;
        if (netId is null) return;

        try
        {
            var netService = RunManager.Instance?.NetService;
            if (netService is null) return;

            var resolved = PlatformUtil.GetPlayerName(netService.Platform, netId.Value);
            if (!string.IsNullOrEmpty(resolved) && !ulong.TryParse(resolved, out _))
                name = resolved;

            try
            {
                var localNetId = Traverse.Create(netService).Property("LocalPlayer").Field("NetId").GetValue<ulong>();
                isLocal = localNetId == netId.Value;
            }
            catch
            {
                // LocalPlayer not resolvable — keep default (treat as local to match solo behavior)
            }
        }
        catch { }
    }
}
