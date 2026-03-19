# Phase 1: World and Region Foundation

## Scope

Phase 1 establishes the smallest useful world model for the MVP. It creates a generated world made of connected regions with environmental state that later phases can use for ecology, pressure, and survival logic.

This phase intentionally stops at world structure and region environment. It does not simulate flora, fauna, groups, discoveries, advancements, adaptation, or chronicle events.

## World Model

`World` is the root simulation object for this phase.

- `Seed`: the deterministic seed used for generation.
- `Regions`: the generated collection of `Region` objects.

The world owns all regions directly. There is no cell grid, climate map, or separate geography layer in MVP Phase 1.

## Region Model

Each `Region` is a pure region-level simulation unit with stable, debug-friendly state.

- `Id`: stable region identifier such as `region-01`.
- `Name`: readable region name such as `Amber Basin`.
- `Biome`: broad environmental character of the region.
- `WaterAvailability`: simple low / medium / high water model.
- `Fertility`: numeric region fertility value on a 0.05 to 0.95 MVP scale.
- `SeasonalSeverity`: how harsh seasonal conditions tend to be.
- `NeighborIds`: connected neighboring regions for future movement and pressure spread.

## Environmental Fields

### Biome

Biome is a lightweight summary of the region's general environment. The MVP set is:

- `Plains`
- `Forest`
- `Desert`
- `Wetlands`
- `Highlands`
- `Tundra`

There is intentionally no separate climate type field in this phase.

### Water Availability

Water availability is the MVP moisture model:

- `Low`
- `Medium`
- `High`

This is used during generation and is expected to influence later ecology and survival systems.

### Fertility

Fertility is a numeric region-level value representing how productive the region is likely to be for future ecological systems. In MVP form it is derived from biome, water availability, seasonal severity, and a small seeded variation.

### Seasonal Severity

Seasonal severity is a simple stand-in for temperature tendency / seasonal harshness:

- `Mild`
- `Moderate`
- `Severe`

This keeps the model simple while still providing an environmental gradient that later simulation can react to.

## World Generation

World generation currently uses a deterministic seeded pipeline:

1. Create a modest region count suitable for MVP testing.
2. Build a connected region graph.
3. Assign water availability.
4. Assign seasonal severity using a simple world-span gradient with seeded noise.
5. Resolve biome from water and seasonal severity.
6. Derive fertility from biome, water, seasonal severity, and seeded variation.
7. Assign stable IDs and deterministic readable names.

The generator is intentionally simple and explainable. It does not attempt to simulate realistic geography, tectonics, rainfall, rivers, or a cell map.

## Validation and Debugging

Phase 1 includes validation checks for:

- duplicate region IDs
- invalid neighbor references
- self-neighbor entries
- duplicate neighbor entries
- reciprocal neighbor links
- overall world connectivity

Debug inspection currently uses a development-facing world summary that prints:

- total region count
- each region ID and name
- biome
- water availability
- fertility
- seasonal severity
- neighbor list

## Deferred

The following are intentionally deferred to later phases:

- flora definitions and populations
- fauna definitions and populations
- ecology update systems
- population groups and migration logic
- pressure systems
- discoveries and advancements
- adaptation
- chronicle and storytelling systems
