using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Platform.classes;
using Platform.worlds;

namespace Platform.globals;

public partial class Game : CanvasLayer
{
    private const string SavePath = "user://data.sav";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    private Dictionary<string, World.Status> _worldStatusMap = new();

    public void Save()
    {
        var tree = GetTree();
        var world = (World)tree.CurrentScene;
        var worldName = world.SceneFilePath.GetBaseName().GetFile();
        _worldStatusMap[worldName] = world.CurrentStatus;

        var gameData = new GameData
        {
            WorldStatusMap = _worldStatusMap,
            CurrentScenePath = world.SceneFilePath,
            Player = new GameData.PlayerData
            {
                Direction = world.Player.Direction,
                GlobalPosition = world.Player.GlobalPosition,
                StatsData = PlayerStats.Save()
            }
        };


        var json = JsonSerializer.Serialize(gameData, _jsonSerializerOptions);

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            var err = FileAccess.GetOpenError();
            GD.Print($"Open {SavePath} failed: {err}");
            return;
        }

        GD.Print($"\n>>> Save json: {json}");
        file.StoreString(json);
    }

    public void Load()
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            var err = FileAccess.GetOpenError();
            GD.Print($"No save data found. {err}");
            return;
        }

        var json = file.GetAsText();
        GD.Print($"\n>>> Load json: {json}");
        var gameData = JsonSerializer.Deserialize<GameData>(json, _jsonSerializerOptions);
        GD.Print($"\n>>> GameData: {gameData}");
        GD.Print("");
        GD.Print("\n>>> WorldMap: " + string.Join(", ", gameData.WorldStatusMap.Select(p => $"{p.Key} => {p.Value}")));

        _worldStatusMap = gameData.WorldStatusMap;

        ChangeScene(gameData.CurrentScenePath, new ChangeSceneParams
        {
            Direction = gameData.Player.Direction,
            GlobalPosition = gameData.Player.GlobalPosition,
            IsSaveOldWorld = false,
            Init = () => { PlayerStats.Load(gameData.Player.StatsData); }
        });
    }

    public override void _Ready()
    {
        ColorRect.Color = ColorRect.Color with { A = 0 };
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event.IsActionPressed("ui_right"))
            Load();
        if (@event.IsActionPressed("ui_left"))
            Save();
    }

    public async void ChangeScene(string scenePath, ChangeSceneParams @params)
    {
        var tree = GetTree();

        // 暂停游戏
        tree.Paused = true;
        // 淡出当前场景
        var tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(ColorRect, "color:a", 1, 0.2);
        await ToSignal(tween, Tween.SignalName.Finished);


        var world = (World)tree.CurrentScene;
        // 保存旧世界状态
        if (@params.IsSaveOldWorld)
        {
            var oldWorldName = world.SceneFilePath.GetBaseName().GetFile();
            _worldStatusMap[oldWorldName] = world.CurrentStatus;
        }


        // 切换场景
        tree.ChangeSceneToFile(scenePath);
        // 加载新场景时的初始化
        @params.Init?.Invoke();
        await ToSignal(tree, SceneTree.SignalName.TreeChanged);


        // 恢复新世界状态
        world = (World)tree.CurrentScene;
        var newWorldName = world.SceneFilePath.GetFile().GetBaseName();
        if (_worldStatusMap.TryGetValue(newWorldName, out var newWorldStatus))
            world.CurrentStatus = newWorldStatus;

        // 恢复玩家位置和方向
        if (@params.EntryPointName != null)
            foreach (var node in tree.GetNodesInGroup("entry_points"))
            {
                if (node.Name != @params.EntryPointName)
                    continue;
                var entryPoint = (EntryPoint)node;
                world.UpdatePlayer(entryPoint.GlobalPosition, entryPoint.Direction);
                break;
            }

        if (@params is { Direction: not null, GlobalPosition: not null })
            world.UpdatePlayer((Vector2)@params.GlobalPosition, (Player.DirectionEnum)@params.Direction);

        // 淡入
        tween = CreateTween();
        tween.TweenProperty(ColorRect, "color:a", 0, 0.2);
        // 恢复游戏执行
        tree.Paused = false;
    }

    #region Nested type: ChangeSceneParams

    public struct ChangeSceneParams
    {
        public string? EntryPointName;
        public Vector2? GlobalPosition;
        public Player.DirectionEnum? Direction;
        public bool IsSaveOldWorld = true;
        public Action? Init;

        public ChangeSceneParams()
        {
            EntryPointName = null;
            GlobalPosition = null;
            Direction = null;
            Init = null;
        }
    }

    #endregion

    #region Nested type: GameData

    private record struct GameData()
    {
        public Dictionary<string, World.Status> WorldStatusMap { get; init; } = null!;
        public string CurrentScenePath { get; init; } = null!;
        public PlayerData Player { get; init; } = default;

        #region Nested type: PlayerData

        public record struct PlayerData()
        {
            public Player.DirectionEnum Direction { get; init; } = Platform.Player.DirectionEnum.Right;
            public Vector2 GlobalPosition { get; init; } = default;
            public Stats.Data StatsData { get; init; } = null!;
        }

        #endregion
    }

    #endregion

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Stats PlayerStats = null!;

    [Export] public ColorRect ColorRect = null!;

    #endregion
}