using DotnetInspect.Cli;
using DotnetInspect.Core;

using FluentAssertions;

namespace DotnetInspect.UnitTests;

[TestClass]
public class InspectCommandTests
{
    [TestMethod]
    public async Task ExecuteAsync_WithEmptyPackage_ReturnsExitCode1()
    {
        MockPackageInspector inspector = new();
        MockOutputRenderer renderer = new();
        InspectCommandHandler command = new(inspector, renderer);

        int result = await command.ExecuteAsync("", "1.0.0", null, OutputFormat.Table, false);

        result.Should().Be(1);
        renderer.LastError.Should().Contain("Package ID is required");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithEmptyVersion_ReturnsExitCode1()
    {
        MockPackageInspector inspector = new();
        MockOutputRenderer renderer = new();
        InspectCommandHandler command = new(inspector, renderer);

        int result = await command.ExecuteAsync("TestPackage", "", null, OutputFormat.Table, false);

        result.Should().Be(1);
        renderer.LastError.Should().Contain("Version is required");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithPackageNotFound_ReturnsExitCode2()
    {
        MockPackageInspector inspector = new()
        {
            ExceptionToThrow = new PackageNotFoundException("TestPackage")
        };
        MockOutputRenderer renderer = new();
        InspectCommandHandler command = new(inspector, renderer);

        int result = await command.ExecuteAsync("TestPackage", "1.0.0", null, OutputFormat.Table, false);

        result.Should().Be(2);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithVersionNotFound_ReturnsExitCode3()
    {
        MockPackageInspector inspector = new()
        {
            ExceptionToThrow = new VersionNotFoundException("TestPackage", "1.0.0")
        };
        MockOutputRenderer renderer = new();
        InspectCommandHandler command = new(inspector, renderer);

        int result = await command.ExecuteAsync("TestPackage", "1.0.0", null, OutputFormat.Table, false);

        result.Should().Be(3);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidPackage_ReturnsExitCode0()
    {
        MockPackageInspector inspector = new()
        {
            MetadataToReturn = new PackageMetadata(
                "TestPackage",
                "1.0.0",
                "Test description",
                "Test Author",
                null, null, null, null, null, null, null, null, null, null, null, false,
                [])
        };
        MockOutputRenderer renderer = new();
        InspectCommandHandler command = new(inspector, renderer);

        int result = await command.ExecuteAsync("TestPackage", "1.0.0", null, OutputFormat.Table, false);

        result.Should().Be(0);
        renderer.LastMetadata.Should().NotBeNull();
        renderer.LastMetadata!.Id.Should().Be("TestPackage");
    }

    [TestMethod]
    public async Task ExecuteAsync_PassesFormatToRenderer()
    {
        MockPackageInspector inspector = new()
        {
            MetadataToReturn = new PackageMetadata(
                "TestPackage", "1.0.0", null, null, null, null, null, null, null, null, null, null, null, null, null, false, [])
        };
        MockOutputRenderer renderer = new();
        InspectCommandHandler command = new(inspector, renderer);

        await command.ExecuteAsync("TestPackage", "1.0.0", null, OutputFormat.Json, false);

        renderer.LastFormat.Should().Be(OutputFormat.Json);
    }

    private sealed class MockPackageInspector : IPackageInspector
    {
        public InspectException? ExceptionToThrow { get; set; }
        public PackageMetadata? MetadataToReturn { get; set; }

        public Task<PackageMetadata> InspectAsync(
            string packageId,
            string version,
            string? configPath,
            bool includePrerelease,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(MetadataToReturn!);
        }
    }

    private sealed class MockOutputRenderer : IOutputRenderer
    {
        public PackageMetadata? LastMetadata { get; private set; }
        public OutputFormat LastFormat { get; private set; }
        public string? LastError { get; private set; }

        public void Render(PackageMetadata metadata, OutputFormat format)
        {
            LastMetadata = metadata;
            LastFormat = format;
        }

        public void RenderError(string message)
        {
            LastError = message;
        }
    }
}
