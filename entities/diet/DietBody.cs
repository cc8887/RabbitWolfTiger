using System;

namespace RabbitWolfTiger.Entities.Diet;

/// <summary>Composition boundary. Replacing one part never rewrites the other part.</summary>
public sealed class DietBody
{
    public FaceComponent Face { get; private set; }
    public MouthComponent Mouth { get; private set; }

    public DietBody(FaceComponent face, MouthComponent mouth)
    {
        Face = face ?? throw new ArgumentNullException(nameof(face));
        Mouth = mouth ?? throw new ArgumentNullException(nameof(mouth));
    }

    public void ReplaceFace(FaceComponent face) =>
        Face = face ?? throw new ArgumentNullException(nameof(face));
    public void ReplaceMouth(MouthComponent mouth) =>
        Mouth = mouth ?? throw new ArgumentNullException(nameof(mouth));

    // Species intentionally does not exist in this decision path.
    // This is dietary eligibility only; safety, alive state and turn timing belong to the caller.
    public bool CanEat(DietBody target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return !ReferenceEquals(this, target) && Mouth.CanEat(target.Face);
    }
}
