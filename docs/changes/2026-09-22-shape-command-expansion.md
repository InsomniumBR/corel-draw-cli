# Shape command expansion, and page-wide shape indexing

**Date:** 2026-09-22
**Branch:** worktree-agent-a69e368a207c00ec9
**Status:** Done
**Tracking:** No ADO/issue tracker — personal open-source repo (github.com/InsomniumBR/corel-draw-cli)

## What changed
- Eighteen new `shape` subcommands, grouped into new files under `src/Corel.Cli/Commands/`:
  - `ShapeCreateCommands.cs` — `create-ellipse`, `create-polygon`, `create-line`, `create-text`
    (artistic by default, paragraph with `--paragraph`).
  - `ShapeTransformCommands.cs` — `move`, `rotate`, `scale`, `flip`, `duplicate`, `order`.
  - `ShapeArrangeCommands.cs` — `group`, `ungroup`, `combine`, `break-apart`, `align`, `distribute`.
  - `ShapeStyleCommands.cs` — `fill`, `outline`.
- New shared helpers: `ShapeTargets.cs` (index/range resolution), `CorelEnums.cs`
  (manifest-backed enum-name → value), `RgbColor.cs` (`RRGGBB` parsing).
- `shape list`/`delete`/`rename` now index `ActivePage.Shapes` instead of
  `ActiveDocument.ActiveLayer.Shapes`.
- Grammar entries for every new subcommand in `CommandGrammar.cs`, gating entries for every
  new operation in `src/Corel.Verbs/verbs.json`, and `scripts/samples/badge-row.cdx`.

## Why
`shape` only had create-rect/list/delete/rename, so anything past a rectangle layout needed
`raw` — which cannot index a collection or hold an intermediate object, so most of the object
model was effectively out of reach from a script.

The layer-scoping fix is a real bug, verified live: with a second layer present,
`ActiveLayer.Shapes.Count` reported 1 while `ActivePage.Shapes.Count` reported 3, and
`shape list` showed the short set with nothing indicating why.

## Decisions
- **Ranges come from `Shapes.Range(indexArray)`, not the GUI selection.** The task brief
  expected an array argument not to bind and prescribed a select-each-shape-then-read-
  `Document.Selection` loop instead. Live testing showed the opposite: a real `int[]` handed
  to `Type.InvokeMember` binds fine (only `raw`, whose arguments are text, cannot express an
  array), and `ActiveDocument.Selection` returns an `IVGShape`, not an `IVGShapeRange` —
  `SelectionRange` is the range. `Shapes.Range` was chosen because it leaves the user's own
  selection alone.
- **Enum option words resolve through the cached capability manifest**, never a literal.
  `--axis vertical` → `cdrFlipVertical` → the value in the manifest. Without a manifest the
  command fails pointing at `corel schema generate` rather than guessing a number.
- **Align semantics follow what the object model actually does, not what it is named.**
  `IVGShapeRange.AlignAndDistribute` (the member matching the Align and Distribute docker)
  fails with `DISP_E_TYPEMISMATCH` for every argument shape tried; `IVGShape.AlignToPage` is
  a no-op; `IVGShapeRange.AlignToPage`/`AlignToPageCenter` align the range to its own first
  shape rather than to the page. What works is `AlignToShape(type, FirstShape)` and the
  `AlignRangeTo*` family, so `shape align` offers exactly those: `--to shapes` (default) and
  `--to page|page-center|grid`, which move the set as one block.
- **Colour is assigned into the shape's existing `Fill.UniformColor`/`Outline.Color`** via
  `RGBAssign`, which also converts a no-fill shape to a uniform fill in one step.
- **Z-order is one `shape order --to front|back|forward|backward`**, not four subcommands —
  `shape front` reads poorly next to `shape create-rect` and would add four grammar rows for
  one concept.

## Impact
- `shape list` index numbering changes for any document with more than one layer; a
  single-layer document is unaffected. Documented in `README.md` and `AGENTS.md`.
- No change to `raw`, `script`, `doc`, `docs`, `page`, `image` or `macro`.
- 303 unit tests pass (66 new). Every new subcommand was also exercised against a live
  CorelDRAW X7 instance, and the geometry read back with `shape list --json`; the scratch
  documents were closed afterwards.

## Deliberately out of scope
PowerClip, drop shadow, contour, extrude, envelope, lens, weld/trim/intersect and node-level
curve editing — all need constructed Shape/Color object arguments in multi-argument factory
calls whose signatures are not yet verified.

## Key files
- `src/Corel.Cli/Commands/ShapeTargets.cs` — index and range resolution, page-wide.
- `src/Corel.Cli/Commands/CorelEnums.cs` — manifest-backed enum resolution.
- `src/Corel.Cli/Commands/ShapeArrangeCommands.cs` — the align/distribute mapping.
- `tests/Corel.Cli.Tests/Commands/ShapeCommandsExpansionTests.cs` — command-level tests.
- `tests/Corel.Cli.Tests/Commands/FakeCorelObjects.cs` — the extended fake COM graph.
