# Canonical Biology Architecture

This document is the canonical naming and boundary reference for the biology/ecology rehaul foundation.

It defines architecture only. It does not add full ecology behavior, detailed sapient emergence simulation, or any change to the aggregate monthly tick order.

## SpeciesClass

`SpeciesClass` is the top-level biological classification model.

- `Flora`: plantlike or sessile life lineages that anchor the regional producer layer.
- `Fauna`: non-sapient animal life lineages that occupy the regional consumer/predator layer.
- `Sapient`: active sapient species lineages that act through polity-bearing `PopulationGroup` simulation.

Canonical rule:

- Every active species belongs to exactly one `SpeciesClass`.
- Flora and fauna remain the ecological substrate classes.
- Sapients are biologically downstream of fauna history, but once emerged they are modeled as their own active `SpeciesClass`.

## Sapience Boundary

Sapience does not emerge from flora.

Canonical rule:

- sapience emerges from fauna lineage/history
- the originating lineage remains a fauna lineage in its own history
- the emerged sapient lineage is then classified as `SpeciesClass.Sapient`

Sapience emergence is intentionally rare and causally grounded. It requires a long-running fauna branch with:

- long-term survival stability
- adequate food support
- sustained social or coordination pressure
- favorable learning or adaptation pressure
- continuity over time
- no active collapse state

Most fauna lineages never meet this convergence. When one does, the fauna ancestry remains historically true while the newly emerged lineage becomes an active sapient lineage.

Stability rule:

- sapience progress should build slowly from long-run ecological success, not from short spikes
- collapse, starvation, or weak viability should unwind progress
- a fauna lineage should not repeatedly emit sapient branches as a routine outcome

## Proto-Life Substrate

Regions now have an explicit proto-life substrate layer at the ecology boundary.

The canonical regional substrate concepts are:

- `ProtoFloraCapacity`: aggregate regional capacity for flora-supporting proto-life conditions before concrete flora populations are considered
- `ProtoFaunaCapacity`: aggregate regional capacity for fauna-supporting proto-life conditions before concrete fauna populations are considered
- `ProtoFloraPressure`: current monthly proto-life activation pressure for future flora genesis/recovery conditions
- `ProtoFaunaPressure`: current monthly proto-life activation pressure for future fauna genesis/recovery conditions
- `FloraOccupancyDeficit`: how far active flora occupancy sits below flora-supporting capacity
- `FaunaOccupancyDeficit`: how far active fauna occupancy sits below fauna-supporting capacity
- `FloraSupportDeficit`: how far local flora-support conditions sit below flora-supporting capacity
- `FaunaSupportDeficit`: how far local fauna-support conditions sit below fauna-supporting capacity
- `EcologicalVacancy`: combined open ecological room across flora and fauna layers
- `RecentCollapseOpening`: temporary opening left by recent collapse/extinction turnover

Capacity and pressure are not the same thing.

- capacity = what the region could support in structural ecological terms
- pressure = how strongly the substrate is currently pushing toward future refill/recovery conditions

High-capacity regions can still have low pressure if they are already occupied or environmentally quiet.
Low-capacity regions do not automatically gain high pressure.

Stability rule:

- proto pressure is damped by saturation, harshness, and weak active ecology
- empty regions do not auto-refill just because capacity exists
- proto-flora pressure can only weakly assist proto-fauna pressure; active food-base support must still exist

Boundary rule:

- proto-life substrate is regional ecological groundwork
- it is not itself a species population model
- it informs seeding architecture and later ecology layers
- it does not replace explicit flora/fauna populations
- proto pressure alone does not directly create species
- new flora/fauna species can emerge only through a staged genesis pipeline:
  - sustained proto-pressure readiness
  - a shorter candidate window after strong readiness has held
  - a rare lineage-candidate trigger
  - establishment viability
  - success or failure with cooldown and pressure reduction

Genesis boundary:

- flora genesis requires sustained proto-flora pressure, suitable environment, open ecological room, and stability
- fauna genesis requires sustained proto-fauna pressure, suitable environment, real food-base support, open ecological room, and stability
- genesis remains aggregate and region-based
- genesis is intentionally rare and suppressed in already-full ecosystems
- failed attempts spend some local opportunity through pressure loss and cooldown, but successful establishment spends more
- successful genesis is not itself a collapse opening and must not behave like hidden respawn

Flora boundary:

- flora is the real regional producer layer and ecological energy base
- flora abundance rises and falls from local support, consumption, harshness, spread, and recovery opportunity
- weak flora should materially weaken fauna and sapient food support above it

Fauna boundary:

- fauna survive through actual resolved food access, not generic regional food totals
- usable food means diet-resolved intake after access friction, conversion, and local availability are applied
- explicit diet links are staged: preferred foods first, fallback foods only for remaining unmet intake
- underfeeding first creates hunger pressure, then suppresses reproduction, then causes starvation and migration pressure

Sapient ecology boundary:

- sapients are intentional ecological actors, but they are still bound to usable-food truth
- sapients may intentionally hunt or forage only species their polity has actually discovered
- `Encountered` contact can support awareness and future progress, but not reliable extraction
- sapient extraction depletes the same regional flora/fauna abundance used by the food web
- if known usable species plus accessible reserves do not cover need, sapients accumulate hunger and then starve

## Encounter and Discovery

The canonical player-facing discovery-state naming is:

- `Encountered`: a polity has contact, signs, or limited observation, but has not yet unlocked a durable finding
- `Discovered`: a polity has unlocked a durable finding from repeated exposure, contact, pressure, or continuity

`Unknown` remains the absence state when none of the above exist.

Boundary rule:

- `Encountered` is not yet durable discovery
- `Discovered` is the durable informational unlock that should affect polity decision-making
- there is no separate player-facing `Knowledge` layer

For flora/fauna awareness specifically:

- `Encountered` = signs, sightings, or incidental contact with a species
- `Discovered` = the polity recognizes the species as a distinct resource, prey, threat, or recurring organism closely enough to use it intentionally

System boundary:

- species awareness is tracked per polity-species relationship
- progress is staged and monthly, not instant full unlock
- lower abundance slows progress because scarce species generate less repeatable contact
- passive regional presence alone should usually only build `Encountered` slowly
- `Discovered` requires repeated exposure over time
- intentional flora/fauna exploitation requires `Discovered`
- discovery may inform planning, messaging, and future progress, but it does not come from arbitrary timers or abstract research
- this is still separate from broader research or speculative science systems

## Progression Ownership

The progression ownership split is:

- species awareness owns flora/fauna species familiarity through `Unknown -> Encountered -> Discovered`
- broader discoveries own non-species findings such as routes, materials, conditions, and methods
- advancements own repeatable operational capability

These systems are related, but they do not mean the same thing.

- species awareness answers "have we encountered or discovered this species yet?"
- discoveries answer "what durable finding has the polity learned about the world?"
- advancements answer "what can we now do reliably?"

## System Boundaries

This foundation keeps the existing aggregate simulation design intact.

- `SimulationEngine` remains the canonical monthly orchestrator.
- Monthly tick assumptions remain unchanged.
- Population-level ecology remains aggregate and region-driven.
- This phase does not add speculative future systems beyond the canonical biology/naming model.

## Stability Controls

The biology stack is tuned around a few explicit failure modes:

- proto-life hidden respawn is controlled by decay, harshness drag, saturation suppression, readiness windows, candidate windows, and cooldowns
- food-web oscillation is controlled by conversion inefficiency, prey refuge, fallback penalties, lagged reproduction, and migration relief
- sapient food-truth violations are controlled by discovery-gated intentional use plus starvation from usable-food shortfall
- discovery burstiness is controlled by staged awareness and monthly gain caps
- starvation chaos is controlled by progressive hunger and shortage carryover rather than one-tick binary survival
