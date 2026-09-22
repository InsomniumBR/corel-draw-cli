# ADR-002: Attach to CorelDRAW via late-bound COM (Type.InvokeMember + ProgID CoCreateInstance), never generated interop or the ROT

**Date:** 2026-09-19
**Status:** Accepted
**Triggered by:** docs/changes/2026-09-19-initial-build.md

## Context
CorelDRAW exposes a VBA-compatible OLE Automation object model (`VGCore.dll` /
`vgcoreauto.tlb`), but no existing open-source .NET client for it could be found. Two open
questions had to be resolved before any code could be written: how to attach to an
already-running instance, and how to call its members from .NET.

Early attempts to attach via `Marshal.GetActiveObject`/the Running Object Table (the standard
approach for Office-style automation) failed with `MK_E_UNAVAILABLE`, even after trying
focus-toggle workarounds. Verified empirically, including with two CorelDRAW processes running
simultaneously: CorelDRAW never registers itself in the ROT, under any condition tried.

Early attempts to call members via C#'s `dynamic` keyword also failed: the .NET DLR's COM
binder could not bind a real, existing method call against this object model.

## Decision
- **Attach**: resolve the ProgID (`CorelDRAW.Application` / `CorelDRAW.Application.<N>`) via
  `Type.GetTypeFromProgID`, then `Activator.CreateInstance` (this performs a `CoCreateInstance`
  under the hood). Because CorelDRAW is a single-instance COM server, this reliably routes to
  the already-open window regardless of which window currently has focus. The ROT is never
  used.
- **Member access**: every COM call goes through `Type.InvokeMember` with combined
  `BindingFlags.GetProperty | BindingFlags.InvokeMethod` (needed because some members, e.g.
  `VBProjects.Item`, are exposed as methods rather than properties, and a single binding-flag
  set has to handle both). `null` arguments map to `Type.Missing` for omitted optional COM
  parameters. C#'s `dynamic` is never used against this object model anywhere in the codebase.
- **No generated interop assembly** (`tlbimp`) is checked in or generated per machine. Member
  names/signatures are instead verified once via a permanent `ITypeInfo`-based scanner
  (`Corel.Schema.TypeLibScanner`) that reads the real type library and produces a cached
  capability manifest — the ground truth for what a given CorelDRAW version's object model
  actually exposes, rather than assumed from documentation.
- **True multi-process instance targeting does not exist and is not attempted.** Since a
  second `CorelDRW.exe` process is entirely invisible to COM (never in the ROT), "which
  instance to work on" only ever means picking among open `Documents` within the one
  automatable process (`docs list`/`docs use`), never among separate OS processes.

## Alternatives considered
- **`tlbimp`-generated interop assembly** — rejected: would need regenerating per user machine
  and per installed CorelDRAW version, directly against the goal of a tool that works across
  versions without a per-machine code-generation step.
- **`dynamic`** — tried first, rejected: failed to bind a real method during development;
  `Type.InvokeMember` was the only mechanism that worked reliably.
- **ROT-based attach (`GetActiveObject`) with retry/focus-toggle heuristics** — tried and
  rejected: the underlying premise (CorelDRAW registers in the ROT under some condition) is
  false, confirmed empirically; no amount of retry/focus logic fixes a fundamentally absent ROT
  registration.
- **Direct `IRunningObjectTable` enumeration** as a more "robust" alternative to
  `GetActiveObject` — not pursued once the ROT registration itself was confirmed absent; there
  is nothing in the ROT to enumerate.

## Consequences
- The tool works against any installed CorelDRAW version's object model without regenerating
  interop code, at the cost of losing compile-time type checking on COM calls — a wrong member
  name or argument count only fails at runtime. This is mitigated by validating names/arities
  against the generated capability manifest (script preflight, verb availability warnings)
  before executing.
- Because there is no ROT registration to rely on, this tool can never distinguish between, or
  target, multiple simultaneously running CorelDRAW processes — only documents within the one
  automatable instance. This is a hard platform limitation, documented in the README, not a gap
  to fix later.
- Every COM-calling code path must run on a single STA thread (`Corel.Automation.StaThreadHost`)
  since `Activator.CreateInstance`-based attach and `Type.InvokeMember` calls are apartment-
  sensitive.
