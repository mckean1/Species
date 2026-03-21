# Phase 3: Region Ecology State

## Scope

Phase 3 turns regions into living ecological containers by attaching seeded flora and fauna population state to every region.

Under the canonical biology architecture, region ecology also has a proto-life substrate boundary beneath concrete species populations.

This phase adds initial ecology only. It does not simulate growth, decline, feeding, migration, pressure, groups, discoveries, advancements, adaptations, or chronicle behavior over time.

## RegionEcosystem

`RegionEcosystem` is the aggregate living-state container owned by each region.

- `ProtoLifeSubstrate`: regional proto-life groundwork that defines:
  - `ProtoFloraCapacity`
  - `ProtoFaunaCapacity`
  - `ProtoFloraPressure`
  - `ProtoFaunaPressure`
  - `FloraOccupancyDeficit`
  - `FaunaOccupancyDeficit`
  - `FloraSupportDeficit`
  - `FaunaSupportDeficit`
  - `EcologicalVacancy`
  - `RecentCollapseOpening`
  - `ProtoFloraReadinessMonths`
  - `ProtoFaunaReadinessMonths`
  - `ProtoFloraGenesisCooldownMonths`
  - `ProtoFaunaGenesisCooldownMonths`
- `FloraPopulations`: flora population values keyed by flora species ID.
- `FaunaPopulations`: fauna population values keyed by fauna species ID.

The values are aggregate whole-number population levels, not individual entities or groups.

## Storage Model

Population state is stored directly on the region through its `RegionEcosystem`.

- Proto-life substrate is stored separately from concrete populations.
- Flora populations are stored as `species ID -> population value`.
- Fauna populations are stored as `species ID -> population value`.

`ProtoFloraCapacity` / `ProtoFaunaCapacity` describe latent ecological room.
`ProtoFloraPressure` / `ProtoFaunaPressure` describe active monthly substrate pressure.

Capacity is structural. Pressure is dynamic.
Pressure can rise when regions are underfilled and connected to suitable neighboring ecology, and it can decay when vacancy closes or harsh conditions drag it down.
Vacancy alone is not enough to keep pressure high forever. Empty or harsh regions now lose pressure unless suitability, neighboring spread, and active ecological support continue to justify it.

This keeps the MVP state compact, inspectable, and easy to evolve into later ecology simulation without introducing fine-grained entity management.

## Ecological Seeding

Phase 3 seeds ecology during world generation using region conditions plus the Phase 2 species definitions.

The canonical order is:

1. derive regional proto-life substrate capacity/pressure
2. seed flora populations from regional conditions and flora definitions
3. seed fauna populations from regional conditions plus seeded flora/fauna support

Later monthly substrate updates can adjust proto pressures. In the current biology architecture, rare new-species genesis can eventually occur from this substrate, but only through sustained readiness, rare trigger windows, establishment checks, and cooldowns. Proto pressure alone still does not directly create species.

For fauna in particular, readiness and establishment now require active food-base support from real flora/fauna abundance. Proto-fauna pressure does not override an empty food web.

### Flora Seeding

Flora seeding considers:

- `CoreBiomes` as a strong positive fit
- `SupportedWaterAvailabilities` as a practical support gate
- region fertility against `HabitatFertilityMin` / `HabitatFertilityMax`
- flora `RegionalAbundance`, `GrowthRate`, `RecoveryRate`, and `SpreadTendency`
- small seed-based variation

Flora generally seeds more broadly and more strongly than fauna.

### Fauna Seeding

Fauna seeding considers:

- `CoreBiomes` as a strong positive fit
- `SupportedWaterAvailabilities` as a practical support gate
- lightweight ecological plausibility from seeded flora and already-seeded non-carnivore support
- small seed-based variation

Current MVP diet interpretation:

- Herbivores rely primarily on flora support.
- Omnivores use a middle-ground mix of flora and prey support.
- Carnivores rely primarily on prey support and seed conservatively when prey is weak.

This is intentionally not a full food-web simulator. It is just enough to produce plausible regional starting ecology.

## Validation and Debugging

Phase 3 adds validation for:

- missing region ecosystems
- unknown flora species IDs in region populations
- unknown fauna species IDs in region populations
- negative population values
- consistency between seeded ecology and the available catalogs

Debug output now includes, for each region:

- region ID and name
- biome
- water availability
- fertility
- flora populations
- fauna populations

## Deferred

The following remain intentionally deferred:

- flora growth and decline over time
- fauna consumption and reproduction over time
- predator-prey simulation
- migration over time
- population groups
- pressure systems
- discoveries, advancements, and adaptations
- chronicle systems

## Setup For Later Phases

Phase 3 prepares:

- Phase 4 by providing seeded flora and fauna state that can change over time
- Phase 5 by creating region-level ecological ground truth that later pressure and survival systems can reference
