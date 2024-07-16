using System;
using Godot;

namespace Platform.classes;

[GlobalClass]
public partial class Stats : Node
{
    #region Delegates

    [Signal]
    public delegate void EnergyChangedEventHandler();

    [Signal]
    public delegate void HealthChangedEventHandler(bool skipAnimation);

    #endregion

    private float _energy;
    private int _health;

    [Export] public float EnergyRegen = 0.8f;
    [Export] public float MaxEnergy = 10;
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
            EmitSignal(SignalName.HealthChanged, false);
        }
    }

    [Export]
    public float Energy
    {
        get => _energy;
        set
        {
            value = Mathf.Clamp(value, 0, MaxEnergy);
            if (Math.Abs(value - _energy) < float.Epsilon)
                return;
            _energy = value;
            EmitSignal(SignalName.EnergyChanged);
        }
    }


    public override void _Ready()
    {
        Reset();
    }

    public void Reset()
    {
        Health = MaxHealth;
        Energy = MaxEnergy;
    }

    public override void _Process(double delta)
    {
        Energy += EnergyRegen * (float)delta;
    }
}