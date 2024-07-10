using System;
using Godot;

namespace Platform.classes;

public partial class StateMachine<TState> : Node where TState : struct, Enum
{
    private TState _currentState; 

    private TState CurrentState
    {
        get => _currentState;
        set
        {
            if (Owner is not IStateMachine<TState> owner)
                throw new OwnerIsNotIStateMachine();

            owner.TransitionState(CurrentState, value);
            _currentState = value;
            StateTime = 0;
        }
    }

    public double StateTime;


    public override async void _Ready()
    {
        await ToSignal(Owner, Node.SignalName.Ready);
        CurrentState = Enum.GetValues<TState>()[0];
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Owner is not IStateMachine<TState> owner)
            throw new OwnerIsNotIStateMachine();

        while (true)
        {
            var next = owner.GetNextState(CurrentState);
            if (next.Equals(CurrentState))
                break;
            CurrentState = next;
        }

        owner.TickPhysics(CurrentState, delta);
        StateTime += delta;
    }

    #region Nested type: OwnerIsNotIStateMachine

    private class OwnerIsNotIStateMachine : Exception
    {
        public OwnerIsNotIStateMachine()
            : base("Owner is not IStateMachine who has a state machine")
        {
        }
    }

    #endregion
}