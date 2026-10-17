using Godot;
using RabbitWolfTiger.Ui.Localization;
using RabbitWolfTiger.Autoloads;

namespace RabbitWolfTiger.Ui.Hud;

/// <summary>
/// HUD：纯投影层，只监听信号 + 从数据源拉取初始值，绝不持有/修改玩家数据。
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _healthLabel = null!;

    public override void _Ready()
    {
        _healthLabel = GetNode<Label>("%HealthLabel");
        GetNode<Label>("%Title").Text = UiText.T("LegacyTitle");
        _healthLabel.Text = UiText.Health(100, 100);

        GameEvents.Instance.PlayerHealthChanged += OnPlayerHealthChanged;
        GameEvents.Instance.LevelReady += OnLevelReady;
    }

    public override void _ExitTree()
    {
        // autoload 生命周期与场景相同，仍显式断开，养成 ghost-connection 防护习惯。
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PlayerHealthChanged -= OnPlayerHealthChanged;
            GameEvents.Instance.LevelReady -= OnLevelReady;
        }
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        _healthLabel.Text = UiText.Health(current, max);
    }

    private void OnLevelReady(string levelName)
    {
        GD.Print($"[HUD] level '{levelName}' ready");
    }
}

