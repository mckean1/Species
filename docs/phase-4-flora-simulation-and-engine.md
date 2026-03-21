# Phase 4: Flora Simulation and SimulationEngine

## Scope

Phase 4 introduces the first real monthly simulation step. Flora is no longer static seeded state; it now changes over time as the aggregate ecological base layer.

This phase also introduces `SimulationEngine` as the canonical orchestrator for monthly ticks.

## Tick Timing

- `1 simulation tick = 1 month`

World state now carries a simulation date using `CurrentYear` and `CurrentMonth`.

## SimulationEngine

`SimulationEngine` is the central runtime orchestrator for simulation ticks.

Its intended long-term monthly order is:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Population group simulation
5. Pressure update
6. Discovery and advancement checks
7. Adaptation checks
8. Chronicle generation
9. End-of-tick finalization

In Phase 4, the implemented order is:

1. Advance month
2. Flora simulation
3. End-of-tick finalization

Fauna and later systems remain deferred.

## Flora Simulation

Flora simulation only updates flora species already present in a region. This is intentional.

There is no automatic flora reintroduction in Phase 4. If a species reaches `0` in a region, it is extinct there until a future explicit causal system reintroduces it.

### Target Abundance Concept

For each flora species already present in a region, the system:

1. evaluates local support from water, biome, fertility, and broad climate fit
2. grows flora from that support using `GrowthRate` and `RecoveryRate`
3. declines flora from recent consumption pressure, harshness, and poor support
4. allows bounded spread into neighboring suitable regions when established flora is strong enough
5. stores the result as a bounded whole number

This keeps flora behavior causal, bounded, and easy to inspect.

### Habitat Inputs

Flora support and abundance are currently driven by:

- supported vs unsupported water
- whether the region biome is in `CoreBiomes`
- how close region fertility is to `HabitatFertilityMin` / `HabitatFertilityMax`
- flora `GrowthRate`
- flora `RecoveryRate`
- flora `UsableBiomass`
- flora `ConsumptionResilience`
- flora `SpreadTendency`
- flora `RegionalAbundance`

Unsupported or harsh support drives strong decline. Strong established flora can also spread to nearby suitable regions.

### Extinction

Regional flora extinction is supported directly:

- populations can decline to `0`
- `0` means extinction from that region
- extinct flora is removed from active regional flora storage
- there is no random primitive-life spawning or deep botany sub-simulation

## Validation and Debugging

Phase 4 adds:

- simulation-date validation
- tick month-advance validation
- checks that unsupported-water flora does not grow
- monthly flora change output with prior value, target, new value, outcome, and main causal factors

## Deferred

The following remain deferred to Phase 5 and beyond:

- fauna simulation behavior
- population groups
- pressure systems
- discoveries and advancements
- adaptation
- chronicle generation
- flora spread between regions
- flora reintroduction from extinction
