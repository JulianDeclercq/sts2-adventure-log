using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AdventureLog.AdventureLogCode.UI;

public sealed class CreatureHighlighter
{
    private readonly Node _treeAnchor;
    private readonly List<NCreature> _highlighted = new();

    public CreatureHighlighter(Node treeAnchor) => _treeAnchor = treeAnchor;

    public void Highlight(uint? combatId)
    {
        if (combatId is null) return;

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom is null) return;

        foreach (var creatureNode in combatRoom.CreatureNodes)
        {
            if (creatureNode.Entity?.CombatId == combatId)
            {
                // Already selected by the game (e.g. while aiming a card): leave it alone,
                // so Clear() doesn't hide the game's own reticle.
                if (creatureNode._selectionReticle?.IsSelected == true) return;
                _highlighted.Add(creatureNode);
                creatureNode.ShowSingleSelectReticle();
                return;
            }
        }
    }

    public void Clear()
    {
        foreach (var creatureNode in _highlighted)
        {
            if (GodotObject.IsInstanceValid(creatureNode))
                creatureNode.HideSingleSelectReticle();
        }
        _highlighted.Clear();
    }
}
