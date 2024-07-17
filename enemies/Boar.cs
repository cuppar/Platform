using System;
using System.Diagnostics;
using Godot;
using Platform.classes;

namespace Platform.enemies;

public partial class Boar : Enemy, IStateMachine<Boar.State>
{
    #region State enum

    public enum State
    {
        Idle,
        Run,
        Walk,
        Hurt,
        Dying
    }

    #endregion

    private Damage? _pendingDamage;

    [Export] public float KnockBackAmount = 512;

    public Boar()
    {
        _stateMachine = StateMachine<State>.Create(this);
    }

    #region IStateMachine<State> Members

    public void TransitionState(State fromState, State toState)
    {
        // GD.Print($"[{nameof(Boar)}][{Engine.GetPhysicsFrames()}] {fromState} => {toState}");
        switch (toState)
        {
            case State.Idle:
                AnimationPlayer.Play("idle");
                if (_wallChecker.IsColliding())
                    Direction = (DirectionEnum)((int)Direction * -1);
                break;
            case State.Walk:
                AnimationPlayer.Play("walk");
                if (!_floorChecker.IsColliding())
                {
                    Direction = (DirectionEnum)((int)Direction * -1);
                    _floorChecker.ForceRaycastUpdate();
                }

                break;
            case State.Run:
                AnimationPlayer.Play("run");
                break;
            case State.Hurt:
                AnimationPlayer.Play("hit");
                Debug.Assert(_pendingDamage != null);

                Stats.Health -= _pendingDamage.Amount;
                var hitDirection = _pendingDamage.Source.GlobalPosition.DirectionTo(GlobalPosition);

                Velocity = hitDirection * KnockBackAmount;
                Direction = hitDirection.X > 0 ? DirectionEnum.Left : DirectionEnum.Right;

                _pendingDamage = null;

                break;
            case State.Dying:
                AnimationPlayer.Play("die");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(toState), toState, null);
        }
    }

    public State GetNextState(State currentState, out bool keepCurrent)
    {
        keepCurrent = false;
        if (Stats.Health <= 0)
        {
            if (currentState == State.Dying)
                keepCurrent = true;
            return State.Dying;
        }

        if (_pendingDamage != null)
            return State.Hurt;

        switch (currentState)
        {
            case State.Idle:
                if (CanSeePlayer())
                    return State.Run;
                if (_stateMachine.StateTime > 2)
                    return State.Walk;
                break;
            case State.Walk:
                if (CanSeePlayer())
                    return State.Run;
                if (_wallChecker.IsColliding() || !_floorChecker.IsColliding())
                    return State.Idle;
                break;
            case State.Run:
                if (!CanSeePlayer() && _calmDownTimer.IsStopped())
                    return State.Walk;
                break;
            case State.Hurt:
                if (!AnimationPlayer.IsPlaying())
                    return State.Run;
                break;
            case State.Dying:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }

        keepCurrent = true;
        return currentState;
    }

    public void TickPhysics(State currentState, double delta)
    {
        switch (currentState)
        {
            case State.Idle or State.Hurt or State.Dying:
                Move(0, delta);
                break;
            case State.Walk:
                Move(MaxSpeed / 3, delta);
                break;
            case State.Run:
                if (_wallChecker.IsColliding() || !_floorChecker.IsColliding())
                    Direction = (DirectionEnum)((int)Direction * -1);
                Move(MaxSpeed, delta);
                if (CanSeePlayer())
                    _calmDownTimer.Start();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }
    }

    #endregion

    private void OnHurt(HitBox hitBox)
    {
        _pendingDamage = new Damage((Node2D)hitBox.Owner, 1);
    }


    private bool CanSeePlayer()
    {
        if (!_playerChecker.IsColliding())
            return false;
        return _playerChecker.GetCollider() is Player;
    }

    public override void _Ready()
    {
        base._Ready();
        HurtBox.Hurt += OnHurt;
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private RayCast2D _wallChecker = null!;

    [Export] private RayCast2D _playerChecker = null!;
    [Export] private RayCast2D _floorChecker = null!;
    private StateMachine<State> _stateMachine;
    [Export] private Timer _calmDownTimer = null!;

    #endregion
}