# Phase 2: Species Definitions for Ecology

## Scope

Phase 2 adds the smallest clean definition layer for ecology. It introduces flora and fauna species templates plus starter catalogs that later phases can use to seed region-level ecology.

Under the canonical biology architecture, these templates occupy `SpeciesClass.Flora` and `SpeciesClass.Fauna`. `SpeciesClass.Sapient` is a separate active class once fauna-lineage sapience emerges.

This phase defines species only. It does not create region populations, run ecology updates, simulate consumption, or introduce groups, pressure, discoveries, advancements, adaptation, or chronicle behavior.

## Definition Models

### FloraSpeciesDefinition

`FloraSpeciesDefinition` describes a flora template that later phases can place into regions.

- `Id`: stable species identifier.
- `Name`: readable definition name.
- `SpeciesClass`: canonical class marker. Flora definitions are always `SpeciesClass.Flora`.
- `CoreBiomes`: biomes the species is strongly associated with.
- `SupportedWaterAvailabilities`: water conditions the species can practically function in.
- `HabitatFertilityMin` / `HabitatFertilityMax`: fertility band where the flora functions best.
- `GrowthRate`: normalized indicator of how quickly the flora expands under good local support.
- `RecoveryRate`: normalized indicator of how well the flora rebounds after depletion.
- `UsableBiomass`: normalized indicator of how much usable edible biomass the flora contributes.
- `ConsumptionResilience`: normalized indicator of how well the flora withstands repeated consumption pressure.
- `SpreadTendency`: normalized indicator of how readily the flora spreads into nearby suitable regions.
- `RegionalAbundance`: normalized indicator of the flora's typical standing abundance when locally established.

### FaunaSpeciesDefinition

`FaunaSpeciesDefinition` describes a fauna template for later ecology seeding.

- `Id`: stable species identifier.
- `Name`: readable definition name.
- `SpeciesClass`: canonical class marker. Fauna definitions are always `SpeciesClass.Fauna`.
- `CoreBiomes`: biomes the species is strongly associated with.
- `SupportedWaterAvailabilities`: water conditions the species can practically function in.
- `HabitatFertilityMin` / `HabitatFertilityMax`: fertility band where the fauna can function best.
- `DietCategory`: broad diet role marker used for high-level ecological classification and presentation.
- `DietLinks`: explicit weighted food-web links that identify named preferred foods and optional fallback foods.
- `RequiredIntake`: normalized amount of monthly intake needed to remain fed.
- `ReproductionRate`: normalized indicator of how quickly the species can reproduce when sufficiently fed.
- `MortalitySensitivity`: normalized indicator of how sharply persistent shortages convert into mortality.
- `Mobility`: normalized indicator of how readily the species can relocate when local conditions remain poor.
- `FeedingEfficiency`: normalized indicator of how effectively the species converts available food sources into usable intake.
- `PredatorVulnerability`: normalized indicator of how easily the species is consumed by other fauna.
- `FoodYield`: normalized amount of food the species provides to later consumers or survival systems.
- `RegionalAbundance`: normalized indicator of the fauna's typical standing abundance when locally established.

## Shared Meaning

### CoreBiomes

`CoreBiomes` identifies where a species most naturally belongs in MVP terms. It is a strong association signal for later seeding and ecological fit, not a hard exclusivity rule.

### SupportedWaterAvailabilities

`SupportedWaterAvailabilities` is the practical support list for the water conditions a species can tolerate well enough to exist in.

### DietCategory

The MVP diet categories are:

- `Herbivore`
- `Carnivore`
- `Omnivore`

These are broad ecological roles only. Actual monthly feeding behavior is driven by `DietLinks`.

### DietLinks

`DietLinks` is the canonical explicit food-web definition for fauna.

- `FaunaDietTargetKind.FloraSpecies`: intake drawn from a named flora species.
- `FaunaDietTargetKind.FaunaSpecies`: intake drawn from a named prey fauna species.
- `FaunaDietTargetKind.ScavengePool`: limited low-efficiency fallback intake from incidental scavenging pressure without adding a separate carrion persistence system.

Each link has a normalized weight and may be marked as fallback. Preferred links are attempted first; fallback links only help cover remaining unmet intake.

### Flora Biomass and Fauna Yield

- `UsableBiomass` is how much practical flora biomass a region can draw from that species.
- `FoodYield` is how much food a fauna species provides.

Both values are simple normalized inputs for aggregate ecological cause-and-effect systems.

## Catalogs and Validation

Phase 2 adds separate flora and fauna catalogs with small starter sets. These catalogs are simple registries that provide explicit definition lists and lookup by ID.

Validation currently checks:

- no duplicate IDs
- no blank IDs or names
- no empty core biome lists
- no empty supported water availability lists
- valid flora fertility bands
- nonnegative numeric simulation values
- valid fauna diet category values

## Debugging

Phase 2 adds development-facing summary output for flora and fauna definitions so the starter catalogs can be inspected from the console.

## Deferred

The following are intentionally deferred:

- region flora populations
- region fauna populations
- ecology tick/update systems
- flora growth behavior
- fauna consumption behavior
- migration simulation
- groups and pressures
- discoveries, advancements, and adaptation
- chronicle systems

## Phase 3 Setup

This phase prepares Phase 3 by providing a stable catalog of flora and fauna templates that regions can reference when initial ecology state is introduced.
