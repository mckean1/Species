# Phase 19: Multi-Polity Interaction and Conflict

Phase 19 adds the first explicit external-political layer between polities without replacing the Chronicle-first MVP structure or turning the sim into a tactical warfare game.

## Implemented Baseline

- Polities now keep bounded pairwise `InterPolityRelation` records.
- Relationship state is driven by real geography and contact:
  - shared active or recently used regions
  - adjacent frontier use
  - exposed settlements
  - contested regional presence
- Relationships now accumulate:
  - trust
  - hostility
  - cooperation
  - frontier friction
  - escalation
  - raid pressure
  - short external memory such as raids, peace, and cooperation months
- Polities now derive an `ExternalPressureState` from those relations each month.

## Relationship Progression

The system remains aggregate and score-driven. Pairwise records can settle into broad states such as:

- Unknown
- Neutral
- Wary
- Cooperative
- Rival
- Hostile
- Raiding conflict
- Open conflict
- Uneasy peace

These states are consequences of recurring contact, pressure, memory, and geography. They are not generated from arbitrary AI mood changes.

## Conflict Scope

- Conflict escalates through friction and memory rather than appearing as instant arbitrary war.
- Raids and open conflict are resolved in aggregate.
- Outcomes can damage frontier settlements, remove stores, raise threat and migration pressure, and intensify later rivalry.
- There is still no tactical combat layer, battlefield simulation, treaty simulator, or kingdom/empire scaling in this phase.

## Internal Feedback

External pressure now feeds back into existing internal systems:

- law proposal pressure
- bloc influence and satisfaction
- legitimacy, cohesion, authority, and compliance
- polity-facing summaries and Chronicle reporting

This keeps outside politics part of the same causal polity simulation instead of a disconnected diplomacy screen.

## Chronicle and UI

- Chronicle can now surface notable cooperation, hostility, raids, open conflict, and uneasy peace.
- Known Polities now uses actual inter-polity relationship state instead of placeholder proximity labels.
- The Polity screen now includes a compact outside-relations summary.

## Explicit Non-Goals

This phase still does not implement:

- tactical combat
- armies or unit movement
- formal treaties
- tribute/vassalage/subordination systems
- kingdom/empire hierarchy
- deep diplomacy content trees

The implemented layer is intentionally bounded, geography-driven, and easy to extend later.
