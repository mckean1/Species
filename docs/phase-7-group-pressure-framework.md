# Phase 7: Group Pressure Framework

## Scope

Phase 7 adds the first causal interpretation layer for `PopulationGroup` actors. Groups now own a `PressureState`, and that state is recalculated each month from current group conditions and current regional ecology.

This phase calculates pressures only. It does not make groups act on them yet.

## PressureState

`PressureState` is a compact derived state container stored on each `PopulationGroup`.

It contains:

- `FoodPressure`
- `WaterPressure`
- `ThreatPressure`
- `OvercrowdingPressure`
- `MigrationPressure`

All pressure values use a whole-number `0-100` scale:

- `0` = none
- `100` = extreme

## Monthly Recalculation

Pressure values are recalculated fresh each month through `SimulationEngine`.

Current monthly order is:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. End-of-tick finalization

The system overwrites the previous monthly pressure values rather than storing long-lived pressure memory.

## Pressure Meanings

### FoodPressure

Derived from:

- `StoredFood`
- `Population`
- local flora support
- local fauna support

Low food reserves, weak ecology, and high population relative to support all increase `FoodPressure`.

### WaterPressure

Derived directly from regional `WaterAvailability`.

- low water -> high pressure
- medium water -> moderate pressure
- high water -> low pressure

### ThreatPressure

Derived from dangerous fauna in the current region.

For MVP Phase 7, carnivore abundance is the primary threat signal.

### OvercrowdingPressure

Derived from group population relative to broad local ecology support.

Even a single group can experience overcrowding pressure if it is large relative to the support offered by the current region.

### MigrationPressure

Derived after the other pressures as a synthesis pressure.

It combines food, water, threat, and overcrowding pressure into a simple “leave this place” signal for later phases.

## Debugging

Phase 7 adds debug output for:

- group identity
- current region
- population and stored food
- all five pressures
- simple reason hints for food, water, threat, overcrowding, and migration

## Deferred

The following remain intentionally deferred:

- group gathering or hunting behavior
- stored food consumption and starvation
- migration execution
- discovery, advancement, or adaptation behavior
- diplomacy, politics, settlements, and culture
- long-lived pressure memory systems
- action selection or AI planning

## Setup For Later Phases

Phase 7 prepares:

- Phase 8 by providing the pressure inputs needed for group survival and consumption behavior
- Phase 9 by creating a derived “leave or stay” signal that later migration systems can act on
