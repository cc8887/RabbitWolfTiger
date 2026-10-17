using System;
using System.Linq;
using Godot;
using RabbitWolfTiger.Ui.Localization;
using RabbitWolfTiger.Entities.Diet;

namespace RabbitWolfTiger.Levels.DietDemo;

/// <summary>Isolated composition sandbox, not the ferry turn simulation or final artwork.</summary>
public partial class DietDemo : Node2D
{
    private DietCatalog _catalog = null!;
    private DietBody[] _bodies = Array.Empty<DietBody>();
    private readonly int[] _mouthIndexes = { 0, 1, 2, 3, 4 };
    private readonly Sprite2D[] _faces = new Sprite2D[5];
    private readonly Sprite2D[] _mouths = new Sprite2D[5];
    private readonly Label[] _details = new Label[5];
    private readonly string[] _names = { "Human", "Rabbit", "Wolf", "Carrot", "Tiger" };

    public override void _Ready()
    {
        _catalog = DietCatalog.FromJson(FileAccess.GetFileAsString("res://resources/diet/initial_diets.json"));
        ResetBodies();
        AddLabel(UiText.T("DietTitle"), new Vector2(30, 25), 25);
        AddLabel(UiText.T("DietLegend"), new Vector2(30, 70), 20);
        AddLabel(UiText.T("DietInstructions"), new Vector2(30, 105), 18);
        for (int i = 0; i < _bodies.Length; i++)
        {
            int index = i;
            float x = 30 + i * 245;
            AddLabel(UiText.Character(_names[i]), new Vector2(x, 160), 24);
            BuildParts(i, new Vector2(125 + i * 245, 255));
            _details[i] = AddLabel("", new Vector2(x, 330), 17);
            _details[i].Name = $"Details{i}";
            _details[i].Size = new Vector2(230, 180);
            _details[i].AutowrapMode = TextServer.AutowrapMode.WordSmart;
            AddButton(UiText.T("SwapMouths"), new Vector2(x, 520), () =>
            {
                _mouthIndexes[index] = (_mouthIndexes[index] + 1) % _catalog.Templates.Count;
                _bodies[index].ReplaceMouth(_catalog.Templates[_mouthIndexes[index]].Mouth);
                Refresh();
            });
            AddButton(UiText.T("SwapFaces"), new Vector2(x, 565), () =>
            {
                FaceColor[] colors = Enum.GetValues<FaceColor>();
                int next = (Array.IndexOf(colors, _bodies[index].Face.Color) + 1) % colors.Length;
                _bodies[index].ReplaceFace(new FaceComponent(colors[next]));
                Refresh();
            });
        }
        AddButton(UiText.T("ResetParts"), new Vector2(30, 620), () => { ResetBodies(); Refresh(); });
        AddButton(UiText.T("DietBack"), new Vector2(250, 620), ReturnToPortraits);
        AddLabel(UiText.T("DietFooter"), new Vector2(30, 675), 17);
        Refresh();
    }

    private void ResetBodies()
    {
        Species[] species = { Species.Human, Species.Rabbit, Species.Wolf, Species.Carrot, Species.Tiger };
        _bodies = species.Select(_catalog.Create).ToArray();
        for (int i = 0; i < species.Length; i++)
            _mouthIndexes[i] = _catalog.Templates.ToList().FindIndex(t => t.Species == species[i]);
    }

    private void Refresh()
    {
        for (int i = 0; i < _bodies.Length; i++)
        {
            DietBody body = _bodies[i];
            var targets = _bodies.Select((target, index) => (target, index))
                .Where(pair => body.CanEat(pair.target)).Select(pair => _names[pair.index]);
            _details[i].Text = UiText.DietDetails(body, targets);
            _details[i].AddThemeFontSizeOverride("font_size", 16);
            _faces[i].Texture = GD.Load<Texture2D>(FacePath(body.Face.Color));
            _mouths[i].Visible = body.Mouth.Size != MouthSize.None;
            if (_mouths[i].Visible)
                _mouths[i].Texture = GD.Load<Texture2D>($"res://resources/diet/art/mouths/mouth_open_{body.Mouth.Size.ToString().ToLowerInvariant()}.png");
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), new Color("202838"));
        for (int i = 0; i < _bodies.Length; i++)
        {
            for (int c = 0; c < _bodies[i].Mouth.EdibleColors.Count; c++)
                DrawRect(new Rect2(30 + i * 245 + c * 28, 310, 20, 12), DisplayColor(_bodies[i].Mouth.EdibleColors[c]));
        }
    }

    private void BuildParts(int index, Vector2 center)
    {
        var root = new Node2D { Name = $"Parts{index}", Position = center };
        AddChild(root);
        _faces[index] = new Sprite2D { Name = "Face" };
        root.AddChild(_faces[index]);
        for (int side = -1; side <= 1; side += 2)
        {
            var eyeRoot = new Node2D { Name = side < 0 ? "LeftEye" : "RightEye", Position = new Vector2(side * 18, -12) };
            root.AddChild(eyeRoot);
            eyeRoot.AddChild(new Sprite2D
            {
                Name = "White", Texture = GD.Load<Texture2D>("res://resources/diet/art/eyes/sclera_blank.png"),
                Scale = new Vector2(0.38f, 0.38f)
            });
            // Smaller independent pupil remains entirely inside the white; no tracking motion this round.
            eyeRoot.AddChild(new Sprite2D
            {
                Name = "Pupil", Texture = GD.Load<Texture2D>("res://resources/diet/art/eyes/pupil_brown.png"),
                Scale = new Vector2(0.22f, 0.22f)
            });
        }
        _mouths[index] = new Sprite2D { Name = "Mouth", Position = new Vector2(0, 22), Scale = new Vector2(0.65f, 0.65f) };
        root.AddChild(_mouths[index]);
    }

    private static string FacePath(FaceColor color) => "res://resources/diet/art/faces/" + (color switch
    {
        FaceColor.Skin => "face_human_skin.png", FaceColor.Cream => "face_rabbit_cream.png",
        FaceColor.Gray => "face_wolf_gray.png", FaceColor.Orange => "face_carrot_orange.png",
        FaceColor.GoldenOrange => "face_tiger_orange.png", _ => throw new ArgumentOutOfRangeException(nameof(color))
    });

    private static Color DisplayColor(FaceColor color) => color switch
    {
        FaceColor.Skin => new Color("e8b18f"), FaceColor.Cream => new Color("f3e9ce"),
        FaceColor.Gray => new Color("93969e"), FaceColor.Orange => new Color("f47931"),
        FaceColor.GoldenOrange => new Color("f4ba48"), _ => throw new ArgumentOutOfRangeException(nameof(color))
    };

    private Label AddLabel(string text, Vector2 position, int size)
    {
        var label = new Label { Text = text, Position = position };
        label.AddThemeFontSizeOverride("font_size", size);
        AddChild(label);
        return label;
    }

    private void AddButton(string text, Vector2 position, Action action)
    {
        var button = new Button { Text = text, Position = position, Size = new Vector2(195, 35) };
        button.Pressed += action;
        AddChild(button);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F2 }) ReturnToPortraits();
    }

    private void ReturnToPortraits() => GetTree().ChangeSceneToFile("res://levels/portrait_gallery/portrait_gallery.tscn");
}
