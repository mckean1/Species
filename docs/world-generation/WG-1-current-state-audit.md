# WG-1: Current-State Audit Against the Canonical World Generation Direction

## 1. Purpose

This document audits the current `Species` implementation against the canonical world-generation direction established in `docs/world-generation/WG-0-canonical-direction.md`.

WG-1 is an assessment phase, not a broad implementation phase. Its purpose is to describe current reality in code terms, identify which parts of the architecture already support the long-term causal direction, and call out the shortcuts, incompatibilities, and missing pieces that later roadmap phases must address.

This audit uses the current codebase as the source of truth. It is intended to be a practical architecture document for later WG phases rather than aspirational design writing.

## 2. Current Startup / Initialization Pipeline

### 2.1 What currently happens at startup

The current startup flow is defined in `Species.Client/Program.cs`.

1. Starter flora, fauna, and sapient catalogs are created.
2. `WorldGenerator.Create(floraCatalog, faunaCatalog)` is called.
3. `DiscoveryCatalog.CreateForWorld(world)` and `AdvancementCatalog.CreateStarterSet()` are created from the generated world.
4. Startup validation runs through `ClientRuntimeValidation.ValidateStartup(...)`.
5. `SimulationEngine` is constructed with the generated world.
6. `PlayerClientRuntime` starts immediately and begins active play.

There is no separate historical world bootstrap, no long pre-player simulation span, and no later player-entry handoff step in the current startup path.

### 2.2 What is generated first

`WorldGenerator` first creates the physical regional frame:

- region count and region IDs
- region names
- region connectivity
- `WaterAvailability`
- `Biome`
- fertility
- material profile

This is aligned with the intended physical-world-first direction.

### 2.3 What organic life/state already exists immediately

The same `WorldGenerator.Create(...)` call then moves directly beyond a physical-world-only foundation.

`RegionEcosystemSeeder` seeds each region with:

- flora populations
- fauna populations
- flora and fauna biological profiles
- a `ProtoLifeSubstrate`

This means the startup world already begins with a formed flora/fauna layer rather than a primitive-life-only bootstrap.

### 2.4 Whether the game starts polity-ready/player-ready

Yes. The game currently starts in a polity-ready and player-ready condition.

After ecology seeding, `WorldGenerator.Create(...)` immediately calls `PopulationGroupSpawner.Spawn(...)`, which adds:

- sapient `PopulationGroup` actors
- one `Polity` per starting group
- government forms
- home/core region assignments
- a focal polity ID for the player

`PlayerClientRuntime` then starts immediately, and `PlayerViewStateCoordinator` resolves and enforces a focal polity for the client view.

### 2.5 What appears seeded, preconstructed, assumed, or fabricated beyond the long-term target architecture

The current startup path preconstructs several late-stage outcomes that the long-term architecture expects to emerge through simulation:

- sapient population groups are inserted during world generation rather than arising after historical development
- polities are inserted during world generation rather than forming from prior sapient history
- government forms are assigned during spawning rather than developing from earlier social/political evolution
- initial region knowledge, local discoveries, and species awareness are preloaded onto groups at spawn time
- a focal player polity exists immediately
- the client and validation layers assume the world already contains population groups and polities

A notable mismatch is that `SapientSpeciesCatalog.CreateStarterSet()` currently returns an empty catalog, while `PopulationGroupSpawner` still creates sapient groups using the hardcoded species ID `species-human`. In other words, playable sapient actors currently exist by startup assumption rather than by the sapient-species pipeline.

## 3. Causal Ladder Assessment

The table below classifies the current codebase against the intended causal ladder.

| Rung | Status | Current assessment | Key code areas |
| --- | --- | --- | --- |
| Physical world | Present and aligned | `WorldGenerator` builds regions, neighbors, biome, fertility, water, and material profile before later systems run. This is the strongest current alignment with the long-term direction. | `Species.Domain/Generation/WorldGenerator.cs` |
| Primitive life | Present but shortcut-driven | A proto-life substrate exists, but the actual startup path seeds fully formed flora and fauna populations immediately through `RegionEcosystemSeeder`. The bootstrap is not primitive-life-first in the canonical sense. | `Species.Domain/Generation/RegionEcosystemSeeder.cs`, `Species.Domain/Models/ProtoLifeSubstrate.cs` |
| Ecosystem stability | Present but simplified | Flora, fauna, and proto-pressure are simulated monthly with aggregate regional models. This provides a real ecological layer, but it is still a compact starter ecology rather than a deep-time biosphere bootstrap. | `Species.Domain/Simulation/FloraSimulationSystem.cs`, `Species.Domain/Simulation/FaunaSimulationSystem.cs`, `Species.Domain/Simulation/ProtoPressureSystem.cs` |
| Species differentiation | Present but simplified | `BiologicalEvolutionSystem` tracks regional profiles, trait drift, extinctions, proto-genesis, and biological history. However, the main startup world already begins with differentiated starter species catalogs and does not rely on long historical differentiation to reach play. | `Species.Domain/Simulation/BiologicalEvolutionSystem.cs` |
| Migration / range dynamics | Present but simplified | Fauna migration and sapient group migration both exist, but they are monthly aggregate systems operating over already-formed ecological and social actors. They are useful foundations, not a full historical range-development pipeline. | `Species.Domain/Simulation/FaunaSimulationSystem.cs`, `Species.Domain/Simulation/MigrationSystem.cs` |
| Sapient emergence preconditions | Present but simplified | `BiologicalEvolutionSystem` contains explicit sapience-emergence progress and trigger logic on fauna lineages. This is causally aligned in spirit, but it is not the authoritative path to playable sapients because startup bypasses it entirely. | `Species.Domain/Simulation/BiologicalEvolutionSystem.cs`, `Species.Domain/Constants/SapienceEmergenceConstants.cs` |
| Early sapient survival / continuity | Present but incompatible with long-term direction | Survival, migration, discovery, and advancement logic operate on sapient `PopulationGroup` actors, but those groups currently require a `PolityId` and are born into a polity-bearing world. There is no clean non-polity early sapient continuity phase yet. | `Species.Domain/Models/PopulationGroup.cs`, `Species.Domain/Simulation/GroupSurvivalSystem.cs`, `Species.Domain/Simulation/MigrationSystem.cs` |
| Discovery foundations | Present but shortcut-driven | Discoveries are correctly modeled as knowledge and are causally gated by exposure, movement, pressure, and contact. However, spawn-time knowledge seeding and the assumption of existing sapient groups/polities mean the discovery model starts after major emergence steps have already been skipped. | `Species.Domain/Catalogs/DiscoveryCatalog.cs`, `Species.Domain/Simulation/DiscoverySystem.cs`, `Species.Domain/Generation/PopulationGroupSpawner.cs` |
| Advancement foundations | Present but simplified | Advancements remain distinct from discoveries and are prerequisite-gated. This is aligned. But the system is designed around already-existing sapient groups, polity context, and starter-era practical opportunities rather than a deep historical bootstrap. | `Species.Domain/Catalogs/AdvancementCatalog.cs`, `Species.Domain/Simulation/AdvancementSystem.cs` |
| Settlement formation | Present but shortcut-driven | `SettlementSystem` can found and abandon settlements from ongoing group/polity activity. That is useful causal scaffolding. However, settlements can only arise inside already-existing polities; there is no pre-polity settlement formation path. | `Species.Domain/Simulation/SettlementSystem.cs` |
| Polity formation | Present but incompatible with long-term direction | The codebase contains polity evolution, scaling, fragmentation, and interaction systems, but not first polity emergence from non-polity sapients. Initial polities are fabricated by `PopulationGroupSpawner` during startup. | `Species.Domain/Generation/PopulationGroupSpawner.cs`, `Species.Domain/Simulation/PoliticalScalingSystem.cs` |
| Historical carry-forward into active play | Missing | Active play continues on the same `SimulationEngine` once startup finishes, which is good. But there is no distinct historical run-to-era step and no later player-entry handoff into a historically developed world. | `Species.Client/Program.cs`, `Species.Domain/Simulation/SimulationEngine.cs`, `Species.Client/Presentation/PlayerClientRuntime.cs` |

## 4. Shortcut / Legacy Behavior Inventory

The following behaviors should be treated as transitional or legacy relative to the WG-0 direction.

### 4.1 Startup jumps directly from world generation to sapient/polity play

`WorldGenerator.Create(...)` generates the physical world, seeds ecology, and then immediately inserts sapient groups and polities through `PopulationGroupSpawner.Spawn(...)`.

This is the single clearest current break from the intended causal ladder.

### 4.2 Mature ecology is seeded rather than grown from primitive life

`RegionEcosystemSeeder` directly seeds flora and fauna populations for every region. The project already has proto-life concepts and proto-pressure, but startup does not currently begin from primitive life alone.

### 4.3 First sapients are not produced by the sapient-emergence path

The biological layer can create `SapientSpeciesDefinition` entries through `BiologicalEvolutionSystem`, but startup bypasses that path and creates sapient `PopulationGroup` actors directly.

There is currently no authoritative bridge from emergent sapient species to sustained sapient populations, continuity, and later playable societies.

### 4.4 Polities are fabricated rather than formed

`PopulationGroupSpawner` creates one polity per starting group and assigns a government form immediately. Current political systems then operate on those pre-existing polities.

This is transitional scaffolding, not the intended long-term polity formation model.

### 4.5 Knowledge is partially preloaded

`PopulationGroupSpawner` seeds:

- known home and neighboring regions
- local flora/fauna/water/region discoveries
- initial species awareness

This is practical for immediate play, but it skips part of the intended discovery-history chain.

### 4.6 Startup validation and client flow assume a post-emergence world

`ClientRuntimeValidation.ValidateStartup(...)` validates polities and population groups as part of the startup contract. `PlayerClientRuntime` and `PlayerViewStateCoordinator` assume a focal polity is available immediately.

This means the current client cannot cleanly start from a sapient-free or polity-free prehistory world.

### 4.7 Some engine work is already player-era specific

`SimulationEngine.Tick()` always runs `LawProposalSystem.Run(..., PlayerPolityId)`, which makes the core tick shape partially aware of active-play governance flow.

This does not invalidate shared-engine use, but it does show that the current engine is not yet cleanly separated between general historical simulation and player-era overlays.

## 5. SimulationEngine Ownership / Shared-Engine Readiness

### 5.1 What the current engine already owns well

The current `SimulationEngine` is already the main owner of most live simulation once the startup world exists. A monthly tick currently advances:

- flora simulation
- fauna simulation
- biological evolution
- proto-pressure
- group survival
- migration
- settlement updates
- material economy
- discovery progression
- advancement progression
- social identity
- inter-polity interaction
- political scaling
- chronicle recording

This is a strong foundation for the long-term shared-engine goal.

### 5.2 Systems that already fit the shared-engine model

The ecology, biology, migration, discovery, advancement, settlement, polity-scaling, and chronicle systems are all structured as world-in / world-out simulation steps. That is the right overall shape for a shared historical-play engine.

The aggregate monthly design is also consistent with the project's performance preference.

### 5.3 Systems tightly coupled to immediate player/polity existence

Several pieces are still tightly coupled to a post-emergence player world:

- `PopulationGroup` requires `PolityId`
- `DiscoverySystem` and `AdvancementSystem` use polity context and polity-wide knowledge flow
- `SettlementSystem` assumes settlements belong to existing polities
- `PoliticalScalingSystem` evolves existing polities rather than creating first polities
- `LawProposalSystem` is explicitly run for `PlayerPolityId`
- client synchronization and rendering assume a current focal polity

### 5.4 Systems that would need bridging for pre-polity historical simulation

The biggest bridging needs are:

- a path from emergent sapient species to non-polity sapient populations
- a path from non-polity sapient continuity to first settlement behavior
- a path from early settlement/social continuity to first polity formation
- an engine mode or phase boundary for skipping player-era governance behaviors before a player exists

### 5.5 Overall readiness assessment

The current `SimulationEngine` plausibly supports long-span historical simulation in principle, because most major domains are already modeled as shared monthly simulation steps.

However, it is not yet historical-bootstrap ready in practice. The startup contract still fabricates sapients and polities, and several downstream systems assume those actors already exist. The engine shape is promising; the bootstrap and early-causality layers are the main gap.

## 6. System Area Observations

### 6.1 Startup and world initialization

Startup is compact and deterministic, but it currently collapses multiple roadmap phases into one call path. `Program.cs` moves from world generation to immediate player runtime without a historical simulation stage.

### 6.2 Physical world foundation

The physical world foundation is one of the healthier parts of the current architecture. Regions, neighbors, biomes, water, fertility, and material profile are created before later simulation systems consume them.

### 6.3 Organic / ecosystem layer

The ecosystem layer is real, not decorative. The project already simulates flora, fauna, ecological pressure, biological profiles, extinction pressure, and trait drift. This is a meaningful base for later world-generation work.

The main weakness is that startup begins from an already-populated flora/fauna world rather than an authoritative primitive-life-first bootstrap.

### 6.4 Discovery / knowledge / advancement

The project preserves an important architectural distinction:

- discoveries are knowledge of the world
- advancements unlock capability

That separation is already valuable and should be preserved. The main limitation is that both systems currently assume a post-emergence sapient actor model and partially inherit startup shortcuts.

### 6.5 Settlement / polity assumptions

Settlement and polity systems are substantial, but they are layered on top of pre-existing polities. Current code supports polity evolution, not first polity origin.

This is one of the biggest roadmap mismatches.

### 6.6 Chronicle / UI / player-entry assumptions

`ChronicleSystem` is broadly useful for a historical game because it can record biological, survival, discovery, settlement, and political changes. That is aligned with inspectable history.

The client layer is not historical-bootstrap ready. Current UI flow assumes an immediate focal polity and immediate player presence in the world.

### 6.7 Performance / time-scale risk

The project already prefers aggregate regional and group-level simulation over fine-grained actor simulation. That is good.

The main time-scale risk is not basic algorithm shape; it is the cost and design consequences of running the full monthly player-era stack across a very long pre-player historical span. Some form of staged fidelity, selective system gating, or historical stepping strategy will likely be needed later, but that should be solved without breaking the shared-engine principle.

## 7. Severity-Ranked Gap List

### 7.1 Critical blockers

1. **World generation currently fabricates sapient groups and polities.**
   - `WorldGenerator.Create(...)` plus `PopulationGroupSpawner.Spawn(...)` bypass the intended causal ladder.
2. **There is no historical run-to-era bootstrap.**
   - Startup goes straight from generated world to active play.
3. **There is no first-polity emergence path.**
   - Political systems evolve existing polities but do not form them from earlier sapient continuity.
4. **Sapient emergence is not connected to playable societal actors.**
   - Emergent sapient species can be recorded, but startup playable sapients come from direct spawning instead.

### 7.2 Major rework areas

1. **Primitive-life-only seeding is not the authoritative bootstrap path.**
2. **Early sapient survival and continuity are still modeled inside a polity-bearing actor structure.**
3. **Discovery and advancement foundations are good, but they start too late in the ladder.**
4. **`SimulationEngine` includes some player-era governance assumptions in the standard tick.**
5. **Client startup and validation assume a focal polity and immediate player-ready world.**

### 7.3 Future-depth gaps

1. **Deeper historical inspectability across long spans still needs stronger end-to-end support.**
2. **Physical world generation may later need more environmental depth, but that is not the current blocker.**
3. **Long-run performance strategy for deep historical simulation still needs explicit design work.**

### 7.4 Transitional acceptable limitations

1. **Aggregate monthly simulation is an acceptable current modeling level.**
2. **Starter flora/fauna/discovery/advancement catalogs are acceptable scaffolding while the bootstrap path is replaced.**
3. **Current polity and settlement systems are useful downstream scaffolding even though they currently start too early.**
4. **The client remaining player-era focused is acceptable for now, as long as it does not redefine the canonical architecture.**

## 8. Roadmap Implications

The audit supports the existing general roadmap direction. The current codebase does not need a different destination; it needs a cleaner ordering of enabling work.

### 8.1 Near-term sequencing still appears correct

WG-2 through WG-5 still appear directionally correct as the near-term sequence, provided they are used to remove the current causal shortcuts in order rather than layering more content on top of them.

In practical terms, the next phases should still move through this shape:

1. separate and clean up the startup/bootstrap boundary
2. make primitive-life-first and early ecological history more authoritative
3. bridge from biological sapient emergence into early sapient continuity and first societal formation
4. add historical run-to-era and player-entry handoff on top of the shared `SimulationEngine`

### 8.2 Dependency risks

The main dependency risks are:

- if startup remains responsible for creating sapients and polities, later historical phases will stay partially fake
- if emergent sapient species are not bridged into population continuity, sapience will remain disconnected from the playable world
- if player-era governance and UI assumptions are not isolated, historical simulation will either be polluted with player logic or require a second engine shape
- if settlement/polity origin is deferred too long, more downstream systems will continue to assume fabricated early political actors

### 8.3 Ordering adjustment to keep in mind

No major roadmap reorder is suggested by this audit, but one emphasis should be clear: first-emergence bridging is a higher dependency than richer late political content. The roadmap should continue to prioritize causal bootstrap work over adding more advanced world-state flavor.

## 9. WG-1 Exit Criteria Assessment

WG-1 is satisfied by this audit.

Definition of done for WG-1:

- the current startup and initialization path is documented against WG-0
- the current implementation is mapped against the causal ladder
- shortcut and legacy behaviors are explicitly named
- `SimulationEngine` ownership and shared-engine readiness are assessed
- major system-area observations are recorded
- gap severity is prioritized for later phases
- roadmap implications are stated clearly enough to guide WG-2 and beyond

This document completes that assessment work and provides the repo-visible current-state audit needed for later roadmap phases.
