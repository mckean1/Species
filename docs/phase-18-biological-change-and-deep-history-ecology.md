# Phase 18: Biological Change and Deep-History Ecology

Phase 18 adds the long-horizon biological layer to the simulation while keeping the project aggregate, region-driven, and causal.

## What Changed

- Species now carry lineage baselines, baseline biological traits, and optional parent-lineage references.
- Regions now carry inherited biological profiles for local flora and fauna populations.
- Regional pressure memory now accumulates repeated cold, heat, drought, scarcity, competition, predation, and terrain stress.
- Local traits now drift slowly under repeated pressure with explicit tradeoffs.
- Regional divergence now develops before any speciation outcome.
- Rare speciation can now create daughter species from persistent successful regional divergence.
- Regional and full-species extinction now leave ecological and deep-history traces.
- Fossils/remnants now come from real extinct branches and species turnover, not random generation.

## Regional Biological Architecture

- Species baseline remains the lineage-level truth in flora and fauna catalogs.
- Regional biological profiles now hold local inherited expression:
  - trait profile
  - pressure memory
  - continuity
  - successful-months memory
  - viability
  - divergence state
  - speciation progress
- Biological inheritance is regional. One region's pressure does not rewrite a lineage globally.

## Trait Model

The current biological trait set is intentionally small:

- cold tolerance
- heat tolerance
- drought tolerance
- flexibility
- body size
- reproduction
- mobility
- defense
- resilience

These are broad aggregate traits, not genetics.

## Pressure and Inheritance

Regional profiles now accumulate slow pressure memory from actual conditions such as:

- climate mismatch
- drought
- scarcity and food shortfall
- competition
- predator load
- terrain mismatch

Traits drift only slowly and only through repeated pressure. Drift is sticky but not permanent; when pressure fades, traits can slowly ease back toward lineage baselines.

## Tradeoffs

Biological drift is intentionally tradeoff-based:

- cold hardiness competes with heat performance
- heat adaptation competes with cold performance
- drought handling tends to reduce size or reproductive push
- flexibility helps scarcity handling but costs specialization
- defense and larger size improve survival but tend to cost reproductive efficiency
- mobility helps hard terrain/range use but can cost some defensive efficiency

There are no free upgrades.

## Divergence and Speciation

Divergence is now regional and staged:

- `MinorVariation`
- `StrongRegionalVariant`
- `PreSpeciation`

Speciation requires long continuity, strong divergence, sustained success, and a viable local branch. It is intentionally rare.

When speciation happens:

- a new daughter species definition is created from the parent lineage
- the daughter species is seeded only in the source region
- the parent species can continue elsewhere
- lineage linkage is preserved through parent references

## Extinction and Fossils

Regional extinction and full lineage extinction are now explicit outcomes.

- regional extinction can create fossil/remnant records
- global extinction marks a lineage extinct in the species catalog
- fossil records are generated only from real extinct branches or species collapse
- biological history records are stored regionally for divergence/speciation/extinction turnover

## Scope Guardrails

This is still not:

- per-animal simulation
- genetics
- breeding trees
- explicit family lineages
- a detailed mutation model

The biological layer remains aggregate, pressure-driven, slow-moving, and designed for future extension.
