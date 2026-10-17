using System;
using System.IO;

namespace RabbitWolfTiger.Tests.Diet;

public static class Program
{
    public static int Main()
    {
        try
        {
            int count = DietRulesChecks.Run(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "initial_diets.json")));
            Console.WriteLine($"PASS: {count} diet rule checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
