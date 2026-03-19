# Phase 12: Chronicle MVP

## Scope

Phase 12 adds a dedicated `Chronicle` as the main player-facing output layer. Chronicle-worthy events are now recorded when they happen, stored in chronicle history, and revealed later into a newest-first visible feed so history unfolds instead of dumping immediately.

This phase replaces the old player-facing raw monthly log output with Chronicle-driven output. The Chronicle remains concise, sparse, and one-line oriented.

## Chronicle Purpose

The Chronicle is now the main player-facing simulation output.

It is responsible for:

- storing all chronicle records
- storing pending unrevealed records
- exposing the visible feed in newest-first order
- revealing entries over time instead of showing everything immediately

Debug inspection remains separate from the player-facing feed.

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
9. Chronicle update
10. End-of-tick finalization

This preserves the causal rule that Chronicle records are created only after meaningful monthly outcomes are known.

## Chronicle Entries

Each chronicle entry stores:

- the simulation month when the event happened
- the group involved
- an event category
- one rendered message line
- optional tags

Chronicle entries remain short and readable.

## Feed Behavior

The visible Chronicle behaves like a feed:

- newly revealed entries appear at the top
- older visible entries move downward
- entries are delayed before reveal
- recorded time and reveal time remain separate

In MVP form, events are recorded immediately, wait in a pending queue, and are revealed later according to a simple monthly delay and reveal cap.

## Notable Event Filtering

Only notable outcomes create Chronicle records in MVP form:

- migration
- meaningful food shortage
- meaningful decline from hunger
- group extinction
- discovery unlock
- advancement unlock

Routine monthly stability and low-value internal simulation steps are intentionally excluded.

## Wording Rules

Chronicle messages are one-line, concise, and group-first where practical.

The wording distinction remains strict:

- discoveries use `discovered`
- advancements use `learned`

Examples of the intended style:

- group migrated to a new region
- group suffered food shortages
- group discovered local flora
- group learned improved gathering
- group died out

## Output Model Change

The old player-facing raw tick dump no longer fits the design and has been replaced by Chronicle-driven output.

Development-facing Chronicle inspection still exists for validation and tuning, but it is separate from the player-facing feed.

## Deferred

The following remain intentionally deferred:

- polished Chronicle UI
- large narrative templating
- paragraph storytelling
- feed filtering UI
- historical summaries
- debug traces of every system step
- broader narrative systems

## Setup For Later Phases

Phase 12 prepares:

- future visibility/debug phases by making Chronicle state explicit
- later Chronicle polish without changing simulation causality
- future narrative/history work built on recorded discoveries, advancements, migration, hardship, and extinction
