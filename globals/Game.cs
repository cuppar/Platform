using System.Collections.Generic;
using Godot;
using Platform.classes;
using Platform.worlds;

namespace Platform.globals;

public partial class Game : CanvasLayer
{
    private readonly Dictionary<string, World.Status> _worldStatusMap = new();

    public override void _Ready()
    {
        ColorRect.Color = ColorRect.Color with { A = 0 };
    }

    public async void ChangeScene(string scenePath, string entryPointName)
    {
        var tree = GetTree();

        tree.Paused = true;
        var tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(ColorRect, "color:a", 1, 0.2);
        await ToSignal(tween, Tween.SignalName.Finished);

        var world = (World)tree.CurrentScene;
        var oldWorldName = world.SceneFilePath.GetBaseName().GetFile();
        _worldStatusMap[oldWorldName] = world.CurrentStatus;

        tree.ChangeSceneToFile(scenePath);
        await ToSignal(tree, SceneTree.SignalName.TreeChanged);

        world = (World)tree.CurrentScene;
        var newWorldName = world.SceneFilePath.GetFile().GetBaseName();
        if (_worldStatusMap.TryGetValue(newWorldName, out var newWorldStatus))
            world.CurrentStatus = newWorldStatus;

        foreach (var node in tree.GetNodesInGroup("entry_points"))
        {
            if (node.Name != entryPointName)
                continue;
            var entryPoint = (EntryPoint)node;
            ((World)tree.CurrentScene).UpdatePlayer(entryPoint.GlobalPosition, entryPoint.Direction);
            break;
        }

        tween = CreateTween();
        tween.TweenProperty(ColorRect, "color:a", 0, 0.2);
        tree.Paused = false;
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Stats PlayerStats = null!;

    [Export] public ColorRect ColorRect = null!;

    #endregion
}