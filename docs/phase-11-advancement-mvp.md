# Phase 11: Advancement MVP

## Scope

Phase 11 adds group-owned advancements as practical capabilities unlocked from discoveries plus repeated practical use. Advancements now exist as explicit content, groups accumulate lightweight advancement evidence, monthly advancement evaluation runs after discovery evaluation, and learned advancements directly improve execution in survival and migration.

This phase adds advancements only. It does not add tech trees, research points, sharing, Chronicle generation, or civilization-scale progression.

## Discovery Versus Advancement

The distinction remains strict:

- Discovery = "we know"
- Advancement = "we can"

Discoveries improve decision-making only.
Advancements directly improve execution and capability.

In MVP form, the direct execution improvements now live in advancements, not discoveries.

## Monthly Order

`SimulationEngine` now runs the effective monthly order:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Group survival and consumption
6. Migration decision and movement
7. Discovery evaluation
8. Advancement evaluation
9. End-of-tick finalization

This preserves the causal rule that practical capability comes after lived experience and any new knowledge unlocked from it.

## Advancement Content

Advancement names are written to read naturally after "learned".

MVP content includes:

- Improved Gathering
- Improved Hunting
- Food Storage
- Organized Travel
- Local Resource Use

These are capabilities and methods, not facts.

## Evidence Tracking

Each group now keeps lightweight advancement evidence for:

- successful gathering after relevant flora knowledge
- successful hunting after relevant fauna knowledge
- months with food surplus / stored food
- repeated travel on known routes
- repeated successful survival in known regions

There is no large skill framework, no decay, and no sharing in this phase.

## Unlock Rules

Advancements unlock through explicit threshold checks.

Typical pattern:

- a relevant discovery prerequisite exists when appropriate
- the group repeats a practical behavior or use case
- the threshold is met
- the advancement is learned

Unlocks are binary in MVP form.

## Practical Effects

Advancements now directly affect execution:

- Improved Gathering increases food gained when gathering
- Improved Hunting increases food gained when hunting
- Food Storage improves the effective value of StoredFood when it is used
- Organized Travel improves migration execution on known routes
- Local Resource Use improves extraction in regions the group understands well

These are direct capability effects and are therefore advancements, not discoveries.

## Chronicle Readiness

Advancement names are authored so future Chronicle text can naturally say things like:

- group learned Improved Gathering
- group learned Food Storage
- group learned Organized Travel

Chronicle generation itself remains deferred.

## Deferred

The following remain intentionally deferred:

- large tech trees
- research points
- advancement sharing
- forgetting or decay
- broader civilization progression
- Chronicle generation

## Setup For Later Phases

Phase 11 prepares:

- later Chronicle work by making capability unlocks explicit and chronicle-readable
- later progression systems by preserving the discovery-first, capability-second structure
- deeper survival and movement simulation by allowing practical methods to alter outcomes causally
