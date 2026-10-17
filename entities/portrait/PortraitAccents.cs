using Godot;

namespace RabbitWolfTiger.Entities.Portrait;

/// <summary>New native vector components, NOT cropped or reconstructed source artwork.</summary>
public partial class PortraitAccents : Node2D
{
    private static Texture2D? _muzzleTexture;
    public Vector2 MuzzleCenter { get; private set; }
    public Vector2 MuzzleSize { get; private set; }
    public bool TigerCheeks { get; private set; }
    public void Configure(PortraitRecipe recipe, bool tiger)
    {
        MuzzleCenter = recipe.Muzzle; MuzzleSize = recipe.MuzzleSize; TigerCheeks = tiger;
        var muzzle = GetNode<Sprite2D>("Muzzle");
        _muzzleTexture ??= GD.Load<Texture2D>("res://resources/portrait/generated/muzzle_double_lobe.svg");
        muzzle.TextureFilter = TextureFilterEnum.Linear;
        muzzle.Texture = _muzzleTexture; muzzle.Position = MuzzleCenter;
        muzzle.Scale = MuzzleSize / _muzzleTexture.GetSize(); muzzle.Visible = MuzzleSize.X > 0;
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (!TigerCheeks) return;
        foreach (float side in new[] { -1f, 1f })
        {
            DrawColoredPolygon(new[] { new Vector2(side * 49, -4), new Vector2(side * 31, 1), new Vector2(side * 49, 7) }, new Color("613718"));
            DrawColoredPolygon(new[] { new Vector2(side * 47, 14), new Vector2(side * 34, 16), new Vector2(side * 45, 24) }, new Color("613718"));
        }
    }
}
