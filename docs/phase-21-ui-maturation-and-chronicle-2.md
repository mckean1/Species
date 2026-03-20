# Phase 21: UI Maturation and Chronicle 2.0

Phase 21 matures the console client without changing the underlying simulation stack.

## Canonical UI Truths

- The simulation still uses `SimulationEngine` as the canonical monthly orchestrator.
- Chronicle remains the main/default player-facing screen.
- Region Viewer remains a supporting current-state screen.
- The simulation now starts paused.
- `SPACE` pauses and unpauses.
- `N` advances exactly one month while paused.
- `ENTER` now confirms, selects, or opens the currently highlighted UI item.
- `TAB` still cycles screens.
- `Up` / `Down` move the active selection through list-based screens.
- `Left` / `Right` switch Chronicle modes.
- `Backspace` always returns Chronicle to `Live` and snaps it back to the latest/current view.

## Chronicle 2.0

Chronicle now operates in three modes:

- `Live`: newest important developments first, with near-duplicate suppression
- `Archive`: older visible entries, browsable without snapping back during live updates
- `Milestones`: a selective high-value history stream for formation, crisis, law, discovery, conflict, fragmentation, and other major turns

All three Chronicle modes share a persistent `Urgent` section that surfaces only current high-trust alerts such as:

- a pending law decision
- severe store or survival crisis
- fast-rising outside threat
- fragmentation danger
- major governance instability

Urgent is a state summary, not a history feed. Resolution and escalation still enter Chronicle history as normal entries when appropriate.

## Law Screen Changes

The Laws screen now separates:

- `Pending Decisions`
- `Recent Outcomes`
- `Active Laws`

Active laws use the player-facing status set:

- `Active`
- `Under Pressure`
- `Contested`
- `Failing`

Pending law decisions use contextual `Pass` / `Veto` interaction rather than hidden single-letter shortcuts.

## Presentation Rules

- Chronicle text should prefer cause-and-effect phrasing over flat labels.
- The UI remains width-aware and console-friendly.
- Screens should lead with readable summaries instead of raw debug output.
- This phase does not add major new simulation systems; it improves readability, browsing, and player guidance on top of existing simulation truth.
