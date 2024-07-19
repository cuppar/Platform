using Godot;
using Platform.classes;
using Platform.globals;

namespace Platform.objects;

public partial class SaveStone : Interactable
{
    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public AnimationPlayer AnimationPlayer = null!;

    #endregion

    public override void Interact()
    {
        base.Interact();
        AnimationPlayer.Play("activated");
        AutoloadManager.Game.Save();
    }
}