using Godot;
using Platform.globals;

namespace Platform.ui;

public partial class VolumeSlider : HSlider
{
    [Export] public SoundManager.Bus Bus { get; set; }

    public override void _Ready()
    {
        base._Ready();
        Value = SoundManager.GetVolume(Bus);
        ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        SoundManager.SetVolume(Bus, (float)value);
        Game.SaveConfig();
    }
}