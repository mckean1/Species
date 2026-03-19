# Species

MVP simulation prototype for a region-based world model.

Current implementation notes:

- Phase 1 world and region foundation is implemented.
- Phase 2 species definition catalogs for ecology are implemented.
- Phase 3 region ecology state seeding is implemented.
- Phase 4 monthly flora simulation and the SimulationEngine tick orchestrator are implemented.
- Phase 5 monthly fauna simulation and ecology consumption are implemented.
- Phase 6 population group actor spawning is implemented.
- Phase 7 group pressure recalculation is implemented.
- Phase 8 group survival and consumption is implemented.
- Phase 9 movement and migration is implemented.
- The console client generates a deterministic connected world and prints a debug summary.
- The console client also prints starter flora and fauna definition summaries.
- The world debug summary now includes seeded flora and fauna populations per region.
- The console client now runs a monthly tick and prints flora and fauna change details.
- The console client also prints population group state for inspection.
- The console client now prints recalculated group pressures and pressure reasons.
- The console client now prints group survival, stored food use, shortage, and starvation results.
- The console client now prints migration decisions, neighbor scores, and movement outcomes.
- Phase 1 details are documented in [docs/phase-1-world-region-foundation.md](docs/phase-1-world-region-foundation.md).
- Phase 2 details are documented in [docs/phase-2-species-definitions.md](docs/phase-2-species-definitions.md).
- Phase 3 details are documented in [docs/phase-3-region-ecology-state.md](docs/phase-3-region-ecology-state.md).
- Phase 4 details are documented in [docs/phase-4-flora-simulation-and-engine.md](docs/phase-4-flora-simulation-and-engine.md).
- Phase 5 details are documented in [docs/phase-5-fauna-simulation.md](docs/phase-5-fauna-simulation.md).
- Phase 6 details are documented in [docs/phase-6-population-groups.md](docs/phase-6-population-groups.md).
- Phase 7 details are documented in [docs/phase-7-group-pressure-framework.md](docs/phase-7-group-pressure-framework.md).
- Phase 8 details are documented in [docs/phase-8-group-survival-and-consumption.md](docs/phase-8-group-survival-and-consumption.md).
- Phase 9 details are documented in [docs/phase-9-movement-and-migration.md](docs/phase-9-movement-and-migration.md).
