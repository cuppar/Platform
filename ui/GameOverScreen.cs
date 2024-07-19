using Godot;
using Platform.globals;

namespace Platform.ui;

public partial class GameOverScreen : Control
{
    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public AnimationPlayer AnimationPlayer = null!;

    #endregion

    public override void _Ready()
    {
        base._Ready();
        Hide();
        SetProcessInput(false);
    }

    public void ShowGameOver()
    {
        Show();
        SetProcessInput(true);
        AnimationPlayer.Play("enter");
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        GetWindow().SetInputAsHandled();

        if (AnimationPlayer.IsPlaying()) return;
        if (@event is not (InputEventKey or InputEventMouseButton or InputEventJoypadButton)) return;
        if (!@event.IsPressed() || @event.IsEcho()) return;

        if (AutoloadManager.Game.HasSaveFile())
            AutoloadManager.Game.Load();
        else
            AutoloadManager.Game.BackToTitle();
    }
}