using Godot;
using Platform.classes;

namespace Platform.globals;

public partial class Game : Node
{
    public async void ChangeScene(string scenePath, string entryPointName)
    {
        var tree = GetTree();
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
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Stats PlayerStats = null!;

    #endregion
}