using System.Net;

using DotnetInspect.Core;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using NuGetPackageDependency = NuGet.Packaging.Core.PackageDependency;

namespace DotnetInspect.NuGet;

public sealed class NuGetPackageInspector : IPackageInspector
{
    public async Task<PackageMetadata> InspectAsync(
        string packageId,
        string version,
        string? configPath,
        bool includePrerelease,
        CancellationToken cancellationToken = default)
    {
        ISettings settings = LoadSettings(configPath);
        PackageSourceProvider sourceProvider = new(settings);
        IEnumerable<PackageSource> packageSources = sourceProvider.LoadPackageSources()
            .Where(s => s.IsEnabled);

        if (!packageSources.Any())
        {
            throw new FeedAccessException("No enabled package sources found.");
        }

        NuGetVersion? targetVersion = null;
        if (!NuGetVersion.TryParse(version, out targetVersion))
        {
            throw new InvalidArgumentsException($"Invalid version format: '{version}'");
        }

        using SourceCacheContext cache = new();
        ILogger logger = NullLogger.Instance;

        foreach (PackageSource source in packageSources)
        {
            try
            {
                PackageMetadata? metadata = await TryGetPackageFromSourceAsync(
                    source, packageId, targetVersion, cache, logger, cancellationToken);

                if (metadata != null)
                {
                    return metadata;
                }
            }
            catch (FatalProtocolException ex) when (IsAuthenticationError(ex))
            {
                throw new AuthenticationException($"Authentication failed for source '{source.Name}': {ex.Message}", ex);
            }
            catch (FatalProtocolException ex)
            {
                throw new FeedAccessException($"Failed to access source '{source.Name}': {ex.Message}", ex);
            }
        }

        // Check if package exists at all (any version)
        bool packageExists = await PackageExistsOnAnySourceAsync(
            packageSources, packageId, cache, logger, cancellationToken);

        if (!packageExists)
        {
            throw new PackageNotFoundException(packageId);
        }

        throw new VersionNotFoundException(packageId, version);
    }

    private static ISettings LoadSettings(string? configPath)
    {
        if (!string.IsNullOrEmpty(configPath))
        {
            if (!File.Exists(configPath))
            {
                throw new InvalidArgumentsException($"Config file not found: '{configPath}'");
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
            string fileName = Path.GetFileName(configPath);
            return Settings.LoadSpecificSettings(directory, fileName);
        }

        return Settings.LoadDefaultSettings(Directory.GetCurrentDirectory());
    }

    private static async Task<PackageMetadata?> TryGetPackageFromSourceAsync(
        PackageSource source,
        string packageId,
        NuGetVersion version,
        SourceCacheContext cache,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        SourceRepository repository = Repository.Factory.GetCoreV3(source);
        FindPackageByIdResource? findResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        if (findResource == null)
        {
            return null;
        }

        // Check if the specific version exists
        IEnumerable<NuGetVersion> versions = await findResource.GetAllVersionsAsync(
            packageId, cache, logger, cancellationToken);

        if (!versions.Contains(version))
        {
            return null;
        }

        // Download the package to get the nuspec
        using MemoryStream packageStream = new();
        bool downloaded = await findResource.CopyNupkgToStreamAsync(
            packageId, version, packageStream, cache, logger, cancellationToken);

        if (!downloaded)
        {
            return null;
        }

        packageStream.Position = 0;

        try
        {
            using PackageArchiveReader reader = new(packageStream);
            NuspecReader nuspec = await reader.GetNuspecReaderAsync(cancellationToken);

            return MapNuspecToMetadata(nuspec);
        }
        catch (Exception ex) when (ex is not InspectException)
        {
            throw new NuspecParseException($"Failed to parse nuspec for {packageId} {version}: {ex.Message}", ex);
        }
    }

    private static PackageMetadata MapNuspecToMetadata(NuspecReader nuspec)
    {
        List<DependencyGroup> dependencyGroups = [];

        foreach (PackageDependencyGroup group in nuspec.GetDependencyGroups())
        {
            List<Core.PackageDependency> dependencies = [];

        foreach (NuGetPackageDependency dep in group.Packages)
            {
                dependencies.Add(new Core.PackageDependency(
                    dep.Id,
                    dep.VersionRange?.ToString()));
            }

            dependencyGroups.Add(new DependencyGroup(
                group.TargetFramework?.GetShortFolderName(),
                dependencies));
        }

        RepositoryMetadata? repoMeta = nuspec.GetRepositoryMetadata();

        return new PackageMetadata(
            Id: nuspec.GetId(),
            Version: nuspec.GetVersion().ToString(),
            Description: nuspec.GetDescription(),
            Authors: nuspec.GetAuthors(),
            Owners: nuspec.GetOwners(),
            LicenseExpression: nuspec.GetLicenseMetadata()?.License,
            LicenseUrl: nuspec.GetLicenseUrl(),
            ProjectUrl: nuspec.GetProjectUrl(),
            IconUrl: nuspec.GetIconUrl(),
            Copyright: nuspec.GetCopyright(),
            Tags: nuspec.GetTags(),
            ReleaseNotes: nuspec.GetReleaseNotes(),
            RepositoryUrl: string.IsNullOrEmpty(repoMeta?.Url) ? null : repoMeta.Url,
            RepositoryType: string.IsNullOrEmpty(repoMeta?.Type) ? null : repoMeta.Type,
            RepositoryCommit: string.IsNullOrEmpty(repoMeta?.Commit) ? null : repoMeta.Commit,
            RequireLicenseAcceptance: nuspec.GetRequireLicenseAcceptance(),
            DependencyGroups: dependencyGroups);
    }

    private static async Task<bool> PackageExistsOnAnySourceAsync(
        IEnumerable<PackageSource> sources,
        string packageId,
        SourceCacheContext cache,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (PackageSource source in sources)
        {
            try
            {
                SourceRepository repository = Repository.Factory.GetCoreV3(source);
                FindPackageByIdResource? findResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

                if (findResource == null)
                {
                    continue;
                }

                IEnumerable<NuGetVersion> versions = await findResource.GetAllVersionsAsync(
                    packageId, cache, logger, cancellationToken);

                if (versions.Any())
                {
                    return true;
                }
            }
            catch
            {
                // Continue to next source
            }
        }

        return false;
    }

    private static bool IsAuthenticationError(FatalProtocolException ex)
    {
        if (ex.InnerException is HttpRequestException httpEx)
        {
            return httpEx.StatusCode == HttpStatusCode.Unauthorized ||
                   httpEx.StatusCode == HttpStatusCode.Forbidden;
        }

        return ex.Message.Contains("401") ||
               ex.Message.Contains("403") ||
               ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);
    }
}
