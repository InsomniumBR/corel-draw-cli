# Generic `--vars` script templating

**Date:** 2026-09-21
**Branch:** main
**Status:** Done
**Tracking:** No ADO/issue tracker — personal open-source repo (github.com/InsomniumBR/corel-draw-cli)

## What changed
- Added `src/Corel.Cli/ScriptVars.cs`: pure `Parse`/`Validate`/`Apply` functions for a
  `--vars KEY=VALUE,KEY2=VALUE2` flag on `corel script run`.
- Any `.cdx` script line can contain `{PLACEHOLDER}` tokens anywhere (not just a save path);
  `Apply` substitutes them all before execution.
- `Validate` rejects the run up front, listing every referenced placeholder with no supplied
  value, instead of substituting nothing and failing partway through the script.
- Removed the earlier, narrower `--output`/`${OUTPUT}` mechanism (added one commit prior,
  `cd44c65`) and its test file `ScriptRunnerOutputSubstitutionTests.cs`, replaced by
  `ScriptVarsTests.cs` (8 tests).
- Updated `Program.cs` (parses the new `--vars` global flag) and `ScriptRunner.cs` (calls
  `ScriptVars.Validate`/`Apply` instead of the old output-specific logic).
- Updated `README.md` and `AGENTS.md` to document `--vars`/`{PLACEHOLDER}` generically.
- Updated all three sample scripts (`scripts/samples/hello-rectangle.cdx`,
  `outer-aligned-12x19.cdx`, `same-size-combination-12x19.cdx`) and the production copies in
  `D:\My Drive\FancyLabz\Products\Layouts\scripts\` to use the new flag.

## Why
`corel script run` needed some way to parameterize a script (at minimum, an output path) so
the same `.cdx` file could be reused across runs/destinations. A first pass implemented this
narrowly as `--output <path>` + `${OUTPUT}`. The user pushed back on the narrow scope directly:
"isn't better to create generic 'environment variables' that you pass and that could be used
by the script as you want?" — with a concrete example of a multi-variable design
(`--vars OUTPUT=...,WIDTH=12,HEIGHT=19,UNIT=INCHES`) and explicit approval of the flag name
("I like the name --vars though").

## Alternatives considered
- Keep `--output`/`${OUTPUT}` as a special case and add other flags per variable as needed —
  rejected: doesn't scale, every new script need would require a new CLI flag.
- Environment-variable-style implicit lookup (read from the OS environment) — rejected in
  favor of an explicit, script-run-scoped `--vars` flag: keeps script runs self-contained and
  reproducible from the command line alone.

## Impact
- Breaking change to the (very recently added, not yet released) `--output` flag — removed
  outright rather than deprecated, since it had not shipped in any published release.
- No impact on curated verbs, REPL, or `raw` — isolated to `script run`.
- All 34 unit tests pass; verified end-to-end against a live CorelDRAW instance (both the
  missing-variable rejection path and successful runs producing real `.cdr` output for all
  three sample scripts).

## Key files
- `src/Corel.Cli/ScriptVars.cs` — the new pure `Parse`/`Validate`/`Apply` implementation.
- `src/Corel.Cli/ScriptRunner.cs` — wires `ScriptVars` into the script execution path.
- `src/Corel.Cli/Program.cs` — parses the `--vars` global flag before dispatch.
- `tests/Corel.Cli.Tests/ScriptVarsTests.cs` — 8 tests covering parse/validate/apply.
- `README.md`, `AGENTS.md` — human- and agent-facing docs for the feature.

## Related
- **ADR:** [ADR-001: Script parameterization uses a generic `--vars`/`{PLACEHOLDER}` mechanism, not single-purpose flags](../adrs/001-generic-vars-mechanism.md)
