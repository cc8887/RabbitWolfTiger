using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RabbitWolfTiger.Entities.Diet;

namespace RabbitWolfTiger.Tests.Diet;

/// <summary>Same assertions run in a plain .NET executable and the Godot headless scene.</summary>
public static class DietRulesChecks
{
    public static int Run(string json)
    {
        int count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            count++;
        }
        void Reject(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { count++; return; }
            throw new InvalidOperationException(message);
        }
        void RejectJson(string invalidJson, string message)
        {
            try { DietCatalog.FromJson(invalidJson); }
            catch (ArgumentException) { count++; return; }
            catch (JsonException) { count++; return; }
            throw new InvalidOperationException(message);
        }
        string MutateJson(Action<JsonNode> change)
        {
            JsonNode node = JsonNode.Parse(json)!;
            change(node);
            return node.ToJsonString();
        }

        DietCatalog catalog = DietCatalog.FromJson(json);
        Check(catalog.Templates.Count == 5, "Five templates required.");
        Check(catalog.Templates.Select(t => t.Face.Color).Distinct().Count() == 5, "Five distinct faces.");
        DietBody human = catalog.Create(Species.Human);
        Check(human.Face.Color == catalog.Create(Species.Human).Face.Color, "Humans share face color.");
        DietBody rabbit = catalog.Create(Species.Rabbit);
        DietBody wolf = catalog.Create(Species.Wolf);
        DietBody tiger = catalog.Create(Species.Tiger);
        DietBody carrot = catalog.Create(Species.Carrot);
        Check(rabbit.CanEat(carrot), "Rabbit eats carrot color.");
        Check(!rabbit.CanEat(human), "Rabbit cannot eat human color.");
        Check(wolf.CanEat(rabbit) && wolf.CanEat(human), "Wolf's two colors.");
        Check(!wolf.CanEat(carrot) && !wolf.CanEat(tiger), "Wolf excludes other colors.");
        Check(tiger.CanEat(wolf) && tiger.CanEat(rabbit) && tiger.CanEat(human), "Tiger's three colors.");
        Check(!tiger.CanEat(carrot), "Tiger excludes carrot color.");
        Check(!carrot.CanEat(rabbit), "None mouth cannot eat.");
        Check(!rabbit.CanEat(rabbit), "No self predation.");
        var sameColorEater = new DietBody(new FaceComponent(FaceColor.Skin),
            new MouthComponent(MouthSize.Small, new[] { FaceColor.Skin }));
        Check(!sameColorEater.CanEat(sameColorEater), "Identity guard excludes edible self.");
        Check(sameColorEater.CanEat(human), "Same face on a different body is edible.");

        var originalFace = rabbit.Face;
        rabbit.ReplaceMouth(tiger.Mouth);
        Check(rabbit.CanEat(wolf) && !rabbit.CanEat(carrot), "Diet follows replaced mouth.");
        Check(ReferenceEquals(originalFace, rabbit.Face), "Mouth change preserves face.");
        var originalMouth = carrot.Mouth;
        carrot.ReplaceFace(wolf.Face);
        Check(tiger.CanEat(carrot), "Predation follows replaced face, not species.");
        Check(ReferenceEquals(originalMouth, carrot.Mouth), "Face change preserves mouth.");
        Check(catalog.Create(Species.Carrot).Face.Color == FaceColor.Orange, "Instances do not mutate templates.");

        FaceColor[] recipe = { FaceColor.GoldenOrange };
        var alternateSmall = new MouthComponent(MouthSize.Small, recipe);
        recipe[0] = FaceColor.Skin;
        Check(alternateSmall.CanEat(tiger.Face) && !alternateSmall.CanEat(human.Face), "Recipe copied defensively.");
        Check(alternateSmall.Size == catalog.Create(Species.Rabbit).Mouth.Size &&
            !catalog.Create(Species.Rabbit).Mouth.CanEat(tiger.Face), "Same size can have different recipes.");

        FaceColor[] colors = Enum.GetValues<FaceColor>();
        foreach (MouthSize size in Enum.GetValues<MouthSize>())
        {
            int required = (int)size;
            for (int length = 0; length <= colors.Length; length++)
            {
                FaceColor[] chosen = colors.Take(length).ToArray();
                if (length == required)
                {
                    var mouth = new MouthComponent(size, chosen);
                    Check(mouth.EdibleColors.Count == required, "Exact recipe length.");
                    foreach (FaceColor color in colors)
                        Check(mouth.CanEat(new FaceComponent(color)) == chosen.Contains(color), "Exact membership.");
                }
                else Reject(() => new MouthComponent(size, chosen), "Invalid recipe length accepted.");
            }
        }
        Reject(() => new MouthComponent(MouthSize.Medium, new[] { FaceColor.Cream, FaceColor.Cream }), "Duplicates accepted.");
        Reject(() => new MouthComponent(MouthSize.Large, new[] { FaceColor.Cream, FaceColor.Gray, FaceColor.Cream }), "Large duplicate accepted.");
        Reject(() => new MouthComponent(MouthSize.Small, new[] { (FaceColor)99 }), "Unknown color accepted.");
        Reject(() => new MouthComponent((MouthSize)99, Array.Empty<FaceColor>()), "Unknown size accepted.");
        Reject(() => new FaceComponent((FaceColor)99), "Unknown face accepted.");
        Reject(() => new MouthComponent(MouthSize.None, null!), "Null recipe accepted.");
        Reject(() => rabbit.ReplaceMouth(null!), "Null mouth accepted.");
        Reject(() => rabbit.ReplaceFace(null!), "Null face accepted.");
        Reject(() => DietCatalog.FromJson(json.Replace("\"Cream\"", "\"Unknown\"")), "Unknown JSON color accepted.");
        Reject(() => DietCatalog.FromJson(json.Replace("\"Cream\"", "\"1\"")), "Numeric JSON color accepted.");
        Reject(() => DietCatalog.FromJson(json.Replace("\"Rabbit\"", "\"Human\"")), "Duplicate species accepted.");
        Reject(() => DietCatalog.FromJson(json.Replace("\"Cream\"", "\"Skin\"")), "Duplicate face accepted.");
        Reject(() => DietCatalog.FromJson(json.Replace("\"version\": 1", "\"version\": 2")), "Unknown version accepted.");
        RejectJson("null", "Null catalog accepted.");
        RejectJson("{}", "Missing required fields accepted.");
        RejectJson(MutateJson(node => node["extra"] = true), "Unknown JSON field accepted.");
        RejectJson(MutateJson(node => node["templates"] = null), "Null templates accepted.");
        RejectJson(MutateJson(node => node["templates"]![0] = null), "Null template accepted.");
        RejectJson(MutateJson(node => node["templates"]![0]!["edibleColors"] = null), "Null JSON recipe accepted.");
        RejectJson(MutateJson(node => node["templates"]![1]!["edibleColors"]![0] = null), "Null recipe color accepted.");
        RejectJson(MutateJson(node => node["templates"]![0]!["faceColor"] = null), "Null face ID accepted.");
        RejectJson(MutateJson(node => node["humanMouthStatus"] = ""), "Missing decision status accepted.");
        return count;
    }
}
