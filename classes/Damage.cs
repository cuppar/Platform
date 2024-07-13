using Godot;

namespace Platform.classes;

public partial class Damage : RefCounted
{
    public Damage()
    {
    }

    public Damage(Node2D source, int amount)
    {
        Source = source;
        Amount = amount;
    }

    public int Amount { get; }
    public Node2D Source { get; } = null!;
}