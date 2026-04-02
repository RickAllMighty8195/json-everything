---
applyTo: "*.cs"
---

# Code Style Instructions

This document covers C# style conventions that complement `.editorconfig`.

## Source Of Truth

- `.editorconfig` is authoritative for formatting, indentation, brace style, and naming rules.
- `json-everything.sln.DotSettings` (JetBrains/ReSharper) is also authoritative where it defines style or inspection behavior.
- Follow existing style in nearby files when rules are not explicit.

## Required Practices

- Use file-scoped namespaces.
- Keep nullable reference types enabled and handle nullability explicitly.
- Prefer immutable design where practical (`readonly` fields, init-only or constructor initialization).
- Use expressive type and member names; avoid abbreviations unless they are established JSON terms.
- Keep methods focused and small.

## Comments And Documentation

- Do not add comments that restate obvious code flow.
- Public package APIs commonly include XML documentation in this repo. When adding or changing public APIs, preserve existing documentation quality and style.

## JSON And Serialization Patterns

- Preserve `System.Text.Json`-centric patterns used by the repository.
- Use shared serializer contexts/options when already established by the package.
- Avoid introducing alternate JSON stacks unless explicitly requested.

## Compatibility Mindset

- Avoid unnecessary public API churn.
- Keep binary and source compatibility in mind for package consumers.
