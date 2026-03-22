# WG-2: Physical World Foundation Rework

## 1. Purpose

WG-2 strengthens the non-organic substrate of the generated world so later roadmap phases inherit a more causally meaningful foundation.

This phase is about the physical world only. It sharpens the region-level environmental model, improves world generation so nearby regions form more coherent environmental patterns, and clarifies the contracts that later ecology, migration, settlement, and long-history simulation are expected to consume.

WG-2 does not implement later roadmap phases. It does not add deeper ecology, sapient emergence, settlement formation, polity formation, or a historical bootstrap. It improves the physical substrate those later phases depend on.

This document should be read as the implementation-focused physical-world follow-on to `docs/world-generation/WG-0-canonical-direction.md` and `docs/world-generation/WG-1-current-state-audit.md`.

## 2. Current-State Physical Foundation Summary

Before WG-2, the codebase already had a real physical layer in `WorldGenerator` and `Region`:

- connected regions
- region names and stable IDs
- `Biome`
- `WaterAvailability`
- `Fertility`
- `NeighborIds`
- `RegionMaterialProfile`

That foundation was directionally correct, but it had several weaknesses.

### 2.1 What was already causally meaningful

- `NeighborIds` already mattered for movement and spread.
- `WaterAvailability` already influenced ecology, survival, and discoveries.
- `Fertility` already acted as a broad productivity signal.
- `Biome` already provided a useful summary category for ecology and content matching.

### 2.2 Main weaknesses before WG-2

1. **The generator relied too heavily on per-region randomness.**
   - Water, biome, and fertility were rolled mostly region-by-region.
   - This produced variation, but not much larger-scale environmental structure.

2. **The physical model lacked explicit climate and terrain axes.**
   - `Biome` was carrying too much meaning by itself.
   - There was no explicit temperature regime and no explicit terrain ruggedness signal.

3. **`Fertility` was useful but under-explained.**
   - It was partly causal and partly a convenience score.
   - Later systems would have been tempted to compensate for missing physical structure with special-case logic.

4. **Connectivity was physically weak.**
   - The old neighbor graph could create arbitrary non-local links, which made environmental adjacency feel less grounded.

5. **Material opportunity generation was too loosely tied to the physical substrate.**
   - Some material values depended more on broad flavor assumptions than on a clean physical foundation.

## 3. Canonical Physical World Model

WG-2 keeps the physical schema compact. The goal is not decorative detail. The goal is a small set of high-value region fields where each field has downstream purpose.

### 3.1 Retained and canonical region fields

#### `NeighborIds`

Defines which regions are directly connected.

Why it exists:

- migration and spread need explicit adjacency
- regional patterns only matter if movement and contact respect them
- historical simulation needs a stable geography-like graph

#### `TemperatureBand`

A compact climate axis describing whether a region is broadly `Cold`, `Temperate`, or `Hot`.

Why it exists:

- later ecology needs a direct thermal constraint instead of inferring everything from biome labels
- primitive-life support and later species pressure need a simple climate regime
- downstream historical systems need a durable explanation for why some regions are harsher or more productive

#### `WaterAvailability`

A compact accessible-water / moisture summary using `Low`, `Medium`, and `High`.

Why it exists:

- water access is one of the strongest causal drivers for ecology, survival, carrying capacity, and settlement attractiveness
- it remains intentionally aggregate for performance and maintainability

#### `TerrainRuggedness`

A compact terrain axis describing whether a region is broadly `Flat`, `Rolling`, or `Rugged`.

Why it exists:

- terrain should matter for movement difficulty, spread friction, extraction character, and later settlement preference
- it separates terrain constraints from biome flavor
- it gives `Highlands` a stronger physical cause instead of using biome as a stand-in for all terrain effects

#### `Biome`

A broad environmental summary such as `Plains`, `Forest`, `Desert`, `Wetlands`, `Highlands`, or `Tundra`.

Why it exists:

- it remains a useful summary category for species compatibility, content matching, and readable world identity
- it should be a resolved summary of underlying physical conditions, not the only real environmental axis

#### `Fertility`

A normalized productivity potential score.

Why it exists:

- later ecology and carrying-capacity work need a compact aggregate productivity input
- future settlement and food-support systems need a broad answer to why one region is more productive than another
- it remains a high-value aggregate field, but WG-2 tightens it into a derived physical output rather than a mostly free-floating label

#### `RegionMaterialProfile`

A regional material opportunity summary used by existing downstream systems.

Why it exists:

- current discovery, extraction, and support systems already consume it
- WG-2 keeps it, but tightens it so more of it follows the physical substrate

### 3.2 Canonical meaning of the model as a whole

The WG-2 physical model should answer a small set of important questions before life is considered:

- how hot or cold is this region in broad terms?
- how much accessible water does it tend to have?
- how easy or difficult is its terrain?
- what broad environment does that combination resolve into?
- how productive is it likely to be?
- what kinds of regional material opportunities should be favored by those conditions?

That is enough to support later causal work without overfitting the world model.

## 4. Physical World Generation Model

### 4.1 High-level generation shape

WG-2 generation now follows this broad pattern:

1. build a connected region graph with mostly local adjacency
2. generate low-frequency world-scale signals for temperature, moisture, and terrain ruggedness
3. smooth those signals so neighboring regions form coherent areas rather than isolated random rolls
4. resolve compact physical categories from those signals
5. derive biome and fertility from the underlying physical axes
6. derive material opportunity tendencies from the physical substrate

### 4.2 Meaningful regional variation

The key WG-2 change is that environmental variation should come from coherent regional patterns rather than shallow per-region randomness.

The generator now creates:

- broader warm/cool bands
- broader wetter/drier zones
- clusters of flatter and more rugged terrain
- local adjacency that usually stays geographically near on the region ring

This does not attempt fine-grained geographic realism. It is still an aggregate generator. But it is now better at producing neighboring regions that differ for understandable reasons instead of feeling independently rolled.

### 4.3 Why the generator stays compact

WG-2 does not add tectonics, rivers, coastlines, or a cell map.

That complexity is intentionally out of scope for this stage. The project preference is still for aggregate, maintainable simulation inputs. WG-2 only adds the physical distinctions that materially improve later causality.

## 5. Downstream Causal Contracts

WG-2 is successful only if later phases can consume the physical foundation without inventing compensating hacks.

### 5.1 Primitive life and ecology support

Later primitive-life and ecology work should treat the physical foundation as the base constraint layer.

Expected meanings:

- `TemperatureBand` constrains thermal viability and broad environmental pressure
- `WaterAvailability` constrains hydration, moisture support, and wet/dry ecological opportunity
- `TerrainRuggedness` constrains spread ease and broad support efficiency
- `Fertility` summarizes how much biological productivity the region can plausibly sustain
- `Biome` remains a readable summary category, not the only environmental truth

### 5.2 Flora productivity potential

Later flora systems should read the physical foundation as the answer to why a region grows more or less biomass.

Expected meanings:

- higher fertility should mean better broad plant productivity potential
- water-poor regions should require adaptation or accept weaker support
- cold or rugged regions should place natural limits on growth even when they are not completely barren

### 5.3 Fauna support implications

Later fauna systems should inherit their support envelope from the same physical base.

Expected meanings:

- fauna range stability should depend partly on the productivity and climate regime of connected regions
- drier or harsher terrain should create stronger support friction
- neighboring regions with related physical conditions should be more plausible spread targets than sharply different ones

### 5.4 Movement, spread, and migration difficulty

Later movement-oriented systems should use the region graph plus terrain and environment as the main causal base.

Expected meanings:

- `NeighborIds` define where direct movement is possible
- `TerrainRuggedness` should contribute to movement friction and continuity cost
- major water scarcity should raise survival pressure and reduce route attractiveness
- local adjacency should matter more than arbitrary graph shortcuts

### 5.5 Carrying-capacity implications

Later carrying-capacity logic should not treat capacity as arbitrary.

Expected meanings:

- fertility and water are the main broad support signals
- temperature and terrain modify how effectively that support turns into long-run stability
- harsh regions can still matter historically, but they should usually be less forgiving without special enabling causes

### 5.6 Future settlement attractiveness

Settlement formation is not implemented here, but WG-2 defines the expected physical reading.

Expected meanings:

- accessible water should be one of the strongest settlement positives
- moderate fertility should improve long-run local support
- flatter or rolling terrain should generally be easier to occupy and consolidate than rugged terrain
- regional materials should follow the physical substrate closely enough that settlement advantages do not need to be fabricated later

### 5.7 Environmental pressure and regional identity

Later history systems should be able to explain why regions feel different.

Expected meanings:

- environmental identity should come from consistent physical conditions, not just names or flavor text
- harshness, richness, and strategic value should trace back to physical causes
- when later systems produce divergent outcomes, the world should remain legible as a chain of prior conditions

## 6. Removed / Tightened / Transitional Elements

### 6.1 Tightened elements

- `Fertility` is retained, but it is now more explicitly a derived physical productivity output.
- `Biome` is retained, but it is narrowed to a resolved summary rather than the only meaningful environmental axis.
- regional connectivity is tightened toward local geography-style adjacency instead of arbitrary random links.
- `RegionMaterialProfile` is tightened so more of its values follow physical conditions directly.

### 6.2 Added elements

- `TemperatureBand` is added as the canonical thermal axis.
- `TerrainRuggedness` is added as the canonical terrain-friction axis.

### 6.3 Transitional elements

`RegionMaterialProfile` is still partially transitional.

Most of it now follows the physical substrate more directly, but `Hides` still depends on ecology-facing state and is not part of the canonical non-organic substrate in the strict sense. It remains for current downstream compatibility and should be revisited in a later phase when physical and biological material sourcing are separated more cleanly.

## 7. WG-2 Exit Criteria

WG-2 is done when all of the following are true:

- the repo has a clear physical-world design document for this stage
- the region model includes a compact but stronger physical foundation than the pre-WG-2 state
- world generation produces more coherent regional physical variation rather than mostly isolated random labels
- retained physical fields have clear downstream purpose
- weak physical abstractions are either tightened or explicitly marked transitional
- later phases have explicit contracts for how to consume the physical substrate
- the implementation remains aggregate, maintainable, and within the project’s performance preference

That is the WG-2 target: a stronger physical world foundation that later phases can build on causally, without pretending later complexity has already been implemented.
