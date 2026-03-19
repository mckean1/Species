# MVP Integration / Balance / Cleanup Pass

## Purpose

This pass tightens the completed MVP into a more coherent prototype without adding new major systems.

The focus is:

- end-to-end monthly flow consistency
- Chronicle-first player presentation
- Region Viewer readability
- screen/navigation cleanup
- tuning-hook clarity
- lightweight guardrails for common invalid states

## Canonical Monthly Flow

The MVP now preserves this monthly order:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Group survival and consumption
6. Migration decision and movement
7. Discovery evaluation
8. Advancement evaluation
9. Chronicle update and feed progression
10. End-of-tick finalization

`SimulationEngine` remains the canonical orchestrator for this flow.

## Player-Facing Structure

The MVP player-facing structure is now:

- Chronicle as the main/default screen
- Region Viewer as the supporting current-state screen
- `TAB` cycles between screens and wraps
- `ENTER` advances one month in the client

Chronicle remains the main player-facing history view.
Region Viewer remains a player-facing state view, not a debug console.

## Discoveries Versus Advancements

The distinction remains strict:

- Discovery = "we know"
- Advancement = "we can"

Discoveries affect decision-making only.
Advancements affect execution/capability directly.

## Logging / Output Cleanup

The old player-facing raw log approach remains retired.

Player-facing output is now Chronicle-first, with Region Viewer as the other MVP screen.
Development-only validation and formatters may still exist, but they do not act as competing player-facing interfaces.

## Chronicle Cleanup Notes

Chronicle now preserves:

- one-line notable messages
- newest-first visible feed
- delayed reveal
- `discovered` wording for discoveries
- `learned` wording for advancements

The integration pass also keeps Chronicle conservative by avoiding routine low-value event spam and guarding against duplicate same-event records within a month.

## Region Viewer Cleanup Notes

Region Viewer now presents:

- current date
- region basics
- flora and fauna populations
- groups in the region
- neighbor visibility
- short discovery/advancement summaries where useful

It is intended as a normal player screen, not a diagnostic dump.

## Adaptation / Biological Change

The earlier MVP adaptation layer remains removed.

Long-term biological change is still deferred to a later dedicated mutation / inheritance / evolution system and is not part of the MVP cleanup pass.

## Main Tuning Hotspots

The biggest remaining tuning hotspots after this pass are:

- flora target and adjustment rates
- fauna food need versus prey/flora depletion
- group monthly food need and starvation loss severity
- migration trigger pressure and destination-margin thresholds
- discovery and advancement threshold pacing
- Chronicle hardship-event thresholds and pacing
- starting group viability versus early ecological carrying capacity

## Deferred

Still intentionally deferred after this pass:

- mutation / inheritance / evolution
- settlements and polity systems
- new large UI frameworks
- new screen families beyond Chronicle and Region Viewer
- broad feature expansion beyond cleanup/integration
