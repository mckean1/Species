# Species

Species is a simulation-first command-line fantasy world prototype.

The simulation is now split between `PopulationGroup` constituents and explicit `Polity` actors.

- `PopulationGroup` remains the demographic, ecological, migration, discovery, and advancement-bearing unit.
- `Polity` is now the player-guided political actor that owns government form, law state, political blocs, and player-facing political identity.
- Larger later-phase polity architecture such as settlements, territorial control, economy, diplomacy, and scaling beyond the current MVP remains deferred.

## Current Prototype Truths

- `SimulationEngine` is the canonical monthly orchestrator.
- Chronicle is the main/default player-facing screen.
- Region Viewer is a supporting player-facing screen.
- Additional player-facing screens currently exist for `Polity`, `Laws`, `Advancements`, `Known Polities`, and `Known Species`.
- `TAB` cycles screens and wraps.
- `SPACE` pauses or unpauses the simulation.
- Simulation auto-advances while unpaused.
- `ENTER` advances one month only while paused.
- Tick delay is `1000ms`.
- Player-facing screens are focal-polity knowledge-aware rather than omniscient.
- Discoveries remain knowledge.
- Advancements remain capability.
- Laws, enacted-law monthly effects, political blocs, and polity-facing governance presentation are already implemented on explicit polity state.

## Canonical Monthly Flow

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Enacted law monthly effects
6. Group survival and consumption
7. Migration decision and movement
8. Discovery evaluation
9. Advancement evaluation
10. Political bloc monthly update
11. Law proposal update
12. Chronicle update and feed progression
13. End-of-tick finalization

`SimulationEngine` remains the canonical orchestrator for this flow.

## Scope Notes

Implemented prototype layers already include:

- Chronicle-first presentation
- knowledge-aware region and polity screens
- explicit `Polity` actors in `World`
- law proposals and player pass/veto decisions
- enacted laws with monthly effects
- lightweight political blocs backing proposals

Still deferred:

- settlements / territorial control
- trade / economy
- diplomacy and war overhauls
- kingdom / empire scaling
- full faction / institution simulation
- mutation / inheritance / evolution

## Main Tuning Hotspots

- flora growth/decline tuning
- fauna food consumption and depletion pressure
- group food need and starvation severity
- migration thresholds and score margins
- discovery and advancement pacing
- Chronicle hardship-event noisiness
- law/bloc weight tuning
- starting group viability

## Docs

- [docs/phase-1-world-region-foundation.md](docs/phase-1-world-region-foundation.md)
- [docs/phase-2-species-definitions.md](docs/phase-2-species-definitions.md)
- [docs/phase-3-region-ecology-state.md](docs/phase-3-region-ecology-state.md)
- [docs/phase-4-flora-simulation-and-engine.md](docs/phase-4-flora-simulation-and-engine.md)
- [docs/phase-5-fauna-simulation.md](docs/phase-5-fauna-simulation.md)
- [docs/phase-6-population-groups.md](docs/phase-6-population-groups.md)
- [docs/phase-7-group-pressure-framework.md](docs/phase-7-group-pressure-framework.md)
- [docs/phase-8-group-survival-and-consumption.md](docs/phase-8-group-survival-and-consumption.md)
- [docs/phase-9-movement-and-migration.md](docs/phase-9-movement-and-migration.md)
- [docs/phase-10-discovery-mvp.md](docs/phase-10-discovery-mvp.md)
- [docs/phase-11-advancement-mvp.md](docs/phase-11-advancement-mvp.md)
- [docs/phase-12-chronicle-mvp.md](docs/phase-12-chronicle-mvp.md)
- [docs/phase-13-region-viewer-and-screen-navigation.md](docs/phase-13-region-viewer-and-screen-navigation.md)
- [docs/mvp-integration-balance-cleanup-pass.md](docs/mvp-integration-balance-cleanup-pass.md)
