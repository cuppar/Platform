using Godot;
using Platform.classes;
using Platform.worlds;

namespace Platform.globals;

public partial class Game : CanvasLayer
{
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


        tree.ChangeSceneToFile(scenePath);
        await ToSignal(tree, SceneTree.SignalName.TreeChanged);

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