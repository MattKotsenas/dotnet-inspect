using DotnetInspect.Core;

using FluentAssertions;

namespace DotnetInspect.UnitTests;

[TestClass]
public class ModelsTests
{
    [TestMethod]
    public void PackageIdentity_RecordEquality_Works()
    {
        PackageIdentity id1 = new("Test", "1.0.0");
        PackageIdentity id2 = new("Test", "1.0.0");

        id1.Should().Be(id2);
    }

    [TestMethod]
    public void PackageMetadata_CanBeCreatedWithAllNullOptionals()
    {
        PackageMetadata metadata = new(
            Id: "TestPackage",
            Version: "1.0.0",
            Description: null,
            Authors: null,
            Owners: null,
            LicenseExpression: null,
            LicenseUrl: null,
            ProjectUrl: null,
            IconUrl: null,
            Copyright: null,
            Tags: null,
            ReleaseNotes: null,
            RepositoryUrl: null,
            RepositoryType: null,
            RepositoryCommit: null,
            RequireLicenseAcceptance: false,
            DependencyGroups: []);

        metadata.Id.Should().Be("TestPackage");
        metadata.Version.Should().Be("1.0.0");
        metadata.DependencyGroups.Should().BeEmpty();
    }

    [TestMethod]
    public void DependencyGroup_CanHoldDependencies()
    {
        DependencyGroup group = new(
            "net8.0",
            [
                new PackageDependency("Dep1", "[1.0.0, 2.0.0)"),
                new PackageDependency("Dep2", null)
            ]);

        group.TargetFramework.Should().Be("net8.0");
        group.Dependencies.Should().HaveCount(2);
        group.Dependencies[0].Id.Should().Be("Dep1");
        group.Dependencies[0].VersionRange.Should().Be("[1.0.0, 2.0.0)");
        group.Dependencies[1].VersionRange.Should().BeNull();
    }
}
