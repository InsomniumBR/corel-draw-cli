# AGENTS.md — guidance for AI agents using corel-draw-cli

This file is for any AI coding/automation agent (Claude Code, or any other) that needs
to drive CorelDRAW, or that's working on this repo's own source. It's the single
source of truth for agent-facing usage — the README covers the same tool for humans,
with more prose and less "do this exact thing."

## What this tool is, in one paragraph

`corel-draw-cli` (binary name `corel`) attaches to an **already-running** CorelDRAW
instance via COM automation and drives it: create/resize/delete shapes, resize pages,
switch between open documents, inject VBA macros, and reach the entire object model
generically when a curated command doesn't exist yet. There are exactly three ways to
drive it, all sharing one grammar:

1. **One-shot commands** — `corel <command> [args...]`, one process per command.
2. **A `.cdx` script** — `corel script run <file.cdx>` — plain text, one command per
   line, no `corel` prefix, `#` comments. The natural choice for building anything with
   more than 2-3 shapes: compute every coordinate up front, write one script, run it
   once.
3. **An interactive shell** — `corel shell` — a REPL holding one persistent connection.
   Rarely what an agent wants; scripts are more reproducible and reviewable.

If you're an agent about to build a layout, design, or any multi-step CorelDRAW
manipulation: **write a `.cdx` script and run it**, don't drive the shell interactively
one command at a time, and don't shell out to `corel` once per shape either (each
one-shot invocation is a fresh COM attach — fine for a single command, wasteful and
slower for a sequence).

## Before you do anything

1. **CorelDRAW must already be open** on the machine you're running on. This tool does
   not launch it.
2. Check `corel` is installed and on PATH: run `corel` with no arguments. If that
   fails, it's not installed yet:
   ```powershell
   cd S:\GIT\corel-draw-cli   # or wherever this repo is cloned
   dotnet pack src/Corel.Cli/Corel.Cli.csproj -c Release -o ./nupkg
   dotnet tool install -g --add-source ./nupkg corel-draw-cli
   ```
3. **First-time-ever setup** (skip if `%LOCALAPPDATA%\corel-draw-cli\installs.json`
   already exists):
   ```powershell
   corel detect           # finds installed CorelDRAW version(s) via the registry
   corel schema generate  # caches that version's full object-model manifest
   ```
   Only needs to be redone if CorelDRAW itself is updated/reinstalled.

## Facts that will bite you if you skip them

- **Coordinates are Y-up.** `Left`/`Top`/`Right`/`Bottom` on `shape create-rect` are in
  the document's current unit, with the page's bottom-left corner at `(0, 0)` and Y
  increasing **upward**. A rectangle spanning the full height of a 12"-tall page is
  `--top 12 --bottom 0`, not `--top 0 --bottom 12`.
- **`--corner-radius` is 0–100 roundness**, not a radius in inches/points.
- **Units**: `in`, `mm`, `cm`, `pt`, `px`, `ft`, `yd`, `pica` (plus long-form aliases).
  A brand-new document defaults to inches.
- **"Which document" means an open document, never a separate OS process.** CorelDRAW
  never registers itself in the Windows Running Object Table, and a second
  `CorelDRW.exe` process is entirely invisible to COM. There is no way to target one of
  several separate running CorelDRAW processes — only `corel docs list` / `docs use
  <name>` to pick among documents open in the one automatable instance.
- **There is no `macro run`.** CorelDRAW's COM model has no way to invoke
  document-embedded VBA code from outside the app (confirmed empirically — every
  plausible API for this was tried and fails). `macro install` embeds a module into
  the document's own VBA project so it's saved with the `.cdr` file; a human runs it
  from inside CorelDRAW afterward (Alt+F8, or a bound shortcut).
- **`doc save --as <path>` needs the target directory to already exist.** Saving to a
  path whose folder doesn't exist doesn't just fail cleanly — it can leave the document
  closed. Create the directory first if you're not sure it exists.
- **Quote paths with spaces in `.cdx` scripts** exactly like a shell would:
  `doc save --as "C:\Users\name\My Documents\file.cdr"`. The script tokenizer honors
  double quotes and strips them.
- Only **CorelDRAW X7** is verified on real hardware. The tool is built to work across
  versions via runtime reflection against whatever's actually installed (no hardcoded
  per-version assumptions), but treat other versions as best-effort until verified.

## Full command reference

```
corel doc new                                    # create a blank document
corel doc save [--as <path>]                     # save (SaveAs when --as is given)
corel docs list                                  # list open documents, marks active
corel docs use <name>                            # switch the active document
corel docs active                                # print the active document's name

corel page size                                  # print active page size
corel page size --width W --height H [--unit U]  # resize active page

corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]
corel shape list                                 # list shapes on the active layer
corel shape delete --index N                     # delete by 1-based position

corel macro install --name <Module> --file <path.bas>

corel raw get <dotted.path>                      # e.g. ActiveDocument.ActivePage.SizeWidth
corel raw set <dotted.path> <value>
corel raw call <dotted.path> [args...]           # pass the literal string `null` for
                                                  # an omitted optional COM parameter

corel script run <file.cdx> [--force]            # run a script
corel shell                                      # interactive REPL

corel detect / corel schema generate             # (re-)run first-time setup
```

Global flag: `--corel-version N` pins a specific installed version if more than one is
registered; omit to use the detected default.

### The `raw` escape hatch

Curated verbs above cover the common cases. Everything else in CorelDRAW's object
model — several thousand properties and methods across ~200 interfaces — is reachable
through `raw`, which walks a dotted path from the `Application` root:

```
raw get ActiveDocument.ActivePage.SizeWidth
raw set ActiveDocument.Unit 1
raw call ActiveDocument.ActiveLayer.CreateRectangle 0 1 1 0 null null null null
```

If you need something not in the curated list, don't guess a method name — check the
generated capability manifest first (`corel schema generate`, then read
`%LOCALAPPDATA%\corel-draw-cli\schemas\corel-<version>.json`) so you're calling a
member that's actually confirmed to exist, with its real parameter names.

## Building a layout — the recommended agent workflow

1. **Compute every rectangle's coordinates up front**, in a scratch calculation, before
   writing any CorelDRAW commands. Remember Y-up. If the design has a "biggest" element
   that should be an exact target size with everything else proportional to it, scale
   from that anchor — don't assume the page/canvas size is a bounding box unless that's
   explicitly what's wanted (see `scripts/samples/` for both variants).
2. Write one `.cdx` script: `doc new`, `page size` (a size reference, useful as an
   anchor even when the actual shapes extend beyond it), one `shape create-rect` line
   per rectangle, then `doc save --as <path>` (directory must already exist).
3. `corel script run` it.
4. **Verify by reading the data back — don't just trust the command output.** Open the
   saved file (or use the still-open document) and check actual shape positions/sizes:
   ```
   raw get ActiveDocument.ActivePage.SizeWidth
   raw get ActiveDocument.ActivePage.SizeHeight
   ```
   and for each shape, its `LeftX`/`TopY`/`RightX`/`BottomY`/`SizeWidth`/`SizeHeight`
   (via `raw get`, or a short throwaway program using the same `Type.InvokeMember`
   pattern as `src/Corel.Automation/ComInvoker.cs` if you need to loop over many
   shapes at once — there's no single CLI command that dumps every shape's exact
   geometry yet).
5. Clean up stray test documents when you're done iterating — CorelDRAW is a live GUI
   app a human may be watching, not a sandboxed test harness. Close documents you
   created for verification once you've confirmed the result.

See `scripts/samples/` for two complete, verified real-world examples:
`outer-aligned-12x19.cdx` and `same-size-combination-12x19.cdx` — both gallery-wall
layouts where a page size is used purely as a size anchor for the largest rectangle(s),
not a bounding box.

## Extending the tool itself

If you're working on this repo's own source (not just using the built CLI):

- **Curated verb → raw path mapping** lives in `src/Corel.Verbs/verbs.json` — add an
  entry here to register a new verb's availability-check binding. `requiresInterface`/
  `requiresMember` must match real names from a generated capability manifest, not
  guessed.
- **New curated commands** go in `src/Corel.Cli/Commands/` (one file per verb group),
  wired into `src/Corel.Cli/CommandDispatcher.cs`.
- **All COM calls must go through `Corel.Automation.ComInvoker`** (`Type.InvokeMember`
  with combined `BindingFlags.GetProperty | BindingFlags.InvokeMethod`) — never C#'s
  `dynamic` keyword against these COM objects; it's been empirically unreliable here.
- **Never guess a member name.** Generate and read the capability manifest
  (`corel schema generate`, or `src/Corel.Schema/TypeLibScanner.cs` directly) to
  confirm a method/property actually exists and get its real parameter names before
  writing code that calls it.
- **Tests**: everything under `tests/` must run without a live CorelDRAW instance —
  pure logic only (manifest parsing, verb availability, argument/tokenizer parsing).
  There is deliberately no CI story for the COM layer itself; verify that manually
  against a real running instance, the same way every claim in the README and this
  file was verified.
- Full project layout and development commands are in `README.md`.
