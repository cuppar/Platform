using Godot;

namespace Platform.classes;

[GlobalClass]
public partial class EntryPoint : Marker2D
{
    [Export] public Player.DirectionEnum Direction = Player.DirectionEnum.Right;

    public override void _Ready()
    {
        AddToGroup("entry_points");
    }
}