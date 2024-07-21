using Godot;

namespace Platform.globals;

public partial class SoundManager : Node
{
    public void PlaySFX(string sfxName)
    {
        var player = SFX.GetNode<AudioStreamPlayer>(sfxName);
        player?.Play();
    }

    public void SetupUISounds(Node node)
    {
        if (node is Button button)
        {
            button.Pressed += () => PlaySFX("UIPress");
            button.FocusEntered += () => PlaySFX("UIFocus");
        }

        foreach (var child in node.GetChildren())
            SetupUISounds(child);
    }

    public void PlayBGM(AudioStream audioStream)
    {
        if (BGMPlayer.Stream == audioStream && BGMPlayer.Playing)
            return;
        BGMPlayer.Stream = audioStream;
        BGMPlayer.Play();
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Node SFX = null!;

    [Export] public AudioStreamPlayer BGMPlayer = null!;

    #endregion
}