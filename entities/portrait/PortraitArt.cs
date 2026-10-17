using System.Collections.Generic;
using Godot;
using RabbitWolfTiger.Entities.Diet;

namespace RabbitWolfTiger.Entities.Portrait;

/// <summary>Shared imported resources. No texture allocations during animation.</summary>
public static class PortraitArt
{
    private static readonly Dictionary<string, Texture2D> Cache = new();
    public static Texture2D Texture(string relative)
    {
        if (!Cache.TryGetValue(relative, out var texture))
            Cache[relative] = texture = GD.Load<Texture2D>("res://resources/portrait/art/" + relative + ".png");
        return texture;
    }
    public static string Face(FaceColor color) => "01_faces/" + (color switch
    {
        FaceColor.Cream => "face_rabbit_cream", FaceColor.Gray => "face_wolf_gray",
        FaceColor.Orange => "face_carrot_orange", FaceColor.GoldenOrange => "face_tiger_orange",
        _ => "face_human_skin"
    });
}
