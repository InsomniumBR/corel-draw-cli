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
2. Check `corel` is installed and on PATH: run `corel` with no arguments. It prints the
   command overview and **exits 0** — that is a successful install check, not an error.
   `corel help`, `corel --help` and `corel <command> --help` do the same thing in more
   detail, and none of them attach to (or launch) CorelDRAW. If the binary isn't found,
   it's not installed yet:
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
  double quotes and strips them. An **unterminated quote is an error**, not a silently
  accepted line. Backslash is never an escape (Windows paths are full of them) — to put
  a literal `"` in an argument, double it inside a quoted segment: `"say ""hi"""`.
- **`--corel-version N` must come before the command**: `corel --corel-version 17 raw get
  ActiveDocument.Name`. Every other flag belongs to the command it's written on, so a
  command's own `--force`/`--vars` is never mistaken for a global one.
- Only **CorelDRAW X7** is verified on real hardware. The tool is built to work across
  versions via runtime reflection against whatever's actually installed (no hardcoded
  per-version assumptions), but treat other versions as best-effort until verified.

## Full command reference

```
corel doc new                                    # create a blank document
corel doc save [--as <path>] [--embed-vba]       # save (SaveAs when --as is given);
                                                  # --embed-vba embeds installed macros
                                                  # in the saved file, off by default
corel docs list                                  # list open documents, marks active
corel docs use <name|index>                      # switch the active document - errors
                                                  # on an ambiguous name, use the index
                                                  # from `docs list` instead
corel docs active                                # print the active document's name

corel page size                                  # print active page size
corel page size --width W --height H [--unit U]  # resize active page
corel page add [--after N]                       # add a page, appended or after page N
corel page delete --index N                      # delete a page by 1-based index
corel page goto --index N                        # make a page active by 1-based index
corel page list                                  # list pages, marks active

corel layer list                                 # list layers on the active page, marks active
corel layer add --name <name>                    # create a layer on the active page (becomes active)
corel layer use (--name <name>|--index N)        # make a layer active
corel layer delete (--name <name>|--index N)     # delete a layer from the active page

corel shape create-rect --left L --top T --right R --bottom B [--unit U] [--corner-radius N]
corel shape list [--json]                        # list shapes on the active layer
corel shape delete --index N                     # delete by 1-based position - remaining
                                                  # shapes renumber immediately after

corel macro install --name <Module> --file <path.bas>

corel raw get <dotted.path>                      # e.g. ActiveDocument.ActivePage.SizeWidth
corel raw set <dotted.path> <value>
corel raw call <dotted.path> [args...]           # pass the literal string `null` for
                                                  # an omitted optional COM parameter

corel script run <file.cdx> [--vars K=V,K2=V2] [--force]  # run a script
corel shell                                      # interactive REPL

corel help [command]                             # full usage, never connects to COM
corel detect / corel schema generate             # (re-)run first-time setup
```

Global flag: `--corel-version N` pins a specific installed version if more than one is
registered; omit to use the detected default. It is recognised **only before the command
word**.

Every command above is reachable from all three drivers — one-shot, `corel shell`, and a
`.cdx` script — including `script run` (a script may run another script). The one
exception is `shell` itself, which owns the process's stdin and can only be started from
a terminal.

`corel shell` exits with the status of the last command it ran, so a piped session
(`Get-Content cmds.txt | corel shell`) reports a real exit code.

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
member that's actually confirmed to exist, with its real parameter names. A `raw`
error naming an unknown member or a wrong argument count means: go check that manifest
before retrying with a guess.

**`raw get` is not guaranteed to be read-only.** The invoker combines
`GetProperty|InvokeMethod` (some COM libraries expose an indexed accessor as a method,
not a property), so `raw get ActiveDocument.Close` really does invoke `Close` and
close the document. Don't treat `raw get` on an unfamiliar member as a safe probe.

### Script variables (`--vars`)

Any `.cdx` script can reference `{PLACEHOLDER}` tokens anywhere in any line (not just a
save path) — `corel script run <file.cdx> --vars KEY=VALUE,KEY2=VALUE2,...` replaces
every occurrence before running. This is generic, not special-cased for output paths:
parameterize a page size, a unit, anything the script needs (see
`scripts/samples/hello-rectangle.cdx` for a `{WIDTH}`/`{HEIGHT}`/`{UNIT}` example).
If a script references a placeholder that wasn't provided, the run is rejected up
front — listing every missing name, in dark yellow — rather than substituting nothing
and failing confusingly partway through.

### What preflight actually checks, and when it runs

Everything below runs **before line 1 executes and before any COM connection is made**,
so a bad script never half-mutates a document and never launches CorelDRAW:

| Check | Overridable with `--force`? |
|---|---|
| The `.cdx` file exists | no |
| Every `{PLACEHOLDER}` has a value (all missing ones listed at once) | no |
| Every line tokenizes (no unterminated quote) | no |
| Every line is a known command with a known subcommand | no |
| Required flags present, no unknown/repeated flags, `--width`/`--height` together | no |
| Flag values are the right shape (number, whole number, known unit, non-empty path) | no |
| A `macro install --file` source actually exists | no |
| Safety screening (see below) | **yes** |
| Every mapped verb exists in the target version's cached capability manifest | **yes** |

Blocking failures are reported with their real line number in the file (comments and
blank lines counted), all of them at once, not just the first.

`raw` is the deliberate exception to structural validation: only its own fixed grammar
(`get`/`set`/`call` plus the right number of operands) is checked. Its dotted path and
call arguments are arbitrary object-model members with arbitrary arity and types — the
only thing that could confirm them is the capability manifest, which is exactly what the
availability check already does separately.

### Safety screening

Scripts are screened at load time for operations outside drawing automation. A hit
aborts the run with every violation listed; `--force` waives it, same convention as the
availability check.

**Checked:**

- A `raw` dotted path whose *whole segment* (case-insensitive) is one of: `Shell`,
  `ShellExecute`, `Run`, `RunMacro`, `Exec`, `Execute`, `CreateObject`, `GetObject`,
  `WScript`, `WshShell`, `FileSystemObject`, `Kill`, `RmDir`, `SetAttr`, `RegWrite`,
  `RegRead`, `RegDelete`, `SaveSetting`, `GetSetting`, `DeleteSetting`, `VBE`,
  `VBProjects`, `VBComponents`, `CodeModule`, `AddFromString`, `AddFromFile`. Whole-
  segment matching means a CorelDRAW member that merely *contains* one of these
  (`ShellSortOrder`) is unaffected.
- Any argument that is literally a shell/filesystem automation ProgID: `WScript.Shell`,
  `Shell.Application`, `Scripting.FileSystemObject`, `MSXML2.XMLHTTP`,
  `InternetExplorer.Application`.
- The **contents** of a `macro install --file <x.bas>`, scanned textually for
  `Shell`, `CreateObject`, `GetObject`, `Kill`, `RmDir`, `SetAttr`, `RegWrite`,
  `RegRead`, `RegDelete`, `SaveSetting`, `GetSetting`, `DeleteSetting`, `Declare`
  (Win32 P/Invoke from VBA).
- Any argument containing a control character.
- `--as` / `--file` path values: rejected for a `..` traversal segment, characters the
  OS rejects in a path, or emptiness.

Variables are substituted **before** screening, so a `--vars` value cannot smuggle a
path or a denylisted member past it.

**Deliberately out of scope** (say so rather than assume it's covered):

- Shell metacharacter "injection" — no argument ever reaches a shell; every call goes
  straight to COM.
- Allow-listing which directories a script may write to — the tool has no notion of a
  project root, so `..` traversal is refused but an absolute path anywhere is allowed.
- Semantic analysis of VBA — the `.bas` scan is textual, so a denylisted word inside a
  comment or a string literal still trips it (use `--force`, or reword).
- One-shot commands and the REPL are **not** screened. Screening is a property of
  running a *script file*, which may have been generated or fetched; a command typed
  directly at your own terminal is not an injection vector.

## Building a layout — the recommended agent workflow

1. **Compute every rectangle's coordinates up front**, in a scratch calculation, before
   writing any CorelDRAW commands. Remember Y-up. If the design has a "biggest" element
   that should be an exact target size with everything else proportional to it, scale
   from that anchor — don't assume the page/canvas size is a bounding box unless that's
   explicitly what's wanted (see `scripts/samples/` for both variants).
2. Write one `.cdx` script: `doc new`, `page size` (a size reference, useful as an
   anchor even when the actual shapes extend beyond it), one `shape create-rect` line
   per rectangle, then `doc save --as "{OUTPUT}"` — use a `{PLACEHOLDER}` rather than
   hardcoding a path, so the same script is reusable with any destination.
3. `corel script run <file>.cdx --vars OUTPUT=<path>` it (directory must already
   exist — omitting a placeholder the script actually uses is rejected up front,
   before anything runs).
4. **Verify by reading the data back — don't just trust the command output.** Open the
   saved file (or use the still-open document) and check actual shape positions/sizes:
   ```
   raw get ActiveDocument.ActivePage.SizeWidth
   raw get ActiveDocument.ActivePage.SizeHeight
   corel shape list --json
   ```
   `shape list --json` dumps every shape's index, resolved type name, name, and
   `LeftX`/`TopY`/`RightX`/`BottomY`/`SizeWidth`/`SizeHeight` geometry in one call — the
   normal way to check several shapes at once, no throwaway program needed.
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
  wired into `src/Corel.Cli/CommandDispatcher.cs`, **and declared in
  `src/Corel.Cli/CommandGrammar.cs`** — that one table is what help, structural
  validation and the script preflight all read, so a command missing from it is rejected
  as unknown, and a command whose flags are wrong there is validated wrongly everywhere.
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
