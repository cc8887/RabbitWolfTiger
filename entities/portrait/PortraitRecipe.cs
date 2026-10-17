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
        Species.Rabbit => new(23, 5, 23, 15.5f, new(0, 19), 12, new(0, 25), new(55, 28), new(0, 29), -59, 1.1f, true) { FaceScaleY = .76f },
        Species.Wolf => new(23, 4, 24, 16, new(0, 18), 13, new(0, 25), new(56, 30), new(0, 29), -50, 1.06f, true) { FaceScaleY = .78f },
        Species.Tiger => new(23, 4, 24, 16, new(0, 18), 13, new(0, 26), new(52, 30), new(0, 29), -46, 1f, true) { FaceScaleY = .82f },
        Species.Carrot => new(22, 4, 23, 15.5f, new(0, 18), 12, new(0, 27), new(53, 32), new(0, 29), 0, 1, true) { FaceScaleY = .84f },
        _ => new(21, 5, 22, 15, new(0, 20), 11, Vector2.Zero, Vector2.Zero, new(0, 31), 0, 1, false)
    };
}
