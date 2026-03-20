# Phase 9: Movement and Migration

## Scope

Phase 9 adds the first real movement response for population groups. Groups now consider whether to leave their current region after the month's survival outcome, compare only neighboring regions, and move at most one region per month when a nearby option is meaningfully better.

Migration scoring now uses a knowledge-safe regional view rather than direct world truth. Unknown neighboring regions are evaluated from rumor/default expectations and known-world baselines, while previously visited regions can be judged from partial or fully discovered local knowledge.

This phase adds migration decisions and movement execution only. It does not add settlements, diplomacy, territorial control, long-range exploration, pathfinding, or route systems.

## Monthly Order

`SimulationEngine` now runs the effective monthly order:

1. Advance month
2. Flora simulation
3. Fauna simulation
4. Group pressure recalculation
5. Group survival and consumption
6. Migration decision and movement
7. End-of-tick finalization

Migration happens after survival so the group first experiences the current month and only then decides whether it should relocate for the next one.

## Two-Stage Migration Rule

Migration uses two stages:

1. Consider moving
2. Compare neighboring regions

Groups consider migration when current conditions are stressful enough. Migration decisions now use `group.Pressures.Migration.EffectiveValue` as the main trigger, with severe shortage, severe starvation, or very low `StoredFood` acting as simple supporting signals.

The migration pressure shown to the player remains the compressed display value. Internal migration pressure is persistent, decays toward zero each month, and is not gameplay-capped at `100`.

Because migration pressure now persists, migration tuning assumes memory rather than a fresh monthly reset:

- the effective-pressure trigger is higher than the old fresh-month trigger
- a short recent-move restraint reduces immediate re-migration
- required destination margins rise after recent moves and when a return path is involved
- severe shortage or starvation can still force migration consideration sooner

Even when migration is considered, a group only moves if the best neighboring region is sufficiently better than the current region.

## Neighbor-Only Movement

Movement is local only in MVP form:

- a group scores its current region
- it scores only directly adjacent neighboring regions
- it may move to at most one neighboring region in a month

There are no world jumps, no multi-hop searches, and no long-range route plans in this phase.

## Region Scoring

Migration scoring is survival-oriented and uses:

- subsistence-aware gathering support
- subsistence-aware hunting support
- water knowledge
- local threat knowledge
- a small known-region bonus
- a small unknown-region penalty
- a return-to-last-region penalty

The current region is always scored alongside neighbors so a group can stay when leaving is not justified.

Exact hidden support, exact hidden threat, and exact hidden resource totals are not used for undiscovered regions. Partial experience produces partial estimates instead of exact truth.

## SubsistenceMode Influence

`SubsistenceMode` changes how groups judge destination quality:

- `Gatherer` weights flora and water more heavily
- `Hunter` weights fauna and lower threat more heavily
- `Mixed` uses a more balanced score

This remains explicit and simple rather than becoming a broader planning framework.

## Knowledge and History

When a group moves:

- `CurrentRegionId` updates to the destination
- the destination is added to `KnownRegionIds`
- `LastRegionId` records the previous region
- `MonthsSinceLastMove` resets to `0`

When a group stays, `MonthsSinceLastMove` increments.

This is the minimal migration-history support for debugging, anti-thrashing, and later chronicle work.

## Anti-Thrashing

Phase 9 includes simple anti-thrashing tools:

- a neighbor must beat the current region by a minimum score margin
- returning immediately to `LastRegionId` receives a penalty
- very recent movement raises the score margin required for another move unless pressure is extreme

This reduces obvious back-and-forth bouncing without introducing a route-memory system.

## Debugging

Phase 9 adds monthly debug output for:

- migration consideration trigger
- current-region score
- neighbor scores
- winning destination
- whether a move happened
- old and new region
- `LastRegionId`
- `MonthsSinceLastMove`
- decision reasons

These diagnostics now reflect knowledge-safe estimates rather than omniscient region scoring.

## Deferred

The following remain intentionally deferred:

- settlement founding
- territorial systems
- diplomacy and trade
- long-range exploration
- multi-step pathfinding
- group splitting and merging
- explicit movement costs

## Setup For Later Phases

Phase 9 prepares:

- the next MVP phase by giving groups a spatial response to pressure and scarcity
- later chronicle systems by storing minimal migration history
- later expansion work by keeping migration local, causal, and inspectable
