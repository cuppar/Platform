using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Platform.worlds;

public partial class World : Node2D
{
    private Status _currentStatus = new();

    public Status CurrentStatus
    {
        get
        {
            _currentStatus.AliveEnemies.Clear();
            foreach (var enemy in GetTree().GetNodesInGroup("enemies"))
            {
                var path = GetPathTo(enemy);
                _currentStatus.AliveEnemies.Add(path);
            }

            return _currentStatus;
        }
        set
        {
            _currentStatus = value;
            foreach (var enemy in GetTree().GetNodesInGroup("enemies"))
            {
                var path = GetPathTo(enemy);
                if (!_currentStatus.AliveEnemies.Contains(path))
                    enemy.QueueFree();
            }
        }
    }

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
        Player.FallFromHeight = position.Y;
        Player.Direction = entryPointDirection;
        Camera.ResetSmoothing();
        Camera.ForceUpdateScroll();
    }

    #region Nested type: Status

    public record Status
    {
        public List<string> AliveEnemies = new();

        public override string ToString()
        {
            return $"Status(AliveEnemies=[{string.Join(", ", AliveEnemies.Select(x => x))}])";
        }
    }

    #endregion


    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Camera2D Camera = null!;

    [Export] public TileMap TileMap = null!;
    [Export] public Player Player = null!;

    #endregion
}