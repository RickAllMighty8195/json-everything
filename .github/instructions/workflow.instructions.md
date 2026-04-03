---
applyTo: "**"
---

# Agent Workflow

This file defines how AI agents should orient themselves before making changes in this repository.

## Before Making Any Code Changes

1. List instruction files in `.github/instructions/*.instructions.md` and read relevant files for the task.
2. If present, read `.notes/personal-preferences.instructions.md` for local user preferences that are intentionally not committed.
   - Project instructions in `.github/instructions/` always take precedence over personal preferences.
3. Always read these first:
   - `workflow.instructions.md`
   - `commands.instructions.md`
4. Read additional files by concern:
   - `code-style.instructions.md` when editing C# code.
   - `architecture.instructions.md` when adding projects, files, package references, or changing structure.
   - `tests.instructions.md` when editing tests or changing behavior.
   - `apis.instructions.md` when editing `src/JsonSchema.Api/` or its tests.
   - `domain-knowledge.instructions.md` when implementing behavior tied to JSON specifications or package intent.
   - `instructions.instructions.md` when editing `.instructions.md` files.
5. For C# style and formatting, treat `.editorconfig` and `json-everything.sln.DotSettings` in the repo root as authoritative.
6. Prefer minimal, targeted changes. Do not refactor unrelated code.

## Principles

- Treat instruction files as the desired state for future contributions.
- Preserve public API compatibility unless the task explicitly requires a breaking change.
- Keep changes aligned with the repository's library-first design (packages under `src/`).
- Add or update tests for behavioral changes.
- Keep instructions dynamic. Do not wait for an explicit request to update instruction files when feedback reveals a missing or incorrect rule.
- If the user corrects a mistake or asks for a specific behavior — even once — update the relevant instruction file immediately alongside the code fix, without waiting to be asked.
