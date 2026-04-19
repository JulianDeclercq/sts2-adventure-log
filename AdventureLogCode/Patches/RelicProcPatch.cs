using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Patches RelicModel.Flash(IEnumerable&lt;Creature&gt;) — fired whenever a relic activates.
/// The parameterless Flash() overload delegates into this one, so a single patch catches both.
/// </summary>
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.Flash), typeof(IEnumerable<Creature>))]
public static class RelicProcPatch
{
    [HarmonyPostfix]
    public static void Postfix(RelicModel __instance, IEnumerable<Creature> __0)
    {
        try
        {
            var relicName = __instance.Title?.GetFormattedText() ?? __instance.GetType().Name;
            var relicId = __instance.Id?.Entry ?? __instance.GetType().Name;
            var ownerNetId = __instance.Owner?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            var ownerCreatureId = __instance.Owner?.Creature?.CombatId;
            var targetNames = new List<string>();
            var targetIds = new List<uint?>();
            if (__0 is not null)
            {
                foreach (var c in __0)
                {
                    if (c is null) continue;
                    // Drop self-targets (e.g. parameterless Flash() targets Owner.Creature).
                    if (ownerCreatureId.HasValue && c.CombatId == ownerCreatureId) continue;
                    targetNames.Add(c.Name);
                    targetIds.Add(c.CombatId);
                }
            }

            AdventureLogTracker.RecordRelicProc(
                relicName, relicId, __instance,
                targetNames, targetIds,
                ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording relic proc: {e.Message}");
        }
    }
}
