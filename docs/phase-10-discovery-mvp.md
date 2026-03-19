# Phase 10: Discovery MVP

## Scope

Phase 10 adds group-owned discoveries as knowledge unlocked from repeated lived experience. Discoveries now exist as explicit content, groups accumulate lightweight evidence for them over time, and monthly discovery evaluation runs after survival and migration.

This phase adds discoveries only. It does not add advancements, raw output bonuses, research systems, Chronicle generation, or discovery sharing.

## Discovery Versus Advancement

The distinction is strict:

- Discovery = "we know"
- Advancement = "we can"

Discoveries improve decision-making only.

Discoveries do not directly improve:

- gathering yield
- hunting yield
- storage size
- travel speed
- survival output

If a change directly improves capability or output, it belongs to a later advancement phase instead.

## Monthly Order

`SimulationEngine` now runs the effective monthly order:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Group survival and consumption
6. Migration decision and movement
7. Discovery evaluation
8. End-of-tick finalization

This keeps discoveries grounded in completed lived actions from the month.

## Discovery Content

Discovery content is authored so it reads naturally after "discovered".

Examples in MVP form include:

- region flora knowledge
- region fauna knowledge
- region water-source knowledge
- region-condition knowledge
- route knowledge between neighboring regions

The catalog is generated for the current world so discoveries stay region- and route-specific while remaining explicit definitions.

## Evidence Tracking

Each group now keeps a lightweight evidence state for repeated experience:

- successful gathering months by region
- successful hunting months by region
- months spent in region
- water exposure months by region
- route traversal counts

There is no large memory framework, no decay, and no discovery sharing in this phase.

## Unlock Rules

Discovery unlocks are binary and threshold-based.

Examples:

- repeated successful gathering in one region unlocks local flora knowledge
- repeated successful hunting in one region unlocks local fauna knowledge
- repeated residence in one region unlocks local region conditions
- repeated water exposure in one region unlocks local water-source knowledge
- repeated traversal of the same neighboring connection unlocks route knowledge

## Decision Effects Only

Discoveries now affect decision inputs, especially migration scoring:

- known local flora improves how flora opportunity is recognized
- known local fauna improves how fauna opportunity and threat are recognized
- known local water sources improve water-aware evaluation
- known local region conditions improve confidence in the region profile
- known routes reduce uncertainty when evaluating a neighboring move

These are decision-context effects only. They do not modify direct action output.

## Chronicle Readiness

Discovery names are written so future Chronicle text can naturally say things like:

- group discovered Copper March Flora
- group discovered Iron Coast Water Sources
- group discovered Copper March to Iron Coast Route

Chronicle generation itself remains deferred.

## Deferred

The following remain intentionally deferred:

- advancement definitions and unlocks
- raw bonuses tied to discoveries
- discovery sharing between groups
- forgetting or decay
- research trees or research points
- Chronicle generation

## Setup For Later Phases

Phase 10 prepares:

- Phase 11 by preserving a clean boundary between knowledge and capability
- later Chronicle work by making discoveries explicit and chronicle-readable
- later group decision systems by giving groups experience-based knowledge of regions and routes
