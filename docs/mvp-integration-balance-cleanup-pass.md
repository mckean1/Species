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
5. Enacted law monthly effects
6. Group survival and consumption
7. Migration decision and movement
8. Discovery evaluation
9. Advancement evaluation
10. Chronicle update and feed progression
11. End-of-tick finalization

`SimulationEngine` remains the canonical orchestrator for this flow.

## Player-Facing Structure

The MVP player-facing structure is now:

- Chronicle as the main/default screen
- Region Viewer and supporting knowledge/polity screens for current state
- `TAB` cycles between screens and wraps
- `SPACE` toggles run/pause
- simulation advances automatically while running
- `ENTER` advances one month only while paused

Chronicle remains the main player-facing history view.
Region Viewer remains a player-facing state view, not a debug console.
Player-facing screens respect focal-polity knowledge limits instead of exposing global truth.

## Discoveries Versus Advancements

The distinction remains strict:

- Discovery = "we know"
- Advancement = "we can"

Discoveries affect decision-making only.
Advancements affect execution/capability directly.
Decision systems now consume knowledge-safe region views instead of raw hidden region truth for forward-looking evaluation.

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
- region basics only when known
- flora and fauna only when discovered or plausibly observed
- groups in the region without exact hidden internals
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

The cleanup pass also replaces stable-ID first-come food contention with explicit region-level proportional allocation plus deterministic rotating tie resolution, so shared scarcity is no longer biased by group ordering.

## Deferred

Still intentionally deferred after this pass:

- mutation / inheritance / evolution
- settlements and polity systems
- new large UI frameworks
- new screen families beyond Chronicle and Region Viewer
- broad feature expansion beyond cleanup/integration
- weak enforcement and region-specific resistance to enacted laws
- law legitimacy by class, faith, or region
- slot-based government/legal structures beyond simple title conflicts
- multi-step reform chains beyond one-step repeal/replacement
- legal memory or tradition effects around repealed laws
- government-form-specific repeal mechanics beyond the shared MVP rules
- full faction and backing-bloc simulation
- proposal debates, petitions, and competing support/opposition coalitions
- proposal origin memory and law popularity tracking by subgroup or region
- regional resistance to law enforcement
- class, faith, or subgroup-specific compliance differences
- lawbreaking events, crackdowns, and richer enforcement decay/recovery
- enforcement tied to institutions, infrastructure, or bureaucracy
- corruption and active resistance movements weakening enforcement
- government-form transitions, reform paths, and government replacement
- elections, office rotation, and representation systems
- noble houses, guilds, temple hierarchies, and court politics as real entities
- regional autonomy within large governments
- legal codification, legitimacy, charters, and succession/inheritance systems
- advanced bureaucratic capacity systems beyond the current law-enforcement layer
- regional bloc variation inside large polities
- inter-bloc rivalries, coalitions, and negotiated bargaining
- coups, uprisings, and broader political crisis behavior
- bloc memory, long-term ideology, and political legitimacy by bloc support
