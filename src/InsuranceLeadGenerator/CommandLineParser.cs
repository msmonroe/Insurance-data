using System.Globalization;

namespace InsuranceData;

internal static class CommandLineParser
{
    public static GeneratorOptions Parse(string[] args, string currentDirectory, DateTime currentUtc)
    {
        string outputDirectory = Path.Combine(currentDirectory, "GeneratedLeadData");
        long? records = null;
        int? files = null;
        int seed = GeneratorOptions.DefaultSeed;
        double exactDuplicatePercent = GeneratorOptions.DefaultExactDuplicatePercent;
        double modifiedDuplicatePercent = GeneratorOptions.DefaultModifiedDuplicatePercent;
        double invalidPercent = GeneratorOptions.DefaultInvalidPercent;
        bool validationMode = false;
        bool includeStructuralErrors = false;

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];
            switch (argument)
            {
                case "--output":
                    outputDirectory = RequireValue(args, ref i, argument);
                    break;
                case "--records":
                    records = ParseLong(RequireValue(args, ref i, argument), argument);
                    break;
                case "--files":
                    files = ParseInt(RequireValue(args, ref i, argument), argument);
                    break;
                case "--seed":
                    seed = ParseInt(RequireValue(args, ref i, argument), argument);
                    break;
                case "--exact-duplicate-percent":
                    exactDuplicatePercent = ParseDouble(RequireValue(args, ref i, argument), argument);
                    break;
                case "--modified-duplicate-percent":
                    modifiedDuplicatePercent = ParseDouble(RequireValue(args, ref i, argument), argument);
                    break;
                case "--invalid-percent":
                    invalidPercent = ParseDouble(RequireValue(args, ref i, argument), argument);
                    break;
                case "--validation-mode":
                    validationMode = true;
                    break;
                case "--include-structural-errors":
                    includeStructuralErrors = true;
                    break;
                case "--help":
                case "-h":
                case "/?":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        long totalRecords = records ?? (validationMode ? 100_000 : GeneratorOptions.DefaultRecordCount);
        int fileCount = files ?? (validationMode ? 2 : GeneratorOptions.DefaultFileCount);

        if (totalRecords <= 0)
        {
            throw new ArgumentException("--records must be greater than zero.");
        }

        if (fileCount <= 0)
        {
            throw new ArgumentException("--files must be greater than zero.");
        }

        if (totalRecords < fileCount)
        {
            throw new ArgumentException("--records must be greater than or equal to --files.");
        }

        if (exactDuplicatePercent < 0 || modifiedDuplicatePercent < 0 || invalidPercent < 0)
        {
            throw new ArgumentException("Percentage values cannot be negative.");
        }

        if (exactDuplicatePercent + modifiedDuplicatePercent + invalidPercent >= 100d)
        {
            throw new ArgumentException("Duplicate and invalid percentages must sum to less than 100.");
        }

        return new GeneratorOptions
        {
            OutputDirectory = Path.GetFullPath(outputDirectory),
            TotalRecords = totalRecords,
            FileCount = fileCount,
            Seed = seed,
            ExactDuplicatePercent = exactDuplicatePercent,
            ModifiedDuplicatePercent = modifiedDuplicatePercent,
            InvalidPercent = invalidPercent,
            ValidationMode = validationMode,
            IncludeStructuralErrors = includeStructuralErrors,
            CurrentUtc = currentUtc,
        };
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- --output <path> --records <count> --files <count> --seed <seed>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --output <path>                      Output directory. Default: ./GeneratedLeadData");
        Console.WriteLine("  --records <count>                   Total records to generate. Default: 50000000");
        Console.WriteLine("  --files <count>                     Number of CSV files. Default: 10");
        Console.WriteLine("  --seed <seed>                       Deterministic seed. Default: 8675309");
        Console.WriteLine("  --exact-duplicate-percent <value>   Approximate percent of exact duplicates. Default: 10");
        Console.WriteLine("  --modified-duplicate-percent <value> Approximate percent of modified duplicates. Default: 16");
        Console.WriteLine("  --invalid-percent <value>           Approximate percent of intentionally invalid rows. Default: 2");
        Console.WriteLine("  --validation-mode                   Generate a smaller dataset and run full inspection checks.");
        Console.WriteLine("  --include-structural-errors         Allow intentionally malformed CSV structure in some invalid rows.");
    }

    private static string RequireValue(string[] args, ref int index, string argument)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {argument}.");
        }

        index++;
        return args[index];
    }

    private static int ParseInt(string value, string argument)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            throw new ArgumentException($"Invalid integer value '{value}' for {argument}.");
        }

        return parsed;
    }

    private static long ParseLong(string value, string argument)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
        {
            throw new ArgumentException($"Invalid integer value '{value}' for {argument}.");
        }

        return parsed;
    }

    private static double ParseDouble(string value, string argument)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed))
        {
            throw new ArgumentException($"Invalid numeric value '{value}' for {argument}.");
        }

        return parsed;
    }
}
