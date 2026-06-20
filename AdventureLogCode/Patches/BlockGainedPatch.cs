using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace AdventureLog.AdventureLogCode.Patches;

/// <summary>
/// Logs block gained by a creature. Patches the History sink so the recorded
/// amount is post-Dexterity / post-Frail / post-relic-mod (e.g. Daughter of the
/// Wind +1) — generic across any source that bumps the value through the
/// BeforeBlockGained hook chain.
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.BlockGained))]
public static class BlockGainedPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState __0, Creature __1, int __2, ValueProp __3, CardPlay? __4)
    {
        try
        {
            var receiver = __1;
            var amount = __2;
            var sourcePlay = __4;
            if (receiver is null || amount <= 0) return;

            var ownerNetId = receiver.Player?.NetId;
            OwnerResolver.Resolve(ownerNetId, out var ownerName, out var isLocal);

            var sourceCardName = sourcePlay?.Card?.Title;

            AdventureLogTracker.RecordBlockGained(
                receiver.Name, receiver.CombatId, amount, sourceCardName,
                ownerNetId, ownerName, isLocal);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[AdventureLog] Error recording block gained: {e.Message}");
        }
    }
}
