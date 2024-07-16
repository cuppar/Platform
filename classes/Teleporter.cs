using Godot;
using Platform.globals;

namespace Platform.classes;

[GlobalClass]
public partial class Teleporter : Interactable
{
    [Export] public string EntryPointName = null!;
    [Export(PropertyHint.File, "*.tscn")] public string Path = null!;

    public override void Interact()
    {
        base.Interact();
        AutoloadManager.Game.ChangeScene(Path, EntryPointName);
    }
}