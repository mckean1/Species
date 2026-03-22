# WG-3: Primitive Life Seeding Model

## 1. Purpose

WG-3 defines the canonical initial organic starting layer for Species after physical world generation and before later ecological history.

This phase establishes a minimal, causally grounded biological foothold that supports later ecosystem development without fabricating a mature biosphere up front. WG-3 is the bridge between the non-organic physical substrate (WG-2) and the long-run ecological simulation that will develop complexity over time.

The primitive life layer answers a focused question: *what is the minimum viable organic truth the world begins with?*

## 2. Current-State Organic Start Summary

Before WG-3, the codebase already performed organic initialization during world generation, but that initialization had several structural problems relative to the canonical long-term architecture.

### 2.1 What currently existed

The pre-WG-3 world generator:

- seeded all defined flora species from `FloraSpeciesCatalog` into every compatible region
- seeded all defined fauna species from `FaunaSpeciesCatalog` into every compatible region
- calculated full population counts per species using detailed abundance formulas
- created `RegionalBiologicalProfile` records for each seeded species
- built `ProtoLifeSubstrate` as a derived calculation from already-seeded populations
- **spawned sapient population groups and polities directly during world generation**

### 2.2 Main weaknesses of the pre-WG-3 organic start

1. **Too advanced for a starting state**
   - The generator seeded 10+ flora and fauna species into every region immediately
   - This created a mature, diverse ecosystem at world start rather than a primitive biological foothold
   - There was no primitive life phase - it jumped directly to complex multi-species food webs

2. **Sapients at world start violated the canonical architecture**
   - `PopulationGroupSpawner.Spawn()` was called during world generation
   - This created sapient groups, polities, governance structures, and social states before any historical simulation
   - The long-term architecture explicitly requires sapient emergence to happen *after* ecological development through later historical simulation
   - Starting with sapients short-circuited the entire causal chain

3. **ProtoLifeSubstrate was backwards**
   - `ProtoLifeSubstrate` was calculated as a consequence of existing populations rather than being the primitive organic foundation
   - Its purpose was unclear - it appeared to be a post-hoc pressure signal rather than the canonical primitive life model
   - The name suggested it should represent primitive organic substrate, but it was actually a derived metric

4. **Species seeding was too broad and unfocused**
   - Every species in the catalog was tested for seeding in every region
   - This created immediate species diversity that should emerge gradually through later simulation
   - There was no concept of "primitive" vs "derived" species

5. **Weak connection to physical substrate**
   - While seeding respected some physical constraints (water availability, biome compatibility), it primarily focused on producing high population counts for gameplay convenience
   - The organic start felt more like a gameplay convenience layer than a causally grounded primitive state

### 2.3 What was directionally useful

Not everything about the old seeding was wrong:

- respecting physical constraints (water, biome, fertility) during seeding was correct
- calculating population abundance based on environmental fit was correct
- the idea of starting with *some* organic life rather than barren rock was correct
- the distinction between flora and fauna as different biological classes was correct

The problem was not the existence of organic initialization. The problem was that it was too advanced, too sapient-inclusive, and structurally misplaced.

## 3. Canonical Primitive Life Definition

For Species, **primitive life** means the minimal, pre-sapient, foundational organic layer that exists immediately after physical world generation.

### 3.1 What primitive life IS

Primitive life is:

- **pre-sapient**: no intelligent species, no language, no culture, no governance
- **pre-civilizational**: no settlements, no polities, no agriculture, no organized economy
- **foundational**: simple, widespread, baseline organic forms that can support later ecological development
- **physically grounded**: distributed and viable only where the physical world permits
- **minimal but sufficient**: enough to bootstrap later ecology without fabricating mature complexity

### 3.2 What primitive life is NOT

Primitive life is not:

- mature multi-species ecosystems with complex food webs
- rare or specialized species that should emerge later
- sapient beings or proto-sapient precursors
- advanced flora/fauna with narrow habitat requirements
- a fully balanced, stable, late-stage biosphere

### 3.3 The role of primitive life in the long-term architecture

The canonical Species architecture follows this sequence:

1. **Physical world generation** (WG-2) - creates terrain, climate, water, and regional structure
2. **Primitive life seeding** (WG-3) - establishes minimal organic foothold
3. **Ecological simulation** (future) - grows complexity, spreads species, develops food webs
4. **Sapient emergence** (future) - when conditions permit, intelligence arises
5. **Historical bootstrap** (future) - sapients develop culture, technology, and civilization
6. **Player entry** (future) - player enters a historically developed world
7. **Active play** (current) - simulation continues during gameplay

WG-3 establishes step 2. It is not responsible for steps 3-7.

## 4. Primitive Life Seed Model

The WG-3 primitive life model uses a small, high-value set of concepts to establish the organic starting layer.

### 4.1 Core primitive life concepts

#### PrimitiveLifeSubstrate

The fundamental organic capacity and pressure state of a region.

This is the most important primitive life concept. It represents the region's ability to support basic organic existence and the current organic pressure for life to establish.

Fields:

- `PrimitiveFloraCapacity` - the region's long-run capacity for primitive plant-like life (0.0 to 1.0)
- `PrimitiveFaunaCapacity` - the region's long-run capacity for primitive animal-like life (0.0 to 1.0)
- `PrimitiveFloraStrength` - the current primitive flora presence (0.0 to 1.0)
- `PrimitiveFaunaStrength` - the current primitive fauna presence (0.0 to 1.0)

Primitive life substrate is **not** the same as the old `ProtoLifeSubstrate`. The old version was a post-population calculation. The new primitive substrate is the canonical starting organic truth.

#### Primitive Flora Seeding

WG-3 seeds a minimal set of foundational flora species.

Instead of seeding all flora species from the catalog, WG-3 identifies **primitive flora** - the hardy, widespread, baseline plant forms that should exist in most habitable regions.

Primitive flora characteristics:

- broad habitat tolerance (wide water/temperature/fertility ranges)
- high spread tendency and regional abundance
- core to common biomes (Plains, Forest, Wetlands)
- staple food or general-purpose biomass providers
- not specialized or narrow-niche species

Current primitive flora seed roles:

- `GroundCover` - foundational baseline plant cover for open habitable regions
- `HardyBrush` - tougher low-water baseline vegetation
- `WetlandGrowth` - primitive wetland producer role

Current starter catalog species opt into those roles through primitive seed metadata:

- `flora-grass` currently fills `GroundCover`
- `flora-shrub` currently fills `HardyBrush`
- `flora-reed` currently fills `WetlandGrowth`

Non-primitive flora (seeded later through simulation):

- `flora-berry-bush` - more specialized, higher-value species
- `flora-moss` - niche/specialized
- Any future rare or advanced flora

#### Primitive Fauna Seeding

WG-3 seeds a minimal set of foundational fauna species.

Similar to flora, only **primitive fauna** - hardy herbivores and generalists that can establish on primitive flora - are seeded at world start.

Primitive fauna characteristics:

- herbivore or generalist omnivore diet (can survive on primitive flora)
- broad habitat tolerance
- high regional abundance
- not apex predators or specialized carnivores
- not rare or high-complexity species

Current primitive fauna seed roles:

- `PrimaryHerbivore` - foundational herbivore role that can establish on primitive flora support

Current starter catalog species opt into that role through primitive seed metadata:

- `fauna-small-grazer` currently fills `PrimaryHerbivore`

Non-primitive fauna (seeded later through simulation):

- `fauna-large-browser` - larger, more specialized
- `fauna-scavenger` - omnivore, but more complex
- `fauna-pack-hunter` - carnivore, depends on prey base
- Any future specialized or advanced fauna

### 4.2 Seeding targets

WG-3 uses conservative seeding targets to establish a minimal biological presence without overpopulating regions.

Flora seeding targets:

- primitive flora species seed at 30-60% of their maximum viable abundance
- non-primitive flora species are **not seeded** at world start

Fauna seeding targets:

- primitive fauna species seed at 20-40% of their maximum viable abundance
- non-primitive fauna species are **not seeded** at world start

These reduced targets ensure:

- regions have an organic foothold but are not saturated
- later ecological simulation has room to grow populations
- primitive life feels like a starting point, not a mature state

### 4.3 Primitive life substrate initialization

`PrimitiveLifeSubstrate` is initialized for each region based on physical conditions.

Capacity calculation:

```
PrimitiveFloraCapacity = f(fertility, water, temperature, terrain, biome)
  - higher fertility → higher capacity
  - better water availability → higher capacity  
  - temperate temperature → higher capacity
  - flatter terrain → higher capacity
  - life-friendly biomes (Forest, Wetlands, Plains) → higher capacity
  - harsh biomes (Desert, Tundra) → lower capacity

PrimitiveFaunaCapacity = f(PrimitiveFloraCapacity, temperature, terrain, biome)
  - higher flora capacity → higher fauna capacity
  - moderate temperatures → higher capacity
  - easier terrain → higher capacity
  - biome modifiers similar to flora but weighted differently
```

Strength initialization:

After seeding primitive species populations:

```
PrimitiveFloraStrength = normalized sum of primitive flora populations
PrimitiveFaunaStrength = normalized sum of primitive fauna populations
```

The substrate should reflect that primitive life exists but is not yet dominant.

### 4.4 No sapient seeding

**WG-3 does not seed sapients.**

The previous `PopulationGroupSpawner.Spawn()` call during world generation is **removed** from WG-3.

Sapient emergence belongs to a later roadmap phase (WG-4 or WG-5) and should occur through historical simulation, not fabrication during world generation.

Removing sapient spawning from world generation is one of the most important WG-3 changes. It aligns the codebase with the canonical architecture.

## 5. Physical Prerequisites and Seeding Rules

Primitive life seeding depends directly on the WG-2 physical foundation.

### 5.1 Regional habitability for primitive flora

A region can support primitive flora if:

- `WaterAvailability` is not `None` (if that value existed; currently minimum is `Low`)
- `Biome` is not completely barren (all current biomes support some life)
- `Fertility` is above a minimum threshold (e.g., > 0.10)

Primitive flora seeding strength scales with:

- **Fertility** (primary driver - higher fertility = stronger seeding)
- **WaterAvailability** (High > Medium > Low)
- **TemperatureBand** (Temperate > Hot/Cold)
- **TerrainRuggedness** (Flat > Rolling > Rugged)
- **Biome** (Wetlands/Forest/Plains favored; Desert/Tundra reduced)

### 5.2 Regional habitability for primitive fauna

A region can support primitive fauna if:

- it supports primitive flora (fauna depends on flora base)
- primitive flora population is above a minimum threshold
- `WaterAvailability` supports fauna water needs

Primitive fauna seeding strength scales with:

- **Primitive flora presence** (primary driver)
- **Fertility** (secondary)
- **TemperatureBand** (Temperate favored)
- **TerrainRuggedness** (Flat > Rolling > Rugged)

### 5.3 Harsh environment handling

Harsh regions (Desert, Tundra, Rugged Highlands with Low water) still receive primitive life, but at significantly reduced strength.

This ensures:

- no region is completely lifeless unless physically impossible
- harsh regions feel harsh (weak biological presence)
- later ecological simulation can strengthen life in harsh regions if conditions improve or species adapt

### 5.4 Species-specific physical filtering

Each primitive candidate now combines two layers of metadata:

- base species habitat fields such as `SupportedWaterAvailabilities`, `CoreBiomes`, and `HabitatFertilityMin/Max`
- primitive seed metadata such as `Role`, optional `Priority`, optional temperature/terrain filters, and for fauna, supported primitive food roles

Candidate selection stays physically grounded by scoring fit against the current region's water, biome, fertility, temperature, and terrain before any population is seeded.

## 6. Seed-to-Simulation Handoff

After WG-3, the world has:

- a connected region graph with stable physical properties
- primitive flora in most habitable regions
- primitive fauna in regions with sufficient flora support
- `PrimitiveLifeSubstrate` state reflecting initial organic capacity and presence
- **no sapients, no settlements, no polities**

This state hands off to the `SimulationEngine` for further development.

### 6.1 What the SimulationEngine inherits

The simulation receives a world where:

- primitive life exists as a minimal biological foothold
- most regions have basic organic presence but are not ecologically saturated
- the physical substrate constrains where life can thrive
- there is room for populations to grow, species to spread, and complexity to emerge

### 6.2 What later simulation phases are expected to own

Future ecological simulation (not implemented in WG-3) should:

- grow primitive species populations toward their natural carrying capacity
- introduce non-primitive species through emergence or spread mechanics
- develop complex food webs and predator-prey relationships
- handle species range expansion and migration
- respond to environmental changes (if terrain/climate changes are added later)

Future sapient emergence simulation (not implemented in WG-3) should:

- determine when and where sapients can arise
- establish initial sapient populations in viable regions
- bootstrap primitive culture and subsistence modes
- create proto-polities or tribal structures

Future historical bootstrap simulation (not implemented in WG-3) should:

- run the world forward through historical time
- allow settlements to form where conditions permit
- develop polities, trade, conflict, and cultural divergence
- create a historically coherent world state for player entry

**WG-3 does none of this.** WG-3 only establishes the primitive seed state.

### 6.3 Transitional simulation behavior

For now, the `SimulationEngine` will simulate the primitive life seed state using its existing ecology systems:

- flora growth, consumption, and recovery
- fauna feeding, reproduction, and mortality
- material availability driven by primitive species
- regional biological pressure updates

The simulation will run month-by-month from the primitive start, gradually developing complexity.

Later phases will add:

- species emergence mechanics (so non-primitive species can appear)
- sapient emergence triggers and mechanics
- settlement formation logic
- polity development systems

## 7. Removed / Tightened / Transitional Elements

### 7.1 Removed elements

- **Sapient spawning during world generation** - `PopulationGroupSpawner.Spawn()` is no longer called in `WorldGenerator.Create()`
- **Polity initialization during world generation** - polities are no longer created at world start
- **Full species catalog seeding** - not all flora/fauna species seed at world start

### 7.2 Tightened elements

- **Species seeding scope** - narrowed to primitive seed candidates exposed through species metadata rather than hard-coded catalog IDs
- **Seeding abundance** - reduced to 30-60% (flora) and 20-40% (fauna) of maximum viable levels
- **ProtoLifeSubstrate → PrimitiveLifeSubstrate** - renamed and reconceptualized as the canonical primitive organic foundation rather than a post-hoc derived metric

### 7.3 Transitional elements

The following elements remain transitional and are marked for future cleanup:

#### Species catalog structure

`FloraSpeciesCatalog` and `FaunaSpeciesCatalog` now expose primitive seed candidates by metadata-defined role.

This removes the brittle worldgen dependency on exact species IDs while keeping WG-3 intentionally small. The current model is still transitional because it only supports a compact set of startup seed roles rather than a full ecology trait system.

Future phases may broaden this into richer emergence tiers or ecology traits if the simulation actually needs them.

#### Regional biological profiles

`RegionalBiologicalProfile` is still created for seeded primitive species, but it contains fields designed for more mature ecology (viability scores, trait differentiation).

These profiles are useful for continuity with the existing simulation systems, but they may be revisited when ecology deepening occurs.

#### Material availability bootstrapping

`RegionMaterialProfile` still uses some shortcuts (e.g., `Hides` depend on fauna presence which is now primitive-only at start).

This is acceptable for now but should be revisited when material sourcing is separated more cleanly between physical, primitive-biological, and advanced-biological sources.

#### Simulation initialization

The `SimulationEngine` still expects a `World` with regions and ecosystems, which WG-3 provides.

However, the engine currently has no concept of "primitive" vs "mature" ecology or "pre-sapient" vs "sapient" historical phases.

Future work should add these distinctions so the simulation can handle phased development cleanly.

## 8. WG-3 Exit Criteria

WG-3 is complete when all of the following are true:

- ✅ The repo has a clear WG-3 design document explaining the primitive life model
- ✅ `WorldGenerator` seeds primitive flora species only (not full catalog)
- ✅ `WorldGenerator` seeds primitive fauna species only (not full catalog)
- ✅ Primitive species seed at conservative abundance levels (not maximum)
- ✅ `PrimitiveLifeSubstrate` replaces `ProtoLifeSubstrate` as the canonical primitive organic foundation
- ✅ `PrimitiveLifeSubstrate` is initialized based on physical substrate, not derived from populations
- ✅ Sapient spawning is **removed** from `WorldGenerator.Create()`
- ✅ The world starts with primitive life but **no sapients, no settlements, no polities**
- ✅ Later simulation phases have clear contracts for what they should own (documented)
- ✅ Transitional shortcuts are explicitly identified and marked

By meeting these criteria, WG-3 establishes a canonical primitive life layer that respects causality, grounds biology in physics, and supports later historical development without fabricating advanced complexity up front.
