# dotnet-inspect

A cross-platform .NET global tool that inspects NuGet packages and prints their `.nuspec` metadata.

## Installation

```bash
dotnet tool install -g dotnet-inspect
```

## Usage

```bash
dotnet inspect <package> --version <version> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `<package>` | The NuGet package ID to inspect (required) |

### Options

| Option | Description |
|--------|-------------|
| `--version <version>` | The package version to inspect (required) |
| `--config <path>` | Path to a custom nuget.config file |
| `--format <format>` | Output format: `table` (default) or `json` |
| `--include-prerelease` | Include prerelease versions when resolving |

### Examples

Inspect a specific version of a package:

```bash
dotnet inspect Newtonsoft.Json --version 13.0.3
```

Output as JSON:

```bash
dotnet inspect Newtonsoft.Json --version 13.0.3 --format json
```

Use a custom NuGet configuration:

```bash
dotnet inspect MyPrivatePackage --version 1.0.0 --config ./nuget.config
```

## Output

### Table Format (default)

Displays package metadata in organized sections:
- **Metadata**: ID, Version, Description, Authors, License, etc.
- **Repository**: URL, Type, Commit (if available)
- **Dependencies**: Grouped by target framework

### JSON Format

Returns a JSON array containing the package metadata:

```json
[
  {
    "id": "Newtonsoft.Json",
    "version": "13.0.3",
    "nuspec": {
      "description": "Json.NET is a popular high-performance JSON framework for .NET",
      "authors": "James Newton-King",
      "licenseExpression": "MIT",
      ...
    }
  }
]
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid arguments |
| 2 | Package not found |
| 3 | Version not found |
| 4 | Feed access error |
| 5 | Authentication failure |
| 6 | Nuspec parse error |
| 7 | Unexpected internal error |

## Authentication

The tool supports authenticated NuGet feeds through:
- Credentials in `nuget.config`
- Azure Artifacts Credential Provider
- Other NuGet credential plugins

## License

MIT
