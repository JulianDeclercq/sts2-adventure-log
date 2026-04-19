using Godot;

namespace AdventureLog.AdventureLogCode.UI;

/// <summary>
/// Thin invisible strip on one edge or corner of <see cref="AdventureLogPanel"/>. Drag to resize.
/// Panel uses absolute positioning (anchors all 0), so each edge maps directly to one offset.
/// Corner handles combine the relevant edge actions.
/// </summary>
public partial class PanelEdgeHandle : Control
{
    public enum Edge
    {
        Left, Right, Top, Bottom,
        TopLeft, TopRight, BottomLeft, BottomRight,
    }

    public const float Thickness = 8f;
    public const float MinWidth = 250f;
    public const float MinHeight = 200f;

    public Edge Kind { get; init; }

    private AdventureLogPanel _panel = null!;
    private bool _dragging;
    private Vector2 _startMouse;
    private float _startOffsetLeft;
    private float _startOffsetRight;
    private float _startOffsetTop;
    private float _startOffsetBottom;

    public override void _Ready()
    {
        _panel = GetParent<AdventureLogPanel>();
        switch (Kind)
        {
            case Edge.Left:
                AnchorLeft = 0; AnchorRight = 0; AnchorTop = 0; AnchorBottom = 1;
                OffsetLeft = 0; OffsetRight = Thickness;
                OffsetTop = Thickness; OffsetBottom = -Thickness;
                MouseDefaultCursorShape = CursorShape.Hsize;
                break;
            case Edge.Right:
                AnchorLeft = 1; AnchorRight = 1; AnchorTop = 0; AnchorBottom = 1;
                OffsetLeft = -Thickness; OffsetRight = 0;
                OffsetTop = Thickness; OffsetBottom = -Thickness;
                MouseDefaultCursorShape = CursorShape.Hsize;
                break;
            case Edge.Top:
                AnchorLeft = 0; AnchorRight = 1; AnchorTop = 0; AnchorBottom = 0;
                OffsetLeft = Thickness; OffsetRight = -Thickness;
                OffsetTop = 0; OffsetBottom = Thickness;
                MouseDefaultCursorShape = CursorShape.Vsize;
                break;
            case Edge.Bottom:
                AnchorLeft = 0; AnchorRight = 1; AnchorTop = 1; AnchorBottom = 1;
                OffsetLeft = Thickness; OffsetRight = -Thickness;
                OffsetTop = -Thickness; OffsetBottom = 0;
                MouseDefaultCursorShape = CursorShape.Vsize;
                break;
            case Edge.TopLeft:
                AnchorLeft = 0; AnchorRight = 0; AnchorTop = 0; AnchorBottom = 0;
                OffsetLeft = 0; OffsetRight = Thickness;
                OffsetTop = 0; OffsetBottom = Thickness;
                MouseDefaultCursorShape = CursorShape.Fdiagsize;
                break;
            case Edge.TopRight:
                AnchorLeft = 1; AnchorRight = 1; AnchorTop = 0; AnchorBottom = 0;
                OffsetLeft = -Thickness; OffsetRight = 0;
                OffsetTop = 0; OffsetBottom = Thickness;
                MouseDefaultCursorShape = CursorShape.Bdiagsize;
                break;
            case Edge.BottomLeft:
                AnchorLeft = 0; AnchorRight = 0; AnchorTop = 1; AnchorBottom = 1;
                OffsetLeft = 0; OffsetRight = Thickness;
                OffsetTop = -Thickness; OffsetBottom = 0;
                MouseDefaultCursorShape = CursorShape.Bdiagsize;
                break;
            case Edge.BottomRight:
                AnchorLeft = 1; AnchorRight = 1; AnchorTop = 1; AnchorBottom = 1;
                OffsetLeft = -Thickness; OffsetRight = 0;
                OffsetTop = -Thickness; OffsetBottom = 0;
                MouseDefaultCursorShape = CursorShape.Fdiagsize;
                break;
        }
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _GuiInput(InputEvent ev)
    {
        switch (ev)
        {
            case InputEventMouseButton mb when mb.ButtonIndex == MouseButton.Left:
                if (mb.Pressed)
                {
                    _dragging = true;
                    _startMouse = GetGlobalMousePosition();
                    _startOffsetLeft = _panel.OffsetLeft;
                    _startOffsetRight = _panel.OffsetRight;
                    _startOffsetTop = _panel.OffsetTop;
                    _startOffsetBottom = _panel.OffsetBottom;
                }
                else
                {
                    if (_dragging) _panel.SavePosition();
                    _dragging = false;
                }
                AcceptEvent();
                break;

            case InputEventMouseMotion when _dragging:
                Apply(GetGlobalMousePosition());
                AcceptEvent();
                break;
        }
    }

    private void Apply(Vector2 mouse)
    {
        var dx = mouse.X - _startMouse.X;
        var dy = mouse.Y - _startMouse.Y;

        if (Kind is Edge.Left or Edge.TopLeft or Edge.BottomLeft)
        {
            var newLeft = _startOffsetLeft + dx;
            if (_panel.OffsetRight - newLeft < MinWidth)
                newLeft = _panel.OffsetRight - MinWidth;
            _panel.OffsetLeft = newLeft;
        }
        if (Kind is Edge.Right or Edge.TopRight or Edge.BottomRight)
        {
            var newRight = _startOffsetRight + dx;
            if (newRight - _panel.OffsetLeft < MinWidth)
                newRight = _panel.OffsetLeft + MinWidth;
            _panel.OffsetRight = newRight;
        }
        if (Kind is Edge.Top or Edge.TopLeft or Edge.TopRight)
        {
            var newTop = _startOffsetTop + dy;
            if (_panel.OffsetBottom - newTop < MinHeight)
                newTop = _panel.OffsetBottom - MinHeight;
            _panel.OffsetTop = newTop;
        }
        if (Kind is Edge.Bottom or Edge.BottomLeft or Edge.BottomRight)
        {
            var newBottom = _startOffsetBottom + dy;
            if (newBottom - _panel.OffsetTop < MinHeight)
                newBottom = _panel.OffsetTop + MinHeight;
            _panel.OffsetBottom = newBottom;
        }
    }
}
