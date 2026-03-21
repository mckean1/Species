# Phase 19: Sapience Emergence

## Scope

Phase 19 adds rare biological sapience emergence from fauna lineage history.

This is not a random spawn system and not a civilization system. It extends the existing biological evolution loop so a fauna branch can, in rare cases, produce an emergent sapient lineage when long-running ecological and adaptive conditions converge.

## Causal Model

Sapience emerges only from fauna lineage history.

The emergence criteria are intentionally strict:

- long continuity and persistence
- repeated successful survival under good food support
- sustained social or coordination pressure
- sustained learning or adaptation pressure
- strong viability
- no active collapse or starvation state
- only one sapient emergence per qualifying fauna lineage branch, rather than repeated regional re-rolls from the same lineage

Most fauna branches never satisfy this convergence.

## Monthly Integration

The implementation lives inside `BiologicalEvolutionSystem`.

Each month, fauna regional profiles now accumulate:

- stable-support months
- social-coordination-pressure months
- learning-pressure months
- slow sapience progress

Only when those conditions remain favorable for a long time can sapience progress climb high enough for a rare emergence trigger.

This is intentionally a convergence pipeline, not a direct threshold unlock:

1. long-run ecological stability accumulates
2. coordination and learning pressure accumulate
3. sapience progress advances slowly under those favorable conditions
4. only then can a low monthly emergence chance fire

## Outcome

When sapience emerges:

- a new `SapientSpeciesDefinition` is created
- it records the fauna lineage it emerged from
- the new lineage is classified as `SpeciesClass.Sapient`
- biological history records the emergence

The fauna ancestry remains conceptually true. The sapient lineage is new and downstream from that fauna branch rather than replacing fauna history.

## Stability Note

The first biology tuning pass now actively tries to suppress these long-run failure modes:

- empty harsh regions quietly refilling through proto pressure alone
- fauna genesis without real food-base support
- repeated genesis retries from the same local opportunity
- predator-prey overshoot from overly efficient predation
- sapients bypassing ecological food truth
- species awareness unlocking use too quickly
- starvation swinging between too-soft persistence and binary collapse

## Deferred

This phase does not add:

- full polity formation from new emergent sapients
- agriculture or civilization systems
- demographic transfer from fauna populations into sapient actor populations
- broad sapient worldgen replacement

Those remain later-layer work.
