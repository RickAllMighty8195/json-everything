---
applyTo: "*.cs,*.csproj"
---

# Architecture Instructions

This repository is a multi-package .NET library suite centered on JSON tooling for `System.Text.Json`.

## Solution Structure

- Core libraries are under `src/<PackageName>/`.
- Tests are typically under `src/<PackageName>.Tests/`.
- Shared testing helpers are in `src/TestHelpers/`.
- Supporting tools are in `tools/`.
- A Blazor WebAssembly site lives in `json-everything.net/`.

## Project Organization

- Keep package concerns isolated by folder and project.
- Add new functionality to the most specific existing package when possible.
- Add a new package only when behavior does not fit existing package boundaries.
- Keep package names and namespaces aligned with existing patterns (`Json.*`, `Yaml2JsonNode`, etc.).

## Target Frameworks And Packaging

- Library projects are commonly multi-targeted (`netstandard2.0`, `net8.0`, `net9.0`, `net10.0`).
- Do not remove target frameworks unless explicitly requested.
- Preserve signing, package metadata, and release-note links in project files unless task requirements say otherwise.

## Dependency Direction

- Prefer low coupling between packages.
- Use project references intentionally and avoid circular dependencies.
- Keep external dependencies minimal and justified.

## Behavior Changes

- Any behavioral change should include tests in the corresponding `*.Tests` project.
- Avoid broad refactors when a local fix is sufficient.
