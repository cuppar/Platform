using Godot;
using Platform.globals;

namespace Platform.ui;

public partial class PauseScreen : Control
{
    public override void _Ready()
    {
        base._Ready();
        AutoloadManager.SoundManager.SetupUISounds(this);
        Hide();
        VisibilityChanged += () => GetTree().Paused = Visible;
        Resume.Pressed += OnResume;
        Quit.Pressed += OnQuit;
    }

    public void ShowPauseScreen()
    {
        Show();
        Resume.GrabFocus();
    }

    private void OnResume()
    {
        Hide();
    }

    private void OnQuit()
    {
        AutoloadManager.Game.BackToTitle();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (!@event.IsActionPressed("pause")) return;
        Hide();
        GetWindow().SetInputAsHandled();
    }

    #region Child

    [ExportGroup("ChildDontChange")]
    [Export]
    public Button Resume { get; set; } = null!;

    [Export] public Button Quit { get; set; } = null!;

    #endregion
}