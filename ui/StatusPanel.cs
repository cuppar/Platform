using Godot;
using Platform.classes;

namespace Platform.ui;

public partial class StatusPanel : HBoxContainer
{
    public override void _Ready()
    {
        Stats.HealthChanged += UpdateHealth;
        UpdateHealth();
    }

    private void UpdateHealth()
    {
        var percentage = (float)Stats.Health / Stats.MaxHealth;
        HealthBar.Value = percentage;
        var easedHealthBar = HealthBar.GetNode<TextureProgressBar>("EasedHealthBar");
        CreateTween().TweenProperty(easedHealthBar, "value", percentage, 0.3);
    }

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    public TextureProgressBar HealthBar = null!;

    [Export] public Stats Stats = null!;

    #endregion
}