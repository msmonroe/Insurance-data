using System.Globalization;

namespace InsuranceData;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
            Console.WriteLine("Cancellation requested. Finishing the current write operation...");
        };

        try
        {
            GeneratorOptions options = CommandLineParser.Parse(args, Directory.GetCurrentDirectory(), DateTime.UtcNow);
            Console.WriteLine($"Output directory: {options.OutputDirectory}");
            Console.WriteLine($"Records: {options.TotalRecords.ToString("N0", CultureInfo.InvariantCulture)} across {options.FileCount} file(s)");
            Console.WriteLine($"Seed: {options.Seed}");
            Console.WriteLine($"Validation mode: {(options.ValidationMode ? "ENABLED" : "DISABLED")}");

            var generator = new InsuranceLeadGenerator(options);
            return await generator.RunAsync(cancellationSource.Token).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Argument error: {ex.Message}");
            CommandLineParser.PrintUsage();
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Generation cancelled.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Generation failed: {ex}");
            return 1;
        }
    }
}
