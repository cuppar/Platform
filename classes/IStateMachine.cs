namespace Platform.classes;

public interface IStateMachine<TState>
{
    public void TransitionState(TState fromState, TState toState);
    public TState GetNextState(TState currentState);
    public void TickPhysics(TState currentState, double delta);
}