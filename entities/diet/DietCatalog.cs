using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RabbitWolfTiger.Entities.Diet;

public enum Species { Human, Rabbit, Wolf, Carrot, Tiger }

/// <summary>Initial assembly data only. Gameplay never queries species to decide diet.</summary>
public sealed class DietCatalog
{
    public sealed record Template(Species Species, FaceComponent Face, MouthComponent Mouth);
    public IReadOnlyList<Template> Templates { get; }
    public string HumanMouthStatus { get; }

    private DietCatalog(Template[] templates, string humanMouthStatus)
    {
        Templates = Array.AsReadOnly(templates);
        HumanMouthStatus = humanMouthStatus;
    }

    public DietBody Create(Species species)
    {
        Template template = Templates.Single(item => item.Species == species);
        return new DietBody(template.Face, template.Mouth);
    }

    public static DietCatalog FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        Config config = JsonSerializer.Deserialize<Config>(json, options)
            ?? throw new ArgumentException("Catalog must be an object.");
        if (config.Version != 1) throw new ArgumentException("Unsupported catalog version.");
        if (string.IsNullOrWhiteSpace(config.HumanMouthStatus))
            throw new ArgumentException("Human mouth decision status must be explicit.");
        if (config.Templates == null || config.Templates.Any(item => item == null))
            throw new ArgumentException("Templates cannot be null.");
        Template[] templates = config.Templates.Select(item => new Template(
            Parse<Species>(item.Species), new FaceComponent(Parse<FaceColor>(item.FaceColor)),
            new MouthComponent(Parse<MouthSize>(item.MouthSize),
                (item.EdibleColors ?? throw new ArgumentException("Recipe cannot be null."))
                .Select(Parse<FaceColor>)))).ToArray();
        if (templates.Length != Enum.GetValues<Species>().Length ||
            templates.Select(item => item.Species).Distinct().Count() != templates.Length)
            throw new ArgumentException("Exactly one initial template per species is required.");
        if (templates.Select(item => item.Face.Color).Distinct().Count() != templates.Length)
            throw new ArgumentException("The five initial species must have five distinct face colors.");
        return new DietCatalog(templates, config.HumanMouthStatus);
    }

    private static T Parse<T>(string text) where T : struct, Enum
    {
        // Reject numeric enum strings, unknown values and accidental casing errors.
        if (text == null || !Enum.GetNames<T>().Contains(text) || !Enum.TryParse(text, out T value))
            throw new ArgumentException($"Unknown {typeof(T).Name}: '{text}'.");
        return value;
    }

    public sealed class Config
    {
        public required int Version { get; init; }
        public required string HumanMouthStatus { get; init; }
        public required Entry[] Templates { get; init; }
    }

    public sealed class Entry
    {
        public required string Species { get; init; }
        public required string FaceColor { get; init; }
        public required string MouthSize { get; init; }
        public required string[] EdibleColors { get; init; }
    }
}
