using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace RabbitWolfTiger.Tests.Diet;

/// <summary>Real scene integration through Button.Pressed; not a mouse-input test.</summary>
public partial class DietDemoIntegration : Node
{
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    public override async void _Ready()
    {
        try
        {
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="));
            if (capture != null && (DisplayServer.GetName() == "headless" || RenderingServer.GetCurrentRenderingMethod() == "dummy"))
                throw new InvalidOperationException("Capture requires a real rendering display; headless screenshots are unsupported.");
            var demo = GD.Load<PackedScene>("res://levels/diet_demo/diet_demo.tscn").Instantiate<Node2D>();
            AddChild(demo);
            await Frame();
            Button[] buttons = demo.GetChildren().OfType<Button>().ToArray();
            Button[] mouths = buttons.Where(b => b.Text == "Swap mouth").ToArray();
            Button[] faces = buttons.Where(b => b.Text == "Swap face").ToArray();
            Label[] details = Enumerable.Range(0, 5).Select(i => demo.GetNode<Label>($"Details{i}")).ToArray();
            Check(mouths.Length == 5 && faces.Length == 5 && details.Length == 5, "Five interactive cards required.");
            for (int i = 0; i < 5; i++)
            {
                Node2D parts = demo.GetNode<Node2D>($"Parts{i}");
                Check(parts.GetNode<Sprite2D>("Face").Texture != null, "Face texture must load.");
                foreach (string side in new[] { "LeftEye", "RightEye" })
                {
                    var white = parts.GetNode<Sprite2D>($"{side}/White");
                    var pupil = parts.GetNode<Sprite2D>($"{side}/Pupil");
                    Check(white != pupil && white.Texture != null && pupil.Texture != null &&
                        white.Texture.ResourcePath != pupil.Texture.ResourcePath, "Eye white and pupil must be separate textured nodes.");
                }
            }
            var face = demo.GetNode<Sprite2D>("Parts0/Face");
            var mouth = demo.GetNode<Sprite2D>("Parts0/Mouth");
            Check(face.Texture.ResourcePath.EndsWith("face_human_skin.png") && !mouth.Visible, "Initial human uses Skin and provisional None.");
            mouths[0].EmitSignal(Button.SignalName.Pressed);
            await Frame();
            Check(mouth.Visible && mouth.Texture.ResourcePath.EndsWith("mouth_open_small.png"), "Mouth swap updates visible mouth texture.");
            Check(face.Texture.ResourcePath.EndsWith("face_human_skin.png"), "Mouth swap preserves face texture.");
            Check(details[0].Text.Contains("Small") && details[0].Text.Contains("Orange") && details[0].Text.Contains("Carrot"), "Mouth swap updates recipe and eligible target text.");
            faces[0].EmitSignal(Button.SignalName.Pressed);
            await Frame();
            Check(face.Texture.ResourcePath.EndsWith("face_rabbit_cream.png"), "Face swap updates face texture.");
            Check(mouth.Texture.ResourcePath.EndsWith("mouth_open_small.png") && details[0].Text.Contains("Small"), "Face swap preserves mouth and recipe.");
            foreach (Button button in mouths.Concat(faces))
                for (int cycle = 0; cycle < 5; cycle++)
                {
                    button.EmitSignal(Button.SignalName.Pressed);
                    await Frame();
                    for (int card = 0; card < details.Length; card++)
                        Check(details[card].GetGlobalRect().End.Y <= mouths[card].GlobalPosition.Y, "Card text overlaps the mouth button.");
                }
            buttons.Single(b => b.Text == "Reset parts").EmitSignal(Button.SignalName.Pressed);
            await Frame();
            Check(face.Texture.ResourcePath.EndsWith("face_human_skin.png") && !mouth.Visible && details[0].Text.Contains("None"), "Reset restores initial face, mouth visibility and text.");
            // Worst-case target list: leave the tiger face GoldenOrange and make all four other faces Skin.
            for (int card = 0; card < 4; card++)
                for (int attempts = 0; attempts < 5 && !demo.GetNode<Sprite2D>($"Parts{card}/Face").Texture.ResourcePath.EndsWith("face_human_skin.png"); attempts++)
                {
                    faces[card].EmitSignal(Button.SignalName.Pressed);
                    await Frame();
                }
            await Frame();
            Check(details[4].Text.Contains("GoldenOrange") && details[4].Text.Contains("Large") &&
                details[4].Text.Contains("Human, Rabbit, Wolf, Carrot"), "Worst-case card must contain Large, GoldenOrange and four eligible targets.");
            Check(details[4].GetGlobalRect().End.Y <= mouths[4].GlobalPosition.Y, "Worst-case card overlaps its button.");
            foreach (Label label in demo.GetChildren().OfType<Label>())
                Check(label.GetRect().End.X <= 1280 && label.GetRect().End.Y <= 720, "Label extends outside the viewport.");
            buttons.Single(b => b.Text == "Reset parts").EmitSignal(Button.SignalName.Pressed);
            await Frame();
            if (capture != null)
            {
                string path = capture["--capture=".Length..];
                Check(System.IO.Path.IsPathFullyQualified(path), "Capture path must be absolute.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Image screenshot = GetViewport().GetTexture().GetImage();
                Check(!screenshot.IsEmpty() && screenshot.GetWidth() == 1280 && screenshot.GetHeight() == 720, "Screenshot must be a nonempty 1280x720 rendered image.");
                Error result = screenshot.SavePng(path);
                Check(result == Error.Ok, $"Screenshot save failed: {result}");
                GD.Print($"CAPTURE: {path}");
            }
            GD.Print($"PASS: {_checks} real-scene UI integration checks (Button.Pressed signals; no physical mouse test).");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
}
