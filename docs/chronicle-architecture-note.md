# Chronicle Architecture Note

Chronicle is now a structured, player-facing history feed rather than a generic log sink.

## Rule

- simulation systems should emit structured `ChronicleEventCandidate` data
- `ChroniclePolicy` decides whether a candidate becomes a Chronicle entry, an alert, diagnostics-only output, or suppression
- final player-facing message formatting is centralized rather than owned by arbitrary systems
- Chronicle entries intentionally use canonical short templates and typed tokens so entity classes can be colorized consistently
- Chronicle is for historical changes; alerts are for active conditions that still need attention
- Chronicle entries are tokenized into typed text segments instead of relying on regex over final raw strings
- direct raw string Chronicle writes are no longer the intended pattern
- diagnostics and debug-style output should stay out of the player-facing Chronicle feed
