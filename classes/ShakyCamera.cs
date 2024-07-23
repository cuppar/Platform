using Godot;
using Platform.globals;

namespace Platform.classes;

public partial class ShakyCamera : Camera2D
{
    public float Strength { get; set; }
    [Export] public float RecoverySpeed { get; set; } = 16;

    public override void _Ready()
    {
        base._Ready();
        AutoloadManager.Game.CameraShouldShaky += OnCameraShouldShake;
    }

    private void OnCameraShouldShake(float amount)
    {
        Strength += amount;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        Offset = new Vector2(
            (float)GD.RandRange(-Strength, Strength),
            (float)GD.RandRange(-Strength, Strength)
        );
        Strength = Mathf.MoveToward(Strength, 0, RecoverySpeed * (float)delta);
    }
}