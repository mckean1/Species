# WG-3 Pre-WG-4 Cleanup

## Purpose

This document describes the cleanup work performed after WG-3 primitive life seeding to establish a clearer, more stable foundation for WG-4 ecosystem development.

WG-3 successfully established metadata-driven primitive life seeding and removed sapient spawning from world generation. However, several transitional issues remained that could create confusion or technical debt as WG-4 ecosystem work begins.

This cleanup resolves those issues while staying tightly scoped and preserving working behavior.

---

## Remaining WG-3 Transitional Issues (Pre-Cleanup)

### 1. Dual substrate models with calculation duplication

**Issue:**
- `PrimitiveLifeSubstrate` was introduced as the "canonical" primitive organic state
- `ProtoLifeSubstrate` was retained as "legacy for backward compatibility"
- Both models calculate capacity using nearly identical formulas
- `BuildLegacyProtoLifeSubstrate()` in `PrimitiveLifeSeeder` duplicates the capacity calculation logic
- Simulation systems (`ProtoPressureSystem`, `BiologicalEvolutionSystem`) still depend on `ProtoLifeSubstrate`

**Why this is a problem:**
- Capacity calculation drift: if one formula changes, the other could diverge
- Unclear which substrate is the actual source of truth
- Maintenance burden: changes to capacity logic must be made in two places
- Conceptual confusion: "canonical" substrate isn't actually driving the simulation

### 2. Named catalog species still represent "primitive" life

**Issue:**
- WG-3 seeds concrete named catalog species: "Grass", "Shrub", "Reed", "Small Grazer"
- These are full species definitions with names, biological traits, tags, and all mature-species metadata
- The seeding is metadata-driven (good) but the entities being seeded are still mature-looking species

**Why this might be acceptable:**
- The metadata model (`PrimitiveSeedRole`) successfully decouples worldgen from specific species IDs
- The starter catalog can be modified without changing worldgen code
- Named species are simpler to work with than abstract substrate layers
- WG-4 can build on top of these species with divergence/evolution

**Why this might need improvement:**
- Conceptually, "Grass" and "Small Grazer" feel like mature ecology, not primitive substrate
- Documentation suggests WG-3 should be more minimal and foundational
- If these species are meant to be transitional placeholders, that intent isn't clear

**Resolution:**
- Keep named species for now as the pragmatic implementation
- Clearly document that these are "starter primitive species" not mature ecology
- Ensure WG-4 treats them as starting points for ecosystem development, not as endpoints
- Future work (post-WG-4) could introduce more abstract primitive substrate if needed

### 3. Primitive-world not usable without polities

**Issue:**
- World now starts with no sapients, no polities, no settlements
- `World.FocalPolityId` will be empty
- `PlayerViewState.FocalPolityId` will be empty
- Many client screens assume a polity exists (Laws, Politics, Government, etc.)
- The client will fail startup validation or crash when trying to render polity-dependent screens
- No meaningful way to inspect or interact with the primitive-world state

**Why this is a problem:**
- Developer cannot inspect the WG-3 output
- Cannot test primitive life seeding results
- Cannot verify biological profiles, populations, or substrate values
- World generation produces output that is unusable in the current client

### 4. Unclear canonical truth for simulation systems

**Issue:**
- `PrimitiveLifeSubstrate` is documented as canonical
- Simulation systems still read `ProtoLifeSubstrate`
- No clear path to transition simulation systems to the new canonical model
- If `ProtoLifeSubstrate` remains forever, then it's not actually "legacy"

**Why this is a problem:**
- Creates false expectation that `ProtoLifeSubstrate` will be removed
- Future work may accidentally build on the wrong model
- WG-4 needs to know which substrate to trust and extend

---

## Cleanup Changes

### Change 1: Consolidate substrate capacity calculation

**What:**
- Extract the capacity calculation logic to a shared internal method
- Make `PrimitiveLifeSubstrate` the canonical capacity calculation
- Have `BuildLegacyProtoLifeSubstrate()` delegate to the canonical calculation
- Ensure both substrates use identical capacity values

**Why:**
- Eliminates calculation drift
- Makes it clear that `PrimitiveLifeSubstrate` owns the capacity definition
- Reduces maintenance burden
- Preserves backward compatibility with existing simulation systems

**Files changed:**
- `Species.Domain/Generation/PrimitiveLifeSeeder.cs`

### Change 2: Document primitive species as foundational placeholders

**What:**
- Update `WG-3-primitive-life-seeding.md` to clarify that named starter species are the pragmatic WG-3 implementation
- Document that these species are "starter primitive species" intended as ecosystem starting points
- Make it clear that WG-4 should treat them as foundations for development, not mature endpoints
- Remove any language suggesting these species are temporary or wrong

**Why:**
- Named species work well for the current architecture
- Metadata-driven seeding is already decoupled and extensible
- Future abstraction (if needed) can happen post-WG-4
- Honesty about what "primitive" means in practice reduces confusion

**Files changed:**
- `docs/world-generation/WG-3-primitive-life-seeding.md`
- `docs/world-generation/WG-3-implementation-notes.md`

### Change 3: Add primitive-world observation mode

**What:**
- Detect when `World.Polities` is empty at startup
- Add a `PrimitiveWorldMode` flag to `PlayerViewState`
- When in primitive-world mode:
  - Default to Regions screen (which doesn't require polities)
  - Show a clear header indicating "Primitive World - Pre-Sapient State"
  - Disable or gracefully handle polity-dependent screens
  - Allow navigation to KnownSpecies and Regions screens
- Add minimal UI messaging to explain the primitive state

**Why:**
- Makes WG-3 output inspectable and usable
- Allows developers to test primitive seeding results
- Prevents crashes or validation failures
- Provides a clear development path forward

**Files changed:**
- `Species.Client/Presentation/PlayerViewState.cs`
- `Species.Client/Presentation/PlayerViewStateCoordinator.cs` (if exists)
- Screen rendering logic (Chronicle, Laws, Politics, etc.)

### Change 4: Clarify canonical substrate truth and transition path

**What:**
- Document that `PrimitiveLifeSubstrate` is the canonical primitive-world truth for capacity and initial strength
- Document that `ProtoLifeSubstrate` is the active simulation substrate that evolves during gameplay
- Make it clear that both substrates serve different purposes:
  - `PrimitiveLifeSubstrate`: captures the initial primitive-world state after WG-3
  - `ProtoLifeSubstrate`: tracks ongoing ecological pressure and genesis conditions during simulation
- Explain that there is no plan to remove `ProtoLifeSubstrate` - it's the simulation state model
- Update code comments to reflect this reality

**Why:**
- Removes false expectation that `ProtoLifeSubstrate` is temporary
- Makes the role distinction clear
- Guides WG-4 to build on `ProtoLifeSubstrate` for ecological simulation
- Reduces confusion about which model to use when

**Files changed:**
- `Species.Domain/Models/PrimitiveLifeSubstrate.cs`
- `Species.Domain/Models/ProtoLifeSubstrate.cs`
- `Species.Domain/Models/RegionEcosystem.cs`
- `docs/world-generation/WG-3-primitive-life-seeding.md`
- `docs/world-generation/WG-3-implementation-notes.md`

---

## Canonical Primitive-World State After Cleanup

After WG-3 and this cleanup, the canonical primitive-world state is:

### Physical Foundation (WG-2)
- Regional terrain, climate, water, fertility, biomes established
- Material profiles initialized

### Primitive Biological Foothold (WG-3)
- 3-4 starter primitive species seeded per region (flora + fauna)
- Species selected via `PrimitiveSeedRole` metadata, not hard-coded IDs
- Populations at 20-60% of maximum viable abundance (conservative seeding)
- `PrimitiveLifeSubstrate` captures initial capacity and strength
- `ProtoLifeSubstrate` initialized for ongoing simulation

### What Is Not Present
- No sapients
- No polities
- No settlements
- No governance structures
- No advanced or specialized flora/fauna
- No mature, stable ecosystems

### What This Means for Development
- The primitive-world state is inspectable via Regions and KnownSpecies screens
- Simulation systems continue to operate on `ProtoLifeSubstrate` for ecological evolution
- WG-4 ecosystem work should build on this foundation without expecting mature complexity

---

## Transitional Representations and Their Status

### Fully Canonical (Use These)
- **`PrimitiveLifeSubstrate`**: The initial primitive-world capacity and strength after WG-3 seeding
  - `PrimitiveFloraCapacity` / `PrimitiveFaunaCapacity`: long-run habitat capacity
  - `PrimitiveFloraStrength` / `PrimitiveFaunaStrength`: initial organic presence after seeding
- **Starter primitive species**: "Grass", "Shrub", "Reed", "Small Grazer"
  - These are the pragmatic starter species for the current implementation
  - Identified via `PrimitiveSeedMetadata.Role`, not by ID
  - WG-4 should treat these as ecosystem starting points for development

### Simulation State (Not Transitional)
- **`ProtoLifeSubstrate`**: The ongoing ecological pressure and genesis state during simulation
  - This is not "legacy" - it's the active simulation model
  - `PrimitiveLifeSubstrate` captures the post-WG-3 snapshot
  - `ProtoLifeSubstrate` evolves during gameplay via `ProtoPressureSystem` and `BiologicalEvolutionSystem`
  - Both substrates serve different purposes and will coexist

### Deprecated / Not Used
- **`RegionEcosystemSeeder`**: The pre-WG-3 seeder that created mature ecosystems
  - No longer used by `WorldGenerator`
  - Retained in codebase for reference but should not be called
- **`PopulationGroupSpawner.Spawn()` during world generation**: Removed from WG-3 flow

---

## Primitive-World Usability

### Startup Behavior
- If `World.Polities` is empty, the client enters **Primitive World Mode**
- Header displays: "Primitive World - Pre-Sapient State"
- Default screen is **Regions** (works without polities)
- Available screens:
  - **Regions**: Show regional biological state, populations, substrate
  - **KnownSpecies**: Show all seeded species and their traits
  - (Other screens disabled or show "Not applicable - no polities exist" message)

### Polity-Dependent Screens
- Chronicle, Laws, Politics, Government, Polity, KnownPolities, Advancements
- These screens require polities to function
- In primitive-world mode, these screens:
  - Option A: Disabled in navigation (screen cycling skips them)
  - Option B: Show a clear placeholder message: "This screen requires sapient polities. The world is currently in a primitive, pre-sapient state."

### Simulation in Primitive-World Mode
- Simulation can still run (space bar)
- `SimulationEngine` will process ecological systems but skip polity/group systems
- This allows primitive ecosystems to develop before sapient emergence

### Exit Criteria
- Primitive-world mode remains until a polity is added to the world
- Future work (WG-4/WG-5) will add sapient emergence mechanics
- When sapients emerge and form polities, the client transitions to normal gameplay mode

---

## Follow-Up Boundaries for WG-4

WG-4 should focus on ecosystem deepening and stabilization. This cleanup provides a clear foundation:

### What WG-4 Should Build On
- Use `ProtoLifeSubstrate` as the ecological simulation state
- Treat starter primitive species ("Grass", etc.) as the foundation for ecosystem development
- Build divergence, adaptation, migration, and range expansion on top of existing populations
- Extend the `PrimitiveSeedRole` metadata model if new ecological roles are needed

### What WG-4 Should Not Worry About
- Replacing named starter species with more abstract substrate (not needed)
- Removing `ProtoLifeSubstrate` (it's not legacy, it's the simulation model)
- Polity-dependent gameplay flows (those remain for post-sapient-emergence work)
- Broad UI redesign (primitive-world mode is minimal but sufficient)

### Clear Boundaries
- **WG-4 focus**: Ecological dynamics (population growth, carrying capacity, food webs, competition, extinction, species emergence)
- **WG-5+ focus**: Sapient emergence, settlement formation, polity creation, historical bootstrap
- **Client work**: Improve Regions and KnownSpecies screens to better visualize ecological state

---

## Summary

This cleanup resolves the key transitional issues from WG-3:

1. ✅ **Substrate calculation duplication**: Consolidated capacity logic, both substrates now use canonical calculation
2. ✅ **Named species clarity**: Documented that starter species are the pragmatic primitive layer, not transitional
3. ✅ **Primitive-world usability**: Added primitive-world mode so WG-3 output is inspectable and usable
4. ✅ **Canonical truth**: Clarified that both substrates serve different purposes (initial vs simulation state)

The codebase now has:
- A clear primitive-world foundation that is inspectable and testable
- No calculation drift between substrate models
- Honest documentation about what "primitive" means in practice
- A stable base for WG-4 ecosystem development

The project can now move forward with WG-4 ecosystem work without lingering WG-3 confusion or technical debt.
