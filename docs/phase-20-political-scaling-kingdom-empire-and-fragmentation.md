# Phase 20: Political Scaling, Kingdom, Empire, and Fragmentation

Phase 20 adds the first explicit large-state history layer on top of the existing polity, governance, social, and inter-polity systems.

## Implemented Baseline

- Polities now carry a bounded `PoliticalScaleState`.
- Scale is driven by real capacity:
  - settlement count
  - regional reach
  - material resilience and storage support
  - governance capacity
  - external success
- Larger states now accumulate structural burdens:
  - coordination strain
  - distance strain
  - composite complexity
  - overextension pressure
  - fragmentation risk
- Polities can now hold active political attachments to other polities in broad forms such as:
  - subordinate attachment
  - federated attachment
  - direct integration

## State Forms

The implemented scale forms are:

- Local polity
- Regional state
- Kingdom-like realm
- Composite realm
- Empire-like state
- Fragmenting realm

These are structural state summaries, not decorative rank labels.

## Attachment and Integration

When one stronger polity gains durable leverage over another, the outcome is no longer limited to simple coexistence.

The current layer can produce:

- direct integration of a weak polity into a stronger center
- subordinate attachment under a stronger center
- federated or loose attachment under a stronger center

These outcomes are still aggregate. They do not add title trees, dynastic offices, or detailed contract systems.

## Overreach and Fragmentation

Larger scale now increases risk as well as power.

- wide reach raises distance strain
- multiple settlements and attached polities raise coordination burden
- composite rule raises integration burden
- poor legitimacy, cohesion, and authority raise fragmentation risk
- overstretched large states can now:
  - lose subordinate attachments
  - suffer breakaway successor states
  - shift into an explicit fragmenting state form

Fragmentation is driven by accumulated strain rather than random collapse.

## Existing-System Feedback

Political scale now feeds back into:

- governance and compliance
- law proposal pressure
- bloc behavior
- inter-polity behavior
- polity-facing summaries
- Chronicle major political transitions

This keeps scaling part of the same causal simulation instead of becoming a separate late-game minigame.

## Explicit Non-Goals

This phase still does not implement:

- feudal title ladders
- dynastic family trees
- court politics
- detailed vassal contracts
- province-by-province administration
- detailed bureaucracy management

The system remains intentionally aggregate, bounded, and extendable.
