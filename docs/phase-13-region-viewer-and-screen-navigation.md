# Phase 13: Region Viewer and Screen Navigation

## Scope

Phase 13 adds the first lightweight player-facing screen/navigation layer for the MVP.

The current MVP screen set is:

1. Chronicle
2. Region Viewer

Chronicle remains the main/default screen. Region Viewer is a separate player-facing screen for current world state.

This phase does not add new simulation behavior. It adds screen selection and region viewing only.

## Screen Model

The client now tracks a current screen with a small explicit screen identifier model.

The MVP screen set is intentionally small and wraps when cycling:

- Chronicle
- Region Viewer

`TAB` cycles forward through the screen set and wraps back to Chronicle.

## Default Screen

Chronicle remains the default screen on startup.

This preserves the design rule that unfolding history is the main player-facing view.

## Region Viewer

Region Viewer is a player-facing current-state screen.

It shows:

- region name and region ID
- biome
- water availability
- fertility
- flora populations
- fauna populations
- groups currently in the region
- neighboring regions

When groups are present, the screen also shows a short summary of their:

- population
- stored food
- subsistence mode
- known discoveries
- learned advancements

This is presented as a world-state viewer, not as a raw diagnostics dump.

## Region Navigation

While Region Viewer is active, the player can browse regions in stable order.

MVP controls:

- `Right Arrow`, `D`, or `N` moves to the next region
- `Left Arrow`, `A`, or `P` moves to the previous region

Region browsing wraps around the available region list.

These controls only affect region selection while Region Viewer is active.

## Chronicle Versus Region Viewer

The distinction remains:

- Chronicle = unfolding history feed
- Region Viewer = current state of one region

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
