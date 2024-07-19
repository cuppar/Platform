using Godot;
using Platform.classes;

namespace Platform.enemies;

public partial class Enemy : CharacterBody2D
{
    #region Delegates

    [Signal]
    public delegate void DiedEventHandler();

    #endregion

    private float _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
    [Export] public float Acceleration;
    [Export] public float MaxSpeed = 230;

    public Enemy()
    {
        Acceleration = MaxSpeed / 0.1f;
    }

    public override void _Ready()
    {
        AddToGroup("enemies");
    }

    protected void Move(float speed, double delta)
    {
        Velocity = Velocity with
        {
            X = Mathf.MoveToward(Velocity.X, speed * (int)Direction, Acceleration * (float)delta),
            Y = Velocity.Y + _gravity * (float)delta
        };
        MoveAndSlide();
    }

    private void Die()
    {
        EmitSignal(SignalName.Died);
        QueueFree();
    }

    #region Direction

    protected enum DirectionEnum
    {
        Left = -1,
        Right = +1
    }

    private DirectionEnum _direction = DirectionEnum.Left;


    [Export]
    protected DirectionEnum Direction
    {
        get => _direction;
        set => SetDirection(value);
    }

    private async void SetDirection(DirectionEnum value)
    {
        if (_direction == value)
            return;

        if (!IsNodeReady())
            await ToSignal(this, Node.SignalName.Ready);

        _direction = value;
        _graphics.Scale = _graphics.Scale with { X = -(int)_direction };
    }

    #endregion

    #region Child

    [ExportGroup("ChildDontChange")] [Export]
    private Node2D _graphics = null!;


    [Export] private CollisionShape2D _collisionShape = null!;
    [Export] protected AnimationPlayer AnimationPlayer = null!;
    [Export] protected HurtBox HurtBox = null!;
    [Export] protected Stats Stats = null!;

    #endregion
}