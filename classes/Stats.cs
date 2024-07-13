using Godot;

namespace Platform.classes;

[GlobalClass]
public partial class Stats : Node
{
    private int _health;
    [Export] public int MaxHealth = 3;

    [Export]
    public int Health
    {
        get => _health;
        set
        {
            value = Mathf.Clamp(value, 0, MaxHealth);
            // ReSharper disable once RedundantCheckBeforeAssignment
            if (value == _health)
                return;
            _health = value;
        }
    }


    public override void _Ready()
    {
        Health = MaxHealth;
    }
}