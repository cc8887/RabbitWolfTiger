using System;
using System.Linq;
using Godot;
using RabbitWolfTiger.Ui.Localization;
using RabbitWolfTiger.Entities.Diet;
using RabbitWolfTiger.Entities.Portrait;

namespace RabbitWolfTiger.Levels.PortraitReference;

/// <summary>Actual Godot side-by-side registration view. Reference crops are never used by the avatar rig.</summary>
public partial class PortraitReference : Node2D
{
    public ModularPortrait[] Portraits { get; private set; } = Array.Empty<ModularPortrait>();
    private readonly StyleBoxFlat _panel = new() { BgColor = new Color("233044"), CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12 };
    public override async void _Ready()
    {
        try
        {
            Text(UiText.T("ReferenceTitle"), new Vector2(28, 18), 24);
            Text(UiText.T("ReferenceSubtitle"), new Vector2(28, 53), 16);
            var back = new Button { Name = "Back", Text = UiText.T("GalleryBack"), Position = new Vector2(1090, 20), Size = new Vector2(160, 34) };
            back.Pressed += Back; AddChild(back);
            string[] names = { "Rabbit", "Wolf", "Tiger", "Carrot", "Refugee", "Assassin", "King", "Guard" };
            Species[] species = { Species.Rabbit, Species.Wolf, Species.Tiger, Species.Carrot, Species.Human, Species.Human, Species.Human, Species.Human };
            string[] crops = { "01", "02", "04", "03", "05", "13", "10", "11" };
            Vector2[] centers = { new(51, 86), new(53.5f, 83.5f), new(54.5f, 82), new(51, 83), new(53.5f, 84), new(54, 87), new(56, 85), new(55, 85) };
            float[] widths = { 90, 93, 95, 88, 87, 90, 92, 91 };
            var catalog = DietCatalog.FromJson(Godot.FileAccess.GetFileAsString("res://resources/diet/initial_diets.json"));
            var scene = GD.Load<PackedScene>("res://entities/portrait/modular_portrait.tscn");
            Portraits = new ModularPortrait[8];
            for (int i = 0; i < 8; i++)
            {
                float x = 28 + i % 4 * 310, y = 95 + i / 4 * 285;
                Text(UiText.Character(names[i]), new Vector2(x + 13, y + 10), 21);
                Text(UiText.T("Reference"), new Vector2(x + 35, y + 36), 13);
                Text(UiText.T("Modular"), new Vector2(x + 188, y + 36), 13);
                var reference = new Sprite2D { Name = "Reference" + i, Centered = false,
                    Texture = GD.Load<Texture2D>($"res://resources/portrait/reference/example_01_{crops[i]}.png") };
                float scale = 96 / widths[i]; reference.Scale = Vector2.One * scale;
                reference.Position = new Vector2(x + 77, y + 157) - centers[i] * scale; AddChild(reference);
                var portrait = scene.Instantiate<ModularPortrait>();
                portrait.Name = "Portrait" + i; portrait.Position = new Vector2(x + 215, y + 157);
                portrait.Scale = Vector2.One * (96f / 103f); AddChild(portrait);
                portrait.Configure(species[i], names[i], catalog.Create(species[i]));
                portrait.Animator.FreezeNeutralReference(); Portraits[i] = portrait;
                Text(UiText.T(i == 4 ? "RefugeeReference" : "ReferenceCardNote"), new Vector2(x + 10, y + 237), 12);
            }
            Text(UiText.T("ReferenceFooter"), new Vector2(28, 687), 14);
            GD.Print("[REFERENCE] Ready: 8 neutral centered paused avatars");
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--reference-capture="))?[20..];
            if (capture != null)
            {
                if (!System.IO.Path.IsPathFullyQualified(capture) || DisplayServer.GetName() == "headless" || RenderingServer.GetCurrentRenderingMethod() == "dummy")
                    throw new InvalidOperationException("Reference capture requires an absolute destination and actual GL display.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Error result = GetViewport().GetTexture().GetImage().SavePng(capture);
                if (result != Error.Ok) throw new InvalidOperationException("Reference PNG save failed: " + result);
                GD.Print("[REFERENCE] Captured: " + capture); GetTree().Quit();
            }
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), new Color("141d2a"));
        for (int i = 0; i < 8; i++) DrawStyleBox(_panel, new Rect2(28 + i % 4 * 310, 95 + i / 4 * 285, 292, 270));
    }
    private void Text(string text, Vector2 position, int size)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color("e8decb")); AddChild(label);
    }
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F5 }) Back();
    }
    private void Back() => GetTree().ChangeSceneToFile("res://levels/portrait_gallery/portrait_gallery.tscn");
}
