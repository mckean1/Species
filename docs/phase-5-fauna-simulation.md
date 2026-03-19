# Phase 5: Fauna Simulation

## Scope

Phase 5 adds the first real monthly fauna ecology loop. Fauna no longer stays at seeded static values. Each month fauna consumes food from the actual regional ecology state and then adjusts population from food actually consumed plus habitat support.

## Tick Placement

- `1 simulation tick = 1 month`

Within the monthly ecology flow, Phase 5 now runs:

1. Advance month
2. Flora simulation
3. Fauna consumption
4. Fauna population adjustment
5. End-of-tick finalization

`SimulationEngine` remains the canonical orchestrator for this order.

## Food Consumption Rules

Fauna outcome is based on food actually consumed, not theoretical food availability alone.

For each fauna species already present in a region:

1. determine habitat support
2. calculate food need from current population and `FoodRequirement`
3. consume eligible food based on `DietCategory`
4. measure actual consumption
5. compute fulfillment ratio
6. adjust population from fulfillment plus habitat support

### Herbivores

- consume flora only
- use current regional flora populations
- flora `FoodValue` influences usable food
- consumed flora reduces the real regional flora populations

### Carnivores

- consume fauna only
- may consume herbivores, omnivores, and other carnivores
- may not consume their own species
- consumed prey reduces the real regional fauna populations

### Omnivores

- consume both flora and fauna
- use a simple split with fallback when one side is weak
- consumed flora and fauna both reduce the real regional ecology state

## Habitat Support

Fauna habitat support remains intentionally simple:

- supported water is the practical support gate
- unsupported water strongly penalizes survival
- core biomes provide strong habitat support
- non-core biomes reduce support

## Extinction

Regional fauna extinction is supported directly:

- fauna populations may decline to `0`
- `0` means extinction from that region
- extinct fauna is removed from the active regional fauna storage
- no automatic fauna reintroduction exists in this phase

Future reappearance must come from an explicit causal system.

## Validation and Debugging

Phase 5 adds debug output for:

- month/date
- fauna species and region
- prior and new population
- food needed
- food actually consumed
- fulfillment ratio
- habitat support
- consumed flora summary
- consumed fauna summary
- growth / decline / extinction outcome

Validation now also checks:

- negative fauna populations
- cannibalism is excluded
- tick month advancement remains correct

## Deferred

The following remain deferred:

- population groups
- pressure systems
- discoveries and advancements
- adaptations
- chronicle generation
- migration between regions
- prey preference matrices
- explicit kill events or combat simulation
- social behavior and age structure
