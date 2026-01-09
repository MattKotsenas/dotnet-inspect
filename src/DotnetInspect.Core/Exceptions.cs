namespace DotnetInspect.Core;

public abstract class InspectException : Exception
{
    public abstract int ExitCode { get; }

    protected InspectException(string message) : base(message) { }
    protected InspectException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidArgumentsException : InspectException
{
    public override int ExitCode => 1;
    public InvalidArgumentsException(string message) : base(message) { }
}

public class PackageNotFoundException : InspectException
{
    public override int ExitCode => 2;
    public PackageNotFoundException(string packageId)
        : base($"Package '{packageId}' was not found on any configured feed.") { }
}

public class VersionNotFoundException : InspectException
{
    public override int ExitCode => 3;
    public VersionNotFoundException(string packageId, string version)
        : base($"Version '{version}' of package '{packageId}' was not found.") { }
}

public class FeedAccessException : InspectException
{
    public override int ExitCode => 4;
    public FeedAccessException(string message) : base(message) { }
    public FeedAccessException(string message, Exception innerException) : base(message, innerException) { }
}

public class AuthenticationException : InspectException
{
    public override int ExitCode => 5;
    public AuthenticationException(string message) : base(message) { }
    public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}

public class NuspecParseException : InspectException
{
    public override int ExitCode => 6;
    public NuspecParseException(string message) : base(message) { }
    public NuspecParseException(string message, Exception innerException) : base(message, innerException) { }
}

public class UnexpectedInternalException : InspectException
{
    public override int ExitCode => 7;
    public UnexpectedInternalException(string message) : base(message) { }
    public UnexpectedInternalException(string message, Exception innerException) : base(message, innerException) { }
}
