using System;
using System.Linq;
using Godot;
using RabbitWolfTiger.Entities.Diet;
using RabbitWolfTiger.Entities.Portrait;

namespace RabbitWolfTiger.Levels.PortraitGallery;

/// <summary>Visual composition lab. Role costumes have no gameplay powers.</summary>
public partial class PortraitGallery : Node2D
{
    public ModularPortrait[] Portraits { get; private set; } = Array.Empty<ModularPortrait>();
    public DietBody[] Bodies { get; private set; } = Array.Empty<DietBody>();
    public FaceExpression Expression { get; private set; }
    private readonly string[] _names = { "Rabbit", "Wolf", "Tiger", "Carrot", "Refugee", "Assassin", "King", "Guard" };
    private readonly Species[] _species = { Species.Rabbit, Species.Wolf, Species.Tiger, Species.Carrot, Species.Human, Species.Human, Species.Human, Species.Human };
    private readonly Label[] _details = new Label[8];
    private readonly Label[] _expressions = new Label[8];
    public int SelectedIndex { get; private set; } = -1;
    private readonly StyleBoxFlat _cardStyle = new() { BgColor = new Color("233044"), CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14 };
    private DietCatalog _catalog = null!;
    private Label _state = null!;
    private bool _paused;
    private int _headIndex;
    private int _mouthIndex;

    public override void _Ready()
    {
        _catalog = DietCatalog.FromJson(FileAccess.GetFileAsString("res://resources/diet/initial_diets.json"));
        Bodies = _species.Select(_catalog.Create).ToArray();
        Portraits = new ModularPortrait[8];
        Label("MODULAR PORTRAITS", new Vector2(28, 18), 27, "ecdec4");
        Label("Original art / 8 characters / 5 expressions / independent gaze + blink", new Vector2(28, 53), 16, "acb6c9");
        Button("Rules / F2", "Return", new Vector2(1110, 24), 140, OpenDietDemo);
        Button("Legacy movement / F4", "LegacySandbox", new Vector2(870, 24), 220, OpenLegacySandbox);
        int e = 0;
        foreach (FaceExpression expression in Enum.GetValues<FaceExpression>())
        {
            FaceExpression captured = expression;
            Button(expression.ToString(), "Expression" + expression, new Vector2(28 + e * 132, 89), 122, () => SetExpression(captured)); e++;
        }
        Button("Blink", "Blink", new Vector2(700, 89), 95, () => { foreach (var p in Portraits) p.Animator.Blink(); });
        Button("Pause / resume", "Pause", new Vector2(805, 89), 150, TogglePause);
        Button("Gaze: follow / center", "Gaze", new Vector2(965, 89), 285, ToggleGaze);
        var scene = GD.Load<PackedScene>("res://entities/portrait/modular_portrait.tscn");
        for (int i = 0; i < 8; i++)
        {
            float x = 28 + (i % 4) * 310; float y = 140 + (i / 4) * 250;
            Label(_names[i], new Vector2(x + 18, y + 12), 22, "f4dfbb");
            _expressions[i] = Label("", new Vector2(x + 190, y + 46), 13, "e7c78f");
            int selected = i;
            Button("Select", "Select" + i, new Vector2(x + 207, y + 10), 70, () => SelectPortrait(selected));
            var portrait = scene.Instantiate<ModularPortrait>();
            portrait.Name = "Portrait" + i; portrait.Position = new Vector2(x + 147, y + 105); portrait.Scale = Vector2.One * .85f;
            AddChild(portrait); Portraits[i] = portrait;
            portrait.Configure(_species[i], _names[i], Bodies[i]);
            portrait.Animator.SetExpression((FaceExpression)(i % 5));
            _details[i] = Label("", new Vector2(x + 13, y + 200), 13, "d6ddea");
            _details[i].Name = "Details" + i;
        }
        Button("Swap faces", "SwapFaces", new Vector2(28, 650), 150, SwapFaces);
        Button("Swap mouths", "SwapMouths", new Vector2(188, 650), 150, SwapMouths);
        Button("Swap headwear", "SwapHeadParts", new Vector2(348, 650), 170, SwapHeadParts);
        Button("All", "SelectAll", new Vector2(528, 650), 70, () => SelectPortrait(-1));
        _state = Label("", new Vector2(614, 652), 14, "e7c78f");
        Label("Expression mouths are visual only. Gameplay mouth + edible colors are always shown. Human None is provisional; no role abilities.", new Vector2(28, 694), 13, "a6b3c7");
        RefreshDetails();
        GD.Print("[GALLERY] Ready: 8 portraits");
    }
    public void SetExpression(FaceExpression expression)
    {
        Expression = expression;
        for (int i = 0; i < Portraits.Length; i++)
            if (SelectedIndex < 0 || SelectedIndex == i) Portraits[i].Animator.SetExpression(expression);
        RefreshDetails();
    }
    public void SelectPortrait(int index)
    {
        if (index < -1 || index >= Portraits.Length) throw new ArgumentOutOfRangeException(nameof(index));
        SelectedIndex = index; RefreshDetails();
    }
    public void SwapFaces()
    {
        FaceColor[] colors = Enum.GetValues<FaceColor>();
        for (int i = 0; i < Bodies.Length; i++)
        {
            Bodies[i].ReplaceFace(new FaceComponent(colors[(Array.IndexOf(colors, Bodies[i].Face.Color) + 1) % colors.Length]));
            Portraits[i].ProjectGameplay(Bodies[i]);
        }
        RefreshDetails();
    }
    public void SwapMouths()
    {
        _mouthIndex = (_mouthIndex + 1) % _catalog.Templates.Count;
        for (int i = 0; i < Bodies.Length; i++)
        {
            Bodies[i].ReplaceMouth(_catalog.Templates[(_mouthIndex + i) % _catalog.Templates.Count].Mouth);
            Portraits[i].ProjectGameplay(Bodies[i]);
        }
        RefreshDetails();
    }
    public void SwapHeadParts()
    {
        string[] parts = { "", "crown", "helmet", "headscarf", "leaves" };
        _headIndex = (_headIndex + 1) % parts.Length;
        foreach (var portrait in Portraits) portrait.SetHeadPart(parts[_headIndex]);
    }
    public void TogglePause()
    {
        _paused = !_paused;
        foreach (var p in Portraits) p.Animator.AnimationPaused = _paused;
        RefreshDetails();
    }
    public void ToggleGaze()
    {
        foreach (var p in Portraits)
        {
            p.Animator.FollowPointer = !p.Animator.FollowPointer;
            if (!p.Animator.FollowPointer) p.Animator.SetGaze(Vector2.Zero);
        }
    }
    private void RefreshDetails()
    {
        for (int i = 0; i < Portraits.Length; i++)
        {
            _expressions[i].Text = Portraits[i].Animator.Expression.ToString();
            _details[i].Text = $"Face: {Portraits[i].DisplayedFace}  |  Mouth: {Portraits[i].GameplayMouthSize}\nEats: {Portraits[i].RecipeText}";
        }
        if (_state != null) _state.Text = $"Target: {(SelectedIndex < 0 ? "all" : _names[SelectedIndex])}  ·  {(_paused ? "paused" : "live")}  ·  Pointer → gaze";
    }
    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), new Color("141d2a"));
        for (int i = 0; i < 8; i++)
        {
            var rect = new Rect2(28 + i % 4 * 310, 140 + i / 4 * 250, 292, 240);
            DrawStyleBox(_cardStyle, rect);
        }
    }
    private Label Label(string text, Vector2 position, int size, string color)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color(color)); AddChild(label); return label;
    }
    private void Button(string text, string name, Vector2 position, float width, Action action)
    {
        var button = new Button { Text = text, Name = name, Position = position, Size = new Vector2(width, 34) };
        button.AddThemeFontSizeOverride("font_size", 16); button.Pressed += action; AddChild(button);
    }
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F2 }) OpenDietDemo();
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F4 }) OpenLegacySandbox();
    }
    private void OpenDietDemo() => GetTree().ChangeSceneToFile("res://levels/diet_demo/diet_demo.tscn");
    private void OpenLegacySandbox() => GetTree().ChangeSceneToFile("res://levels/main/main.tscn");
}
