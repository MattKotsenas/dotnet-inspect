using System.Text.Json;
using System.Text.Json.Serialization;

using DotnetInspect.Core;

using Spectre.Console;

namespace DotnetInspect.Cli;

public sealed class OutputRenderer : IOutputRenderer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Render(PackageMetadata metadata, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Json:
                RenderJson(metadata);
                break;
            case OutputFormat.Table:
            default:
                RenderTable(metadata);
                break;
        }
    }

    public void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }

    private static void RenderJson(PackageMetadata metadata)
    {
        JsonOutput output = new(
            metadata.Id,
            metadata.Version,
            new NuspecJson(
                metadata.Description,
                metadata.Authors,
                metadata.Owners,
                metadata.LicenseExpression,
                metadata.LicenseUrl,
                metadata.ProjectUrl,
                metadata.IconUrl,
                metadata.Copyright,
                metadata.Tags,
                metadata.ReleaseNotes,
                metadata.RepositoryUrl,
                metadata.RepositoryType,
                metadata.RepositoryCommit,
                metadata.RequireLicenseAcceptance,
                metadata.DependencyGroups.Select(g => new DependencyGroupJson(
                    g.TargetFramework,
                    g.Dependencies.Select(d => new DependencyJson(d.Id, d.VersionRange)).ToList()
                )).ToList()
            ));

        List<JsonOutput> outputList = [output];
        string json = JsonSerializer.Serialize(outputList, s_jsonOptions);
        AnsiConsole.WriteLine(json);
    }

    private static void RenderTable(PackageMetadata metadata)
    {
        // Metadata section
        Table metadataTable = new();
        metadataTable.Border(TableBorder.Rounded);
        metadataTable.Title("[bold]Metadata[/]");
        metadataTable.AddColumn("Property");
        metadataTable.AddColumn("Value");

        metadataTable.AddRow("[bold]ID[/]", Markup.Escape(metadata.Id));
        metadataTable.AddRow("[bold]Version[/]", Markup.Escape(metadata.Version));

        if (!string.IsNullOrEmpty(metadata.Description))
        {
            string description = metadata.Description.Length > 100
                ? metadata.Description[..100] + "..."
                : metadata.Description;
            metadataTable.AddRow("[bold]Description[/]", Markup.Escape(description));
        }

        if (!string.IsNullOrEmpty(metadata.Authors))
        {
            metadataTable.AddRow("[bold]Authors[/]", Markup.Escape(metadata.Authors));
        }

        if (!string.IsNullOrEmpty(metadata.Owners))
        {
            metadataTable.AddRow("[bold]Owners[/]", Markup.Escape(metadata.Owners));
        }

        string license = metadata.LicenseExpression ?? metadata.LicenseUrl ?? "N/A";
        metadataTable.AddRow("[bold]License[/]", Markup.Escape(license));

        if (!string.IsNullOrEmpty(metadata.ProjectUrl))
        {
            metadataTable.AddRow("[bold]Project URL[/]", Markup.Escape(metadata.ProjectUrl));
        }

        if (!string.IsNullOrEmpty(metadata.Tags))
        {
            metadataTable.AddRow("[bold]Tags[/]", Markup.Escape(metadata.Tags));
        }

        if (!string.IsNullOrEmpty(metadata.Copyright))
        {
            metadataTable.AddRow("[bold]Copyright[/]", Markup.Escape(metadata.Copyright));
        }

        AnsiConsole.Write(metadataTable);
        AnsiConsole.WriteLine();

        // Repository section
        if (!string.IsNullOrEmpty(metadata.RepositoryUrl) ||
            !string.IsNullOrEmpty(metadata.RepositoryType) ||
            !string.IsNullOrEmpty(metadata.RepositoryCommit))
        {
            Table repoTable = new();
            repoTable.Border(TableBorder.Rounded);
            repoTable.Title("[bold]Repository[/]");
            repoTable.AddColumn("Property");
            repoTable.AddColumn("Value");

            if (!string.IsNullOrEmpty(metadata.RepositoryUrl))
            {
                repoTable.AddRow("[bold]URL[/]", Markup.Escape(metadata.RepositoryUrl));
            }

            if (!string.IsNullOrEmpty(metadata.RepositoryType))
            {
                repoTable.AddRow("[bold]Type[/]", Markup.Escape(metadata.RepositoryType));
            }

            if (!string.IsNullOrEmpty(metadata.RepositoryCommit))
            {
                repoTable.AddRow("[bold]Commit[/]", Markup.Escape(metadata.RepositoryCommit));
            }

            AnsiConsole.Write(repoTable);
            AnsiConsole.WriteLine();
        }

        // Dependencies section
        if (metadata.DependencyGroups.Count > 0 && metadata.DependencyGroups.Any(g => g.Dependencies.Count > 0))
        {
            Table depsTable = new();
            depsTable.Border(TableBorder.Rounded);
            depsTable.Title("[bold]Dependencies[/]");
            depsTable.AddColumn("Target Framework");
            depsTable.AddColumn("Package");
            depsTable.AddColumn("Version");

            foreach (DependencyGroup group in metadata.DependencyGroups)
            {
                if (group.Dependencies.Count == 0)
                {
                    continue;
                }

                string tfm = group.TargetFramework ?? "(any)";

                foreach (Core.PackageDependency dep in group.Dependencies)
                {
                    depsTable.AddRow(
                        Markup.Escape(tfm),
                        Markup.Escape(dep.Id),
                        Markup.Escape(dep.VersionRange ?? "(any)"));
                }
            }

            AnsiConsole.Write(depsTable);
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]No dependencies[/]");
        }
    }

    private sealed record JsonOutput(string Id, string Version, NuspecJson Nuspec);

    private sealed record NuspecJson(
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
        List<DependencyGroupJson> DependencyGroups);

    private sealed record DependencyGroupJson(string? TargetFramework, List<DependencyJson> Dependencies);

    private sealed record DependencyJson(string Id, string? VersionRange);
}
