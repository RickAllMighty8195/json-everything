; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
JSGEN001 | JsonSchemaGeneration | Error | Open generic types are not supported
JSGEN002 | JsonSchemaGeneration | Warning | Recursive types are not fully supported in MVP
JSGEN003 | JsonSchemaGeneration | Warning | Attribute not supported by source generator
JSGEN004 | JsonSchemaGeneration | Error | Type not supported by source generator
