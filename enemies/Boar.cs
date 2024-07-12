using System;
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
        Walk
    }

    #endregion

    public Boar()
    {
        _stateMachine = StateMachine<State>.Create(this);
    }

    #region IStateMachine<State> Members

    public void TransitionState(State fromState, State toState)
    {
        GD.Print($"[{nameof(Boar)}][{Engine.GetPhysicsFrames()}] {fromState} => {toState}");

        switch (toState)
        {
            case State.Idle:
                AnimationPlayer.Play("idle");
                if (_wallChecker.IsColliding())
                    Direction = (DirectionEnum)((int)Direction * -1);
                break;
            case State.Run:
                AnimationPlayer.Play("run");
                break;
            case State.Walk:
                AnimationPlayer.Play("walk");
                if (!_floorChecker.IsColliding())
                {
                    Direction = (DirectionEnum)((int)Direction * -1);
                    _floorChecker.ForceRaycastUpdate();
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(toState), toState, null);
        }
    }

    public State GetNextState(State currentState)
    {
        if (CanSeePlayer())
            return State.Run;

        switch (currentState)
        {
            case State.Idle:
                if (_stateMachine.StateTime > 2)
                    return State.Walk;
                break;
            case State.Run:
                if (_calmDownTimer.IsStopped())
                    return State.Walk;
                break;
            case State.Walk:
                if (_wallChecker.IsColliding() || !_floorChecker.IsColliding())
                    return State.Idle;
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
                Move(0, delta);
                break;
            case State.Run:
                if (_wallChecker.IsColliding() || !_floorChecker.IsColliding())
                    Direction = (DirectionEnum)((int)Direction * -1);
                Move(MaxSpeed, delta);
                if (CanSeePlayer())
                    _calmDownTimer.Start();
                break;
            case State.Walk:
                Move(MaxSpeed / 3, delta);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }
    }

    #endregion

    private bool CanSeePlayer()
    {
        if (!_playerChecker.IsColliding())
            return false;
        return _playerChecker.GetCollider() is Player;
    }

    public override void _Ready()
    {
        Move(100, 0.1);
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