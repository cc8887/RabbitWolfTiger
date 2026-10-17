using System;
using System.Collections.Generic;
using System.Linq;

namespace RabbitWolfTiger.Entities.Diet;

public enum MouthSize { None, Small, Medium, Large }

/// <summary>A replaceable mouth carries its complete recipe. Size sets count only.</summary>
public sealed class MouthComponent
{
    public MouthSize Size { get; }
    public IReadOnlyList<FaceColor> EdibleColors { get; }

    public MouthComponent(MouthSize size, IEnumerable<FaceColor> edibleColors)
    {
        ArgumentNullException.ThrowIfNull(edibleColors);
        int count = size switch
        {
            MouthSize.None => 0, MouthSize.Small => 1,
            MouthSize.Medium => 2, MouthSize.Large => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
        FaceColor[] colors = edibleColors.ToArray();
        if (colors.Any(color => !Enum.IsDefined(color)))
            throw new ArgumentException("Recipe contains an unknown face color.", nameof(edibleColors));
        if (colors.Distinct().Count() != colors.Length)
            throw new ArgumentException("Recipe colors must be unique.", nameof(edibleColors));
        if (colors.Length != count)
            throw new ArgumentException($"{size} requires exactly {count} colors.", nameof(edibleColors));
        Size = size;
        EdibleColors = Array.AsReadOnly(colors);
    }

    public bool CanEat(FaceComponent target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return EdibleColors.Contains(target.Color);
    }
}
