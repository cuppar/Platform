using System;
using Godot;
using Platform.classes;

namespace Platform;

public partial class Player : CharacterBody2D, IStateMachine<Player.State>
{
    private float _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private Player()
    {
        FloorAcceleration = RunSpeed / 0.2f;
        AirAcceleration = RunSpeed / 0.02f;
        var stateMachine = new StateMachine<State>();
        stateMachine.Name = "StateMachine";
        AddChild(stateMachine);
        stateMachine.Owner = this;
    }

    [Export] public float RunSpeed { get; set; } = 200;
    [Export] public float JumpVelocity { get; set; } = -300;
    [Export] public float FloorAcceleration { get; set; }
    [Export] public float AirAcceleration { get; set; }

    #region IStateMachine Members

    public void TransitionState(State fromState, State toState)
    {
    }

    public State GetNextState(State currentState)
    {
        var direction = Input.GetAxis("move_left", "move_right");
        var isStill = Mathf.IsZeroApprox(direction) && Mathf.IsZeroApprox(Velocity.X);

        switch (currentState)
        {
            case State.Idle:
                if (!isStill)
                    return State.Running;
                break;
            case State.Running:
                if (isStill)
                    return (int)State.Idle;
                break;
            case State.Jump:
                break;
            case State.Fall:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }

        return currentState;
    }

    public void TickPhysics(State currentState, double delta)
    {
    }

    #endregion


    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("jump"))
            _jumpRequestTimer.Start();

        if (@event.IsActionReleased("jump"))
        {
            _jumpRequestTimer.Stop();
            if (Velocity.Y < JumpVelocity / 2)
                Velocity = Velocity with { Y = JumpVelocity / 2 };
        }
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
        else if (Velocity.Y < 0)
        {
            _animationPlayer.Play("jump");
        }
        else
        {
            _animationPlayer.Play("fall");
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

    #region Nested type: State

    public enum State
    {
        Idle,
        Running,
        Jump,
        Fall
    }

    #endregion

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private Sprite2D _sprite = null!;

    [Export] private AnimationPlayer _animationPlayer = null!;
    [Export] private Timer _coyoteTimer = null!;
    [Export] private Timer _jumpRequestTimer = null!;

    #endregion
}