namespace DotnetInspect.Core;

public record PackageIdentity(string Id, string Version);

public record PackageMetadata(
    string Id,
    string Version,
    string? Description,
    string? Authors,
    string? Owners,
    string? LicenseExpression,
    string? LicenseUrl,
    string? ProjectUrl,
    string? IconUrl,
    string? Copyright,
    string? Tags,
    string? ReleaseNotes,
    string? RepositoryUrl,
    string? RepositoryType,
    string? RepositoryCommit,
    bool RequireLicenseAcceptance,
    IReadOnlyList<DependencyGroup> DependencyGroups);

public record DependencyGroup(string? TargetFramework, IReadOnlyList<PackageDependency> Dependencies);

public record PackageDependency(string Id, string? VersionRange);
