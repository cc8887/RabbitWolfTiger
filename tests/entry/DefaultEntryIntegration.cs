using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RabbitWolfTiger.Entities.Portrait;
using RabbitWolfTiger.Entities.Player;
using RabbitWolfTiger.Levels.PortraitGallery;

namespace RabbitWolfTiger.Tests.Entry;

/// <summary>Loads the configured application entry, never a hardcoded gallery substitute.</summary>
public partial class DefaultEntryIntegration : Node
{
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
    private async Task Frames(int count = 3)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames();
    }
    private PortraitGallery Gallery()
    {
        Check(GetTree().CurrentScene is PortraitGallery, "Configured home/return path must resolve to the portrait gallery.");
        return (PortraitGallery)GetTree().CurrentScene;
    }
    private async Task Click(Button button)
    {
        button.EmitSignal(Button.SignalName.Pressed);
        await Frames();
    }
    public override async void _Ready()
    {
        try
        {
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture-entry="))?["--capture-entry=".Length..];
            if (capture != null)
            {
                Check(System.IO.Path.IsPathFullyQualified(capture), "Entry capture path must be absolute.");
                Check(DisplayServer.GetName() != "headless" && RenderingServer.GetCurrentRenderingMethod() != "dummy", "Real entry screenshot requires a rendering display; headless/dummy is refused.");
            }
            await Frames(1);
            string configured = ProjectSettings.GetSetting("application/run/main_scene").AsString();
            Check(!string.IsNullOrWhiteSpace(configured), "Project must define application/run/main_scene.");
            // Keep this test node alive as a root sibling while the engine loads the actual configured entry.
            GetTree().CurrentScene = null;
            Check(GetTree().ChangeSceneToFile(configured) == Error.Ok, "Configured entry must load successfully.");
            await Frames();
            var gallery = Gallery();
            Check(gallery.Portraits.Length == 8 && gallery.Bodies.Length == 8, "Default entry must show eight travelers.");
            Check(gallery.FindChildren("*", nameof(PlayerController), true, false).Count == 0, "Default entry must not contain the legacy moving player block.");
            Check(gallery.Portraits.All(p => p.GetNode<Sprite2D>("Face").Texture != null), "Every traveler must have a loaded face texture.");
            GD.Print($"[ENTRY] Configured entry loaded: {configured}; 8 portraits");
            if (capture != null)
            {
                // Capture the unmodified entry before any navigation or test-selected expression.
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Image image = GetViewport().GetTexture().GetImage();
                Check(!image.IsEmpty() && image.GetWidth() == 1280 && image.GetHeight() == 720, "Entry image must be a rendered 1280x720 frame.");
                Check(image.SavePng(capture) == Error.Ok, "Entry screenshot could not be saved.");
                GD.Print($"ENTRY_CAPTURE: {capture}");
            }
            await Click(gallery.GetNode<Button>("ExpressionHappy"));
            Check(gallery.Portraits.All(p => p.Animator.Expression == FaceExpression.Happy), "Default entry expression controls must work.");
            ulong homeId = gallery.GetInstanceId();
            await Press(Key.F3);
            Check(Gallery().GetInstanceId() == homeId, "F3 on the homepage must remain on that homepage.");
            await Click(Gallery().GetNode<Button>("Return"));
            Check(GetTree().CurrentScene.SceneFilePath == "res://levels/diet_demo/diet_demo.tscn", "Rules button must open the diet demo.");
            await Click(GetTree().CurrentScene.GetChildren().OfType<Button>().Single(b => b.Text == "F2 / Return"));
            Gallery();
            await Press(Key.F2);
            Check(GetTree().CurrentScene.SceneFilePath == "res://levels/diet_demo/diet_demo.tscn", "Home F2 must open the diet demo.");
            await Press(Key.F2);
            Gallery();
            await Click(Gallery().GetNode<Button>("LegacySandbox"));
            Check(GetTree().CurrentScene.SceneFilePath == "res://levels/main/main.tscn", "Explicit Legacy button must open the old sandbox.");
            await Click(GetTree().CurrentScene.GetChildren().OfType<Button>().Single(b => b.Text.StartsWith("Home /")));
            Gallery();
            await Press(Key.F4);
            Check(GetTree().CurrentScene.SceneFilePath == "res://levels/main/main.tscn", "Explicit F4 must open the old sandbox.");
            await Press(Key.F3);
            Gallery();
            GD.Print($"PASS: {_checks} configured-entry/navigation checks (real Button.Pressed and Input.ParseInputEvent; no physical mouse test).");
            GetTree().Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); GetTree().Quit(1); }
    }
}
