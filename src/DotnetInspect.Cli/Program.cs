using ConsoleAppFramework;

using DotnetInspect.Cli;
using DotnetInspect.Core;
using DotnetInspect.NuGet;

IPackageInspector inspector = new NuGetPackageInspector();
IOutputRenderer renderer = new OutputRenderer();

InspectCommandHandler handler = new(inspector, renderer);

/// <summary>
/// Inspects a NuGet package and displays its nuspec metadata.
/// </summary>
/// <param name="package">The NuGet package ID to inspect.</param>
/// <param name="version">-v, The package version to inspect (required).</param>
/// <param name="config">-c, Path to a custom nuget.config file.</param>
/// <param name="format">-f, Output format: table (default) or json.</param>
/// <param name="includePrerelease">Include prerelease versions when resolving.</param>
await ConsoleApp.RunAsync(args, async ([Argument] string package, string version, string? config = null, OutputFormat format = OutputFormat.Table, bool includePrerelease = false) =>
{
    int exitCode = await handler.ExecuteAsync(package, version, config, format, includePrerelease);
    Environment.ExitCode = exitCode;
});
