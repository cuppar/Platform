using System.Linq;
using Godot;

namespace Platform.classes;

[GlobalClass]
public partial class Interactable : Area2D
{
    [Signal]
    public delegate void InteractedEventHandler();

    public void Interact()
    {
        GD.Print($"[Interact] {Name}");
        EmitSignal(SignalName.Interacted);
    }

    public Interactable()
    {
        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionMaskValue(2, true);

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }


    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player player)
            return;

        player.RegisterInteractable(this);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is not Player player)
            return;

        player.UnregisterInteractable(this);
    }
}