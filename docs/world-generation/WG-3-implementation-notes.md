# WG-3 Implementation Notes

## Summary of Changes

WG-3 has been implemented to establish the canonical primitive life seeding model. The world now starts with primitive organic life but **no sapients, no settlements, and no polities**.

## Breaking Changes from Pre-WG-3

### World Generation

**Before WG-3:**
- `WorldGenerator.Create()` seeded all flora and fauna species from catalogs
- `WorldGenerator.Create()` called `PopulationGroupSpawner.Spawn()` to create sapients and polities
- World started with 10+ species per region and multiple sapient polities

**After WG-3:**
- `WorldGenerator.Create()` seeds only primitive species using `PrimitiveLifeSeeder`
- Sapient spawning is **removed** from world generation
- World starts with 3-4 primitive species total and **no sapients**

### Primitive Species

**Primitive Flora** (seeded at world start):
- `flora-grass` - foundational grassland flora
- `flora-shrub` - hardy drought-tolerant vegetation
- `flora-reed` - wetland foundational flora

**Primitive Fauna** (seeded at world start):
- `fauna-small-grazer` - foundational herbivore

**Non-Primitive Species** (not seeded at world start):
- `flora-berry-bush`, `flora-moss` - will need emergence mechanics in future
- `fauna-large-browser`, `fauna-scavenger`, `fauna-pack-hunter` - will need emergence mechanics in future

### Seeding Abundance

Primitive species seed at **30-60% (flora)** and **20-40% (fauna)** of their maximum viable abundance, establishing a biological foothold rather than saturation.

### Ecosystem Model Changes

**New Model:**
- `PrimitiveLifeSubstrate` - canonical primitive organic foundation
  - `PrimitiveFloraCapacity` - region's long-run flora capacity
  - `PrimitiveFaunaCapacity` - region's long-run fauna capacity
  - `PrimitiveFloraStrength` - current primitive flora presence
  - `PrimitiveFaunaStrength` - current primitive fauna presence

**Legacy Model (Transitional):**
- `ProtoLifeSubstrate` - kept for backward compatibility with simulation systems
- Will be phased out in future work

### Client Impact

**Expected Behavior:**
- The client will start with a world containing primitive life but **no playable polity**
- `world.Polities` will be empty
- `world.PopulationGroups` will be empty
- `world.FocalPolityId` will be an empty string
- The UI screens that depend on polities (Laws, Politics, etc.) will have nothing to display

**This is intentional.** WG-3 establishes the primitive organic start state. Player-focused gameplay currently depends on sapients existing, which violates the canonical architecture.

### Required Follow-Up Work

To restore playable functionality, future roadmap phases must implement:

1. **WG-4: Ecological Development Phase** (not yet implemented)
   - Species emergence mechanics for non-primitive flora/fauna
   - Population growth and ecosystem maturation
   - Regional ecological divergence
   - Migration and range expansion

2. **WG-5: Sapient Emergence Phase** (not yet implemented)
   - Sapient emergence triggers and conditions
   - Initial sapient population seeding when conditions permit
   - Proto-cultural and proto-polity formation
   - Subsistence mode initialization

3. **WG-6: Historical Bootstrap Phase** (not yet implemented)
   - Run the world forward through historical simulation time
   - Develop settlements, polities, and cultural complexity
   - Create a historically coherent world state
   - Establish the focal polity for player entry

Until these phases are implemented, the client will start with a world containing primitive life but no playable content.

## Temporary Workaround (Development Only)

For development and testing purposes, if you need a playable world, you can:

1. **Option A:** Manually call `PopulationGroupSpawner.Spawn()` after world generation in `Program.cs`
   ```csharp
   var world = WorldGenerator.Create(floraCatalog, faunaCatalog);
   var spawnResult = PopulationGroupSpawner.Spawn(world, 3, new Random(world.Seed));
   world = new World(
       world.Seed, 
       world.CurrentYear, 
       world.CurrentMonth, 
       world.Regions, 
       spawnResult.Groups, 
       world.Chronicle, 
       spawnResult.Polities, 
       spawnResult.Polities.FirstOrDefault()?.Id ?? string.Empty);
   ```

2. **Option B:** Revert `WorldGenerator.Create()` to call `RegionEcosystemSeeder` and `PopulationGroupSpawner` (pre-WG-3 behavior)

**Do not commit these workarounds to the repository.** They exist only for local development convenience while WG-4/5/6 are being implemented.

## Architecture Alignment

WG-3 brings the codebase closer to the canonical Species architecture:

1. ✅ Physical world generation (WG-2)
2. ✅ Primitive life seeding (WG-3)
3. ⏳ Ecological simulation (future - WG-4)
4. ⏳ Sapient emergence (future - WG-5)
5. ⏳ Historical bootstrap (future - WG-6)
6. ⏳ Player entry (future - modified client initialization)
7. ✅ Active play (existing simulation systems)

The world generator now correctly establishes steps 1-2 without fabricating steps 4-6.

## Files Changed

### Added Files
- `docs/world-generation/WG-3-primitive-life-seeding.md` - WG-3 design document
- `Species.Domain/Models/PrimitiveLifeSubstrate.cs` - canonical primitive organic model
- `Species.Domain/Generation/PrimitiveLifeSeeder.cs` - primitive life seeding implementation
- `docs/world-generation/WG-3-implementation-notes.md` - this file

### Modified Files
- `Species.Domain/Models/RegionEcosystem.cs` - added `PrimitiveLifeSubstrate` property
- `Species.Domain/Generation/WorldGenerator.cs` - replaced `RegionEcosystemSeeder` with `PrimitiveLifeSeeder`, removed sapient spawning
- `Species.Domain/Generation/RegionEcosystemSeeder.cs` - marked as transitional/deprecated

### Deprecated but Retained Files
- `Species.Domain/Generation/RegionEcosystemSeeder.cs` - kept for reference and potential future non-world-generation use
- `Species.Domain/Models/ProtoLifeSubstrate.cs` - kept for backward compatibility with simulation systems

## Testing Notes

Build status: ✅ Successful

The codebase compiles without errors. Validators handle empty polity/group collections gracefully.

Runtime behavior: The client will start successfully but will have no playable polity since sapients are not seeded at world start.

## Next Steps

The next roadmap phase should be one of:

1. **WG-4: Ecological Development** - implement species emergence, ecosystem maturation
2. **WG-5: Sapient Emergence** - implement sapient emergence conditions and initial seeding
3. **Client Adaptation** - add a "primitive life observation mode" to the client for viewing worlds without playable polities

These are outside the scope of WG-3.
