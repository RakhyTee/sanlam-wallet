using Wallet.Cli.Models;

namespace Wallet.Cli.Commands;

internal static class ConsoleOutput
{
    public static void PrintError(ProblemResponse? problem) =>
        Console.Error.WriteLine($"Error ({problem?.Status}): {problem?.Title ?? "Request failed."}");
}
