using Godot;

namespace Platform.classes;

[GlobalClass]
public partial class HitBox : Area2D
{
    #region Delegates

    [Signal]
    public delegate void HitEventHandler(HurtBox hurtBox);

    [Signal]
    public delegate void HurtBoxEnteredEventHandler(HurtBox hurtBox);

    #endregion

    public HitBox()
    {
        AreaEntered += OnAreaEntered;
        HurtBoxEntered += OnHurtBoxEntered;
    }

    private void OnHurtBoxEntered(HurtBox hurtBox)
    {
        // GD.Print($"[Hit] {Owner.Name} => {hurtBox.Owner.Name}");
        EmitSignal(SignalName.Hit, hurtBox);
        hurtBox.EmitSignal(HurtBox.SignalName.Hurt, this);
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is HurtBox hurtBox)
            EmitSignal(SignalName.HurtBoxEntered, hurtBox);
    }
}