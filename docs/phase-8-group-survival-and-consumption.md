# Phase 8: Group Survival and Consumption

## Scope

Phase 8 adds the first real monthly group survival loop. Population groups now try to survive by gathering flora, hunting fauna, using `StoredFood`, suffering shortages, and starving when monthly need is not met.

This phase adds survival and consumption only. It does not add migration execution, discoveries, advancements, adaptation, settlements, diplomacy, or politics.

## Monthly Order

`SimulationEngine` now runs the effective monthly order:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Group survival and consumption
6. End-of-tick finalization

This preserves the causal rule that ecology updates before group survival runs.

## Monthly Food Need

Each group calculates a monthly food need from its current `Population` using a simple per-population-unit food need constant.

The value is whole-number based and tuned for MVP readability rather than detailed physiology.

## SubsistenceMode

Group food acquisition uses strict primary-then-fallback order:

- `Gatherer`: gather first, then hunt
- `Hunter`: hunt first, then gather
- `Mixed`: gather first, then hunt

This is intentionally simple and explicit. There is no weighted action blending or planning system in this phase.

## Gathering

Gathering:

- reads real flora populations from the current region
- uses flora `FoodValue` to determine usable food
- reduces actual flora populations in the region

Gathering is aggregate only. There are no species preferences or detailed foraging behaviors yet.

## Hunting

Hunting:

- reads real fauna populations from the current region
- uses fauna `FoodYield` to determine usable food
- reduces actual fauna populations in the region

Hunting is aggregate only. There are no detailed prey strategies, weapon systems, or individual hunts yet.

## StoredFood, Shortage, and Starvation

After acquisition:

1. food gained this month is combined with `StoredFood`
2. the group attempts to satisfy the full monthly need
3. leftover food becomes the new `StoredFood`
4. any deficit becomes a shortage
5. shortages cause starvation loss

Population loss is derived from the severity of the deficit. Groups may decline to `0`, which means extinction.

There is no automatic reintroduction or respawn.

## Debugging

Phase 8 adds debug output for:

- monthly food need
- primary and fallback acquisition order
- food gained from gathering and hunting
- total food acquired
- `StoredFood` before and after
- shortage
- starvation loss
- final population
- survival / decline / extinction outcome

## Deferred

The following remain intentionally deferred:

- migration execution
- discovery, advancement, and adaptation logic
- diplomacy, politics, settlements, and culture
- complex inventory systems
- production chains and preservation systems
- AI planning and complex prey targeting

## Setup For Later Phases

Phase 8 prepares:

- Phase 9 by creating the first real survival pressure that migration systems can react to
- later actor systems by grounding group outcomes in direct ecological interaction
