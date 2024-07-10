using System;
using System.Linq;
using Godot;
using Platform.classes;

namespace Platform;

public partial class Player : CharacterBody2D, IStateMachine<Player.State>
{
    #region State enum

    public enum State
    {
        Idle,
        Running,
        Jump,
        Fall,
        Landing,
        WallSliding,
        WallJump
    }

    #endregion

    private readonly State[] _groundStates = { State.Idle, State.Running, State.Landing };
    private float _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private bool _isFirstTick;

    private Player()
    {
        FloorAcceleration = RunSpeed / 0.2f;
        AirAcceleration = RunSpeed / 0.1f;

        _stateMachine = StateMachine<State>.Create(this);
    }

    [Export] public float RunSpeed { get; set; } = 200;
    [Export] public float JumpVelocity { get; set; } = -300;
    [Export] public Vector2 WallJumpVelocity { get; set; } = new(500, -300);
    [Export] public float FloorAcceleration { get; set; }
    [Export] public float AirAcceleration { get; set; }

    #region IStateMachine<State> Members

    public void TransitionState(State fromState, State toState)
    {
        GD.Print($"[{Engine.GetPhysicsFrames()}] {fromState} => {toState}");
        if (!_groundStates.Contains(fromState) && _groundStates.Contains(toState))
            _coyoteTimer.Stop();

        switch (toState)
        {
            case State.Idle:
                _animationPlayer.Play("idle");
                break;
            case State.Running:
                _animationPlayer.Play("running");
                break;
            case State.Jump:
                _animationPlayer.Play("jump");
                Velocity = Velocity with { Y = JumpVelocity };
                _coyoteTimer.Stop();
                _jumpRequestTimer.Stop();
                break;
            case State.Fall:
                _animationPlayer.Play("fall");
                if (_groundStates.Contains(fromState))
                    _coyoteTimer.Start();
                break;
            case State.Landing:
                _animationPlayer.Play("landing");
                break;
            case State.WallSliding:
                _animationPlayer.Play("wall_sliding");
                break;
            case State.WallJump:
                _animationPlayer.Play("jump");
                Velocity = WallJumpVelocity with { X = WallJumpVelocity.X * GetWallNormal().X };
                _jumpRequestTimer.Stop();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(toState), toState, null);
        }

        _isFirstTick = true;
    }

    public State GetNextState(State currentState)
    {
        var canJump = IsOnFloor() || _coyoteTimer.TimeLeft > 0;
        var shouldJump = canJump && _jumpRequestTimer.TimeLeft > 0;

        if (shouldJump)
            return State.Jump;


        var direction = Input.GetAxis("move_left", "move_right");
        var isStill = Mathf.IsZeroApprox(direction) && Mathf.IsZeroApprox(Velocity.X);

        switch (currentState)
        {
            case State.Idle:
                if (!IsOnFloor())
                    return State.Fall;
                if (!isStill)
                    return State.Running;
                break;
            case State.Running:
                if (!IsOnFloor())
                    return State.Fall;
                if (isStill)
                    return State.Idle;
                break;
            case State.Jump:
                if (Velocity.Y >= 0)
                    return State.Fall;
                break;
            case State.Fall:
                if (IsOnFloor())
                    return isStill ? State.Landing : State.Running;
                if (CanWallSlide())
                    return State.WallSliding;
                break;
            case State.Landing:
                if (!isStill)
                    return State.Running;
                if (!_animationPlayer.IsPlaying())
                    return State.Idle;
                break;
            case State.WallSliding:
                if (IsOnFloor())
                    return State.Idle;
                if (!IsOnWall())
                    return State.Fall;
                if (_jumpRequestTimer.TimeLeft > 0 && !_isFirstTick)
                    return State.WallJump;
                break;
            case State.WallJump:
                if (CanWallSlide() && !_isFirstTick)
                    return State.WallSliding;
                if (Velocity.Y >= 0)
                    return State.Fall;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }

        return currentState;
    }

    public void TickPhysics(State currentState, double delta)
    {
        switch (currentState)
        {
            case State.Idle:
                Move(_gravity, delta);
                break;
            case State.Running:
                Move(_gravity, delta);
                break;
            case State.Jump:
                Move(_isFirstTick ? 0 : _gravity, delta);
                break;
            case State.Fall:
                Move(_gravity, delta);
                break;
            case State.Landing:
                Stand(_gravity, delta);
                break;
            case State.WallSliding:
                Move(_gravity / 3, delta);
                _graphics.Scale = _graphics.Scale with { X = GetWallNormal().X };
                break;
            case State.WallJump:
                if (_stateMachine.StateTime < 0.1)
                    Stand(_isFirstTick ? 0 : _gravity, delta);
                else
                    Move(_gravity, delta);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }

        _isFirstTick = false;
    }

    #endregion

    private bool CanWallSlide() => IsOnWall() && _handChecker.IsColliding() && _footChecker.IsColliding();


    private void Stand(float gravity, double delta)
    {
        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        Velocity = Velocity with
        {
            Y = Velocity.Y + gravity * (float)delta,
            X = Mathf.MoveToward(Velocity.X, 0, acceleration * (float)delta)
        };

        MoveAndSlide();
    }

    private void Move(float gravity, double delta)
    {
        var direction = Input.GetAxis("move_left", "move_right");
        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        Velocity = Velocity with
        {
            Y = Velocity.Y + gravity * (float)delta,
            X = Mathf.MoveToward(Velocity.X, direction * RunSpeed, acceleration * (float)delta)
        };

        if (!Mathf.IsZeroApprox(direction))
            _graphics.Scale = _graphics.Scale with
            {
                X = direction < 0 ? -1 : +1
            };

        MoveAndSlide();
    }


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

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private Node2D _graphics = null!;

    [Export] private AnimationPlayer _animationPlayer = null!;
    [Export] private Timer _coyoteTimer = null!;
    [Export] private Timer _jumpRequestTimer = null!;
    [Export] private RayCast2D _handChecker = null!;
    [Export] private RayCast2D _footChecker = null!;
    private StateMachine<State> _stateMachine;

    #endregion
}