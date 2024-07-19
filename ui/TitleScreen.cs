using Godot;
using Platform.globals;

namespace Platform.ui;

public partial class TitleScreen : Control
{
    public override void _Ready()
    {
        base._Ready();
        NewGameButton.GrabFocus();
        foreach (var node in ButtonContainer.GetChildren())
        {
            var button = (Button)node;
            button.MouseEntered += button.GrabFocus;
        }

        LoadGameButton.Disabled = !AutoloadManager.Game.HasSaveFile();
    }

    private void OnNewGame()
    {
        AutoloadManager.Game.NewGame();
    }

    private void OnLoadGame()
    {
        AutoloadManager.Game.Load();
    }

    private void OnExitGame()
    {
        GetTree().Quit();
    }

    #region Child

    [ExportGroup("ChildDontChange")]
    [Export]
    public Button NewGameButton { get; set; } = null!;

    [Export] public Button LoadGameButton { get; set; } = null!;

    [Export] public VBoxContainer ButtonContainer { get; set; } = null!;

    #endregion
}