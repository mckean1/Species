# WG-3: Primitive Life Seeding Model - Summary

## Implementation Complete

WG-3 has been successfully implemented. The Species codebase now has a canonical primitive life seeding layer that establishes minimal organic starting state after physical world generation.

---

## What Changed

### Core Design

**WG-3 Design Document:** `docs/world-generation/WG-3-primitive-life-seeding.md`

Defines:
- What primitive life means for Species
- Why the pre-WG-3 organic start was too advanced
- The canonical primitive life seed model
- Physical prerequisites for primitive life
- Handoff contracts for later simulation phases
- Exit criteria for WG-3 completion

### Primitive Life Model

**New Canonical Model:** `Species.Domain/Models/PrimitiveLifeSubstrate.cs`

Represents the fundamental organic capacity and presence in each region:
- `PrimitiveFloraCapacity` - long-run capacity for primitive plant-like life
- `PrimitiveFaunaCapacity` - long-run capacity for primitive animal-like life
- `PrimitiveFloraStrength` - current primitive flora presence
- `PrimitiveFaunaStrength` - current primitive fauna presence

This is the true primitive organic foundation, calculated from WG-2 physical substrate.

**Legacy Model:** `Species.Domain/Models/ProtoLifeSubstrate.cs`

Retained for backward compatibility with existing simulation systems. Will be phased out in future work.

### Primitive Life Seeding

**New Seeder:** `Species.Domain/Generation/PrimitiveLifeSeeder.cs`

Seeds only primitive seed candidates at world start:

- worldgen now queries catalogs for species that opt into primitive seeding metadata
- selection is resolved by primitive seed role, environmental fit, and priority ordering
- worldgen no longer depends on exact species IDs embedded in the seeder

**Primitive Flora Roles:**
- `GroundCover`
- `HardyBrush`
- `WetlandGrowth`

**Primitive Fauna Roles:**
- `PrimaryHerbivore`

Current starter catalog entries still map to the same baseline species, but that mapping now lives in species metadata instead of hard-coded seeder literals.

**Non-Primitive Species:**
- NOT seeded at world start
- Will require emergence mechanics in future phases

**Deprecated Seeder:** `Species.Domain/Generation/RegionEcosystemSeeder.cs`

Marked as transitional. This was the pre-WG-3 seeder that created mature multi-species ecosystems. It is no longer used for world generation.

### World Generation Changes

**Modified:** `Species.Domain/Generation/WorldGenerator.cs`

Key changes:
1. Replaced `RegionEcosystemSeeder.Seed()` with `PrimitiveLifeSeeder.Seed()`
2. **Removed `PopulationGroupSpawner.Spawn()` call** - no sapients at world start
3. World now returns with primitive life but no polities/groups

This aligns world generation with the canonical architecture.

### Ecosystem Model Updates

**Modified:** `Species.Domain/Models/RegionEcosystem.cs`

Added:
- `PrimitiveLifeSubstrate` property (new canonical model)

Retained:
- `ProtoLifeSubstrate` property (legacy, for compatibility)

---

## Architectural Impact

### Before WG-3

```
WorldGenerator.Create()
  ├─ Generate physical world (WG-2)
  ├─ Seed ALL flora/fauna species
  └─ Spawn sapient polities ❌ (violates architecture)
       └─ Player can play immediately
```

### After WG-3

```
WorldGenerator.Create()
  ├─ Generate physical world (WG-2) ✅
  └─ Seed PRIMITIVE life only (WG-3) ✅
       └─ World has minimal organic foothold
            └─ NO sapients/polities ✅
                 └─ Future phases must add ecological development and sapient emergence
```

This is the **correct** causal sequence.

---

## Files Added

1. `docs/world-generation/WG-3-primitive-life-seeding.md` - design document
2. `docs/world-generation/WG-3-implementation-notes.md` - implementation notes and breaking changes
3. `docs/world-generation/WG-3-summary.md` - this file
4. `Species.Domain/Models/PrimitiveLifeSubstrate.cs` - primitive life model
5. `Species.Domain/Generation/PrimitiveLifeSeeder.cs` - primitive life seeding implementation
6. `Species.Domain/Models/PrimitiveSeedMetadata.cs` - primitive seeding metadata block
7. `Species.Domain/Enums/PrimitiveSeedRole.cs` - primitive seeding role definitions

## Files Modified

1. `Species.Domain/Models/RegionEcosystem.cs` - added `PrimitiveLifeSubstrate`
2. `Species.Domain/Generation/WorldGenerator.cs` - use primitive seeder, remove sapient spawning
3. `Species.Domain/Generation/RegionEcosystemSeeder.cs` - marked as transitional
4. `Species.Domain/Models/FloraSpeciesDefinition.cs` - added primitive seed metadata
5. `Species.Domain/Models/FaunaSpeciesDefinition.cs` - added primitive seed metadata
6. `Species.Domain/Catalogs/FloraSpeciesCatalog.cs` - added primitive candidate queries and starter metadata
7. `Species.Domain/Catalogs/FaunaSpeciesCatalog.cs` - added primitive candidate queries and starter metadata

---

## WG-3 Exit Criteria Status

✅ The repo has a clear WG-3 design document  
✅ `WorldGenerator` seeds primitive flora species only  
✅ `WorldGenerator` seeds primitive fauna species only  
✅ Primitive species seed at conservative abundance levels  
✅ `PrimitiveLifeSubstrate` replaces `ProtoLifeSubstrate` as canonical foundation  
✅ `PrimitiveLifeSubstrate` is initialized from physical substrate  
✅ Sapient spawning is removed from `WorldGenerator.Create()`  
✅ World starts with primitive life but no sapients/settlements/polities  
✅ Later simulation phases have clear contracts documented  
✅ Transitional shortcuts are explicitly identified  

**All WG-3 exit criteria met.**

---

## Key Decisions Made

### 1. Which Species Are Primitive?

**Primitive Flora:**
- Grass, Shrub, Reed
- Broad habitat tolerance, high abundance, staple food

**Primitive Fauna:**
- Small Grazer only
- Herbivore, can survive on primitive flora alone

**Non-Primitive:**
- Berry Bush (specialized), Moss (niche)
- Large Browser (bigger/rarer), Scavenger (omnivore), Pack Hunter (carnivore)

**Rationale:** Primitive life should be hardy, widespread, baseline forms. Complex species should emerge later.

### 2. Seeding Abundance Targets

**Flora:** 30-60% of maximum viable abundance  
**Fauna:** 20-40% of maximum viable abundance

**Rationale:** Establish a foothold, not saturation. Leave room for later growth.

### 3. Primitive vs Proto Naming

`PrimitiveLifeSubstrate` chosen over renaming `ProtoLifeSubstrate` to avoid breaking existing simulation systems.

Both exist temporarily. Future work should consolidate.

### 4. Sapient Spawning Removal

Sapient spawning during world generation was the biggest architectural violation. WG-3 removes it.

**Impact:** Client currently has no playable content at world start.  
**Future Fix:** WG-5 (Sapient Emergence) and WG-6 (Historical Bootstrap) will restore playability through causal means.

---

## Primitive Life Philosophy

WG-3 establishes the **minimum viable organic truth** the world begins with:

- **Not barren:** Regions have basic life where physics permits
- **Not mature:** Ecosystems are not diverse or balanced yet
- **Not sapient:** No intelligence, no culture, no civilization
- **Causally grounded:** Life distribution follows physical substrate
- **Room to grow:** Populations below carrying capacity, species diversity low

This is the correct starting point for a simulation-first game where complexity should emerge over time.

---

## What WG-3 Does NOT Do

WG-3 is intentionally scoped to primitive life seeding only:

❌ Does not implement species emergence mechanics  
❌ Does not implement ecosystem maturation  
❌ Does not implement sapient emergence  
❌ Does not implement settlement formation  
❌ Does not implement historical bootstrap  
❌ Does not add UI for viewing primitive-life-only worlds  

These are future roadmap phases.

---

## Follow-Up Roadmap

### WG-4: Ecological Development (Future)
- Species emergence and introduction mechanics
- Ecosystem maturation and balancing
- Population growth toward carrying capacity
- Food web complexity development
- Regional ecological divergence

### WG-5: Sapient Emergence (Future)
- Conditions and triggers for sapient emergence
- Initial sapient population seeding
- Proto-cultural and proto-polity formation
- Subsistence mode initialization

### WG-6: Historical Bootstrap (Future)
- Advance world through historical time
- Settlement formation where conditions permit
- Polity development and divergence
- Trade, conflict, cultural evolution
- Create a mature world state for player entry

### Client Adaptation (Future)
- Add "primitive observation mode" for viewing pre-sapient worlds
- Gracefully handle empty polity/group state
- Support player entry after historical bootstrap completes

---

## Build Status

✅ **Build successful** - no compilation errors  
✅ **Validators pass** - handle empty polities/groups gracefully  
⚠️ **Client runtime** - starts successfully but has no playable polity  

---

## Recommended Next Steps

For the Species project roadmap:

1. **Immediate:** Document WG-4 (Ecological Development) target state
2. **Short-term:** Implement species emergence mechanics
3. **Medium-term:** Implement sapient emergence (WG-5)
4. **Long-term:** Implement historical bootstrap (WG-6)

OR

1. **Temporary workaround:** Re-enable sapient spawning in client `Program.cs` for local development until WG-5/6 are ready

---

## Conclusion

WG-3 successfully establishes the canonical primitive life seeding layer for Species.

The world now correctly begins with a minimal organic foothold grounded in the physical substrate, without fabricating sapient civilizations or mature ecosystems.

This is a foundational step toward the long-term architecture where complexity emerges through simulation rather than being invented during world generation.

**WG-3 is complete.**
