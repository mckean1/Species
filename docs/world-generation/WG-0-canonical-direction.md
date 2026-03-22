# WG-0: Canonical World Generation Direction and Guardrails

## 1. Purpose

This document defines the canonical long-term direction for world generation in `Species` and establishes the guardrails that later World Generation Roadmap phases must follow.

WG-0 is an architecture and alignment phase. Its job is to lock the intended direction, name the invariants, identify the major anti-patterns, and make the target handoff between world generation and the normal `SimulationEngine` explicit.

This phase does not introduce broad gameplay refactors or major new mechanics. It establishes the source of truth that later phases are expected to implement.

`Species` is simulation first, game second. The world generation roadmap must preserve that priority.

## 2. Canonical Long-Term Direction

`Species` should ultimately generate a world by first producing its physical environment, then seeding only primitive life, then handing control to the normal `SimulationEngine`, allowing all later ecological, biological, social, and political complexity to emerge through ordinary simulation over time. The long-term goal is not to fabricate a developed world through special-case generation logic, but to allow history to arise from prior simulated state.

In canonical form, the game should work like this:

1. Generate the physical world.
2. Seed primitive life.
3. Hand control to the normal `SimulationEngine`.
4. Let later complexity emerge through simulation over historical time.
5. Allow the player to enter the world at some later historical point.
6. Continue active play using the same `SimulationEngine` rather than switching to a different simulation model.

This direction exists to preserve causality. Important things in the world should exist because something earlier produced them.

## 3. Canonical Pipeline

### 3.1 Physical World Generation

The first stage creates the non-living world state that later history depends on. At minimum, this includes:

- regional layout and connectivity
- biome and climate-relevant conditions
- water availability
- fertility and broad ecological support conditions
- material distribution and other physical constraints

This stage establishes the environmental facts that later systems must respect. It should define what is possible, what is difficult, and what is impossible before life is introduced.

### 3.2 Primitive Life Seeding

After the physical world exists, the world should be seeded only with primitive life and other minimally necessary biological starting conditions.

This stage is intentionally narrow. Its purpose is to give the simulation a plausible starting biosphere, not to fabricate a mature world. Primitive life seeding should provide the earliest ecological base from which later flora, fauna, and eventually more complex outcomes can arise.

If the project uses transitional shortcuts before true primitive-life-first generation is complete, those shortcuts must be treated as temporary legacy behavior rather than the desired end state.

### 3.3 Historical Simulation Handoff

Once the physical world and primitive life starting state exist, the normal `SimulationEngine` becomes the governing system.

From that point forward, the same simulation backbone should drive:

- ecological change
- biological change
- discovery accumulation
- advancement gating
- social development
- settlement formation
- political development
- historical continuity

World generation should stop fabricating late-stage outcomes once this handoff occurs. History after the handoff should be produced by simulation, not by a second hidden world generator that manufactures mature results.

### 3.4 Historical Development

Historical development is the long simulated interval between primitive life seeding and player entry.

This period is not separate in principle from active play. It is the same world being advanced through the same causal simulation model at a different point in time. The main difference is that the player has not entered yet.

### 3.5 Player Entry and Active Play

The player should enter an already-simulated world at some later historical point. Active play is a continuation of existing history, not a replacement for it.

The simulation model used during active play must remain compatible with the one used for historical development. The same `SimulationEngine` should govern both phases so the world does not switch from one reality model to another when the player arrives.

### 3.6 Emergence Requirement

The canonical expectation is that later complexity should emerge through normal simulation over time.

This includes, over the long term:

- ecological complexity
- biological diversification
- sapient presence
- settlements
- polities
- social structures
- political history

These should arise from prior simulated state wherever they are important to the world. They should not be inserted as unexplained flavor outcomes just because a generated world seems too empty.

## 4. Core Invariants / Guardrails

The following guardrails govern all later world generation work.

### 4.1 Everything Must Come From Something

Everything important must come from something.

If a system creates a meaningful world fact, there should be an explainable prior cause for it in simulation state, environmental conditions, prior discoveries, prior advancements, or prior actors.

### 4.2 Causality Is Required

Causality is one of the core ideas of `Species`. The world should be explainable as a chain of prior conditions and prior outcomes.

Uncaused important results are architectural failures unless they are explicitly marked as temporary transitional behavior.

### 4.3 No Fabricated Late-Stage Outcomes

World generation should not fabricate late-stage outcomes without simulated causes.

In the long-term architecture, it is not acceptable to create mature sapient societies, political structures, or ecological richness solely because the world would otherwise feel sparse.

### 4.4 Shared `SimulationEngine`

The `SimulationEngine` is the backbone of the game and must be shared between pre-player history and active play.

Historical simulation and active play should not diverge into separate incompatible architectures.

### 4.5 Discoveries and Advancements Stay Distinct

Discoveries are knowledge of the world.

Advancements unlock capability.

These are not interchangeable concepts and should not be collapsed into a single progression bucket.

### 4.6 Capability Must Remain Gated

Capabilities must remain gated by discoveries, prerequisites, and other real enabling conditions.

The simulation should not grant practical capability merely because the world generator wants a richer starting state.

### 4.7 Honest Worlds Over Decorative Worlds

Generated worlds should be honest even when they are sparse, weak, unstable, uneven, or partially failed.

If simulation produces a harsh or underdeveloped world, the correct response is usually to preserve that truth, not to silently decorate over it with invented richness.

### 4.8 History Must Be Explainable and Inspectable

A generated world should support historical explanation.

Players and developers should be able to inspect why important conditions, actors, and structures exist. The project should prefer systems that leave legible traces of historical causes over systems that create opaque outcomes.

### 4.9 Aggregate Modeling by Default

Performance matters.

By default, world generation and historical simulation should prefer aggregate or systemic modeling over fine-grained per-entity simulation unless finer detail is clearly justified.

Higher detail is allowed when necessary, but the burden is on that detail to justify its cost and architectural value.

### 4.10 Transitional Violations Must Be Named as Legacy

Current systems may temporarily violate the target architecture.

When they do, that behavior should be explicitly treated as legacy or transitional behavior, not as the intended final direction. Future roadmap phases are expected to replace those shortcuts over time.

## 5. Anti-Patterns / Non-Goals

The following are explicit non-goals for the long-term architecture.

- Do not spawn kingdoms, empires, or other mature polities just to make the world feel populated.
- Do not fabricate ecology without upstream ecological support.
- Do not inject sapients, settlements, or polities as flavor in the long-term architecture.
- Do not create separate fake world generation logic for systems that should emerge from normal simulation.
- Do not allow historical simulation and active-play simulation models to drift into incompatible architectures.
- Do not silently invent richness when the simulation produces weakness.
- Do not treat temporary shortcuts as proof that the final architecture should keep them.
- Do not replace causal progression with presentation-driven convenience.

## 6. Phase Boundaries

The roadmap should treat the following boundaries as major architectural transitions.

### 6.1 Physical World Generation

Produces the physical environment and non-living constraints.

Outputs:

- regions and connectivity
- physical and environmental conditions
- material realities
- broad ecological support constraints

Does not canonically output:

- mature sapient actors
- settlements
- polities
- developed historical outcomes

### 6.2 Primitive Life Seeding

Introduces only the earliest biological starting state required to begin deep history.

Outputs:

- primitive life seeds
- minimally necessary initial biosphere state

Does not canonically output:

- mature ecology inserted for flavor
- developed sapient societies
- social or political complexity added by hand

### 6.3 Historical Simulation

Begins when the seeded world is handed to the normal `SimulationEngine`.

Outputs:

- emergent ecological and biological history
- discoverable world knowledge
- capability growth through discoveries and prerequisites
- social, settlement, and political development when simulation supports them
- inspectable historical continuity

### 6.4 Player Entry / Active Play Handoff

The player enters the world after historical simulation has already produced some amount of history.

Requirements:

- active play continues from prior world state
- the same `SimulationEngine` remains in charge
- the world presented to the player reflects prior simulated causes rather than worldgen decoration

## 7. Current-State Alignment Summary

This section is intentionally high level. It is not a deep subsystem audit.

### 7.1 What Currently Appears Aligned

The current codebase already contains several pieces that align with the canonical direction:

- `WorldGenerator` already creates a physical world foundation with regions, fertility, biome, water availability, connectivity, and material profile.
- `RegionEcosystemSeeder` and `ProtoLifeSubstrate` indicate an ecological and proto-life oriented starting direction rather than a purely decorative map generator.
- `SimulationEngine` already acts as the main simulation backbone for ecology, biology, discoveries, advancements, settlements, politics, and chronicle updates.
- discoveries and advancements are modeled as distinct concepts, which matches the intended knowledge-versus-capability separation.
- advancement definitions already use prerequisites and access requirements, which aligns with the requirement that capability remain gated.
- biological history and chronicle systems already suggest a preference for explainable historical state rather than purely static flavor.

### 7.2 What Is Likely Transitional Legacy or Shortcut Behavior

Several current behaviors appear useful for the current game state but should be treated as transitional relative to the long-term architecture:

- `WorldGenerator.Create` currently produces a world and then immediately calls `PopulationGroupSpawner.Spawn`, which inserts sapient population groups, polities, government forms, and a focal polity during generation.
- `PopulationGroupSpawner` assigns initial knowledge and awareness directly at spawn time, which is practical today but not the intended final deep-history pipeline.
- starter catalogs and startup flow currently imply a near-playable world created close to player entry rather than a long historical simulation beginning from primitive life.
- active play currently begins from a directly generated playable world instead of selecting a later historical entry point after a distinct pre-player simulation span.

These behaviors are acceptable only as temporary legacy or transitional shortcuts while the roadmap is incomplete.

### 7.3 Major Gaps Before the Vision Can Fully Work

Major gaps still remain before the canonical direction is fully realized:

- a clean separation between physical world generation and later life-history stages is not yet fully established
- primitive-life-first seeding is not yet the full authoritative bootstrap path for the game
- the historical simulation bootstrap and run-to-era flow does not yet exist as a clear first-class pipeline
- player entry at a later historical point is not yet a defined handoff system
- the project does not yet fully guarantee that every major late-stage world fact is inspectable back to prior simulated causes
- long-run performance strategy for deep historical simulation still needs to be validated and refined under the aggregate-modeling preference

## 8. Risks / Transitional Reality

The project is not expected to jump directly from the current state to the final architecture in one phase.

Some current systems may temporarily violate the target model. That is acceptable only when all of the following are true:

- the violation is recognized as temporary legacy behavior
- the violation does not redefine the canonical direction
- later roadmap phases are expected to replace it
- new work does not deepen dependence on the shortcut without a strong reason

The main risk is architectural drift: adding more special-case world generation that makes the world look richer in the short term while moving the project farther away from causal history.

WG-0 exists partly to stop that drift by establishing the governing direction now.

## 9. WG-0 Exit Criteria

WG-0 is complete when all of the following are true:

- a repo-visible source-of-truth document exists for the canonical world generation direction
- the canonical pipeline is explicitly defined from physical world generation through player entry
- the handoff to the normal `SimulationEngine` is explicit
- the requirement that later complexity emerge through normal simulation is explicit
- the core invariants and guardrails are written down in concrete terms
- the anti-patterns and non-goals are written down in concrete terms
- transitional legacy behavior is explicitly acknowledged as temporary rather than canonical
- the current-state alignment summary names major aligned areas, likely shortcuts, and major gaps at a high level
- later World Generation Roadmap phases can treat this document as the governing architectural direction

## 10. Relationship to the World Generation Roadmap

This document is the governing direction for later World Generation Roadmap phases.

`docs/world-generation/WG-1-current-state-audit.md` records the current-state audit against this direction.

Future WG phases should refine implementation details, phase sequencing, and subsystem changes without violating the architectural rules established here.

If a later phase proposes behavior that conflicts with this document, the default assumption should be that the later phase is wrong unless this document is intentionally revised.

In practical terms, later phases should move the project toward:

- more faithful physical world generation
- cleaner primitive life seeding
- a stronger historical simulation bootstrap
- player entry into already-simulated history
- fewer fabricated late-stage shortcuts
- stronger inspectability of historical causes
- performance-conscious aggregate simulation that still preserves causality
