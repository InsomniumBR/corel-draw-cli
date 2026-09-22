# corel-draw-cli

A command-line interface and interactive shell for driving a **running CorelDRAW
instance** via its COM automation object model (the same VBA-compatible API exposed by
`VGCore.dll`). Built as a generic, reusable tool — not tied to any specific project.

**The goal**: give you (or an AI agent) three interchangeable ways to manipulate
CorelDRAW — one-shot commands, a `.cdx` script for anything with more than a couple of
shapes, or an interactive shell for exploration — all sharing one command grammar, so
nothing you can do in one is unreachable from the others.

**Agent-compatible by design.** If you're pointing an AI coding agent at CorelDRAW
work, see [`AGENTS.md`](AGENTS.md) — it's the single source of truth for agent-facing
usage (setup, full command reference, gotchas, and the recommended workflow for
building a layout from scratch). This README covers the same tool for humans.

Every technical claim in this README was verified against a real, running CorelDRAW X7
instance, not assumed from documentation.

## How it works

CorelDRAW never registers itself in the Windows Running Object Table, so the usual
`GetObject`/`Marshal.GetActiveObject` approach used for Office automation doesn't work
here. Instead, `corel-draw-cli` attaches via `CoCreateInstance`
(`Type.GetTypeFromProgID` + `Activator.CreateInstance`), which — because CorelDRAW is a
single-instance COM server — reliably routes to your already-open CorelDRAW window,
regardless of which window currently has focus.

Every COM call goes through classic `Type.InvokeMember` reflection rather than C#'s
`dynamic` keyword — the .NET DLR's COM binder proved unreliable against this object
model during development (it failed to bind a real, existing method).

**A hard platform limitation worth knowing up front:** because CorelDRAW never
registers in the ROT, and a second `CorelDRW.exe` process is entirely invisible to COM,
there is no way to target a specific one of several *separate running CorelDRAW
processes*. "Which one to work on" only ever means picking among the **open documents**
within the one automatable process (`corel docs list` / `docs use`).

## Prerequisites

- Windows, with CorelDRAW installed (only **CorelDRAW X7** has been verified on real
  hardware; the tool is designed to work across versions via runtime reflection against
  whatever's actually installed, but other versions are best-effort/untested).
- .NET SDK **10.0** or later (`dotnet --version`).

## Getting started

Install as a .NET global tool (requires .NET SDK 10.0+):

```powershell
git clone https://github.com/InsomniumBR/corel-draw-cli
cd corel-draw-cli
dotnet pack src/Corel.Cli/Corel.Cli.csproj -c Release -o ./nupkg
dotnet tool install -g --add-source ./nupkg corel-draw-cli
```

This puts `corel` on your PATH. With CorelDRAW already open, run the one-time setup:

```powershell
corel detect           # finds your CorelDRAW installation(s) via the registry
corel schema generate  # caches that version's full object-model manifest
```

`detect` caches the result at `%LOCALAPPDATA%\corel-draw-cli\installs.json`.
`schema generate` scans the type library and caches a full capability manifest at
`%LOCALAPPDATA%\corel-draw-cli\schemas\corel-<version>.json` — this is what powers the
verb-availability warnings described below. Both only need to be run once (or again
after a CorelDRAW update, via re-running `detect`).

Working on the CLI itself instead of just using it? Run straight from source:
`dotnet run --project src/Corel.Cli -- <command>`.

## Command reference

### Curated verbs

| Command | Does |
|---|---|
| `corel doc new` | Create a blank document |
| `corel doc save [--as <path>]` | Save (`SaveAs` when `--as` is given; target directory must already exist) |
| `corel docs list` | List open documents; marks the active one |
| `corel docs use <name>` | Make a document active (calls its real `Activate()`) |
| `corel docs active` | Print the active document's name |
| `corel page size` | Print the active page's size |
| `corel page size --width W --height H [--unit in\|mm\|...]` | Resize the active page |
| `corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]` | Create a rectangle on the active layer |
| `corel shape list` | List shapes on the active layer |
| `corel shape delete --index N` | Delete a shape by its 1-based position (see `shape list`) |
| `corel macro install --name <Module> --file <path.bas>` | Inject VBA source into the active document's own VBA project |

Unit names accepted anywhere a `--unit` flag appears: `in`, `mm`, `cm`, `pt`, `px`, `ft`,
`yd`, `pica` (and a few long-form aliases).

### The `raw` escape hatch

Curated verbs cover the common cases. Everything else in CorelDRAW's object model —
several thousand properties and methods across ~200 interfaces — is reachable through
`raw`, which walks a dotted path from the `Application` root:

```powershell
corel raw get ActiveDocument.ActivePage.SizeWidth
corel raw set ActiveDocument.Unit 1
corel raw call ActiveDocument.ActiveLayer.CreateRectangle 0 1 1 0 null null null null
```

Pass the literal string `null` for any argument you want treated as an omitted optional
COM parameter (it maps to `Type.Missing` — this is how `CreateRectangle`'s four optional
corner-rounding parameters get skipped above).

### Interactive shell

```
> corel shell
corel-draw-cli - connected to CorelDRAW (Version 17.6.0.1021).
1 document(s) open. Use `docs list` / `docs use <name>`.
Type `exit` or `quit` to leave.

corel> docs list
  [1] Untitled-1  (active)

corel> page size --width 12 --height 19 --unit in
Page size set to 12x19 (in).

corel> shape create-rect --left 0 --top 19 --right 3 --bottom 0
Created rectangle at (0,0)-(3,19).

corel> raw get ActiveDocument.ActiveLayer.Shapes.Count
1

corel> exit
```

One persistent connection for the whole session — state (like which document is
active) carries across commands, same as it would inside CorelDRAW itself.

### Scripts (`.cdx`)

Plain text, one command per line, identical grammar to the shell minus the `corel`
prefix, `#` for comments:

```
# scripts/samples/hello-rectangle.cdx
page size --width 12 --height 19 --unit in
shape create-rect --left 0 --top 19 --right 3 --bottom 0
shape list
```

```powershell
corel script run scripts/samples/hello-rectangle.cdx
```

Before running anything, the script is **preflighted** against the active CorelDRAW
version's cached capability manifest: every line is resolved to its underlying verb and
checked for availability. If any command isn't present in that version's object model,
the whole run is reported and aborted up front — you don't find out three commands in
that something's missing. Pass `--force` to run anyway (each unavailable command still
prints a warning at the point it runs).

> Note: `.cdx` is also used elsewhere (Visual FoxPro/dBase compound index files,
> ChemDraw's molecule format). Unlikely to collide in practice for this niche use, but
> worth knowing if you have those tools installed too.

### Variables (`--vars`)

Any `.cdx` script can reference `{PLACEHOLDER}` tokens anywhere in any line — not just
in a save path. Pass their values with `--vars KEY=VALUE,KEY2=VALUE2,...`:

```powershell
corel script run some-script.cdx --vars WIDTH=12,HEIGHT=19,OUTPUT="C:\path\to\file.cdr"
```

If a script references a placeholder that wasn't provided, the run is rejected up
front, listing exactly which ones are missing — same fail-before-doing-anything
philosophy as the preflight check below — rather than substituting nothing and
producing a confusing failure partway through.

### Samples

`scripts/samples/` has complete, verified example scripts:

| Script | Builds |
|---|---|
| `hello-rectangle.cdx` | A minimal single-rectangle example, parameterized by `{WIDTH}`/`{HEIGHT}`/`{UNIT}` |
| `outer-aligned-12x19.cdx` | An 8-rectangle gallery-wall layout, 4 alternating columns, anchored so the biggest rectangles are exactly 12×19in and the rest scale proportionally from that anchor (the full arrangement is ~55.5×30.8in — much bigger than the 12×19 page, which is just a size reference, not a bounding box); needs `--vars OUTPUT=<path>` |
| `same-size-combination-12x19.cdx` | A 9-rectangle symmetric diamond where every rectangle is the same exact size (12×19in), same anchor-not-bounding-box approach; needs `--vars OUTPUT=<path>` |

```powershell
corel script run scripts/samples/hello-rectangle.cdx --vars WIDTH=12,HEIGHT=19,UNIT=in
corel script run scripts/samples/outer-aligned-12x19.cdx --vars OUTPUT="C:\path\to\Outer-Aligned-12x19.cdr"
```

(The target directory in `OUTPUT` must already exist.)

### Macros

```powershell
corel macro install --name RegenerateLayout --file regenerate.bas
```

This injects the given VBA source as a new standard module into the **active
document's own VBA project** — not global macro storage — so it's saved as part of the
`.cdr` file itself (verified: survives save → close → reopen).

**There is deliberately no `macro run`.** CorelDRAW's COM automation model has no way to
invoke document-embedded VBA code from an external process — the only run mechanism
found (`Application.GMSManager.RunMacro`) is scoped to macros loaded from `.gms` files,
and returns "Macro module not found" for anything in a document's own VBA project, no
matter how the module/macro name is formatted. There's also no hidden
`Application.Run` the way Excel has one. In practice this matches the intended
workflow anyway: you install the macro once, then run it yourself from inside
CorelDRAW — <kbd>Alt</kbd>+<kbd>F8</kbd>, or bound to a toolbar button or shortcut —
after changing the page size.

### Version pinning

If you have multiple CorelDRAW versions installed, pin one with `--corel-version`:

```powershell
corel --corel-version 17 raw get ActiveDocument.Name
```

Omit it to use the default (the first version `detect` found; change it by re-running
`detect`, or manually editing `installs.json`'s `defaultVersion`).

## Extending verb coverage

The curated verbs are deliberately a small, hand-picked slice — `raw` already reaches
the entire object model, generically, with zero code per member. Growing curated
coverage for a new area (or a future CorelDRAW version's new members) means adding an
entry to `src/Corel.Verbs/verbs.json`:

```json
{
  "name": "shape.create-rect",
  "group": "shape",
  "raw": { "call": "ActiveDocument.ActiveLayer.CreateRectangle" },
  "requiresInterface": "IVGLayer",
  "requiresMember": "CreateRectangle"
}
```

`requiresInterface`/`requiresMember` are the only fields anything reads at runtime —
they're what the availability check (`VerbGate`, one-shot commands) and script
preflight validate against a version's cached manifest. The `raw` field is
documentation only, for a human reading this file: it names the underlying operation
so you know what to point `requiresInterface`/`requiresMember` at. Adding a verbs.json
entry does not by itself make the verb do anything — you still need a command handler
under `src/Corel.Cli/Commands/` (wired into `CommandDispatcher`) that performs the
operation and calls `VerbGate.WarnIfUnavailable` with this verb's name. A generated
capability manifest is a very effective reference for picking real interface/member
names:

```powershell
corel schema generate
# see %LOCALAPPDATA%\corel-draw-cli\schemas\corel-17.json for every interface/member
# the type library actually declares
```

## Project layout

```
AGENTS.md              agent-facing reference (setup, commands, gotchas, workflow)
src/
  Corel.Schema/       type-library scanner + capability manifest model (no COM needed)
  Corel.Automation/   COM attach, late-bound invoker, install detection, VBA plumbing
  Corel.Verbs/        the curated verb registry (verbs.json) + availability checks
  Corel.Cli/          the `corel` executable: parsing, REPL, script runner, commands
tests/                unit tests for the COM-independent logic (manifest parsing,
                       verb availability, path/argument parsing) - no CorelDRAW
                       install required, safe to run in CI
scripts/samples/       example .cdx scripts
```

## Development

```powershell
dotnet build
dotnet test
```

Tests that need a live CorelDRAW instance don't exist by design — everything under
`tests/` runs without CorelDRAW installed. Manual verification against a real instance
is how every command in this README was checked; there's no CI story for the COM layer
itself, only for the logic layered on top of it.
