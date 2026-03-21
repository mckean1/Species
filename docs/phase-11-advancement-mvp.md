# Phase 11: Advancement Capability Layer

## Scope

Phase 11 adds group-owned advancements as practical capabilities unlocked from discoveries plus repeated practical use. Advancements now exist as explicit content, groups accumulate lightweight advancement evidence, monthly advancement evaluation runs after discovery evaluation, and learned advancements directly improve execution in survival and migration.

This phase adds advancements only. It does not add tech trees, research points, sharing, Chronicle generation, or civilization-scale progression.

## Discovery Versus Advancement

The distinction remains strict:

- Encounter = "we have signs/contact"
- Discovery = "we have a durable finding"
- Knowledge = "we understand enough to operate from it"
- Advancement = "we can"

Discoveries improve decision-making only.
Advancements directly improve execution and capability.

Direct execution improvements live in advancements, not discoveries or species-awareness states.

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

Advancements are now intentionally slower than discoveries. Each advancement moves through:

- capability progress: repeated evidence that the polity is building a workable method
- adoption progress: continued time under valid conditions until that method becomes a durable operational capability

Both layers are monthly, capped, and condition-driven rather than binary threshold jumps.

## Advancement Content

Advancement names are written to read naturally after "learned".

Current content includes:

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

There is no large skill framework and no sharing in this phase. Advancement pacing now includes slow progress loss when required conditions disappear, so dormant opportunities do not sit at a hidden threshold and burst into multiple same-tick unlocks later.

## Unlock Rules

Advancements now unlock through paced progress rather than raw threshold checks.

Typical pattern:

- a relevant discovery prerequisite exists when appropriate
- the group repeats a practical behavior or use case
- capability progress accumulates
- adoption progress follows more slowly
- the advancement is learned only after both are complete

This preserves the distinction that discovery is awareness while advancement is repeatable operational capability.

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

The indirect Chronicle effect is that important advancement milestones now arrive more naturally over time instead of landing in large same-tick piles without strong ongoing cause.

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
