# Phase 13: Region Viewer and Screen Navigation

## Scope

Phase 13 adds the first lightweight player-facing screen/navigation layer for the MVP.

The current MVP screen set is:

1. Chronicle
2. Polity
3. Region Viewer
4. Laws
5. Advancements
6. Known Polities
7. Known Species

Chronicle remains the main/default screen. Region Viewer is a separate player-facing screen for current world state.

This phase does not add new simulation behavior. It adds screen selection and region viewing only.

## Screen Model

The client now tracks a current screen with a small explicit screen identifier model.

The MVP screen set is intentionally small and wraps when cycling:

- Chronicle
- Polity
- Region Viewer
- Laws
- Advancements
- Known Polities
- Known Species

`TAB` cycles forward through the screen set and wraps back to Chronicle.
`1-7` jump directly to the matching screen in that order.

The screen set is broader than the original phase scope now, but the same Chronicle-first navigation model remains in place.

## Default Screen

Chronicle remains the default screen on startup.

This preserves the design rule that unfolding history is the main player-facing view.

## Region Viewer

Region Viewer is a player-facing current-state screen.

It shows only what the focal polity actually knows or has reason to estimate. Hidden world truth is replaced by `Encounter`, `Discovery`, `Knowledge`, or `Unknown` presentation as appropriate.

It may show:

- region name and region ID
- known or estimated biome / condition context
- known or estimated water conditions
- known or estimated fertility
- discovered flora and fauna details
- visible or inferred local group presence
- neighboring regions

When groups are present, the screen also shows a short summary of their:

- population
- stored food
- subsistence mode
- discovered findings
- learned advancements

This is presented as a world-state viewer, not as a raw diagnostics dump.

## Region Navigation

While Region Viewer is active, the player can browse regions in stable order.

MVP controls:

- `Right Arrow`, `D`, or `N` moves to the next region
- `Left Arrow`, `A`, or `P` moves to the previous region
- `SPACE` toggles pause/unpause
- simulation advances automatically while running
- `ENTER` single-steps one month while paused

Region browsing wraps around the available region list.

These controls only affect region selection while Region Viewer is active.

## Chronicle Versus Region Viewer

The distinction remains:

- Chronicle = unfolding history feed
- Region Viewer = current state of one known region through focal-polity knowledge

Region Viewer does not replace Chronicle and is not framed as a debugging fallback.

## Deferred

The following remain intentionally deferred:

- graphical map presentation
- larger UI frameworks
- filtering/search tools
- additional hotkey systems
- replay/history browsing
- settings and menus

## Setup For Later Phases

Phase 13 prepares:

- a clearer player-facing way to inspect current world state
- future polish around navigation and presentation
- later MVP integration work without changing the simulation core
