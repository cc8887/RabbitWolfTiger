using Godot;

namespace RabbitWolfTiger.Entities.Portrait;

/// <summary>New native vector components, NOT cropped or reconstructed source artwork.</summary>
public partial class PortraitAccents : Node2D
{
    public Vector2 MuzzleCenter { get; private set; }
    public Vector2 MuzzleSize { get; private set; }
    public bool TigerCheeks { get; private set; }
    public void Configure(PortraitRecipe recipe, bool tiger)
    {
        MuzzleCenter = recipe.Muzzle; MuzzleSize = recipe.MuzzleSize; TigerCheeks = tiger; QueueRedraw();
    }
    public override void _Draw()
    {
        if (MuzzleSize.X > 0)
        {
            DrawSetTransform(MuzzleCenter, 0, MuzzleSize / 2);
            DrawCircle(Vector2.Zero, 1, new Color("fffaf1"));
            DrawSetTransform(Vector2.Zero);
        }
        if (!TigerCheeks) return;
        foreach (float side in new[] { -1f, 1f })
        {
            DrawColoredPolygon(new[] { new Vector2(side * 49, -4), new Vector2(side * 31, 1), new Vector2(side * 49, 7) }, new Color("613718"));
            DrawColoredPolygon(new[] { new Vector2(side * 47, 14), new Vector2(side * 34, 16), new Vector2(side * 45, 24) }, new Color("613718"));
        }
    }
}
