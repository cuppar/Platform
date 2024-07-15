using Godot;

namespace Platform.classes;

[GlobalClass]
public partial class Stats : Node
{
    #region Delegates

    [Signal]
    public delegate void HealthChangedEventHandler();

    #endregion

    private int _health;
    [Export] public int MaxHealth = 3;

    [Export]
    public int Health
    {
        get => _health;
        set
        {
            value = Mathf.Clamp(value, 0, MaxHealth);
            if (value == _health)
                return;
            _health = value;
            EmitSignal(SignalName.HealthChanged);
        }
    }


    public override void _Ready()
    {
        Health = MaxHealth;
    }
}