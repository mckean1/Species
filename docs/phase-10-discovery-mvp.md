# Phase 10: Discovery MVP

## Scope

Phase 10 adds group-owned discoveries as durable findings unlocked from repeated lived experience. Discoveries now exist as explicit content, groups accumulate lightweight evidence for them over time, and monthly discovery evaluation runs after survival and migration.

This phase adds discoveries only. It does not add advancements, raw output bonuses, research systems, Chronicle generation, or discovery sharing.

The current architecture now keeps this layer for region, route, and material discoveries, while flora/fauna species awareness is handled as a separate polity-owned staged progress model (`Encounter`, `Discovery`, `Knowledge`) rather than a binary regional unlock.

## Discovery Versus Advancement

The distinction is strict:

- Encounter = "we have signs/contact"
- Discovery = "we have a durable finding"
- Knowledge = "we understand this well enough to operate from it"
- Advancement = "we can"

Discoveries sit between encounter and full knowledge.
They improve decision-making by widening what the polity can safely see or estimate about the world.

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

There is no large memory framework and no discovery sharing in this phase. Discovery pacing now includes slow progress loss when supporting exposure disappears, which keeps stalled discoveries from piling up and then bursting complete later.

## Unlock Rules

Regional/material discovery progression is now paced and progress-based rather than instant threshold completion.

Examples:

- repeated successful gathering in one region unlocks local flora knowledge
- repeated successful hunting in one region unlocks local fauna knowledge
- repeated residence in one region unlocks local region conditions
- repeated water exposure in one region unlocks local water-source knowledge
- repeated traversal of the same neighboring connection unlocks route knowledge

These signals now:

- accumulate monthly progress instead of unlocking immediately at the first threshold crossing
- spend from a limited monthly discovery budget per group
- unlock only after paced progress reaches completion
- reduce same-tick clusters unless there is unusually strong ongoing evidence across multiple fronts

## Decision Effects Only

Discoveries now affect decision inputs, especially migration scoring and other planning pressure:

- known local flora improves how flora opportunity is recognized
- known local fauna improves how fauna opportunity and threat are recognized
- known local water sources improve water-aware evaluation
- known local region conditions improve confidence in the region profile
- known routes reduce uncertainty when evaluating a neighboring move

The implementation now preserves a small distinction between:

- world truth
- actor knowledge / observed regional view
- player-facing presentation of that knowledge

Unknown stays unknown, encounters stay weak/incomplete, discoveries promote specific fields into durable local findings, and sustained familiarity can become stronger knowledge.

These are decision-context effects only. They do not modify direct action output.

By contrast, flora/fauna species awareness now advances monthly from abundance, overlap, conspicuousness, repeated contact, subsistence behavior, and successful interaction. That awareness remains staged and capped per month so species do not become instantly usable just because they are present.

The tuned pacing rule is:

- passive presence mostly builds `Encounter`
- `Discovery` needs repeated overlap/contact over time
- `Knowledge` needs repeated successful use
- scarce species progress much more slowly because low abundance reduces repeatable contact and successful interaction

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
