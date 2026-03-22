# Phase 15: Discovery and Advancement Deepening

Phase 15 deepens the existing discovery and advancement layers without turning the prototype into a tech-tree or institution simulation.

The guiding split is now explicit:

- encounter = first contact, signs, or limited observation
- discovery = durable finding from exposure, pressure, continuity, or contact
- advancement = usable capability that becomes reliable only when real prerequisites support it

## What Changed

Discoveries now emerge from repeated simulation truth, including:

- repeated gathering, hunting, residence, and water exposure
- material exposure and material use
- recurring food, threat, and material strain
- durable settlement or anchoring continuity
- repeated route use and seasonal return patterns
- internal polity continuity and neighboring polity contact

Advancements now require combinations of:

- prerequisite discoveries
- repeated evidence of practical use
- grounded polity conditions such as storage, tool, or shelter support
- continuity and stability where relevant
- sustained need or pressure where relevant

## Discovery and Advancement Scope

The system remains intentionally aggregate.

Implemented discovery additions include:

- `Clay Shaping`
- `Seasonal Tracking`
- `Preservation Clues`
- `Shelter Methods`

Existing region and route discoveries remain in place, but now depend more clearly on repeated exposure rather than simple time passing.

Advancements now use a first-wave causal capability set:

- `Foraging`
- `Small Game Hunting`
- `Large Game Hunting`
- `Fishing`
- `Trapping`
- `Food Drying`
- `Food Storage`
- `Stone Toolmaking`
- `Hide Working`
- `Fiber Working`

This is still not a sprawling abstract tech tree.

## Exposure Memory

`DiscoveryEvidenceState` and `AdvancementEvidenceState` now carry lightweight aggregate memory for:

- tagged flora/fauna/resource opportunity
- recurring food, spoilage, and material pressure
- anchored continuity, stability, and organizational readiness
- discovery spread exposure and repeated surplus opportunity

These are explicit counters, not a cognition model.

## Discovery Spread

Discovery can now spread in two lightweight ways:

- internally across a polity when continuity and durable social contact are present
- across polity contact when groups share or border the same regional space over time

Spread remains limited and definition-driven. This is not an education or institution layer.

## Capability Effects

Advancements now feed back more concretely:

- foraging and hunting capabilities improve subsistence output only after discovered prey/flora opportunities become repeatable practice
- drying and storage capabilities now depend on surplus, spoilage pressure, and continuity
- stone toolmaking improves practical extraction and food processing where tool stone is discovered
- hide and fiber working improve material support only when their source species are both discovered and repeatedly usable

## Design Boundary

This phase does not add:

- formal schooling
- institutions
- religion, language, or myth simulation
- giant research trees
- diplomacy or warfare overhauls
- kingdom or empire hierarchy

The purpose is to make discovery and capability more causally truthful while keeping the model readable and maintainable.

## Scouting Extension

Discovery now also has a lightweight scouting extension at the polity layer.

- scouting is abstract rather than entity-heavy
- it only targets adjacent or otherwise credibly reachable nearby regions
- it requires enough spare stability/support plus at least one motive such as food pressure, migration pressure, resource need, neighbor pressure, opportunity pull, or curiosity
- it can reveal regions, water, visible flora/fauna, tool-stone resources, and sapient encounters
- it does not grant extraction capability or unlock advancements on its own

Scout-sourced Chronicle lines explicitly identify scouts as the source of the discovery or encounter.

## Curiosity Pressure

Curiosity is now a polity pressure rather than a hidden flavor value.

- it rises when a polity goes a long time without real novelty
- novelty means a new discovery, sapient encounter, or learned advancement
- it only grows when reachable nearby unknown opportunity still exists
- severe survival crisis suppresses or zeroes curiosity growth
- curiosity does not reveal anything by itself; it only increases scouting drive and unknown-frontier priority

This keeps exploratory restlessness causal and visible without turning it into abstract research.
