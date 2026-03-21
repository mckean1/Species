# Phase 5: Fauna Simulation

## Scope

Phase 5 adds the first real monthly fauna ecology loop. Fauna no longer stays at seeded static values or relies on abstract food-only shortcuts. Each month fauna attempts to feed from actual regional flora and fauna abundance, accumulates shortage pressure when underfed, reproduces when fed, dies back when shortages persist, and may migrate into better neighboring regions.

## Tick Placement

- `1 simulation tick = 1 month`

Within the monthly ecology flow, Phase 5 now runs:

1. Advance month
2. Flora simulation
3. Fauna feeding and regional consumption
4. Fauna hunger, reproduction, mortality, and migration adjustment
5. End-of-tick finalization

`SimulationEngine` remains the canonical orchestrator for this order.

## Feeding Rules

Fauna outcome is based on food actually consumed from the current ecology state, not theoretical food availability alone.

For each fauna species already present in a region:

1. determine habitat support
2. calculate required intake from current population and `RequiredIntake`
3. resolve explicit preferred diet links from `DietLinks`
4. consume available named flora/prey targets from the actual regional ecology state
5. only if preferred intake remains insufficient, attempt any grounded fallback links
6. measure the resulting usable-food ratio and food shortfall
7. convert that usable-food ratio into food stress state, hunger pressure, and shortage persistence
8. apply reproduction or mortality from fedness, shortage pressure, and support
9. derive migration pressure from persistent local survival failure
10. migrate a bounded share outward if shortages persist and a better neighboring region exists

Food present in a region is not automatically usable food for a fauna species. Usable food is the portion the species can actually convert into intake after diet-link targeting, encounter/refuge friction, fallback penalties, and conversion efficiency are applied.

### Herbivores

- draw almost all intake from explicit flora targets
- use current regional flora populations and live regional flora support values
- flora `UsableBiomass`, `RegionalAbundance`, and recovery/resilience qualities influence usable flora support
- consumed flora reduces the real regional flora populations

If flora is environmentally weak or recently overconsumed, herbivore support falls with it. Flora is not a passive background number behind fauna feeding.

### Carnivores

- draw most intake from explicit prey targets
- may consume herbivores, omnivores, and other carnivores if those targets are linked
- may not consume their own species
- prey with higher `PredatorVulnerability` is easier to consume efficiently
- prey refuge and encounter friction limit how much of a prey population is actually accessible in a month
- prey accessibility tightens further when prey is sparse or predators locally overconcentrate
- consumed prey reduces the real regional fauna populations

### Omnivores

- draw intake from both flora and prey according to explicit weighted diet links
- can fall back across sources when one side is weak, but only after preferred links fail to cover enough intake
- consumed flora and fauna both reduce the real regional ecology state

### Scavenge Share

- `FaunaDietTargetKind.ScavengePool` exists as a limited low-efficiency fallback source
- it is intentionally weak and does not replace actual flora or prey abundance as the main ecological base
- it helps prevent total brittleness without introducing a separate carcass simulation system
- migration scoring and seeding viability also treat fallback support as residual support only, so emergency diet options do not masquerade as strong core ecology

## Habitat Support

Fauna habitat support remains intentionally simple and aggregate:

- supported water is the practical support gate
- unsupported water strongly penalizes feeding success and population recovery
- core biomes provide strong habitat support
- non-core biomes reduce support
- fertility fit provides an additional local support signal through `HabitatFertilityMin` and `HabitatFertilityMax`

## Hunger, Reproduction, and Mortality

- each regional fauna population tracks `HungerPressure`
- each regional fauna population also tracks `FeedingMomentum`, which smooths reproduction and prevents immediate boom-bust breeding swings
- feeding momentum rises more slowly than shortages do, so reproduction lags recovery instead of snapping back immediately
- each regional fauna population resolves into `FedStable`, `HungerPressure`, `SevereShortage`, or `Starvation`
- repeated underfeeding increments shortage persistence
- well-fed populations reproduce according to `ReproductionRate`, filtered through the lagging feeding momentum state
- persistent shortages convert into mortality according to `MortalitySensitivity`
- no usable food escalates starvation rapidly instead of allowing indefinite survival off nominal food presence
- populations can recover when feeding improves because hunger pressure also decays

This is intentionally progressive rather than binary:

- partial shortfall creates hunger pressure
- repeated shortfall suppresses reproduction and raises mortality
- severe or prolonged shortfall becomes starvation

## Migration

- migration is aggregate and region-based
- only a bounded share of a population may migrate in a month
- migration is driven by explicit migration pressure from hunger, shortage persistence, food-stress state, and species `Mobility`
- migration is also a stability valve for predator-prey loops; hungry fauna can bleed outward instead of only amplifying local collapse
- movement only occurs when a neighboring region offers materially better feeding/support conditions
- migration does not create new fauna from nothing; it only redistributes existing population

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
- required intake
- food actually consumed
- food shortfall
- feeding ratio
- habitat support
- hunger pressure
- shortage months
- births and deaths
- migration pressure
- migrated-out amount
- consumed flora summary
- consumed fauna summary
- growth / decline / extinction / migration outcome

Validation now also checks:

- negative fauna populations
- invalid hunger pressure or shortage month state
- cannibalism is excluded
- tick month advancement remains correct

## Deferred

The following remain deferred:

- population groups
- pressure systems
- discoveries and advancements
- adaptations
- chronicle generation
- prey preference matrices
- explicit kill events or combat simulation
- social behavior and age structure
