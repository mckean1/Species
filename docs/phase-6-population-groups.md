# Phase 6: Population Group Actor Model

## Scope

Phase 6 introduces the first real actor layer for the MVP: aggregate `PopulationGroup` actors that live in regions and become the foundation for later survival, pressure, migration, discovery, and advancement systems.

This phase adds actor state, spawning, and inspection only. It does not add group behavior yet.

## PopulationGroup

`PopulationGroup` is the MVP actor model. The simulation uses aggregate groups rather than individuals to keep the model causal, compact, and performant.

Each group currently stores:

- `Id`: stable group identifier
- `Name`: readable group name
- `SpeciesId`: actor species reference
- `CurrentRegionId`: the region the group currently occupies
- `OriginRegionId`: the region where the group began
- `Population`: whole-number group size
- `StoredFood`: whole-number stored food reserve
- `SubsistenceMode`: broad survival posture
- `KnownRegionIds`: regions currently known to the group
- `KnownDiscoveryIds`: discoveries known to the group

## SubsistenceMode

`SubsistenceMode` is a lightweight broad label for how a group is oriented toward survival in MVP form:

- `Gatherer`
- `Hunter`
- `Mixed`

This does not yet create gathering or hunting behavior. It is stored actor state for later systems.

## World Ownership

`World` now owns the collection of `PopulationGroup` objects directly.

For MVP Phase 6, region occupancy is derived from `PopulationGroup.CurrentRegionId`. Regions do not yet maintain resident indexes.

## Initial Spawning

Initial groups are spawned during world setup using a simple viability-based approach.

Current spawning logic:

1. score regions by ecological viability from existing flora, fauna, and fertility
2. choose a modest set of viable regions
3. create a small number of groups in those regions
4. assign starting population, stored food, and subsistence mode
5. initialize knowledge with at least the current/origin region and nearby regions

This keeps initial actors grounded in already-simulated ecological state rather than arbitrary placement.

## Validation and Debugging

Phase 6 adds validation for:

- duplicate group IDs
- blank IDs or names
- blank `SpeciesId`
- invalid current/origin region references
- negative population
- negative stored food
- missing knowledge collections
- missing knowledge of current/origin region

Debug output now includes a development-facing summary of all population groups and their starting state.

## Deferred

The following remain intentionally deferred:

- group survival behavior
- gathering and hunting behavior
- starvation resolution
- migration decisions
- discovery or advancement unlocking
- adaptation progression
- diplomacy, politics, settlements, and culture
- chronicle behavior

## Setup For Later Phases

Phase 6 prepares:

- Phase 7 by providing actor state that can accumulate pressure
- Phase 8 by providing group identity, location, food reserves, and knowledge state for future survival and progression systems
