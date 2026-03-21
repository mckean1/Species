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
  - holding conditions over time
  - a rare lineage-candidate trigger
  - establishment viability
  - success or failure with cooldown and pressure reduction

Genesis boundary:

- flora genesis requires sustained proto-flora pressure, suitable environment, open ecological room, and stability
- fauna genesis requires sustained proto-fauna pressure, suitable environment, real food-base support, open ecological room, and stability
- genesis remains aggregate and region-based
- genesis is intentionally rare and suppressed in already-full ecosystems

## Encounter, Discovery, Knowledge

The canonical knowledge-state naming is:

- `Encounter`: a group has contact, signs, or limited observation, but not durable established understanding
- `Discovery`: a group has unlocked a durable specific finding from repeated exposure
- `Knowledge`: a group has strong operational understanding, usually through sustained local familiarity plus discovery-backed context

`Unknown` remains the absence state when none of the above exist.

Boundary rule:

- `Encounter` is not yet durable discovery
- `Discovery` is not yet full operational knowledge
- `Knowledge` is the strongest player/simulation-facing state short of omniscience

For flora/fauna awareness specifically:

- `Encounter` = signs, sightings, or incidental contact with a species
- `Discovery` = the polity recognizes the species as a distinct resource, prey, threat, or recurring organism
- `Knowledge` = the polity has reliable practical understanding of how to use or respond to that species

System boundary:

- species awareness is tracked per polity-species relationship
- progress is staged and monthly, not instant full unlock
- lower abundance slows progress because scarce species generate less repeatable contact
- passive regional presence alone should usually only build `Encounter` slowly
- `Discovery` requires repeated exposure over time
- `Knowledge` requires repeated successful practical interaction, not just co-presence
- sapient extraction may intentionally target species from `Discovery` onward, but `Discovery` remains partial usability and `Knowledge` remains the reliable use state
- this is still separate from broader research or speculative science systems

## System Boundaries

This foundation keeps the existing aggregate simulation design intact.

- `SimulationEngine` remains the canonical monthly orchestrator.
- Monthly tick assumptions remain unchanged.
- Population-level ecology remains aggregate and region-driven.
- This phase does not add speculative future systems beyond the canonical biology/naming model.
