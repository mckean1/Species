# Phase 8: Group Survival and Consumption

## Scope

Phase 8 adds the first real monthly group survival loop. Population groups now try to survive by gathering flora, hunting fauna, using `StoredFood`, suffering shortages, and starving when monthly need is not met.

Food existing in the world is not the same thing as usable food for a group. Usable food is the amount the group can actually turn into survival this month after subsistence access, action order, storage access, and local reserve access are resolved.

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

## Subsistence Preference And Mode

Sapients now separate long-run subsistence preference from realized monthly extraction mode.

- `ForagerLeaning`: prefers known flora first and hunts as fallback.
- `HunterLeaning`: prefers known fauna first and forages as fallback.
- `Mixed`: shifts primary action toward whichever known usable support is currently stronger.
- `DesperationFallback`: not a stored preference, but an emergency monthly behavior where severe shortage or starvation pushes the group toward any known usable source that can still be intentionally exploited.

Realized monthly food acquisition still uses strict primary-then-fallback order:

- `Gatherer`: gather first, then hunt
- `Hunter`: hunt first, then gather
- `Mixed`: gather first, then hunt

This stays aggregate and explicit. There is no deep planning system, but groups do shift behavior when their known usable support changes.

## Gathering

Gathering:

- reads real flora populations from the current region
- only intentionally targets flora sources the group has sufficient practical knowledge of
- uses polity-owned species awareness, where `Discovery` allows limited intentional use and `Knowledge` allows reliable use
- uses flora `UsableBiomass`-driven gathering support to determine usable food
- reduces actual flora populations in the region

Gathering is aggregate only. There are no species preferences or detailed foraging behaviors yet.

## Hunting

Hunting:

- reads real fauna populations from the current region
- only intentionally targets fauna sources the group has sufficient practical knowledge of
- uses polity-owned species awareness, where `Discovery` allows limited intentional use and `Knowledge` allows reliable use
- uses fauna `FoodYield` to determine usable food
- reduces actual fauna populations in the region

Hunting is aggregate only. There are no detailed prey strategies, weapon systems, or individual hunts yet.

Groups cannot intentionally exploit species just because those species exist in the world. Intentional extraction is knowledge-gated and bound to polity species-awareness state:

- `Encounter` is not enough for deliberate use
- `Discovery` allows deliberate but still limited use
- `Knowledge` is the reliable operational state

Food in the world still is not the same thing as usable food for that actor. A species can be abundant in a region and still contribute little or nothing if the polity has not progressed far enough in its awareness of that species.

## StoredFood, Shortage, and Starvation

After acquisition:

1. food gained this month is combined with `StoredFood`
2. the group attempts to satisfy the full monthly need with usable food, not merely visible stock
3. leftover food becomes the new `StoredFood`
4. any deficit becomes a shortage
5. the group resolves into `FedStable`, `HungerPressure`, `SevereShortage`, or `Starvation`
6. severe or prolonged shortage causes starvation loss

Population loss is derived from the severity of the deficit. Groups may decline to `0`, which means extinction.

With persistent pressure in place, starvation and hardship tuning should read effective food and water pressure as context, not as a direct substitute for actual shortage. One bad month should still be survivable when conditions recover, while repeated shortage under sustained effective pressure should feel harsher.

No usable food is treated as an emergency case and escalates starvation much faster than an ordinary partial shortage.

`StoredFood` and settlement reserves can improve how much usable food a polity extracts from preserved stock, but preservation does not create food out of nothing. The simulation now consumes real stored stock and converts that stock into usable food through a multiplier, rather than multiplying the stock itself.

There is no automatic reintroduction or respawn.

## Debugging

Phase 8 adds debug output for:

- monthly food need
- primary and fallback acquisition order
- subsistence preference and realized extraction plan
- food gained from gathering and hunting
- total food acquired
- `StoredFood` before and after
- usable food consumed
- food stress state
- hunger pressure and shortage months
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
