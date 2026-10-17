using System;
using Godot;

namespace RabbitWolfTiger.Tests.Diet;

public partial class DietRulesHeadless : Node
{
    public override void _Ready()
    {
        try
        {
            int count = DietRulesChecks.Run(FileAccess.GetFileAsString("res://resources/diet/initial_diets.json"));
            GD.Print($"PASS: {count} diet rule checks.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
