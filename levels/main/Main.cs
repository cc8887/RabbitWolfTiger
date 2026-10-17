using Godot;
using RabbitWolfTiger.Autoloads;

namespace RabbitWolfTiger.Levels;

/// <summary>
/// 保留的旧移动实验场：显式选择后才进入，不作为项目首页。
/// </summary>
public partial class Main : Node2D
{
    public override void _Ready()
    {
        GD.Print("[LEGACY] Movement sandbox ready");
        GameEvents.Instance.EmitLevelReady("main");
        var hint = new Label { Text = "Legacy movement test only   |   F2: rules   |   F3: return to portraits", Position = new Vector2(24, 660) };
        AddChild(hint);
        var demoButton = new Button { Text = "Rules demo / 规则示例 (F2)", Position = new Vector2(24, 605) };
        demoButton.Pressed += OpenDietDemo;
        AddChild(demoButton);
        var portraitButton = new Button { Text = "Home / 旅客展示 (F3)", Position = new Vector2(380, 605) };
        portraitButton.Pressed += OpenPortraitGallery;
        AddChild(portraitButton);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F2 })
            OpenDietDemo();
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F3 })
            OpenPortraitGallery();
    }

    private void OpenPortraitGallery() => GetTree().ChangeSceneToFile("res://levels/portrait_gallery/portrait_gallery.tscn");

    private void OpenDietDemo() => GetTree().ChangeSceneToFile("res://levels/diet_demo/diet_demo.tscn");
}

