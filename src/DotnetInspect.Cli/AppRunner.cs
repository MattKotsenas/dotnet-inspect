using ConsoleAppFramework;

using DotnetInspect.Core;
using DotnetInspect.NuGet;

using Spectre.Console;

namespace DotnetInspect.Cli;

/// <summary>
/// Public API for running the CLI, enabling integration tests to call directly without spawning processes.
/// </summary>
public static class AppRunner
{
    public static async Task<int> RunAsync(string[] args, IAnsiConsole console)
    {
        IPackageInspector inspector = new NuGetPackageInspector();
        IOutputRenderer renderer = new OutputRenderer(console);

        InspectCommandHandler handler = new(inspector, renderer);

        int exitCode = 0;
        int previousExitCode = Environment.ExitCode;

        await ConsoleApp.RunAsync(args, async ([Argument] string package, string version, string? config = null, OutputFormat format = OutputFormat.Table, bool includePrerelease = false) =>
        {
            exitCode = await handler.ExecuteAsync(package, version, config, format, includePrerelease);
        });

        // ConsoleAppFramework sets Environment.ExitCode for argument parsing errors
        if (exitCode == 0 && Environment.ExitCode != previousExitCode)
        {
            exitCode = Environment.ExitCode;
            Environment.ExitCode = previousExitCode; // Reset for testing
        }

        return exitCode;
    }
}
