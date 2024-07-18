using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using Platform.classes;
using Platform.globals;

namespace Platform;

public partial class Player : CharacterBody2D, IStateMachine<Player.State>
{
    #region State enum

    public enum State
    {
        NonGroundIdle,
        Idle,
        Running,
        Jump,
        Fall,
        Landing,
        WallSliding,
        WallJump,
        Attack1,
        Attack2,
        Attack3,
        Hurt,
        Dying,
        SlidingStart,
        SlidingLoop,
        SlidingEnd
    }

    #endregion

    private const double SlideLoopDuration = 0.3;

    private readonly State[] _groundStates =
    {
        State.Idle, State.Running, State.Landing,
        State.Attack1, State.Attack2, State.Attack3
    };

    private float _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private bool _isComboRequested;

    private bool _isFirstTick;

    private Damage? _pendingDamage;

    [Export] public bool CanCombo;

    private Interactable[] _interactWith = Array.Empty<Interactable>();

    [Export] public float SlidingEnergy = 4;

    private Stats _stats = null!;


    private Player()
    {
        FloorAcceleration = RunSpeed / 0.2f;
        AirAcceleration = RunSpeed / 0.1f;

        _stateMachine = StateMachine<State>.Create(this);
    }

    public float FallFromHeight { get; set; }

    [Export] public float RunSpeed { get; set; } = 200;
    [Export] public float JumpVelocity { get; set; } = -300;
    [Export] public Vector2 WallJumpVelocity { get; set; } = new(500, -300);
    [Export] public float FloorAcceleration { get; set; }
    [Export] public float AirAcceleration { get; set; }
    [Export] public float KnockBackAmount { get; set; } = 512;
    [Export] public float SlideSpeed { get; set; } = 300;
    [Export] public float LandingHeight { get; set; } = 100;

    #region IStateMachine<State> Members

    public void TransitionState(State fromState, State toState)
    {
        // GD.Print($"[{nameof(Player)}][{Engine.GetPhysicsFrames()}] {fromState} => {toState}");

        if (!_groundStates.Contains(fromState) && _groundStates.Contains(toState))
            CoyoteTimer.Stop();

        switch (toState)
        {
            case State.Idle or State.NonGroundIdle:
                AnimationPlayer.Play("idle");
                break;
            case State.Running:
                AnimationPlayer.Play("running");
                break;
            case State.Jump:
                AnimationPlayer.Play("jump");
                Velocity = Velocity with { Y = JumpVelocity };
                CoyoteTimer.Stop();
                JumpRequestTimer.Stop();
                break;
            case State.Fall:
                AnimationPlayer.Play("fall");
                if (_groundStates.Contains(fromState))
                    CoyoteTimer.Start();
                FallFromHeight = GlobalPosition.Y;
                break;
            case State.Landing:
                AnimationPlayer.Play("landing");
                break;
            case State.WallSliding:
                AnimationPlayer.Play("wall_sliding");
                break;
            case State.WallJump:
                AnimationPlayer.Play("jump");
                Velocity = WallJumpVelocity with { X = WallJumpVelocity.X * GetWallNormal().X };
                JumpRequestTimer.Stop();
                break;
            case State.Attack1:
                AnimationPlayer.Play("attack_1");
                _isComboRequested = false;
                break;
            case State.Attack2:
                AnimationPlayer.Play("attack_2");
                _isComboRequested = false;
                break;
            case State.Attack3:
                AnimationPlayer.Play("attack_3");
                _isComboRequested = false;
                break;
            case State.Hurt:
                AnimationPlayer.Play("hurt");
                Debug.Assert(_pendingDamage != null);

                _stats.Health -= _pendingDamage.Amount;
                var hitDirection = _pendingDamage.Source.GlobalPosition.DirectionTo(GlobalPosition);

                Velocity = hitDirection * KnockBackAmount;

                _pendingDamage = null;
                InvincibleTimer.Start();

                break;
            case State.Dying:
                InvincibleTimer.Stop();
                AnimationPlayer.Play("die");
                Array.Clear(_interactWith);
                break;
            case State.SlidingStart:
                AnimationPlayer.Play("sliding_start");
                SlideRequestTimer.Stop();
                _stats.Energy -= SlidingEnergy;
                break;
            case State.SlidingLoop:
                AnimationPlayer.Play("sliding_loop");
                break;
            case State.SlidingEnd:
                AnimationPlayer.Play("sliding_end");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(toState), toState, null);
        }

        _isFirstTick = true;
    }

    public State GetNextState(State currentState, out bool keepCurrent)
    {
        keepCurrent = false;
        if (_stats.Health <= 0)
        {
            if (currentState == State.Dying)
                keepCurrent = true;
            return State.Dying;
        }

        if (_pendingDamage != null)
            return State.Hurt;

        var canJump = IsOnFloor() || CoyoteTimer.TimeLeft > 0;
        var shouldJump = canJump && JumpRequestTimer.TimeLeft > 0;

        if (shouldJump)
            return State.Jump;

        if (_groundStates.Contains(currentState) && !IsOnFloor())
            return State.Fall;


        var movement = Input.GetAxis("move_left", "move_right");
        var isStill = Mathf.IsZeroApprox(movement) && Mathf.IsZeroApprox(Velocity.X);

        switch (currentState)
        {
            case State.NonGroundIdle:
                return IsOnFloor() ? State.Idle : State.Fall;
            case State.Idle:
                if (Input.IsActionJustPressed("attack"))
                    return State.Attack1;
                if (ShouldSlide())
                    return State.SlidingStart;
                if (!isStill)
                    return State.Running;
                break;
            case State.Running:
                if (Input.IsActionJustPressed("attack"))
                    return State.Attack1;
                if (ShouldSlide())
                    return State.SlidingStart;
                if (isStill)
                    return State.Idle;
                break;
            case State.Jump:
                if (Velocity.Y >= 0)
                    return State.Fall;
                break;
            case State.Fall:
                if (IsOnFloor())
                {
                    var height = GlobalPosition.Y - FallFromHeight;
                    return height >= LandingHeight ? State.Landing : State.Running;
                }

                if (CanWallSlide())
                    return State.WallSliding;
                break;
            case State.Landing:
                if (!AnimationPlayer.IsPlaying())
                    return State.Idle;
                break;
            case State.WallSliding:
                if (IsOnFloor())
                    return State.Idle;
                if (!IsOnWall())
                    return State.Fall;
                if (JumpRequestTimer.TimeLeft > 0 && !_isFirstTick)
                    return State.WallJump;
                break;
            case State.WallJump:
                if (CanWallSlide() && !_isFirstTick)
                    return State.WallSliding;
                if (Velocity.Y >= 0)
                    return State.Fall;
                break;
            case State.Attack1:
                if (!AnimationPlayer.IsPlaying())
                    return _isComboRequested ? State.Attack2 : State.Idle;
                break;
            case State.Attack2:
                if (!AnimationPlayer.IsPlaying())
                    return _isComboRequested ? State.Attack3 : State.Idle;
                break;
            case State.Attack3:
                if (!AnimationPlayer.IsPlaying())
                    return State.Idle;
                break;
            case State.Hurt:
                if (!AnimationPlayer.IsPlaying())
                    return IsOnFloor() ? State.Idle : State.NonGroundIdle;
                break;
            case State.Dying:
                break;
            case State.SlidingStart:
                if (!AnimationPlayer.IsPlaying())
                    return State.SlidingLoop;
                break;
            case State.SlidingLoop:
                if (_stateMachine.StateTime > SlideLoopDuration || IsOnWall())
                    return State.SlidingEnd;
                break;
            case State.SlidingEnd:
                if (!AnimationPlayer.IsPlaying())
                    return State.NonGroundIdle;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }

        keepCurrent = true;
        return currentState;
    }

    public void TickPhysics(State currentState, double delta)
    {
        InteractIcon.Visible = _interactWith.Length != 0;

        if (InvincibleTimer.TimeLeft > 0)
            Graphics.Modulate = Graphics.Modulate with
            {
                A = (float)(Math.Sin(
                    (InvincibleTimer.WaitTime - InvincibleTimer.TimeLeft) / InvincibleTimer.WaitTime
                    * (2 * Math.PI)
                    * 10 // 闪烁10次
                ) * 0.5 + 0.5)
            };
        else
            Graphics.Modulate = Graphics.Modulate with { A = 1 };

        switch (currentState)
        {
            case State.Idle or State.NonGroundIdle:
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
                Direction = GetWallNormal().X > 0 ? DirectionEnum.Right : DirectionEnum.Left;
                break;
            case State.WallJump:
                if (_stateMachine.StateTime < 0.1)
                    Stand(_isFirstTick ? 0 : _gravity, delta);
                else
                    Move(_gravity, delta);

                break;
            case State.Attack1 or State.Attack2 or State.Attack3:
                Stand(_gravity, delta);
                break;
            case State.Hurt or State.Dying:
                Stand(_gravity, delta);
                break;
            case State.SlidingStart or State.SlidingLoop:
                Slide(_gravity, delta);
                break;
            case State.SlidingEnd:
                Stand(_gravity, delta);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }

        _isFirstTick = false;
    }

    #endregion

    private void Slide(float gravity, double delta)
    {
        Velocity = Velocity with
        {
            X = (float)Direction * SlideSpeed,
            Y = Velocity.Y + gravity * (float)delta
        };
        MoveAndSlide();
    }

    private bool ShouldSlide()
    {
        if (SlideRequestTimer.IsStopped())
            return false;
        if (_stats.Energy < SlidingEnergy)
            return false;
        return !FootChecker.IsColliding();
    }

    private bool CanWallSlide()
    {
        return IsOnWall() && HandChecker.IsColliding() && FootChecker.IsColliding();
    }


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
        var movement = Input.GetAxis("move_left", "move_right");
        var acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;
        Velocity = Velocity with
        {
            Y = Velocity.Y + gravity * (float)delta,
            X = Mathf.MoveToward(Velocity.X, movement * RunSpeed, acceleration * (float)delta)
        };

        if (!Mathf.IsZeroApprox(movement))
            Direction = movement > 0 ? DirectionEnum.Right : DirectionEnum.Left;

        MoveAndSlide();
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("jump"))
            JumpRequestTimer.Start();

        if (@event.IsActionReleased("jump"))
        {
            JumpRequestTimer.Stop();
            if (Velocity.Y < JumpVelocity / 2)
                Velocity = Velocity with { Y = JumpVelocity / 2 };
        }

        if (@event.IsActionPressed("attack") && CanCombo)
            _isComboRequested = true;

        if (@event.IsActionPressed("slide"))
            SlideRequestTimer.Start();

        if (@event.IsActionPressed("interact") && _interactWith.Length != 0)
            _interactWith.Last().Interact();
    }

    public override void _Ready()
    {
        _stats = AutoloadManager.Game.PlayerStats;
        HurtBox.Hurt += OnHurt;
        Stand(_gravity, 0.01);
    }

    public void RegisterInteractable(Interactable interactable)
    {
        if (_stateMachine.CurrentState == State.Dying)
            return;
        if (_interactWith.Contains(interactable))
            return;
        _interactWith = _interactWith.Append(interactable).ToArray();
    }

    public void UnregisterInteractable(Interactable interactable)
    {
        _interactWith = _interactWith.Where(v => v != interactable).ToArray();
    }

    private void OnHurt(HitBox hitBox)
    {
        if (InvincibleTimer.TimeLeft > 0)
            return;

        _pendingDamage = new Damage((Node2D)hitBox.Owner, 1);
    }

    private void Die()
    {
        GetTree().ReloadCurrentScene();
        _stats.Reset();
    }

    #region Direction

    public enum DirectionEnum
    {
        Left = -1,
        Right = +1
    }

    private DirectionEnum _direction = DirectionEnum.Right;

    [Export]
    public DirectionEnum Direction
    {
        get => _direction;
        set => SetDirection(value);
    }

    private async void SetDirection(DirectionEnum value)
    {
        if (_direction == value)
            return;

        if (!IsNodeReady())
            await ToSignal(this, Node.SignalName.Ready);

        _direction = value;
        Graphics.Scale = Graphics.Scale with { X = value == DirectionEnum.Right ? +1 : -1 };
    }

    #endregion

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public Node2D Graphics = null!;

    private StateMachine<State> _stateMachine;
    [Export] public AnimationPlayer AnimationPlayer = null!;
    [Export] public Timer CoyoteTimer = null!;
    [Export] public Timer JumpRequestTimer = null!;
    [Export] public RayCast2D HandChecker = null!;
    [Export] public RayCast2D FootChecker = null!;
    [Export] public HurtBox HurtBox = null!;
    [Export] public Timer InvincibleTimer = null!;
    [Export] public AnimatedSprite2D InteractIcon = null!;
    [Export] public Timer SlideRequestTimer = null!;

    #endregion
}