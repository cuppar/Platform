using Godot;
using Platform.globals;

namespace Platform.ui;

public partial class GameEndScreen : Control
{
    private int _currentLineIndex = -1;

    private string[] _lines =
    {
        "大魔王终于被打败了",
        "森林又恢复了往日的宁静",
        "但这一切值得吗？"
    };

    private Tween _tween = null!;

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Label Label = null!;

    #endregion

    private void ShowLine(int lineIndex)
    {
        _currentLineIndex = lineIndex;
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

        if (lineIndex > 0)
            _tween.TweenProperty(Label, "modulate:a", 0, 1);
        else
            Label.Modulate = Label.Modulate with { A = 0 };

        _tween.TweenCallback(Callable.From(() => Label.Text = _lines[_currentLineIndex]));
        _tween.TweenProperty(Label, "modulate:a", 1, 1);
    }


    public override void _Ready()
    {
        base._Ready();
        ShowLine(0);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (_tween.IsRunning()) return;
        if (@event is not (InputEventKey or InputEventMouseButton or InputEventJoypadButton)) return;
        if (!@event.IsPressed() || @event.IsEcho()) return;

        if (_currentLineIndex + 1 < _lines.Length)
            ShowLine(_currentLineIndex + 1);
        else
            AutoloadManager.Game.BackToTitle();
    }
}