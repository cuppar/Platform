using Godot;

namespace Platform;

public partial class Player : CharacterBody2D
{
    private float _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private Player()
    {
        FloorAcceleration = RunSpeed / 0.2f;
        AirAcceleration = RunSpeed / 0.02f;
    }

    [Export] public float RunSpeed { get; set; } = 200;
    [Export] public float JumpVelocity { get; set; } = -300;
    [Export] public float FloorAcceleration { get; set; }
    [Export] public float AirAcceleration { get; set; }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("jump"))
            _jumpRequestTimer.Start();
        if (@event.IsActionReleased("jump") && Velocity.Y < JumpVelocity / 3)
            Velocity = Velocity with { Y = JumpVelocity / 3 };
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetAxis("move_left", "move_right");
        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        Velocity = Velocity with
        {
            Y = Velocity.Y + _gravity * (float)delta,
            X = Mathf.MoveToward(Velocity.X, direction * RunSpeed, acceleration * (float)delta)
        };

        var canJump = IsOnFloor() || _coyoteTimer.TimeLeft > 0;
        var shouldJump = canJump && _jumpRequestTimer.TimeLeft > 0;

        if (shouldJump)
        {
            Velocity = Velocity with { Y = JumpVelocity };
            _coyoteTimer.Stop();
            _jumpRequestTimer.Stop();
        }

        if (IsOnFloor())
        {
            if (Mathf.IsZeroApprox(direction) && Mathf.IsZeroApprox(Velocity.X))
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

        var wasOnFloor = IsOnFloor();
        MoveAndSlide();
        if (IsOnFloor() != wasOnFloor)
        {
            if (!IsOnFloor() && !shouldJump)
                _coyoteTimer.Start();
            else
                _coyoteTimer.Stop();
        }
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private Sprite2D _sprite;

    [Export] private AnimationPlayer _animationPlayer;
    [Export] private Timer _coyoteTimer;
    [Export] private Timer _jumpRequestTimer;

    #endregion
}