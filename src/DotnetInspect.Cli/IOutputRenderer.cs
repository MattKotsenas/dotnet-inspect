using DotnetInspect.Core;

namespace DotnetInspect.Cli;

public interface IOutputRenderer
{
    void Render(PackageMetadata metadata, OutputFormat format);
    void RenderError(string message);
}
