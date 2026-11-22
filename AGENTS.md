# Repository Guidelines

## Project Structure & Module Organization

The current checkout contains an exported dataset under `download/`; converter source code, dependency manifests, and build configuration are absent.

- `download/conversations-*.json`: numbered conversation export files.
- `download/pages/` and `download/sites/`: exported page content and site metadata.
- `download/*.dat`: exported assets; preserve their filenames and references.
- `download/index.html`: bundled export viewer.

Treat `download/` as input data. When adding a converter, place implementation in `src/`, synthetic fixtures and automated checks in `tests/`, and generated Markdown in a separate output directory.

## Build, Test, and Development Commands

Run these PowerShell commands from the repository root:

- `Get-ChildItem -Force`: inspect root files and directories.
- `Get-ChildItem download -File`: inventory top-level export files.
- `Get-Content -Raw download/export_manifest.json | ConvertFrom-Json`: check that the export manifest parses as JSON; this does not validate its schema or asset references.

No build, application startup, or automated test command is currently configured. Document the exact installation, execution, and test commands when introducing implementation code.

## Coding Style & Naming Conventions

No language-specific style configuration, formatter, or linter is present. For new code, use consistent indentation and descriptive names, then configure the chosen language's formatter. Use UTF-8 for Markdown output. Preserve exported identifiers, numbered filenames, and relative asset paths. Keep conversion logic separate from filesystem operations so formatting behavior can be tested independently.

## Testing Guidelines

No testing framework or coverage threshold is defined. Add synthetic fixtures covering Unicode, multiline messages, code fences, missing fields, asset links, and filename collisions. Name tests after observable behavior, such as `preserves_code_fences`. Compare generated Markdown with explicit expected output and confirm original inputs remain unchanged.

## Commit & Pull Request Guidelines

Git metadata and commit history are unavailable in this checkout, so no existing message convention can be verified. Use concise imperative subjects, such as `Add conversation Markdown conversion`.

Describe the change, relevant issues, verification commands and results, and a sanitized input/output example. Include screenshots when changing rendered presentation.

## Data Handling

The export includes user and conversation data. Use synthetic examples in tests, documentation, logs, and pull requests. Keep original exports intact and review generated files for private content before sharing or committing them.
