using Godot;

namespace Platform;

public partial class Player : CharacterBody2D
{
    private float _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
    [Export] public float RunSpeed { get; set; } = 200;
    [Export] public float JumpVelocity { get; set; } = -300;

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetAxis("move_left", "move_right");
        Velocity = Velocity with { Y = Velocity.Y + _gravity * (float)delta, X = direction * RunSpeed };
        if (IsOnFloor() && Input.IsActionPressed("jump")) Velocity = Velocity with { Y = JumpVelocity };
        if (IsOnFloor())
        {
            if (Mathf.IsZeroApprox(direction))
                _animationPlayer.Play("idle");
            else
                _animationPlayer.Play("running");
        }
        else
        {
            _animationPlayer.Play("jump");
        }

        if (!Mathf.IsZeroApprox(direction))
            _sprite.FlipH = direction < 0;

        MoveAndSlide();
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private Sprite2D _sprite;

    [Export] private AnimationPlayer _animationPlayer;

    #endregion
}