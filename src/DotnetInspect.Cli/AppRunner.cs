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

        await ConsoleApp.RunAsync(args, InspectCommand);

        // ConsoleAppFramework sets Environment.ExitCode for argument parsing errors
        if (Environment.ExitCode != previousExitCode)
        {
            exitCode = Environment.ExitCode;
            Environment.ExitCode = previousExitCode; // Reset for testing
        }

        return exitCode;

        /// <summary>
        /// Inspects a NuGet package and displays its .nuspec metadata.
        /// </summary>
        /// <param name="package">The package ID to inspect (e.g., Newtonsoft.Json).</param>
        /// <param name="version">The exact package version to inspect.</param>
        /// <param name="config">Path to a nuget.config file to use for package sources.</param>
        /// <param name="format">Output format: Table or Json.</param>
        /// <param name="includePrerelease">Include prerelease versions when resolving packages.</param>
        async Task InspectCommand(
            [Argument] string package,
            string version,
            string? config = null,
            OutputFormat format = OutputFormat.Table,
            bool includePrerelease = false)
        {
            exitCode = await handler.ExecuteAsync(package, version, config, format, includePrerelease);
        }
    }
}
