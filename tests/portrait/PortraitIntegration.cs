using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RabbitWolfTiger.Ui.Localization;
using RabbitWolfTiger.Tests.Localization;
using RabbitWolfTiger.Entities.Diet;
using RabbitWolfTiger.Entities.Portrait;
using RabbitWolfTiger.Levels.PortraitGallery;
using RabbitWolfTiger.Levels.PortraitReference;

namespace RabbitWolfTiger.Tests.Portrait;

/// <summary>Deterministic real-node tests. Headless verifies behavior/parameters, never rendered pixels.</summary>
public partial class PortraitIntegration : Node
{
    private int _checks;
    private void Check(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
        _checks++;
    }
    private static IEnumerable<Node> Walk(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren())
            foreach (Node node in Walk(child)) yield return node;
    }
    private static string DietState(DietBody[] bodies) => string.Join(";", bodies.Select(body =>
        $"{body.Face.Color}:{body.Mouth.Size}:{string.Join(',', body.Mouth.EdibleColors)}:" +
        string.Join(',', bodies.Select(body.CanEat))));
    private static ShaderMaterial Material(ModularPortrait portrait, string side, string layer) =>
        (ShaderMaterial)portrait.GetNode<Sprite2D>($"Eyes/{side}/{layer}").Material;
    private static void Advance(FaceAnimator animator, int frames)
    {
        for (int frame = 0; frame < frames; frame++) animator.Advance(0.05f);
    }
    // CPU reference for the checked shader aperture contract, not a GPU pixel test.
    private static float Aperture(float x, float y, float openness, float slant)
    {
        if (openness <= .001f) return 0;
        float upper = .5f - openness * .5f + slant * openness * (x - .5f);
        float lower = .5f + openness * .5f;
        return Mathf.SmoothStep(0, 1, Mathf.Clamp((y - upper) / .025f, 0, 1)) *
            (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((y - lower + .025f) / .025f, 0, 1)));
    }
    private void CheckApertureContract()
    {
        string shader = System.Text.RegularExpressions.Regex.Replace(Godot.FileAccess.GetFileAsString("res://entities/portrait/eye_mask.gdshader"), @"\s+", "");
        Check(shader.Contains("slant*openness*(eye_uv.x-0.5)") && shader.Contains("openness<=0.001?0.0:"), "Shader must preserve the tested closed-aperture contract.");
        for (int tilt = -10; tilt <= 10; tilt++)
        {
            bool closed = true;
            for (int x = 0; x <= 100; x++)
                for (int y = 0; y <= 100; y++)
                    closed &= Aperture(x / 100f, y / 100f, 0, tilt / 20f) == 0 &&
                        Aperture(x / 100f, y / 100f, .001f, tilt / 20f) == 0;
            Check(closed, "Fully closed eyes must have zero aperture at every sampled slant and position.");
        }
    }
    private void CheckHelmetEnvelope(ModularPortrait portrait)
    {
        var helmet = portrait.GetNode<Sprite2D>("HeadFront");
        var material = (ShaderMaterial)helmet.Material;
        Check(helmet.Texture.ResourcePath.EndsWith("helmet.png") && helmet.Texture.GetSize() == new Vector2(80, 87), "Guard must keep the original helmet source.");
        Vector2 WindowParameter(string name)
        {
            Variant parameter = material.GetShaderParameter(name);
            // Headless does not compile shader defaults. Read the actual shader declaration rather than changing production state.
            var match = System.Text.RegularExpressions.Regex.Match(material.Shader.Code,
                @"uniform\s+vec2\s+" + name + @"\s*=\s*vec2\(\s*([-0-9.]+)\s*,\s*([-0-9.]+)\s*\)");
            Check(match.Success, "Missing actual helmet shader window declaration: " + name);
            return parameter.VariantType != Variant.Type.Nil ? parameter.AsVector2() : new Vector2(float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
        }
        Vector2 half = WindowParameter("half_size");
        foreach (Vector2 gaze in new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down, new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1) })
        {
            portrait.Animator.SetGaze(gaze); Advance(portrait.Animator, 30);
            foreach (string side in new[] { "LeftEye", "RightEye" })
            {
                Vector2 center = WindowParameter(side == "LeftEye" ? "left_center" : "right_center");
                foreach (string layer in new[] { "White", "Pupil" })
                {
                    var sprite = portrait.GetNode<Sprite2D>($"Eyes/{side}/{layer}");
                    Rect2 rect = sprite.GetRect();
                    foreach (Vector2 corner in new[] { rect.Position, rect.End, new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.End.X, rect.Position.Y) })
                    {
                        Vector2 pixel = helmet.ToLocal(sprite.ToGlobal(corner)) + helmet.Texture.GetSize() / 2;
                        Vector2 distance = (pixel - center).Abs();
                        Check(distance.X <= half.X - .35f && distance.Y <= half.Y - .35f,
                            $"Guard {side}/{layer} envelope outside helmet opening: pixel={pixel}, center={center}, half={half}, gaze={gaze}");
                    }
                }
            }
        }
    }
    private static string? Option(string name) => OS.GetCmdlineUserArgs()
        .FirstOrDefault(arg => arg.StartsWith(name + "="))?[(name.Length + 1)..];

    public override async void _Ready()
    {
        try
        {
            string? capture = Option("--capture");
            string? framesDirectory = Option("--frames-dir");
            if ((capture != null || framesDirectory != null) &&
                (DisplayServer.GetName() == "headless" || RenderingServer.GetCurrentRenderingMethod() == "dummy"))
                throw new InvalidOperationException("Portrait capture requires an actual rendering display; headless/dummy output is refused.");
            foreach (string? path in new[] { capture, framesDirectory })
                if (path != null) Check(System.IO.Path.IsPathFullyQualified(path), "Capture destinations must be absolute.");
            Check(UiText.Locale == "zh_CN" && UiText.FontPath == "res://resources/fonts/RwtSansSC-Regular.otf", "Default UI must use Simplified Chinese and its bundled font.");
            Check(UiText.Expression(FaceExpression.Neutral) == "中性" && UiText.Expression(FaceExpression.Happy) == "开心" &&
                UiText.Expression(FaceExpression.Angry) == "生气" && UiText.Expression(FaceExpression.Scared) == "害怕" &&
                UiText.Expression(FaceExpression.Sad) == "难过", "All expression labels must be Chinese while enum identity stays unchanged.");
            _checks += ChineseUiChecks.DeclaredGlyphs(UiText.FontPath);
            CheckApertureContract();
            foreach (string asset in new[] { "generated/pupil_black.svg", "generated/mouth_neutral_fork.svg", "generated/mouth_neutral_human.svg",
                "generated/muzzle_double_lobe.svg", "generated/nose_heart_pink.svg", "art/02_eyes/sclera_blank.png" })
            {
                using Image imported = GD.Load<Texture2D>("res://resources/portrait/" + asset).GetImage();
                Check(imported.HasMipmaps(), "Imported eye/facial texture must contain real mip levels: " + asset);
            }
            var catalog = DietCatalog.FromJson(Godot.FileAccess.GetFileAsString("res://resources/diet/initial_diets.json"));
            Species[] species = { Species.Human, Species.Human, Species.Human, Species.Human,
                Species.Rabbit, Species.Wolf, Species.Tiger, Species.Carrot };
            string[] roles = { "Refugee", "Assassin", "King", "Guard", "Rabbit", "Wolf", "Tiger", "Carrot" };
            DietBody[] bodies = species.Select(catalog.Create).ToArray();
            var portraits = new ModularPortrait[8];
            var scene = GD.Load<PackedScene>("res://entities/portrait/modular_portrait.tscn");
            for (int i = 0; i < portraits.Length; i++)
            {
                portraits[i] = scene.Instantiate<ModularPortrait>();
                portraits[i].Position = new Vector2(160 + 320 * (i % 4), 180 + 350 * (i / 4));
                AddChild(portraits[i]);
                portraits[i].Configure(species[i], roles[i], bodies[i]);
                var label = new Label { Text = roles[i], Position = portraits[i].Position + new Vector2(-65, 125) };
                AddChild(label);
                portraits[i].Animator.SetProcess(false);
                portraits[i].Animator.FollowPointer = false;
                portraits[i].Animator.AutoBlink = false;
            }
            Check(portraits.Select(p => p.Animator.TimeUntilBlink).Distinct().Count() == 8 &&
                portraits.Select(p => p.Animator.BlinkInterval).Distinct().Count() == 8, "Eight traveler profiles must have independent blink phases and intervals.");
            int[] firstBlinks = Enumerable.Repeat(-1, 8).ToArray();
            foreach (var portrait in portraits) portrait.Animator.AutoBlink = true;
            for (int frame = 0; frame < 100; frame++)
                for (int i = 0; i < 8; i++)
                {
                    portraits[i].Animator.Advance(.05f);
                    if (firstBlinks[i] < 0 && portraits[i].Animator.BlinkAmount > 0) firstBlinks[i] = frame;
                }
            Check(firstBlinks.All(frame => frame >= 0) && firstBlinks.Distinct().Count() == 8, "Automatic first blinks must occur independently rather than in lockstep.");
            foreach (var portrait in portraits) { portrait.Animator.AutoBlink = false; Advance(portrait.Animator, 10); }
            string baseline = DietState(bodies);
            var materialIds = new HashSet<ulong>();
            var helmetMaterialIds = new HashSet<ulong>();
            foreach (var portrait in portraits)
            {
                var accents = portrait.GetNode<PortraitAccents>("Accents");
                Check(accents.MuzzleCenter == portrait.Recipe.Muzzle && accents.MuzzleSize == portrait.Recipe.MuzzleSize,
                    "Muzzle must use the configured reference recipe.");
                Check(accents.TigerCheeks == (species[Array.IndexOf(portraits, portrait)] == Species.Tiger), "Only the tiger recipe receives cheek markings.");
                if (accents.TigerCheeks) Check(portrait.GetNode<Sprite2D>("Marking").Texture.ResourcePath.EndsWith("cross_mark.png"), "Tiger forehead must use the revised original cross marking.");
                var muzzle = accents.GetNode<Sprite2D>("Muzzle");
                Check(muzzle.Texture.ResourcePath.EndsWith("muzzle_double_lobe.svg") && muzzle.TextureFilter == CanvasItem.TextureFilterEnum.LinearWithMipmaps &&
                    (portrait.Recipe.MuzzleSize.X == 0 || (muzzle.Texture.GetSize() * muzzle.Scale).IsEqualApprox(portrait.Recipe.MuzzleSize)) && muzzle.Position == portrait.Recipe.Muzzle &&
                    muzzle.Visible == (portrait.Recipe.MuzzleSize.X > 0), $"Double-lobed muzzle mismatch role={roles[Array.IndexOf(portraits, portrait)]}, texture={muzzle.Texture.ResourcePath}, filter={muzzle.TextureFilter}, size={muzzle.Texture.GetSize() * muzzle.Scale}, expected={portrait.Recipe.MuzzleSize}, position={muzzle.Position}, expectedPosition={portrait.Recipe.Muzzle}, visible={muzzle.Visible}");
                var nose = portrait.GetNode<Sprite2D>("Nose");
                Check(nose.Texture.ResourcePath.EndsWith("nose_heart_pink.svg") && nose.TextureFilter == CanvasItem.TextureFilterEnum.LinearWithMipmaps &&
                    Math.Abs(nose.Texture.GetWidth() * nose.Scale.X - portrait.Recipe.NoseWidth) < .001f && nose.Position == portrait.Recipe.Nose,
                    "Heart-shaped nose SVG must match its explicit recipe and linear filtering.");
                bool assassin = roles[Array.IndexOf(portraits, portrait)] == "Assassin";
                var faceSprite = portrait.GetNode<Sprite2D>("Face");
                Check(assassin ? faceSprite.Scale.IsEqualApprox(new Vector2(.93f, .68f)) && faceSprite.Position == new Vector2(0, -2) &&
                    portrait.Recipe.EyeY == 1 && portrait.Recipe.Nose == new Vector2(0, 16) && portrait.Recipe.Mouth == new Vector2(0, 22) :
                    faceSprite.Scale.IsEqualApprox(new Vector2(1, portrait.Recipe.FaceScaleY)) && faceSprite.Position == Vector2.Zero,
                    "Face registration must match the species recipe or the explicit Assassin role exception.");
                Check(portrait.AvatarOnly && !portrait.GetNode<Sprite2D>("Body").Visible && !portrait.GetNode<Sprite2D>("Equipment").Visible,
                    "Reference-first avatars must hide body and equipment by default.");
                if (roles[Array.IndexOf(portraits, portrait)] == "Guard") CheckHelmetEnvelope(portrait);
                foreach (string side in new[] { "LeftEye", "RightEye" })
                {
                    var white = portrait.GetNode<Sprite2D>($"Eyes/{side}/White");
                    var pupil = portrait.GetNode<Sprite2D>($"Eyes/{side}/Pupil");
                    Check(white.Texture != null && pupil.Texture != null && white != pupil, "Independent source eye layers required.");
                    Check(white.TextureFilter == CanvasItem.TextureFilterEnum.LinearWithMipmaps && pupil.TextureFilter == CanvasItem.TextureFilterEnum.LinearWithMipmaps, "Both independent eye layers must explicitly use mipmapped linear filtering.");
                    Check(pupil.Texture!.ResourcePath.EndsWith("pupil_black.svg"), "Registered eye must use the newly identified black pupil component.");
                    var eye = portrait.GetNode<Node2D>($"Eyes/{side}");
                    Check(eye.Position.IsEqualApprox(new Vector2(side == "LeftEye" ? -portrait.Recipe.EyeX : portrait.Recipe.EyeX, portrait.Recipe.EyeY)), "Configured eyes must use the species-specific registration recipe.");
                    Check(Math.Abs(white.Texture!.GetWidth() * white.Scale.X - portrait.Recipe.WhiteWidth) < .001f &&
                        Math.Abs(pupil.Texture.GetWidth() * pupil.Scale.X - portrait.Recipe.PupilWidth) < .001f,
                        "Independent eye layers must match their reference recipe widths.");
                    Vector2 relative = pupil.Texture.GetSize() * pupil.Scale / (white.Texture.GetSize() * white.Scale);
                    Check(Material(portrait, side, "Pupil").GetShaderParameter("relative_size").AsVector2().IsEqualApprox(relative), "Pupil clipping transform must match the newly registered eye layers.");
                    foreach (var material in new[] { Material(portrait, side, "White"), Material(portrait, side, "Pupil") })
                        Check(materialIds.Add(material.GetInstanceId()), "Every portrait/eye/layer requires its own material state.");
                    Check(Material(portrait, side, "Pupil").GetShaderParameter("pupil_layer").AsBool(), "Pupil clipping must be enabled.");
                    Check(Material(portrait, side, "Pupil").GetShaderParameter("sclera_mask").AsGodotObject() == white.Texture, "Pupil must use the matching real sclera mask.");
                }
                foreach (FaceExpression expression in Enum.GetValues<FaceExpression>())
                {
                    var animator = portrait.Animator;
                    animator.SetExpression(expression);
                    Advance(animator, 20);
                    Check(animator.Expression == expression, "Expression must remain selected after transition.");
                    Check(portrait.GetNode<Sprite2D>("MouthPose").Visible && portrait.GetNode<Sprite2D>("MouthPose").Texture != null, "All expressions need a visible source mouth texture.");
                    float neutralWidth = portrait.GameplayMouthSize switch { MouthSize.Large => 18, MouthSize.Medium => 14, _ => 10 };
                    float expressionWidth = (portrait.GameplayMouthSize switch { MouthSize.Large => 22, MouthSize.Medium => 16, _ => 10 }) * 1.35f;
                    var mouthSprite = portrait.GetNode<Sprite2D>("MouthPose");
                    Check(mouthSprite.TextureFilter == CanvasItem.TextureFilterEnum.LinearWithMipmaps, "All expression mouth textures must explicitly use mipmapped filtering.");
                    Check(Math.Abs(mouthSprite.Texture.GetWidth() * mouthSprite.Scale.X - (expression == FaceExpression.Neutral ? neutralWidth : expressionWidth)) < .001f,
                        "Mouth visual width must match the explicit new reference registration, independently of eligibility.");
                    if (expression == FaceExpression.Neutral)
                    {
                        float height = portrait.GameplayMouthSize switch { MouthSize.Large => 8, MouthSize.Medium => 7, _ => 6 };
                        Check(Math.Abs(mouthSprite.Texture.GetHeight() * mouthSprite.Scale.Y - height) < .001f,
                            "Neutral mouth height must preserve the revised 6/7/8 registration.");
                    }
                    if (portrait.GameplayMouthSize == MouthSize.None)
                    {
                        string mouth = portrait.GetNode<Sprite2D>("MouthPose").Texture.ResourcePath;
                        string neutral = portrait.Recipe.Animal ? "mouth_neutral_fork.svg" : "mouth_neutral_human.svg";
                        Check(expression == FaceExpression.Neutral ? mouth.EndsWith(neutral) :
                            mouth.EndsWith("mouth_flat.png") || mouth.EndsWith("mouth_smile.png") || mouth.EndsWith("mouth_frown.png"),
                            "None must use the registered neutral decoration or a non-predatory expression mouth.");
                    }
                    Check(DietState(bodies) == baseline, "Expression animation must not alter food rules.");
                }
                var a = portrait.Animator;
                a.SetGaze(new Vector2(1000, -1000));
                Advance(a, 30);
                Check(a.TargetGaze.Length() <= 1.0001f && a.Gaze.Length() <= 1.0001f, "Extreme gaze must clamp to unit radius.");
                foreach (string side in new[] { "LeftEye", "RightEye" })
                {
                    var pupil = portrait.GetNode<Sprite2D>($"Eyes/{side}/Pupil");
                    var white = portrait.GetNode<Sprite2D>($"Eyes/{side}/White");
                    Vector2 availableMargin = (white.Texture.GetSize() * white.Scale - pupil.Texture.GetSize() * pupil.Scale) / 2;
                    Check(portrait.GazeRadius > 0 && portrait.GazeRadius <= Math.Min(availableMargin.X, availableMargin.Y), "Gaze radius must fit the eye layers' actual geometric margin.");
                    Check(pupil.Position.Length() <= portrait.GazeRadius + .0001f && Math.Abs(pupil.Position.Length() - portrait.GazeRadius) < .01f,
                        "Extreme gaze must reach, but not exceed, the registered safe radius.");
                    Vector2 expected = pupil.Position / (white.Texture.GetSize() * white.Scale);
                    Check(Material(portrait, side, "Pupil").GetShaderParameter("gaze_uv").AsVector2().IsEqualApprox(expected), "Mask coordinates must track actual pupil displacement.");
                }
                a.SetExpression(FaceExpression.Neutral); Advance(a, 20);
                a.Blink(); Advance(a, 2);
                Check(a.BlinkAmount > .9f, "Explicit blink must close the eyes at mid-cycle.");
                a.AnimationPaused = true;
                var pausedGaze = a.Gaze; float pausedBlink = a.BlinkAmount; float pausedOpen = a.Openness;
                Advance(a, 20);
                Check(a.Gaze == pausedGaze && a.BlinkAmount == pausedBlink && a.Openness == pausedOpen, "Pause must freeze animation channels.");
                a.AnimationPaused = false; Advance(a, 5);
                Check(a.BlinkAmount == 0, "Blink must finish after resume.");
                a.SetExpression(FaceExpression.Happy); a.SetExpression(FaceExpression.Angry); a.SetExpression(FaceExpression.Sad);
                Advance(a, 40);
                Check(a.Expression == FaceExpression.Sad && Math.Abs(a.Openness - .62f) < .01f && a.BlinkAmount == 0, "Rapid changes must settle to the last expression without stale reset.");
                var stableGaze = a.Gaze;
                foreach (string part in new[] { "crown", "helmet", "headscarf", "leaves", "" })
                {
                    portrait.SetHeadPart(part);
                    Check(portrait.HeadPart == part && a.Expression == FaceExpression.Sad && a.Gaze == stableGaze, "Head-part replacement must preserve animation state.");
                    var head = portrait.GetNode<Sprite2D>("HeadFront");
                    if (part == "helmet")
                    {
                        Check(head.Material is ShaderMaterial && helmetMaterialIds.Add(head.Material!.GetInstanceId()), "Helmet cutout material must be independent for each portrait.");
                        ulong identity = head.Material!.GetInstanceId();
                        portrait.SetHeadPart("crown"); Check(head.Material == null, "Leaving helmet must clear its cutout material.");
                        portrait.SetHeadPart("helmet"); Check(head.Material!.GetInstanceId() == identity, "Re-equipping helmet must reuse that portrait's material.");
                    }
                    else Check(head.Material == null, "Non-helmet head parts must not retain the helmet cutout material.");
                }
                var alternate = new DietBody(new FaceComponent(FaceColor.Gray), new MouthComponent(MouthSize.Large,
                    new[] { FaceColor.Skin, FaceColor.Cream, FaceColor.Orange }));
                portrait.ProjectGameplay(alternate);
                Check(portrait.DisplayedFace == FaceColor.Gray && portrait.GameplayMouthSize == MouthSize.Large &&
                    a.Expression == FaceExpression.Sad && a.Gaze == stableGaze, "Face/mouth projection must preserve expression and gaze.");
                Check(DietState(bodies) == baseline, "Visual projection must not mutate original gameplay bodies.");
                // After warming expressions, time advancement must reuse the same nodes, textures and materials.
                ulong[] nodeIds = Walk(portrait).Select(n => n.GetInstanceId()).ToArray();
                ulong[] textureIds = Walk(portrait).OfType<Sprite2D>().Select(n => n.Texture?.GetInstanceId() ?? 0).ToArray();
                ulong[] stableMaterials = Walk(portrait).OfType<Sprite2D>().Select(n => n.Material?.GetInstanceId() ?? 0).ToArray();
                Advance(a, 240);
                Check(nodeIds.SequenceEqual(Walk(portrait).Select(n => n.GetInstanceId())), "Animation must reuse its node tree.");
                Check(textureIds.SequenceEqual(Walk(portrait).OfType<Sprite2D>().Select(n => n.Texture?.GetInstanceId() ?? 0)), "Animation must reuse loaded textures.");
                Check(stableMaterials.SequenceEqual(Walk(portrait).OfType<Sprite2D>().Select(n => n.Material?.GetInstanceId() ?? 0)), "Animation must reuse material instances.");
                Check(DietState(bodies) == baseline, "Time advancement must not alter diet or predation results.");
            }
            foreach (DietBody body in bodies.Where(body => body.Mouth.Size == MouthSize.None))
                foreach (FaceColor color in Enum.GetValues<FaceColor>())
                    Check(!body.Mouth.CanEat(new FaceComponent(color)), "None with decorative expressions cannot eat any face color.");
            // Deliberately mutate one shader instance and verify every other portrait remains isolated.
            var first = Material(portraits[0], "LeftEye", "Pupil");
            var other = Material(portraits[0], "RightEye", "Pupil");
            float before = other.GetShaderParameter("openness").AsSingle();
            first.SetShaderParameter("openness", .123f);
            Check(other.GetShaderParameter("openness").AsSingle() == before, "Left-eye material writes must not leak into the right eye.");
            for (int i = 0; i < portraits.Length; i++)
            {
                portraits[i].Configure(species[i], roles[i], bodies[i]);
                portraits[i].Animator.SetExpression(FaceExpression.Neutral); Advance(portraits[i].Animator, 30);
            }
            foreach (Node child in GetChildren()) child.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var galleryScene = GD.Load<PackedScene>("res://levels/portrait_gallery/portrait_gallery.tscn");
            var gallery = galleryScene.Instantiate<PortraitGallery>(); AddChild(gallery);
            Check(gallery.Portraits.Length == 8 && gallery.Bodies.Length == 8, "Actual gallery must contain eight travelers.");
            string galleryDiet = DietState(gallery.Bodies);
            foreach (var portrait in gallery.Portraits) { portrait.Animator.FollowPointer = false; portrait.Animator.AutoBlink = false; }
            foreach (FaceExpression expression in Enum.GetValues<FaceExpression>())
            {
                gallery.GetNode<Button>("Expression" + expression).EmitSignal(Button.SignalName.Pressed);
                Check(gallery.Portraits.All(p => p.Animator.Expression == expression), "Gallery expression button must affect all selected portraits.");
                Check(DietState(gallery.Bodies) == galleryDiet, "Gallery expression buttons must not change diets.");
            }
            gallery.GetNode<Button>("Select0").EmitSignal(Button.SignalName.Pressed);
            gallery.GetNode<Button>("ExpressionHappy").EmitSignal(Button.SignalName.Pressed);
            Check(gallery.Portraits[0].Animator.Expression == FaceExpression.Happy && gallery.Portraits.Skip(1).All(p => p.Animator.Expression == FaceExpression.Sad), "Single selection must not animate other travelers.");
            var expressions = gallery.Portraits.Select(p => p.Animator.Expression).ToArray();
            foreach (string button in new[] { "SwapFaces", "SwapMouths", "SwapHeadParts" })
            {
                gallery.GetNode<Button>(button).EmitSignal(Button.SignalName.Pressed);
                Check(expressions.SequenceEqual(gallery.Portraits.Select(p => p.Animator.Expression)), "Gallery part swaps must preserve each expression.");
                for (int i = 0; i < 8; i++)
                    Check(gallery.Portraits[i].DisplayedFace == gallery.Bodies[i].Face.Color && gallery.Portraits[i].GameplayMouthSize == gallery.Bodies[i].Mouth.Size, "Swapped visual and gameplay parts must agree.");
            }
            gallery.GetNode<Button>("Pause").EmitSignal(Button.SignalName.Pressed);
            Check(gallery.Portraits.All(p => p.Animator.AnimationPaused), "Gallery pause button must pause all animators.");
            gallery.GetNode<Button>("Pause").EmitSignal(Button.SignalName.Pressed);
            Check(gallery.Portraits.All(p => !p.Animator.AnimationPaused), "Gallery resume button must resume all animators.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (Label label in gallery.GetChildren().OfType<Label>())
                Check(label.GetGlobalRect().End.X <= 1280 && label.GetGlobalRect().End.Y <= 720, "Gallery text extends outside viewport.");
            // Exhaust every face/mouth template combination using real font metrics and card geometry.
            for (int faceCycle = 0; faceCycle < 5; faceCycle++)
            {
                gallery.SwapFaces();
                for (int mouthCycle = 0; mouthCycle < 5; mouthCycle++)
                {
                    gallery.SwapMouths();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    _checks += ChineseUiChecks.Run(gallery, UiText.FontPath);
                    for (int card = 0; card < 8; card++)
                    {
                        Label detail = gallery.GetNode<Label>($"Details{card}");
                        Vector2 cardEnd = new Vector2(28 + card % 4 * 310 + 292, 140 + card / 4 * 250 + 240);
                        Rect2 actual = detail.GetGlobalRect();
                        Vector2 minimumEnd = detail.GlobalPosition + detail.GetCombinedMinimumSize();
                        Check(actual.End.X <= cardEnd.X && actual.End.Y <= cardEnd.Y &&
                            minimumEnd.X <= cardEnd.X && minimumEnd.Y <= cardEnd.Y,
                            $"Card {card} text overflow: '{detail.Text}' rect={actual}; minimum={detail.GetCombinedMinimumSize()}; cardEnd={cardEnd}");
                    }
                }
            }
            gallery.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var reference = GD.Load<PackedScene>("res://levels/portrait_reference/portrait_reference.tscn").Instantiate<PortraitReference>();
            AddChild(reference);
            Check(reference.Portraits.Length == 8, "Reference QA must contain eight live modular portraits.");
            for (int i = 0; i < reference.Portraits.Length; i++)
            {
                var portrait = reference.Portraits[i];
                var animator = portrait.Animator;
                Check(animator.Expression == FaceExpression.Neutral && animator.AnimationPaused && !animator.FollowPointer &&
                    animator.Gaze == Vector2.Zero && animator.TargetGaze == Vector2.Zero && animator.Openness == 1 && animator.BlinkAmount == 0,
                    "Reference QA must freeze a neutral, centered, fully open pose.");
                Advance(animator, 60);
                Check(animator.Gaze == Vector2.Zero && animator.Openness == 1 && animator.BlinkAmount == 0, "Reference QA must remain frozen over time.");
                Check(reference.GetNode<Sprite2D>($"Reference{i}").Texture != null, "Source reference crop must load separately.");
                Check(Walk(portrait).OfType<Sprite2D>().All(sprite => sprite.Texture == null || !sprite.Texture.ResourcePath.Contains("/reference/")),
                    "Live avatar must not substitute a whole reference image for its independent rig.");
            }
            var hood = reference.Portraits[5].GetNode<Sprite2D>("HoodFrame");
            Check(hood.Texture.ResourcePath.EndsWith("hood.png") && hood.Modulate == Colors.White && hood.SelfModulate == Colors.White && hood.Material is ShaderMaterial,
                "Assassin must keep the original hood texture/color with a separate opening mask.");
            _checks += ChineseUiChecks.Run(reference, UiText.FontPath);
            reference.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            gallery = galleryScene.Instantiate<PortraitGallery>(); AddChild(gallery);
            foreach (var portrait in gallery.Portraits) { portrait.Animator.SetProcess(false); portrait.Animator.FollowPointer = false; portrait.Animator.AutoBlink = false; Advance(portrait.Animator, 25); }
            _checks += ChineseUiChecks.Run(gallery, UiText.FontPath);
            await Capture(gallery.Portraits, capture, framesDirectory, gallery.SetExpression);
            GD.Print($"PASS: {_checks} portrait integration checks (real nodes and shader parameters; headless does not verify rendered pixels).");
            GetTree().Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); GetTree().Quit(1); }
    }
    private async Task Capture(ModularPortrait[] portraits, string? path, string? directory, Action<FaceExpression> setExpression)
    {
        if (path == null && directory == null) return;
        async Task Save(string destination)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image image = GetViewport().GetTexture().GetImage();
            Check(!image.IsEmpty() && image.GetWidth() == 1280 && image.GetHeight() == 720, "Capture must be a rendered 1280x720 image.");
            Check(image.SavePng(destination) == Error.Ok, "Could not save rendered PNG.");
        }
        if (path != null) { await Save(path); GD.Print($"PORTRAIT_CAPTURE: {path}"); }
        if (directory != null)
        {
            System.IO.Directory.CreateDirectory(directory);
            for (int frame = 0; frame < 60; frame++)
            {
                if (frame % 12 == 0) setExpression(Enum.GetValues<FaceExpression>()[frame / 12]);
                foreach (var portrait in portraits)
                {
                    portrait.Animator.SetGaze(new Vector2(Mathf.Sin(frame * .5f), -.5f));
                    if (frame % 12 == 6) portrait.Animator.Blink();
                    portrait.Animator.Advance(1f / 24f);
                    portrait.Animator.Advance(1f / 24f);
                }
                await Save(System.IO.Path.Combine(directory, $"portrait-{frame:D3}.png"));
                if (frame % 12 == 11) await Save(System.IO.Path.Combine(directory, $"expression-{Enum.GetValues<FaceExpression>()[frame / 12]}.png"));
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "sequence.json"),
                "{\"fps\":12,\"frames\":60,\"durationSeconds\":5,\"clock\":\"controlled animation: two 1/24s advances per sample; _Process disabled\",\"content\":\"five expressions, gaze, and explicit blinks\"}");
            GD.Print($"PORTRAIT_FRAMES: {directory} (60 real rendered frames, controlled 12fps/5s animation)");
        }
    }
}
