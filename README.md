# Species

Species is a simulation-first command-line fantasy world prototype.

The simulation is now split between `PopulationGroup` constituents and explicit `Polity` actors.

- `PopulationGroup` remains the demographic, ecological, migration, discovery, and advancement-bearing unit.
- `Polity` is now the player-guided political actor that owns government form, law state, political blocs, settlement/presence state, and player-facing political identity.
- Larger later-phase polity architecture such as territorial control, economy, diplomacy, and richer late-game state detail beyond the current MVP remains deferred.

## Current Prototype Truths

- `SimulationEngine` is the canonical monthly orchestrator.
- Chronicle is the main/default player-facing screen.
- Region Viewer is a supporting player-facing screen.
- Additional player-facing screens currently exist for `Polity`, `Laws`, `Advancements`, `Known Polities`, and `Known Species`.
- Simulation now starts paused so the player can read before history begins moving.
- `TAB` cycles screens and wraps.
- `SPACE` pauses or unpauses the simulation.
- `Up` / `Down` navigate lists and menu selections.
- `Left` / `Right` switch Chronicle between `Live`, `Archive`, and `Milestones`.
- `ENTER` confirms, selects, or opens the currently highlighted item.
- `N` advances one month only while paused.
- `Backspace` returns Chronicle to `Live` and snaps back to the latest/current view.
- Simulation auto-advances while unpaused.
- Tick delay is `1000ms`.
- Player-facing screens are focal-polity knowledge-aware rather than omniscient.
- `Encounter -> Discovery -> Knowledge` is the canonical knowledge-state ladder for world familiarity.
- Flora/fauna species awareness uses `Encounter -> Discovery -> Knowledge`, and intentional species exploitation requires `Knowledge`.
- Advancements remain capability.
- `SpeciesClass` is the canonical biology classification model: `Flora`, `Fauna`, `Sapient`.
- Regional ecology now has explicit proto-life substrate naming: `ProtoFloraCapacity`, `ProtoFaunaCapacity`, `ProtoFloraPressure`, `ProtoFaunaPressure`.
- Proto-life capacity is latent ecological room; proto-life pressure is current monthly activation pressure, and the two are intentionally separate.
- Proto-life is a regional substrate, not arbitrary species spawning.
- Sapience is canonically a fauna-lineage emergence that becomes its own active `SpeciesClass` once present.
- Starvation follows usable-food failure, not mere world-food totals.
- Laws, enacted-law monthly effects, political blocs, and polity-facing governance presentation are already implemented on explicit polity state.
- Settlements, polity regional presence, and place-based anchoring are now implemented in lightweight form.
- Aggregate material extraction, limited non-food stores, and simple production bridges are now implemented in lightweight form.
- Discoveries now emerge from repeated exposure, pressure, continuity, and contact; advancements now require grounded capability prerequisites.
- Governance now uses broader structural laws, government-form baselines, legitimacy/cohesion/authority, and uneven law compliance.
- Polities now accumulate social memory, identity tendencies, and traditions from lived simulation history.
- Long-horizon biological change now emerges regionally through inherited local profiles, divergence, rare speciation, extinction, and fossils.
- Inter-polity relations now emerge from geography, contact, memory, cooperation, raids, and bounded conflict escalation.
- Political scaling now lets strong polities consolidate into larger realm forms, bind subordinate or federated attachments, and fragment under overreach.
- Chronicle now supports `Live`, `Archive`, and `Milestones`, with a persistent `Urgent` alert strip across all modes.
- Group pressure now uses persistent `RawValue` / `EffectiveValue` / `DisplayValue` / `SeverityLabel` state.
- Gameplay pressure consumers now read `EffectiveValue`; UI and Chronicle summaries now read `DisplayValue` plus `SeverityLabel`.
- Migration and hardship/Chronicle consumers are retuned for persistent pressure so carryover strain is less noisy and less oscillatory than the old fresh-month model.

## Canonical Monthly Flow

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Regional biological inheritance / divergence / deep-history update
5. Regional proto-life substrate pressure update
6. Group pressure recalculation
7. Enacted law monthly effects
8. Group survival and consumption
9. Migration decision and movement
10. Settlement / polity presence update
11. Material extraction / production bridge update
12. Discovery evaluation
13. Advancement evaluation
14. Social identity / tradition update
15. Inter-polity interaction / external pressure update
16. Political scaling / attachment / fragmentation update
17. Political bloc monthly update
18. Law proposal update
19. Chronicle update and feed progression
20. End-of-tick finalization

`SimulationEngine` remains the canonical orchestrator for this flow.

## Progression Ownership

- Species awareness owns flora/fauna species familiarity and intentional-use eligibility.
- Discoveries own broader non-species findings such as routes, materials, conditions, and methods.
- Advancements own repeatable operational capability.

This split is intentional: discovery is not advancement, and species `Discovery` is not enough to intentionally exploit that species.

## Scope Notes

Implemented prototype layers already include:

- Chronicle-first presentation
- paused startup with Chronicle-centered browsing controls
- knowledge-aware region and polity screens
- explicit `Polity` actors in `World`
- Chronicle `Live` / `Archive` / `Milestones` browsing with persistent urgent alerts
- law proposals and player `Pass` / `Veto` decisions through contextual UI selection
- enacted laws with monthly effects and uneven practical landing
- lightweight political blocs backing proposals from current material and governance strain
- lightweight settlements, regional presence, and anchoring
- aggregate resource extraction, limited material stores, and simple production bridges
- deeper discovery exposure memory and grounded advancement gating
- broader governance with legitimacy, cohesion, authority, and compliance
- social identity, traditions, and regional/core-periphery social distinction
- regional biological inheritance, divergence, speciation, extinction, and fossil traces
- geography-driven inter-polity relations, cooperation, rivalry, raids, and uneasy peace
- capacity-driven large-state scaling, composite attachment, overreach, and fragmentation
- mobile, seasonal, semi-rooted, and anchored polity forms

Still deferred:

- territorial borders / control
- full trade / economy
- formal education / institution simulation
- tactical combat, treaties, and larger diplomacy/war overhauls
- dynastic, feudal-title, and bureaucracy-detail simulation
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
- material extraction and surplus/deficit tuning
- discovery diffusion and advancement pacing tuning
- governance legitimacy/compliance tuning
- social identity and tradition pacing
- biological divergence/speciation/extinction pacing
- inter-polity escalation/de-escalation pacing
- large-state scaling / fragmentation pacing
- starting group viability

## Docs

- [docs/phase-1-world-region-foundation.md](docs/phase-1-world-region-foundation.md)
- [docs/canonical-biology-architecture.md](docs/canonical-biology-architecture.md)
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
- [docs/phase-14-resource-extraction-production-economy-bridges.md](docs/phase-14-resource-extraction-production-economy-bridges.md)
- [docs/phase-15-knowledge-and-advancement-deepening.md](docs/phase-15-knowledge-and-advancement-deepening.md)
- [docs/phase-16-law-and-governance-expansion.md](docs/phase-16-law-and-governance-expansion.md)
- [docs/phase-17-social-structure-and-identity-systems.md](docs/phase-17-social-structure-and-identity-systems.md)
- [docs/phase-18-biological-change-and-deep-history-ecology.md](docs/phase-18-biological-change-and-deep-history-ecology.md)
- [docs/phase-19-multi-polity-interaction-and-conflict.md](docs/phase-19-multi-polity-interaction-and-conflict.md)
- [docs/phase-20-political-scaling-kingdom-empire-and-fragmentation.md](docs/phase-20-political-scaling-kingdom-empire-and-fragmentation.md)
- [docs/phase-21-ui-maturation-and-chronicle-2.md](docs/phase-21-ui-maturation-and-chronicle-2.md)
- [docs/mvp-integration-balance-cleanup-pass.md](docs/mvp-integration-balance-cleanup-pass.md)
