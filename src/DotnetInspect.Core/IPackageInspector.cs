namespace DotnetInspect.Core;

public interface IPackageInspector
{
    Task<PackageMetadata> InspectAsync(
        string packageId,
        string version,
        string? configPath,
        bool includePrerelease,
        CancellationToken cancellationToken = default);
}
