using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Build.Utilities.ProjectCreation;

namespace DotnetInspect.IntegrationTests;

[TestClass]
public class EndToEndTests : VerifyBase
{
    private static string? s_cliPath;
    private static string? s_feedPath;
    private static string? s_nugetConfigPath;

    [ModuleInitializer]
    public static void Initialize()
    {
        MSBuildAssemblyResolver.Register();
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        string? assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string? projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation!, "..", "..", "..", "..", ".."));

        // Find CLI executable
        string configuration = GetConfiguration();
        string tfm = GetTargetFramework();
        s_cliPath = Path.Combine(projectRoot, "src", "DotnetInspect.Cli", "bin", configuration, tfm, "DotnetInspect.Cli.exe");

        if (!File.Exists(s_cliPath))
        {
            s_cliPath = Path.Combine(projectRoot, "src", "DotnetInspect.Cli", "bin", configuration, tfm, "DotnetInspect.Cli");
        }

        // Create fake package feed
        s_feedPath = Path.Combine(Path.GetTempPath(), "DotnetInspect.E2E.Feed", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(s_feedPath);

        CreateTestPackages(s_feedPath);

        // Create nuget.config pointing to fake feed
        s_nugetConfigPath = Path.Combine(s_feedPath, "nuget.config");
        File.WriteAllText(s_nugetConfigPath, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="TestFeed" value="{s_feedPath}" />
              </packageSources>
            </configuration>
            """);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (s_feedPath != null && Directory.Exists(s_feedPath))
        {
            try
            {
                Directory.Delete(s_feedPath, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private static void CreateTestPackages(string feedPath)
    {
        // Create TestPackage.Simple 1.0.0
        PackageFeed.Create(feedPath)
            .Package("TestPackage.Simple", "1.0.0", out Package _)
                .Library("netstandard2.0")
            .Save();

        // Create TestPackage.WithDependencies 2.0.0
        PackageFeed.Create(feedPath)
            .Package("TestPackage.WithDependencies", "2.0.0", out Package _)
                .Library("net8.0")
                .Dependency("TestPackage.Simple", "1.0.0", "net8.0")
                .Library("net9.0")
                .Dependency("TestPackage.Simple", "1.0.0", "net9.0")
            .Save();

        // Create TestPackage.NoDeps 1.0.0 - minimal package
        PackageFeed.Create(feedPath)
            .Package("TestPackage.NoDeps", "1.0.0", out Package _)
                .Library("netstandard2.0")
            .Save();
    }

    private static string GetConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    private static string GetTargetFramework()
    {
#if NET10_0
        return "net10.0";
#elif NET9_0
        return "net9.0";
#else
        return "net8.0";
#endif
    }

    [TestMethod]
    public async Task InspectSimplePackage_TableFormat()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.Simple", "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectSimplePackage_JsonFormat()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.Simple", "--version", "1.0.0", "--config", s_nugetConfigPath!, "--format", "json");

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectPackageWithDependencies_TableFormat()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.WithDependencies", "--version", "2.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectPackageWithDependencies_JsonFormat()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.WithDependencies", "--version", "2.0.0", "--config", s_nugetConfigPath!, "--format", "json");

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectMinimalPackage_TableFormat()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.NoDeps", "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectNonExistentPackage_ReturnsExitCode2()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "NonExistentPackage12345678", "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectNonExistentVersion_ReturnsExitCode3()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.Simple", "--version", "999.999.999", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectWithoutVersion_ReturnsExitCode1()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "TestPackage.Simple", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    [TestMethod]
    public async Task InspectWithoutPackage_ReturnsExitCode1()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync(
            "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, stdout, stderr });
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(params string[] args)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = s_cliPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Disable ANSI colors for consistent output
        startInfo.Environment["NO_COLOR"] = "1";

        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using Process? process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {s_cliPath}");
        }

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Normalize line endings for cross-platform consistency
        stdout = stdout.ReplaceLineEndings("\n");
        stderr = stderr.ReplaceLineEndings("\n");

        return (process.ExitCode, stdout, stderr);
    }
}
