using Godot;

namespace Platform;

public partial class World : Node2D
{
    public override void _Ready()
    {
        var used = TileMap.GetUsedRect().Grow(-1);
        var tileSize = TileMap.TileSet.TileSize;
        Camera.LimitTop = used.Position.Y * tileSize.Y;
        Camera.LimitBottom = used.End.Y * tileSize.Y;
        Camera.LimitLeft = used.Position.X * tileSize.X;
        Camera.LimitRight = used.End.X * tileSize.X;
        Camera.ResetSmoothing();
    }


    public void UpdatePlayer(Vector2 position, Player.DirectionEnum entryPointDirection)
    {
        Player.GlobalPosition = position;
        Player.Direction = entryPointDirection;
        Camera.ResetSmoothing();
        Camera.ForceUpdateScroll();
    }


    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Camera2D Camera = null!;

    [Export] public TileMap TileMap = null!;
    [Export] public Player Player = null!;

    #endregion
}