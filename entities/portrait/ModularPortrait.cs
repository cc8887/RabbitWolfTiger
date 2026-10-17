using System;
using Godot;
using RabbitWolfTiger.Entities.Diet;

namespace RabbitWolfTiger.Entities.Portrait;

/// <summary>Stable reusable sprite slots. Gameplay values are read-only projection inputs.</summary>
public partial class ModularPortrait : Node2D
{
    public FaceAnimator Animator { get; private set; } = null!;
    public FaceColor DisplayedFace { get; private set; }
    public MouthSize GameplayMouthSize { get; private set; }
    public string RecipeText { get; private set; } = "none";
    public string HeadPart { get; private set; } = "";
    private Sprite2D _face = null!, _mouth = null!, _head = null!;
    private readonly Sprite2D[] _pupils = new Sprite2D[2];
    private readonly ShaderMaterial[] _whiteMaterials = new ShaderMaterial[2];
    private readonly ShaderMaterial[] _pupilMaterials = new ShaderMaterial[2];
    private readonly Node2D[] _eyes = new Node2D[2];
    private static Shader? _eyeShader;
    private const float WhiteScale = 0.38f;
    private const float PupilScale = 0.24f;
    private const float GazeRadius = 3.1f;

    public override void _Ready()
    {
        _face = GetNode<Sprite2D>("Face"); _mouth = GetNode<Sprite2D>("MouthPose");
        _head = GetNode<Sprite2D>("HeadFront");
        Animator = GetNode<FaceAnimator>("FaceAnimator");
        _eyeShader ??= GD.Load<Shader>("res://entities/portrait/eye_mask.gdshader");
        var white = PortraitArt.Texture("02_eyes/sclera_blank");
        var pupil = PortraitArt.Texture("02_eyes/pupil_brown");
        for (int side = 0; side < 2; side++)
        {
            _eyes[side] = GetNode<Node2D>(side == 0 ? "Eyes/LeftEye" : "Eyes/RightEye");
            var sclera = _eyes[side].GetNode<Sprite2D>("White");
            _pupils[side] = _eyes[side].GetNode<Sprite2D>("Pupil");
            sclera.Texture = white; sclera.Scale = Vector2.One * WhiteScale;
            _pupils[side].Texture = pupil; _pupils[side].Scale = Vector2.One * PupilScale;
            _whiteMaterials[side] = CreateEyeMaterial(white, false);
            _pupilMaterials[side] = CreateEyeMaterial(white, true);
            _pupilMaterials[side].SetShaderParameter("relative_size", pupil.GetSize() * PupilScale / (white.GetSize() * WhiteScale));
            sclera.Material = _whiteMaterials[side]; _pupils[side].Material = _pupilMaterials[side];
        }
        Part("Nose", "03_noses/nose_triangle_pink", new Vector2(0, 7), .24f);
        Animator.RefreshVisuals();
    }
    private static ShaderMaterial CreateEyeMaterial(Texture2D white, bool pupil)
    {
        var material = new ShaderMaterial { Shader = _eyeShader };
        material.SetShaderParameter("sclera_mask", white);
        material.SetShaderParameter("pupil_layer", pupil);
        return material;
    }
    public void Configure(Species species, string role, DietBody body)
    {
        foreach (string slot in new[] { "HeadBack", "EarLeft", "EarRight", "HeadFront", "Hair", "Marking", "Equipment" })
            GetNode<Sprite2D>(slot).Texture = null;
        Part("Body", "07_body_equipment/robe", new Vector2(0, 75), 1f);
        HeadPart = "";
        if (species is Species.Rabbit or Species.Wolf or Species.Tiger)
        {
            string animal = species.ToString().ToLowerInvariant();
            Part("EarLeft", $"05_head_parts/{animal}_ear_left", new Vector2(0, -57), 1f);
            Part("EarRight", $"05_head_parts/{animal}_ear_right", new Vector2(0, -57), 1f);
            if (species == Species.Tiger) Part("Marking", "06_markings/king_mark", new Vector2(0, -32), .55f);
        }
        if (species == Species.Carrot) SetHeadPart("leaves");
        if (species == Species.Human)
        {
            Part("Hair", "05_head_parts/hair", new Vector2(0, -37), 1.07f);
            switch (role)
            {
                case "Refugee": SetHeadPart("headscarf"); break;
                case "Assassin":
                    Part("HeadBack", "05_head_parts/hood", new Vector2(0, -20), 1.55f);
                    Part("Body", "07_body_equipment/cloak", new Vector2(0, 75), 1.2f);
                    Part("Equipment", "07_body_equipment/dagger", new Vector2(53, 70), .7f); break;
                case "King": SetHeadPart("crown"); Part("Body", "07_body_equipment/cape", new Vector2(0, 75), 1.1f); break;
                case "Guard": SetHeadPart("helmet"); Part("Body", "07_body_equipment/armor", new Vector2(0, 75), 1.15f);
                    Part("Equipment", "07_body_equipment/spear", new Vector2(62, 55), .9f); break;
            }
        }
        Animator.SetBlinkPhase(species switch
        {
            Species.Rabbit => 0, Species.Wolf => 1, Species.Tiger => 2, Species.Carrot => 3,
            _ => role switch { "Refugee" => 4, "Assassin" => 5, "King" => 6, "Guard" => 7, _ => 8 }
        });
        ProjectGameplay(body);
    }
    public void ProjectGameplay(DietBody body)
    {
        // Do not retain DietBody: this component cannot mutate it later through animation callbacks.
        DisplayedFace = body.Face.Color; GameplayMouthSize = body.Mouth.Size;
        RecipeText = body.Mouth.EdibleColors.Count == 0 ? "none (non-predatory)" : string.Join(", ", body.Mouth.EdibleColors);
        _face.Texture = PortraitArt.Texture(PortraitArt.Face(DisplayedFace));
        Animator.RefreshVisuals();
    }
    public void SetHeadPart(string name)
    {
        if (name != "" && name != "crown" && name != "helmet" && name != "headscarf" && name != "leaves")
            throw new ArgumentException("Unknown head slot part", nameof(name));
        HeadPart = name;
        _head.Texture = name == "" ? null : PortraitArt.Texture("05_head_parts/" + name);
        _head.Position = new Vector2(0, name == "headscarf" ? -65 : name == "helmet" ? -72 : -53);
        _head.Scale = Vector2.One * (name == "helmet" ? 1.2f : .95f);
        Animator.RefreshVisuals();
    }
    private void Part(string slot, string asset, Vector2 position, float scale)
    {
        var sprite = GetNode<Sprite2D>(slot); sprite.Texture = PortraitArt.Texture(asset);
        sprite.Position = position; sprite.Scale = Vector2.One * scale;
    }
    public void ApplyMouthPose(FaceExpression expression)
    {
        if (_mouth == null) return;
        string asset = expression switch
        {
            FaceExpression.Happy => "mouth_smile", FaceExpression.Angry => "mouth_teeth",
            FaceExpression.Sad => "mouth_frown", FaceExpression.Scared => "mouth_open_tiny", _ => "mouth_flat"
        };
        // None means no eating capability, not anatomy. Only decorative flat/smile/frown poses for None.
        if (GameplayMouthSize == MouthSize.None && expression is FaceExpression.Scared or FaceExpression.Angry)
            asset = "mouth_frown";
        if (expression == FaceExpression.Neutral && GameplayMouthSize != MouthSize.None)
            asset = "mouth_open_" + GameplayMouthSize.ToString().ToLowerInvariant();
        _mouth.Texture = PortraitArt.Texture("04_mouths/" + asset);
        float width = GameplayMouthSize switch { MouthSize.Large => 39, MouthSize.Medium => 30, MouthSize.Small => 22, _ => 24 };
        float scale = width / _mouth.Texture.GetWidth();
        _mouth.Scale = Vector2.One * scale;
        _mouth.Position = new Vector2(0, 25);
    }
    public void ApplyEyes(Vector2 gaze, float openness, float slant)
    {
        if (_pupils[0] == null) return;
        Vector2 displacement = gaze.LimitLength(1) * GazeRadius;
        for (int i = 0; i < 2; i++)
        {
            _pupils[i].Position = displacement;
            _pupilMaterials[i].SetShaderParameter("gaze_uv", displacement / new Vector2(68 * WhiteScale, 69 * WhiteScale));
            float sideSlant = i == 0 ? slant : -slant;
            _whiteMaterials[i].SetShaderParameter("openness", openness);
            _pupilMaterials[i].SetShaderParameter("openness", openness);
            _whiteMaterials[i].SetShaderParameter("slant", sideSlant);
            _pupilMaterials[i].SetShaderParameter("slant", sideSlant);
        }
    }
}
