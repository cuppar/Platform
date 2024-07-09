using Godot;

namespace Platform;

public partial class World : Node2D
{
    public override void _Ready()
    {
        var used = _tileMap.GetUsedRect().Grow(-1);
        var tileSize = _tileMap.TileSet.TileSize;
        _camera.LimitTop = used.Position.Y * tileSize.Y;
        _camera.LimitBottom = used.End.Y * tileSize.Y;
        _camera.LimitLeft = used.Position.X * tileSize.X;
        _camera.LimitRight = used.End.X * tileSize.X;
        _camera.ResetSmoothing();
    }


    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private Camera2D _camera = null!;

    [Export] private TileMap _tileMap = null!;

    #endregion
}