# Phase 2: Species Definitions for Ecology

## Scope

Phase 2 adds the smallest clean definition layer for ecology. It introduces flora and fauna species templates plus starter catalogs that later phases can use to seed region-level ecology.

This phase defines species only. It does not create region populations, run ecology updates, simulate consumption, or introduce groups, pressure, discoveries, advancements, adaptation, or chronicle behavior.

## Definition Models

### FloraSpeciesDefinition

`FloraSpeciesDefinition` describes a flora template that later phases can place into regions.

- `Id`: stable species identifier.
- `Name`: readable definition name.
- `CoreBiomes`: biomes the species is strongly associated with.
- `SupportedWaterAvailabilities`: water conditions the species can practically function in.
- `PreferredFertilityMin` / `PreferredFertilityMax`: fertility band where the flora performs best.
- `GrowthRate`: normalized indicator of how quickly the flora can recover or expand once simulation begins.
- `FoodValue`: normalized indicator of how useful the flora is as food for later consumers.

### FaunaSpeciesDefinition

`FaunaSpeciesDefinition` describes a fauna template for later ecology seeding.

- `Id`: stable species identifier.
- `Name`: readable definition name.
- `CoreBiomes`: biomes the species is strongly associated with.
- `SupportedWaterAvailabilities`: water conditions the species can practically function in.
- `DietCategory`: broad diet role.
- `FoodRequirement`: normalized amount of food pressure the species will later place on an environment.
- `ReproductionRate`: normalized indicator of how quickly the species can reproduce.
- `MigrationTendency`: normalized indicator of how likely the species is to shift regions later.
- `FoodYield`: normalized amount of food the species provides to later consumers or survival systems.

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

These are broad ecological roles only. They do not yet implement feeding behavior.

### FoodValue and FoodYield

- `FoodValue` is how useful a flora species is as food.
- `FoodYield` is how much food a fauna species provides.

Both values are simple normalized inputs for future ecological cause-and-effect systems.

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
