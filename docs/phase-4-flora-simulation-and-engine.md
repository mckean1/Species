# Phase 4: Flora Simulation And Ecological Base Layer

## Scope

Phase 4 introduces the first real monthly simulation step. Flora is no longer static seeded state; it now changes over time as the aggregate ecological base layer that later fauna and sapient support depend on.

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

The later biology architecture keeps flora at the front of the ecology loop because live flora quality materially shapes the food web above it.

## Flora Simulation

Flora simulation updates established flora and allows bounded spread into suitable neighboring regions. It stays aggregate and region-based.

There is no automatic flora reintroduction in Phase 4. If a species reaches `0` in a region, it is extinct there until a future explicit causal system reintroduces it.

### Monthly Flora Inputs

For each flora species already present in a region, the system:

1. evaluates local support from water, biome, fertility, and broad climate fit
2. resolves a local target abundance from support, regional abundance tendency, proto-flora pressure, vacancy, and recent ecological opening
3. grows flora from support, growth/recovery traits, underfill, and recovery opportunity
4. declines flora from real consumption pressure, harshness, and poor support
5. allows bounded spread into neighboring suitable regions when established flora is strong enough
6. stores the result as a bounded whole number

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
- current regional vacancy / underfill
- recent ecological opening after collapse

Unsupported or harsh support drives strong decline. Underfilled or recently opened regions recover faster when suitable flora remains established nearby.

## Ecological Role

Flora is the real biological base layer.

- live flora abundance under local conditions drives flora ecology support
- fauna herbivory and omnivory consume real flora populations
- sapient foraging consumes real flora populations
- weak flora support should weaken the food web above it

Food is therefore not "generic plants in the world". It is live regional flora translated into usable support through current abundance and local fit.

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

The following remain deferred:

- decorative botany detail
- agriculture or cultivation
- pollination, disease, and plant life-stage cohorts
- deep seasonal plant sub-systems
