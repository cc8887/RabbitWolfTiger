using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RabbitWolfTiger.Entities.Diet;
using RabbitWolfTiger.Entities.Portrait;
using RabbitWolfTiger.Levels.PortraitGallery;

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
            CheckApertureContract();
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
            foreach (var portrait in portraits)
            {
                foreach (string side in new[] { "LeftEye", "RightEye" })
                {
                    var white = portrait.GetNode<Sprite2D>($"Eyes/{side}/White");
                    var pupil = portrait.GetNode<Sprite2D>($"Eyes/{side}/Pupil");
                    Check(white.Texture != null && pupil.Texture != null && white != pupil, "Independent source eye layers required.");
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
                    if (portrait.GameplayMouthSize == MouthSize.None)
                    {
                        string mouth = portrait.GetNode<Sprite2D>("MouthPose").Texture.ResourcePath;
                        Check(mouth.EndsWith("mouth_flat.png") || mouth.EndsWith("mouth_smile.png") || mouth.EndsWith("mouth_frown.png"), "None must use a decorative closed expression mouth.");
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
                    Check(pupil.Position.Length() <= 3.1001f, "Pupil displacement exceeds its allowed eye radius.");
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
            gallery.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            gallery = galleryScene.Instantiate<PortraitGallery>(); AddChild(gallery);
            foreach (var portrait in gallery.Portraits) { portrait.Animator.SetProcess(false); portrait.Animator.FollowPointer = false; portrait.Animator.AutoBlink = false; Advance(portrait.Animator, 25); }
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
