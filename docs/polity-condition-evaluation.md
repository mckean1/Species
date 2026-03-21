# Polity Condition Evaluation

The polity condition pipeline now runs as a single finalized evaluation pass each monthly tick in `SimulationEngine` after political scaling and before bloc and law-facing presentation work.

## Evaluation Order

Each polity is finalized in this order:

1. Material survival assessment
2. Spatial stability and anchoring validation
3. Polity integrity assessment
4. Governance condition assessment
5. Government form and scale validation
6. Finalized polity condition snapshot generation

The implementation lives in `Species.Domain/Simulation/PolityConditionEvaluator.cs`.

## Material Survival

Material survival uses current effective pressure values plus material production deficits and shortage state. Display values may remain capped for presentation, but the evaluator uses the underlying simulation-facing condition bands to classify:

- Food condition
- Water condition
- Threat condition
- Crowding condition
- Migration condition
- Overall severity

Food, water, and migration pressure are load-bearing inputs for all later condition checks.

Threshold intent:

- severity bands are soft thresholds, not hard crashes
- sustained shortage months can escalate food and migration severity faster than a single bad month
- retention buffers exist to reduce edge jitter when a polity is hovering around a band boundary

## Spatial Stability

Anchoring is validated from current reality, not preserved by inertia. An anchored polity now requires a real stable base:

- meaningful presence in the core region
- a valid active primary settlement when the form expects one
- no unresolved extreme displacement state
- migration pressure low enough to support a durable base

If those conditions fail, the polity degrades toward `SemiRooted`, `Seasonal`, or `Mobile` instead of keeping a stale anchored identity.

Stabilization intent:

- temporary seat loss should not immediately erase a genuinely anchored polity
- sustained migration pressure plus loss of recent core presence is treated as true displacement
- seat-dependent forms are still hard-gated when no valid seat remains

## Integrity and Governance

Polity integrity is driven from current fragmentation, overreach, peripheral strain, distance strain, material survival, and spatial stability. Governance is then recalculated from the finalized facts, so legitimacy, cohesion, authority, and governability are consequences of present conditions rather than stale labels.

This means:

- food and water crisis reduce legitimacy and governability
- displacement and migration pressure reduce cohesion
- fragmentation and peripheral strain reduce authority and governability

Calibration intent:

- moderate strain should push governance into strained or failing bands before collapse
- collapse should require clearly multi-axis governance failure, not a single weak metric
- integrity and governance both use small retention buffers to limit month-to-month flips near threshold edges

## Government Form Validation

Government form describes current reality, not prestige or historical continuity. Each tick, advanced forms are revalidated against:

- current population scale
- validated scale form
- integrity band
- governance metrics
- seat and anchoring validity
- material survival state

If a polity no longer qualifies, it is downgraded to the highest form it still truthfully supports.

Downgrade intent:

- retention buffers apply only when holding the current form
- catastrophic material or integrity failure still causes rapid truthful downgrade
- otherwise the evaluator prefers the highest still-valid lower form rather than collapsing directly to the bottom

## UI Contract

Polity-facing UI should consume the finalized `PolityConditionSnapshot` rather than rebuilding issue summaries from ad hoc pressure reads. This keeps:

- current issues
- strengths and problems
- governance notes
- scale notes
- government form
- anchoring state

aligned to one canonical truth source.
