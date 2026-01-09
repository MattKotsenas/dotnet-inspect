using DotnetInspect.Core;

namespace DotnetInspect.Cli;

public sealed class InspectCommandHandler(IPackageInspector inspector, IOutputRenderer renderer)
{
    public async Task<int> ExecuteAsync(
        string package,
        string? version,
        string? config,
        OutputFormat format,
        bool includePrerelease)
    {
        if (string.IsNullOrWhiteSpace(package))
        {
            renderer.RenderError("Package ID is required.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            renderer.RenderError("Version is required.");
            return 1;
        }

        try
        {
            PackageMetadata metadata = await inspector.InspectAsync(
                package,
                version,
                config,
                includePrerelease);

            renderer.Render(metadata, format);
            return 0;
        }
        catch (InspectException ex)
        {
            renderer.RenderError(ex.Message);
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            renderer.RenderError($"Unexpected error: {ex.Message}");
            return 7;
        }
    }
}
