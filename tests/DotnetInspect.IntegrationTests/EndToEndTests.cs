using System.Runtime.CompilerServices;

using Microsoft.Build.Utilities.ProjectCreation;

using Spectre.Console.Testing;

namespace DotnetInspect.IntegrationTests;

[TestClass]
public class EndToEndTests : VerifyBase
{
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
        // Create fake package feed
        s_feedPath = Path.Combine(Path.GetTempPath(), "DotnetInspect.IntegrationTests.Feed", Guid.NewGuid().ToString("N"));
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
            .Package("TestPackage.Simple", "1.0.0", out Package _, "Test Author", "A simple test package for dotnet-inspect integration tests.")
                .Library("netstandard2.0")
            .Save();

        // Create TestPackage.WithDependencies 2.0.0
        PackageFeed.Create(feedPath)
            .Package("TestPackage.WithDependencies", "2.0.0", out Package _, "Dependency Author", "A test package with dependencies.")
                .Library("net8.0")
                .Dependency("net8.0", "TestPackage.Simple", "1.0.0")
                .Library("net9.0")
                .Dependency("net9.0", "TestPackage.Simple", "1.0.0")
            .Save();

        // Create TestPackage.NoDeps 1.0.0 - minimal package
        PackageFeed.Create(feedPath)
            .Package("TestPackage.NoDeps", "1.0.0", out Package _, "Minimal Author", "A minimal package with no dependencies.")
                .Library("netstandard2.0")
            .Save();
    }

    [TestMethod]
    public async Task InspectSimplePackage_TableFormat()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.Simple", "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectSimplePackage_JsonFormat()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.Simple", "--version", "1.0.0", "--config", s_nugetConfigPath!, "--format", "json");

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectPackageWithDependencies_TableFormat()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.WithDependencies", "--version", "2.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectPackageWithDependencies_JsonFormat()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.WithDependencies", "--version", "2.0.0", "--config", s_nugetConfigPath!, "--format", "json");

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectMinimalPackage_TableFormat()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.NoDeps", "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectNonExistentPackage_ReturnsExitCode2()
    {
        (int exitCode, string output) = await RunCliAsync(
            "NonExistentPackage12345678", "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectNonExistentVersion_ReturnsExitCode3()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.Simple", "--version", "999.999.999", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectWithoutVersion_ReturnsExitCode1()
    {
        (int exitCode, string output) = await RunCliAsync(
            "TestPackage.Simple", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    [TestMethod]
    public async Task InspectWithoutPackage_ReturnsExitCode1()
    {
        (int exitCode, string output) = await RunCliAsync(
            "--version", "1.0.0", "--config", s_nugetConfigPath!);

        await Verify(new { exitCode, output });
    }

    private static async Task<(int ExitCode, string Output)> RunCliAsync(params string[] args)
    {
        TestConsole console = new();
        console.Profile.Width = 120;

        // Capture Console.Out/Error for ConsoleAppFramework argument parsing errors
        StringWriter stdoutWriter = new();
        StringWriter stderrWriter = new();
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        try
        {
            Console.SetOut(stdoutWriter);
            Console.SetError(stderrWriter);

            int exitCode = await DotnetInspect.Cli.AppRunner.RunAsync(args, console);

            // Combine outputs: Spectre.Console output + Console output
            string spectreOutput = console.Output;
            string stdoutOutput = stdoutWriter.ToString();
            string stderrOutput = stderrWriter.ToString();

            string combinedOutput = spectreOutput + stdoutOutput + stderrOutput;
            combinedOutput = combinedOutput.ReplaceLineEndings("\n").TrimEnd('\n');

            return (exitCode, combinedOutput);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
