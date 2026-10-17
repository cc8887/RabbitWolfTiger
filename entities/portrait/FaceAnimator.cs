using System;
using Godot;

namespace RabbitWolfTiger.Entities.Portrait;

public enum FaceExpression { Neutral, Happy, Angry, Scared, Sad }

/// <summary>One writer for eye pose; expression, gaze and blink are independent channels.</summary>
public partial class FaceAnimator : Node
{
    public FaceExpression Expression { get; private set; }
    public Vector2 Gaze { get; private set; }
    public Vector2 TargetGaze { get; private set; }
    public bool FollowPointer { get; set; } = true;
    public bool AnimationPaused { get; set; }
    public bool AutoBlink { get; set; } = true;
    public float BlinkAmount { get; private set; }
    public float Openness { get; private set; } = 1;
    private ModularPortrait _portrait = null!;
    private float _blinkTime = -1;
    private float _nextBlink = 2.7f;
    public float BlinkInterval { get; private set; } = 2.7f;
    public float TimeUntilBlink => _nextBlink;
    public void SetBlinkPhase(int seed)
    {
        int phase = Math.Abs(seed % 17);
        _nextBlink = 0.9f + phase * 0.29f;
        BlinkInterval = 2.5f + phase * 0.23f;
    }
    private float _expressionHold;
    private float _targetOpen = 1;
    private float _slant;
    private float _targetSlant;

    public override void _Ready() => _portrait = GetParent<ModularPortrait>();
    public void SetExpression(FaceExpression expression)
    {
        if (!Enum.IsDefined(expression)) throw new ArgumentOutOfRangeException(nameof(expression));
        Expression = expression;
        (_targetOpen, _targetSlant) = expression switch
        {
            FaceExpression.Happy => (0.7f, -0.10f), FaceExpression.Angry => (0.58f, 0.40f),
            FaceExpression.Scared => (1f, 0f), FaceExpression.Sad => (0.62f, -0.32f), _ => (1f, 0f)
        };
        // New expression cancels an idle blink rather than competing for the same aperture.
        _blinkTime = -1; BlinkAmount = 0; _expressionHold = 0.35f;
        if (_portrait != null) { _portrait.ApplyMouthPose(expression); Apply(); }
    }
    public void SetGaze(Vector2 normalized) => TargetGaze = normalized.LimitLength(1);
    public void Blink()
    {
        if (!AnimationPaused && _expressionHold <= 0 && _blinkTime < 0) _blinkTime = 0;
    }
    public override void _Process(double delta) => Advance((float)delta);
    public void Advance(float delta)
    {
        if (AnimationPaused || _portrait == null || !IsInsideTree()) return;
        float dt = Mathf.Clamp(delta, 0, 0.05f);
        if (FollowPointer) SetGaze((_portrait.GetLocalMousePosition() - new Vector2(0, -12)) / 120f);
        float blend = 1 - Mathf.Exp(-14 * dt);
        Gaze = Gaze.Lerp(TargetGaze, blend);
        Openness = Mathf.Lerp(Openness, _targetOpen, blend);
        _slant = Mathf.Lerp(_slant, _targetSlant, blend);
        _expressionHold = Mathf.Max(0, _expressionHold - dt);
        if (_blinkTime >= 0)
        {
            _blinkTime += dt;
            BlinkAmount = Mathf.Sin(Mathf.Min(_blinkTime / 0.20f, 1) * Mathf.Pi);
            if (_blinkTime >= 0.20f) { _blinkTime = -1; BlinkAmount = 0; _nextBlink = BlinkInterval; }
        }
        else if (AutoBlink && _expressionHold <= 0)
        {
            _nextBlink -= dt;
            if (_nextBlink <= 0) Blink();
        }
        Apply();
    }
    public void RefreshVisuals()
    {
        if (_portrait == null) return;
        _portrait.ApplyMouthPose(Expression); Apply();
    }
    private void Apply() => _portrait.ApplyEyes(Gaze, Openness * (1 - BlinkAmount), _slant);
    public override void _ExitTree() { _blinkTime = -1; BlinkAmount = 0; }
}
