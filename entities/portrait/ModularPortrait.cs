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
    private readonly Sprite2D[] _whites = new Sprite2D[2];
    private readonly ShaderMaterial[] _whiteMaterials = new ShaderMaterial[2];
    private readonly ShaderMaterial[] _pupilMaterials = new ShaderMaterial[2];
    private readonly Node2D[] _eyes = new Node2D[2];
    private static Shader? _eyeShader;
    public PortraitRecipe Recipe { get; private set; } = PortraitRecipe.For(Species.Human);
    public float GazeRadius => 2.5f;
    public bool AvatarOnly { get; private set; } = true;
    private Species _species;
    private string _role = "";
    private static Texture2D? _blackPupil, _animalNeutral, _humanNeutral, _pinkNose;
    private static Shader? _hoodShader;

    public override void _Ready()
    {
        _face = GetNode<Sprite2D>("Face"); _mouth = GetNode<Sprite2D>("MouthPose");
        _head = GetNode<Sprite2D>("HeadFront");
        Animator = GetNode<FaceAnimator>("FaceAnimator");
        _eyeShader ??= GD.Load<Shader>("res://entities/portrait/eye_mask.gdshader");
        var white = PortraitArt.Texture("02_eyes/sclera_blank");
        var pupil = _blackPupil ??= GD.Load<Texture2D>("res://resources/portrait/generated/pupil_black.svg");
        for (int side = 0; side < 2; side++)
        {
            _eyes[side] = GetNode<Node2D>(side == 0 ? "Eyes/LeftEye" : "Eyes/RightEye");
            var sclera = _eyes[side].GetNode<Sprite2D>("White");
            _whites[side] = sclera;
            _pupils[side] = _eyes[side].GetNode<Sprite2D>("Pupil");
            sclera.Texture = white; sclera.Scale = Vector2.One * (Recipe.WhiteWidth / white.GetWidth());
            _pupils[side].TextureFilter = TextureFilterEnum.Linear;
            _pupils[side].Texture = pupil; _pupils[side].Scale = Vector2.One * (Recipe.PupilWidth / pupil.GetWidth());
            _whiteMaterials[side] = CreateEyeMaterial(white, false);
            _pupilMaterials[side] = CreateEyeMaterial(white, true);
            _pupilMaterials[side].SetShaderParameter("relative_size", pupil.GetSize() * _pupils[side].Scale / (white.GetSize() * sclera.Scale));
            sclera.Material = _whiteMaterials[side]; _pupils[side].Material = _pupilMaterials[side];
        }
        _pinkNose ??= GD.Load<Texture2D>("res://resources/portrait/generated/nose_heart_pink.svg");
        GetNode<Sprite2D>("Nose").Texture = _pinkNose;
        GetNode<Sprite2D>("Nose").TextureFilter = TextureFilterEnum.Linear;
        _mouth.TextureFilter = TextureFilterEnum.Linear;
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
        _species = species; _role = role; Recipe = PortraitRecipe.For(species);
        foreach (string slot in new[] { "Body", "HeadBack", "EarLeft", "EarRight", "HeadFront", "Hair", "Marking", "Equipment", "HoodFrame" })
        {
            var sprite = GetNode<Sprite2D>(slot); sprite.Texture = null; sprite.Material = null;
        }
        _face.Material = null;
        _face.Position = Vector2.Zero;
        _face.Scale = new Vector2(1, Recipe.FaceScaleY);
        GetNode<Sprite2D>("Hair").Visible = true;
        GetNode<PortraitAccents>("Accents").Configure(Recipe, species == Species.Tiger);
        HeadPart = "";
        if (species is Species.Rabbit or Species.Wolf or Species.Tiger)
        {
            string animal = species.ToString().ToLowerInvariant();
            Part("EarLeft", $"05_head_parts/{animal}_ear_left", new Vector2(0, Recipe.EarY), Recipe.EarScale);
            Part("EarRight", $"05_head_parts/{animal}_ear_right", new Vector2(0, Recipe.EarY), Recipe.EarScale);
            if (species == Species.Tiger) Part("Marking", "06_markings/king_mark", new Vector2(0, -31), .53f);
        }
        if (species == Species.Carrot) SetHeadPart("leaves");
        if (species == Species.Human)
        {
            if (role != "Assassin" && role != "Guard") Part("Hair", "05_head_parts/hair", new Vector2(0, -46), 1.08f);
            switch (role)
            {
                case "Refugee": SetHeadPart("headscarf"); break;
                case "Assassin":
                    // Slim shell, tucked chin and a short source-hair fringe visible through the aperture.
                    Recipe = Recipe with { EyeY = 1, Nose = new Vector2(0, 16), Mouth = new Vector2(0, 26) };
                    _face.Scale = new Vector2(.93f, .68f); _face.Position = new Vector2(0, -2);
                    Part("Hair", "05_head_parts/hair", new Vector2(0, -39), .9f);
                    Part("HoodFrame", "05_head_parts/hood", new Vector2(0, -31), 1);
                    GetNode<Sprite2D>("HoodFrame").Scale = new Vector2(1.5f, 1.8f);
                    _hoodShader ??= GD.Load<Shader>("res://entities/portrait/hood_opening.gdshader");
                    var shell = new ShaderMaterial { Shader = _hoodShader };
                    shell.SetShaderParameter("opening_center", new Vector2(.5f, .74f));
                    shell.SetShaderParameter("opening_radius", new Vector2(.31f, .22f));
                    GetNode<Sprite2D>("HoodFrame").Material = shell;
                    var faceMask = new ShaderMaterial { Shader = _hoodShader };
                    faceMask.SetShaderParameter("keep_inside", true);
                    faceMask.SetShaderParameter("opening_center", new Vector2(.5f, .605f));
                    faceMask.SetShaderParameter("opening_radius", new Vector2(.435f, .389f));
                    _face.Material = faceMask;
                    Part("Body", "07_body_equipment/cloak", new Vector2(0, 75), 1.2f);
                    Part("Equipment", "07_body_equipment/dagger", new Vector2(53, 70), .7f); break;
                case "King": SetHeadPart("crown"); Part("Body", "07_body_equipment/cape", new Vector2(0, 75), 1.1f); break;
                case "Guard": SetHeadPart("helmet"); Part("Body", "07_body_equipment/armor", new Vector2(0, 75), 1.15f);
                    Part("Equipment", "07_body_equipment/spear", new Vector2(62, 55), .9f); break;
            }
            if (role == "Refugee") Part("Body", "07_body_equipment/robe", new Vector2(0, 75), 1f);
        }
        ApplyRecipe();
        SetAvatarOnly(AvatarOnly);
        Animator.SetBlinkPhase(species switch
        {
            Species.Rabbit => 0, Species.Wolf => 1, Species.Tiger => 2, Species.Carrot => 3,
            _ => role switch { "Refugee" => 4, "Assassin" => 5, "King" => 6, "Guard" => 7, _ => 8 }
        });
        ProjectGameplay(body);
    }
    private void ApplyRecipe()
    {
        for (int side = 0; side < 2; side++)
        {
            _eyes[side].Position = new Vector2(side == 0 ? -Recipe.EyeX : Recipe.EyeX, Recipe.EyeY);
            var white = _eyes[side].GetNode<Sprite2D>("White");
            white.Scale = Vector2.One * (Recipe.WhiteWidth / white.Texture.GetWidth());
            _pupils[side].Scale = Vector2.One * (Recipe.PupilWidth / _pupils[side].Texture.GetWidth());
            _pupilMaterials[side].SetShaderParameter("relative_size", _pupils[side].Texture.GetSize() * _pupils[side].Scale / (white.Texture.GetSize() * white.Scale));
        }
        var nose = GetNode<Sprite2D>("Nose");
        nose.Position = Recipe.Nose; nose.Scale = Vector2.One * (Recipe.NoseWidth / nose.Texture.GetWidth());
    }
    public void SetAvatarOnly(bool avatarOnly)
    {
        AvatarOnly = avatarOnly;
        GetNode<Sprite2D>("Body").Visible = !avatarOnly;
        GetNode<Sprite2D>("Equipment").Visible = !avatarOnly;
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
        _head.Position = new Vector2(0, name == "headscarf" ? -48 : name == "helmet" ? -63 : name == "crown" ? -69 : -49);
        _head.Scale = Vector2.One * (name == "helmet" ? 1.35f : name == "headscarf" ? 1.10f : name == "leaves" ? .98f : .93f);
        if (_species == Species.Human && _role != "Assassin") GetNode<Sprite2D>("Hair").Visible = name != "helmet" && name != "headscarf";
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
        // Neutral lips are a compact visual pose; size still has three clearly graded widths.
        // None is a decorative mouth, never an eating capability.
        if (expression == FaceExpression.Neutral)
        {
            _animalNeutral ??= GD.Load<Texture2D>("res://resources/portrait/generated/mouth_neutral_fork.svg");
            _humanNeutral ??= GD.Load<Texture2D>("res://resources/portrait/generated/mouth_neutral_human.svg");
            _mouth.Texture = Recipe.Animal ? _animalNeutral : _humanNeutral;
        }
        else
        {
            if (GameplayMouthSize == MouthSize.None && expression is FaceExpression.Scared or FaceExpression.Angry)
                asset = "mouth_frown";
            _mouth.Texture = PortraitArt.Texture("04_mouths/" + asset);
        }
        float width = GameplayMouthSize switch { MouthSize.Large => 22, MouthSize.Medium => 16, MouthSize.Small => 10, _ => 10 };
        if (expression != FaceExpression.Neutral) width *= 1.35f;
        else width = GameplayMouthSize switch { MouthSize.Large => 18, MouthSize.Medium => 14, _ => 10 };
        _mouth.Scale = Vector2.One * (width / _mouth.Texture.GetWidth());
        if (expression == FaceExpression.Neutral)
        {
            float height = GameplayMouthSize switch { MouthSize.Large => 8, MouthSize.Medium => 7, _ => 6 };
            _mouth.Scale = new Vector2(width / _mouth.Texture.GetWidth(), height / _mouth.Texture.GetHeight());
        }
        _mouth.Position = Recipe.Mouth;
    }
    public void ApplyEyes(Vector2 gaze, float openness, float slant)
    {
        if (_pupils[0] == null) return;
        Vector2 displacement = gaze.LimitLength(1) * GazeRadius;
        for (int i = 0; i < 2; i++)
        {
            _pupils[i].Position = displacement;
            _pupilMaterials[i].SetShaderParameter("gaze_uv", displacement / (_whites[i].Texture.GetSize() * _whites[i].Scale));
            float sideSlant = i == 0 ? slant : -slant;
            _whiteMaterials[i].SetShaderParameter("openness", openness);
            _pupilMaterials[i].SetShaderParameter("openness", openness);
            _whiteMaterials[i].SetShaderParameter("slant", sideSlant);
            _pupilMaterials[i].SetShaderParameter("slant", sideSlant);
        }
    }
}
