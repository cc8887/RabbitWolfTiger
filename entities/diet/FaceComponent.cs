using System;

namespace RabbitWolfTiger.Entities.Diet;

// Color identity is independent of species and of the presentation RGB value.
public enum FaceColor { Skin, Cream, Gray, Orange, GoldenOrange }

public sealed class FaceComponent
{
    public FaceColor Color { get; }

    public FaceComponent(FaceColor color)
    {
        if (!Enum.IsDefined(color)) throw new ArgumentOutOfRangeException(nameof(color));
        Color = color;
    }
}
