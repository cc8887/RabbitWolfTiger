using Godot;

namespace RabbitWolfTiger.Autoloads;

/// <summary>
/// 全局信号总线（autoload: GameEvents）。
/// 只放生命周期级事件，上限 15 个；feature 内部事件用各自场景的局部信号。
/// 信号是过去式事件；命令（对子节点的操作）用直接方法调用，不走信号。
/// </summary>
public partial class GameEvents : Node
{
    public static GameEvents Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }

    /// <summary>主场景就绪，参数为关卡名。订阅方此时可安全拉取初始状态。</summary>
    [Signal] public delegate void LevelReadyEventHandler(string levelName);

    /// <summary>玩家生命值变化（当前值, 最大值）。</summary>
    [Signal] public delegate void PlayerHealthChangedEventHandler(int current, int max);

    public void EmitLevelReady(string levelName)
    {
        EmitSignal(SignalName.LevelReady, levelName);
    }

    public void EmitPlayerHealthChanged(int current, int max)
    {
        EmitSignal(SignalName.PlayerHealthChanged, current, max);
    }
}
