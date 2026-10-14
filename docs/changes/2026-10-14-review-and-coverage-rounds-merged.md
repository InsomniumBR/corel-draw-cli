# Code-quality/UX review round and CorelDRAW coverage-expansion round, both merged to main

**Date:** 2026-10-14
**Branch:** main
**Status:** Done
**Tracking:** No ADO/issue tracker — personal open-source repo (github.com/InsomniumBR/corel-draw-cli).
Tracked on this repo's own local taskboard (`.claude/tasks/board.json`, non-synced), tasks 13-26.

## What changed

Two separate rounds of parallel-agent work, each followed by a manual merge-and-verify pass,
landed on `main` across this session (main went `b60707c` → `7750074` → `741edb9`):

**Round 1 (tasks 13-21)** — an Opus code-quality/UX review of the CLI found 37 findings; 9 tasks
fixed them in parallel git worktrees:
- Validate-before-mutate bugs in `page size`/`shape create-rect` (task 13).
- `SaveDialogWatchdog` scoped to the actual CorelDRAW process instead of system-wide window
  matching; exception-masking fixed (task 14).
- Verb registry hygiene — gated every verb with a real handler, removed 3 dead `verbs.json`
  entries, documented `VerbDefinition.Raw` as non-functional rather than faking it (task 15).
- Script execution & CLI dispatch overhaul — `CommandGrammar.cs`/`CommandValidator.cs` as the
  single source of truth for help/validation/preflight, `ScriptSafety.cs` (denylist for
  Shell/WScript/FileSystemObject/registry/VBA-`Declare` patterns + path traversal), lazy COM
  connect via `CorelSession`, `script run` reachable from `corel shell`, dark-yellow
  missing-variable warnings, REPL exit-code propagation, tokenizer quote-escaping (task 16).
- Friendly "no document open" error, `raw` errors hint at `corel schema generate`, narrowed
  exception handling in `MacroCommands` (task 17).
- `ArgReader` flag-swallowing bug fixed, `docs use` accepts an index and rejects ambiguous
  name matches, save-path directory guard, stale internal comments removed (task 18).
- `shape list` resolves real `cdrShapeType` names from the manifest, `--json` output, `raw get`
  shows a COM object's resolved interface name (task 19).
- Husky pre-commit hooks, `.editorconfig`, `Directory.Build.props` (task 20).
- Test coverage backfill for previously-untested units (task 21).

**Round 2 (tasks 22-26)** — a second Opus audit, this time generating and reading the *real*
CorelDRAW X7 capability manifest (251 interfaces, 5,641 members) rather than relying on
documentation, found that only 11 curated drawing commands existed and that `raw`'s README claim
of reaching "the entire object model" was false (4 verified hard limits: no collection indexing
past `.First`/`.Last`/`.Next`, no object arguments, no binding a returned object across calls, no
by-ref/out parameters). 5 tasks addressed this:
- 18 new `shape` subcommands — create-ellipse/polygon/line/text, transforms, duplicate, z-order,
  group/ungroup/combine/break-apart, align/distribute, fill/outline — plus a real fix for
  `shape list`/`delete`/`rename` only ever seeing `ActiveLayer` instead of the whole page
  (task 22, by far the largest).
- `doc export --format png|jpg|svg|pdf` (via a real held `CreateStructExportOptions()` object)
  and `doc undo`/`doc redo` (task 23).
- New `page add/delete/goto/list` and an entirely new `layer list/add/use/delete` group
  (task 24).
- `raw get`'s error message now distinguishes a genuine path-walk failure from a member that was
  invoked and returned nothing (e.g. `raw get ActiveDocument.Close`); `doc.new`/`docs.use`
  gating entries added (task 25).
- README/AGENTS.md corrected to state `raw`'s real 4 hard limits explicitly, plus the 3 working
  idioms found (`Shapes.All`, `Fill.UniformColor.RGBAssign`, `PublishToPDF`) (task 26).

**Both rounds' merges** were done in an isolated worktree (`merge-integration`, then
`merge-integration-2`) rather than the shared `main` checkout, specifically because a peer Claude
Code session was independently developing an image-import/PowerClip feature directly on `main` at
the same time during round 1 — see Decisions below.

## Why

The two audits (both run by Opus, both grounded in live verification against a running CorelDRAW
X7 instance and/or the generated capability manifest rather than assumption) found real,
previously-undetected gaps: the first in code quality/UX, the second in how much of CorelDRAW's
actual automation surface the tool could reach at all. Fixing both by hand sequentially would have
taken far longer than running focused parallel agents per finding cluster and then reconciling.

## Decisions

- **Group tasks by file/command-group boundary, not by review-finding category.** The project's
  own convention is one command-group per file (`ShapeCommands.cs`, `DocCommands.cs`, etc.) and a
  single `CommandGrammar.cs` table — splitting work by finding instead of by file guarantees
  repeated merge conflicts in the same hot spots. Both rounds kept the largest, most
  architecturally entangled cluster (task 16, then task 22) as one deliberately large task rather
  than parallelizing it further.
- **Do all multi-branch integration work in an isolated worktree, never the shared `main`
  checkout**, once it became clear another live session was editing `main` concurrently. Adopted
  for the rest of this session and for round 2 from the start.
- **Verify the actual committed content after every fast-forward, not just a worktree's local
  test run.** A real incident in round 1: an edit made after `git add` was never re-staged before
  `git commit`, so it silently didn't make it into that merge commit despite tests passing
  locally at the time (tests read from disk, not the git index). Only caught by rebuilding `main`
  fresh after the fast-forward and rerunning the suite. Every commit in round 2 was preceded by
  `git diff --cached --stat` compared against what was actually intended, and this did not recur.
- **FakeCorelObjects.cs's shared-shape-list design** (round 2's hardest conflict): task 22 fixed
  the real "only sees ActiveLayer" bug by giving a fake page one shared shape list; task 24
  independently added multi-page/multi-layer support. Resolved by making every layer created on a
  page share that page's one list — generalizes the bug fix into the multi-layer model instead of
  the merge silently reverting one side.
- **When a task's live testing contradicted its own brief, the live result won.** E.g. task 22
  was told to loop `AddToSelection` per shape because index arrays were assumed not to bind
  through the generic invoker; live testing showed `Shapes.Range(int[])` binds fine from curated
  C# (only `raw`'s *text* parser can't express an array) — shipped the simpler, more correct
  version instead of the originally-prescribed workaround.

## Impact

- `main` was local-only through both rounds (never pushed to `origin` since the project began);
  pushed to `origin/main` later in this same session once the user asked for it, carrying both
  rounds plus a small README fix along with it.
- Test count progressed 34 → 238 (round 1) → 341 (round 2), all passing on a full clean rebuild
  of `main` itself, not just a worktree.
- `shape` command coverage went from 4 subcommands to 22; overall curated commands from roughly
  11 to roughly 40.
- `shape list`/`delete`/`rename` index numbering is now page-wide — changes for any multi-layer
  document, documented in README/AGENTS.md as a breaking behavior change from the prior
  layer-scoped indexing.
- A peer Claude Code session's independently-developed image-import/PowerClip/shape-rename
  feature (commit `d7ece29`) is integrated alongside both rounds and was confirmed still working
  (regression-tested by that session) after each round's merge.

## Deliberately out of scope (both rounds)

- Making `raw` itself general-purpose (path indexing, object-argument support, a result-binding
  handle table, `ParameterModifier` for out params) — flagged to the user as a separate,
  larger architectural decision rather than bundled into either round.
- PowerClip, drop-shadow/contour/extrude/envelope/lens effects, weld/trim/intersect, node-level
  curve editing — all need constructed Shape/Color object arguments with signatures that need
  more live research; correctly deferred per the round-2 audit's own recommendation.

## Key files

- `src/Corel.Cli/CommandGrammar.cs` — single source of truth for help/validation/preflight;
  touched by nearly every task in both rounds.
- `src/Corel.Cli/CommandDispatcher.cs`, `src/Corel.Cli/ScriptRunner.cs`, `src/Corel.Cli/Program.cs`
  — the dispatch/preflight pipeline rebuilt in task 16.
- `src/Corel.Cli/Commands/Shape*.cs` — the expanded shape command surface from task 22.
- `src/Corel.Cli/Commands/LayerCommands.cs` — new command group from task 24.
- `src/Corel.Verbs/verbs.json` — gating entries added across almost every task.
- `tests/Corel.Cli.Tests/Commands/FakeCorelObjects.cs` — the shared fake COM graph, redesigned
  twice during merge to satisfy both rounds' test suites simultaneously.
- `AGENTS.md`, `README.md` — command reference and the corrected `raw` escape-hatch section.
