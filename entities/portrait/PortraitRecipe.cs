using Godot;
using RabbitWolfTiger.Entities.Diet;

namespace RabbitWolfTiger.Entities.Portrait;

/// <summary>Visual registration estimates from the supplied reference, never gameplay species rules.</summary>
public sealed record PortraitRecipe(float EyeX, float EyeY, float WhiteWidth, float PupilWidth,
    Vector2 Nose, float NoseWidth, Vector2 Muzzle, Vector2 MuzzleSize, Vector2 Mouth,
    float EarY, float EarScale, bool Animal)
{
    public float FaceScaleY { get; init; } = .85f;
    public static PortraitRecipe For(Species species) => species switch
    {
        Species.Rabbit => new(23, 5, 23, 14, new(0, 19), 14, new(0, 26), new(42, 23), new(0, 26), -53, .9f, true) { FaceScaleY = .76f },
        Species.Wolf => new(23, 4, 24, 14, new(0, 18), 14, new(0, 26), new(46, 25), new(0, 25), -50, 1.06f, true) { FaceScaleY = .78f },
        Species.Tiger => new(23, 4, 24, 14, new(0, 18), 15, new(0, 26), new(46, 25), new(0, 26), -46, 1f, true) { FaceScaleY = .82f },
        Species.Carrot => new(22, 4, 23, 14, new(0, 18), 14, new(0, 26), new(40, 24), new(0, 25), 0, 1, true) { FaceScaleY = .84f },
        _ => new(21, 5, 22, 14, new(0, 20), 13, Vector2.Zero, Vector2.Zero, new(0, 26), 0, 1, false)
    };
}
