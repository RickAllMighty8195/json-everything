; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 7.3.5

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
JSGEN001 | JsonSchemaGeneration | Error | Open generic types are not supported
JSGEN002 | JsonSchemaGeneration | Error | GeneratedJsonSchemas class must be partial
JSGEN003 | JsonSchemaGeneration | Warning | Duplicate schema property name

