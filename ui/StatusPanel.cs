using Godot;
using Platform.classes;
using Platform.globals;

namespace Platform.ui;

public partial class StatusPanel : HBoxContainer
{
    public override void _Ready()
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        Stats ??= AutoloadManager.Game.PlayerStats;

        Stats.HealthChanged += UpdateHealth;
        Stats.EnergyChanged += UpdateEnergy;
        UpdateHealth(true);
        UpdateEnergy();
    }

    public override void _ExitTree()
    {
        Stats.HealthChanged -= UpdateHealth;
        Stats.EnergyChanged -= UpdateEnergy;
    }

    private void UpdateHealth(bool skipAnimation = false)
    {
        var percentage = (float)Stats.Health / Stats.MaxHealth;
        HealthBar.Value = percentage;
        var easedHealthBar = HealthBar.GetNode<TextureProgressBar>("EasedHealthBar");
        if (skipAnimation)
            easedHealthBar.Value = percentage;
        else
            CreateTween().TweenProperty(easedHealthBar, "value", percentage, 0.3);
    }

    private void UpdateEnergy()
    {
        var percentage = Stats.Energy / Stats.MaxEnergy;
        EnergyBar.Value = percentage;
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public TextureProgressBar HealthBar = null!;

    [Export] public TextureProgressBar EnergyBar = null!;

    [Export] public Stats Stats = null!;

    #endregion
}