# Initial build: corel-draw-cli (M0–M10)

**Date:** 2026-09-19
**Branch:** main
**Status:** Done
**Tracking:** No ADO/issue tracker — personal open-source repo (github.com/InsomniumBR/corel-draw-cli)

## What changed
Built the entire tool from scratch in one session, commit-per-milestone:

- `f1a53d4` (M0) — scaffolded the solution: `Corel.Automation`, `Corel.Schema`, `Corel.Verbs`,
  `Corel.Cli` projects plus their test projects.
- `0a93edb` (M1) — COM attach (`ComAttach.cs`, via `Type.GetTypeFromProgID` +
  `Activator.CreateInstance`) and the late-bound invoker core (`ComInvoker.cs`, via
  `Type.InvokeMember`).
- `b6f50c8` (M2) — `TypeLibScanner.cs`: walks `vgcoreauto.tlb` via `ITypeInfo` to produce a
  ground-truth `CapabilityManifest` (every interface/member CorelDRAW's object model actually
  exposes), cached at `%LOCALAPPDATA%\corel-draw-cli\schemas\corel-<version>.json`.
- `35f0f47` (M3) — CorelDRAW install detection (registry scan for
  `CorelDRAW.Application.<N>`) and `--corel-version` resolution across multiple installs.
- `046156c` (M4) — the curated verb registry (`src/Corel.Verbs/verbs.json`): an extensible
  mapping from friendly verb names to raw COM paths, each with an availability check
  (`requiresInterface`/`requiresMember`) against the cached manifest.
- `bd78ded` (M5) — real CLI dispatch: `raw get/set/call`, `page size`, `shape create-rect/list/
  delete`, `detect`, `schema generate`.
- `7d84c49` (M6) — interactive REPL (`corel shell`) with `docs list/use/active`, one persistent
  COM connection per session.
- `6ccc0d6` (M7) — `.cdx` script runner with preflight validation (every line resolved to a
  verb and checked against the capability manifest before anything executes).
- `95e88cd` (M8) — `macro install`: injects VBA source into the active document's own VBA
  project via `InitializeVBA()` + `VBComponents.AddFromString` (not global macro storage).
- `2b27d7d` (M9) — README with usage samples verified against the live instance.
- `45be22e` (M10) — packaged as a `dotnet` global tool.
- `4e940d3`, `a1e9bb1` — added `doc new`/`doc save --as` (a real gap found after M10), made VBA
  embedding opt-in via `--embed-vba` rather than default-on.

## Why
Needed a way to drive a live, already-running CorelDRAW X7 instance from code, to eventually
generate FancyLabz gallery-wall layouts as CorelDRAW rectangles for later PowerClip use. No
existing open-source .NET wrapper for CorelDRAW's COM/VBA automation model was found anywhere.
The user explicitly directed this be built as a generic, reusable, open-sourced tool first
(`github.com/InsomniumBR/corel-draw-cli`) — not a narrow, project-specific script — with three
interchangeable usage modes (one-shot commands, a REPL, `.cdx` scripts), an extensible
curated-verb-to-raw-path mapping, auto-detection of installed CorelDRAW versions, per-version
capability manifests, and availability warnings/script preflight. A formal plan
(`agile-fluttering-waterfall.md`) was written and approved before implementation, itself
preceded by a structured grilling session covering architecture risk (late-bound COM vs.
generated interop, ROT-attach reliability, CLI/layout scope boundary).

## Alternatives considered
- **Generate a `tlbimp` interop assembly per machine** instead of late-bound `Type.InvokeMember`
  — rejected: would need regenerating per user/per CorelDRAW version, against the explicit
  goal of a tool that works across versions without per-machine setup steps.
- **C#'s `dynamic` keyword** for COM member access — tried and rejected: the DLR's COM binder
  failed to bind a real method during development; `Type.InvokeMember` with combined
  `BindingFlags.GetProperty | BindingFlags.InvokeMethod` was the only reliable approach found.
- **`Marshal.GetActiveObject`/ROT-based attach** — tried and rejected: CorelDRAW never
  registers itself in the Windows Running Object Table, confirmed empirically (including with
  two processes running simultaneously); switched entirely to `CoCreateInstance`-style attach
  via `Activator.CreateInstance` against the ProgID, which reliably routes to the one running
  instance regardless of window focus.
- **Auto-generating curated verb stubs from the capability manifest** for the full ~30-interface
  surface — deferred as a v2 idea; the v1 curated slice (~18 verbs) was hand-written since a
  blind generator can't pick sane defaults for optional params, unit handling, or help text.
- **A `macro run` command** — investigated and abandoned: CorelDRAW's COM model has no way to
  invoke a document-embedded VBA macro from an external process (every plausible API was tried
  and fails); `macro install` embeds the module into the document's own VBA project instead,
  to be run manually from inside CorelDRAW (Alt+F8 or a bound shortcut) afterward.
- **Multi-process instance targeting** (picking among several separate running `CorelDRW.exe`
  processes) — ruled out as impossible: a second process is entirely invisible to COM since
  CorelDRAW never registers in the ROT; "instance selection" only ever means picking among
  open `Documents` within the one automatable process.

## Impact
- Established the tool's whole shape going forward: three usage modes sharing one command
  grammar, ground-truth-verified object model access (no guessed member names), and an
  extensible verb registry as the intended path for growing curated coverage.
- `dotnet tool pack`/`install` required switching `Corel.Cli`/`Corel.Automation`/test projects
  from `net10.0-windows` to plain `net10.0` (`PackAsTool` rejects platform-specific TFMs —
  NETSDK1146), with `[assembly: SupportedOSPlatform("windows")]` added to keep analyzers
  accurate.
- No CI story exists for the COM layer itself (by design — everything under `tests/` is pure
  logic, runnable without CorelDRAW installed); every live-instance claim in the README/AGENTS.md
  was verified manually against the real X7 install on this machine.
- Downstream consumer work (the two FancyLabz gallery-wall layouts) was deliberately kept out
  of this repo — this repo stays generic, layout-specific `.cdx` scripts and geometry live in
  `D:\My Drive\FancyLabz\Products\Layouts\`.

## Key files
- `src/Corel.Automation/ComAttach.cs`, `ComInvoker.cs` — the COM attach + late-bound invocation
  core everything else is built on.
- `src/Corel.Schema/TypeLibScanner.cs`, `CapabilityManifest.cs` — ground-truth object model
  scanning, the alternative to guessing method/property names.
- `src/Corel.Verbs/verbs.json` — the extensible curated-verb-to-raw-path mapping.
- `src/Corel.Cli/Program.cs`, `Repl.cs`, `ScriptRunner.cs`, `Commands/*.cs` — the three usage
  modes (one-shot, shell, `.cdx` scripts) sharing one dispatch path.
- `README.md` — human-facing usage reference, verified against the live instance.

## Related
- **ADR:** [ADR-002: Attach to CorelDRAW via late-bound COM (Type.InvokeMember + ProgID CoCreateInstance), never generated interop or the ROT](../adrs/002-late-bound-com-attach.md)
