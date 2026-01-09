using DotnetInspect.Core;

using FluentAssertions;

namespace DotnetInspect.UnitTests;

[TestClass]
public class ExceptionsTests
{
    [TestMethod]
    public void InvalidArgumentsException_HasCorrectExitCode()
    {
        InvalidArgumentsException ex = new("test");
        ex.ExitCode.Should().Be(1);
    }

    [TestMethod]
    public void PackageNotFoundException_HasCorrectExitCode()
    {
        PackageNotFoundException ex = new("TestPackage");
        ex.ExitCode.Should().Be(2);
        ex.Message.Should().Contain("TestPackage");
    }

    [TestMethod]
    public void VersionNotFoundException_HasCorrectExitCode()
    {
        VersionNotFoundException ex = new("TestPackage", "1.0.0");
        ex.ExitCode.Should().Be(3);
        ex.Message.Should().Contain("TestPackage");
        ex.Message.Should().Contain("1.0.0");
    }

    [TestMethod]
    public void FeedAccessException_HasCorrectExitCode()
    {
        FeedAccessException ex = new("test");
        ex.ExitCode.Should().Be(4);
    }

    [TestMethod]
    public void AuthenticationException_HasCorrectExitCode()
    {
        AuthenticationException ex = new("test");
        ex.ExitCode.Should().Be(5);
    }

    [TestMethod]
    public void NuspecParseException_HasCorrectExitCode()
    {
        NuspecParseException ex = new("test");
        ex.ExitCode.Should().Be(6);
    }

    [TestMethod]
    public void UnexpectedInternalException_HasCorrectExitCode()
    {
        UnexpectedInternalException ex = new("test");
        ex.ExitCode.Should().Be(7);
    }
}
