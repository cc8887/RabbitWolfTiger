using Godot;
using RabbitWolfTiger.Autoloads;

namespace RabbitWolfTiger.Entities.Player;

/// <summary>
/// 玩家实体：8 向移动 + 边界内活动 + 通过信号总线广播生命值。
/// 移动逻辑在 _PhysicsProcess（帧率无关）；受击与治疗后广播 PlayerHealthChanged。
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed { get; set; } = 260.0f;
    [Export] public int MaxHealth { get; set; } = 100;

    [ExportGroup("Movement Bounds")]
    [Export] public Vector2 MinPosition { get; set; } = new(24, 24);
    [Export] public Vector2 MaxPosition { get; set; } = new(1256, 696);

    public int Health { get; private set; }

    public override void _Ready()
    {
        Health = MaxHealth;
        // 初始状态主动广播一次，晚订阅的 HUD 在 _Ready 里自行拉取，不依赖此处。
        GameEvents.Instance.EmitPlayerHealthChanged(Health, MaxHealth);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector(
            "move_left", "move_right", "move_up", "move_down");

        Velocity = input.Normalized() * MoveSpeed;

        if (input != Vector2.Zero)
        {
            // 移动中持续钳制在边界内（用 MoveSpeed * delta 防高速穿界）。
            MoveAndSlide();
            GlobalPosition = GlobalPosition.Clamp(
                MinPosition, MaxPosition);
        }
        else
        {
            Velocity = Vector2.Zero;
        }
    }

    public void ApplyDamage(int amount)
    {
        int before = Health;
        Health = Mathf.Max(0, Health - amount);
        if (Health != before)
        {
            GameEvents.Instance.EmitPlayerHealthChanged(Health, MaxHealth);
        }
    }

    public void Heal(int amount)
    {
        int before = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        if (Health != before)
        {
            GameEvents.Instance.EmitPlayerHealthChanged(Health, MaxHealth);
        }
    }
}
