# Species

MVP simulation prototype for a region-based world model.

Current MVP structure:

- `SimulationEngine` is the canonical monthly orchestrator.
- Chronicle is the main/default player-facing screen.
- Region Viewer is the supporting current-state player screen.
- `TAB` cycles between screens and wraps.
- `ENTER` advances one month in the console client.
- Discoveries are knowledge and affect decision-making.
- Advancements are capability and affect execution.
- The old player-facing raw log/output model has been replaced by Chronicle-first presentation.
- The removed MVP adaptation layer has not been reintroduced.
- Long-term biological change remains deferred to a later mutation / inheritance / evolution system.

Canonical monthly flow:

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

Main tuning hotspots:

- flora growth/decline tuning
- fauna food consumption and depletion pressure
- group food need and starvation severity
- migration thresholds and score margins
- discovery and advancement pacing
- Chronicle hardship-event noisiness
- starting group viability
- Phase 1 details are documented in [docs/phase-1-world-region-foundation.md](docs/phase-1-world-region-foundation.md).
- Phase 2 details are documented in [docs/phase-2-species-definitions.md](docs/phase-2-species-definitions.md).
- Phase 3 details are documented in [docs/phase-3-region-ecology-state.md](docs/phase-3-region-ecology-state.md).
- Phase 4 details are documented in [docs/phase-4-flora-simulation-and-engine.md](docs/phase-4-flora-simulation-and-engine.md).
- Phase 5 details are documented in [docs/phase-5-fauna-simulation.md](docs/phase-5-fauna-simulation.md).
- Phase 6 details are documented in [docs/phase-6-population-groups.md](docs/phase-6-population-groups.md).
- Phase 7 details are documented in [docs/phase-7-group-pressure-framework.md](docs/phase-7-group-pressure-framework.md).
- Phase 8 details are documented in [docs/phase-8-group-survival-and-consumption.md](docs/phase-8-group-survival-and-consumption.md).
- Phase 9 details are documented in [docs/phase-9-movement-and-migration.md](docs/phase-9-movement-and-migration.md).
- Phase 10 details are documented in [docs/phase-10-discovery-mvp.md](docs/phase-10-discovery-mvp.md).
- Phase 11 details are documented in [docs/phase-11-advancement-mvp.md](docs/phase-11-advancement-mvp.md).
- Phase 12 details are documented in [docs/phase-12-chronicle-mvp.md](docs/phase-12-chronicle-mvp.md).
- Phase 13 details are documented in [docs/phase-13-region-viewer-and-screen-navigation.md](docs/phase-13-region-viewer-and-screen-navigation.md).
- MVP integration / cleanup notes are documented in [docs/mvp-integration-balance-cleanup-pass.md](docs/mvp-integration-balance-cleanup-pass.md).
