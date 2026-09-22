# ADR-001: Script parameterization uses a generic `--vars`/`{PLACEHOLDER}` mechanism, not single-purpose flags

**Date:** 2026-09-21
**Status:** Accepted
**Triggered by:** docs/changes/2026-09-21-generic-script-vars.md

## Context
`.cdx` scripts needed a way to be parameterized so the same script file could be reused
across different runs (different output paths, different sizes, etc.) instead of hardcoding
every value into the script text. The first implementation added a single-purpose
`--output <path>` flag with a `${OUTPUT}` placeholder, scoped only to the save path.

## Decision
Script parameterization is generic: `corel script run <file.cdx> --vars KEY=VALUE,KEY2=VALUE2`
substitutes any `{PLACEHOLDER}` token, anywhere in the script text — not limited to output
paths, and not implemented as one bespoke flag per use case. A script that references a
placeholder with no supplied value is rejected before anything runs, listing every missing
name.

Future script inputs (sizes, units, colors, counts, anything else a script might need to vary)
use this same `--vars` mechanism rather than a new dedicated flag.

## Alternatives considered
- Per-purpose flags (`--output`, `--width`, `--height`, ...) added incrementally — rejected,
  doesn't scale and fragments the CLI surface for what is fundamentally one substitution
  problem.
- Reading values from OS environment variables instead of an explicit flag — rejected to keep
  a script run fully self-contained and reproducible from the invoking command line alone.

## Consequences
- Any future "make X configurable in a script" request is satisfied by referencing a new
  `{PLACEHOLDER}` in the `.cdx` file — no CLI/parser changes needed.
- Scripts are only as safe as their author's placeholder naming; there's no type checking on
  substituted values (a script expecting a number gets whatever string was passed).
- The upfront validation (reject on any missing placeholder) trades a small ergonomics cost
  (must supply every placeholder used) for avoiding partial/confusing mid-script failures.
