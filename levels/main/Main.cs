using Godot;
using RabbitWolfTiger.Autoloads;

namespace RabbitWolfTiger.Levels;

/// <summary>
/// 主场景装配节点：装配完成后广播 LevelReady。
/// </summary>
public partial class Main : Node2D
{
    public override void _Ready()
    {
        GD.Print("[MAIN] RabbitWolfTiger ready");
        GameEvents.Instance.EmitLevelReady("main");
    }
}
