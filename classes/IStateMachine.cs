namespace Platform.classes;

public interface IStateMachine<TState>
{
    public void TransitionState(TState fromState, TState toState);
    public TState GetNextState(TState currentState, out bool keepCurrent);
    public void TickPhysics(TState currentState, double delta);
}