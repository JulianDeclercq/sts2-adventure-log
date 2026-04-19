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

        var combatRoom = FindCombatRoom();
        if (combatRoom is null) return;

        foreach (var creatureNode in combatRoom.CreatureNodes)
        {
            if (creatureNode.Entity?.CombatId == combatId)
            {
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

    private NCombatRoom? FindCombatRoom()
    {
        var root = _treeAnchor.GetTree()?.Root;
        if (root is null) return null;

        foreach (var node in root.FindChildren("*", recursive: true, owned: false))
        {
            if (node is NCombatRoom room) return room;
        }
        return null;
    }
}
