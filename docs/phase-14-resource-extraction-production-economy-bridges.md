# Phase 14: Resource Extraction, Production, and Economy Bridges

Phase 14 makes polity place-anchoring materially meaningful without turning the prototype into a detailed market or logistics simulation.

The new layer stays aggregate and performance-oriented:

- food remains on the existing group survival and `Food Stores` path
- non-food material categories are limited to timber, stone, fiber, clay, and hides
- regions now expose lightweight aggregate material opportunities
- polities and settlements can extract from regions they meaningfully use
- stores now extend beyond food in a limited way
- simple production bridges convert stored materials into broad support values instead of crafted items
- local surplus and deficit now influence polity resilience, settlement condition, Chronicle events, and bloc pressures

## Canonical Scope

Implemented in this phase:

- region material profiles derived from biome, fertility, water, flora totals, and fauna totals
- polity-level and settlement-level material stores
- monthly aggregate extraction tied to presence, population, continuity, and light knowledge/capability modifiers
- broad production/support outputs:
  - timber and stone -> shelter / durability support
  - clay and fiber -> storage support
  - stone and timber -> tool support
  - fiber and hides -> basic goods / textile support
- simple surplus and deficit scoring
- Chronicle lines and minimal screen presentation for material conditions

Still deferred:

- currency
- market pricing
- deep trade or logistics routing
- profession or class simulation
- building trees or item crafting catalogs
- diplomacy / warfare economy overhauls
- kingdom or empire scaling

## Monthly Integration

`SimulationEngine` remains the canonical monthly orchestrator.

The only loop change is a surgical inserted step after settlement update:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Enacted law monthly effects
6. Group survival and consumption
7. Migration decision and movement
8. Settlement / polity presence update
9. Material extraction / production bridge update
10. Discovery evaluation
11. Advancement evaluation
12. Political bloc monthly update
13. Law proposal update
14. Chronicle update and feed progression
15. End-of-tick finalization

## Model Notes

Regions now carry a `RegionMaterialProfile`.

Polities now carry:

- aggregate `MaterialStores`
- derived `MaterialProduction`
- shortage/surplus month counters

Settlements now carry:

- local `MaterialStores`
- aggregate `MaterialSupport`
- shortage/surplus month counters

These remain lightweight totals, not inventories of crafted goods.

## Extraction and Production Rules

Extraction is monthly and aggregate. Output scales from:

- current or durable regional presence
- local population as a labor proxy
- continuity of use in the region
- modest knowledge/capability bonuses

Production is also aggregate. Stored materials are interpreted into support bands:

- shelter support
- storage support
- tool support
- textile/basic-goods support

Those supports roll into broad resilience, surplus, and deficit values for the polity.

## Design Boundary

This is not yet a full economy.

The phase exists to make the simulation more causally grounded:

- material outputs come from real regions
- settlements can accumulate meaningful local stores
- polity stability can now reflect more than food alone
- later phases can add trade, logistics, and scaling on top of explicit existing state
