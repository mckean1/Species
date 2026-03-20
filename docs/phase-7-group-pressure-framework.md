# Phase 7 Pressure Framework

The pressure model is now persistent. Groups still track five gameplay pressures, but each pressure carries three views of the same underlying state instead of a single capped monthly score.

## Pressure State

Each `PopulationGroup` owns a `PressureState` with:

- `Food`
- `Water`
- `Threat`
- `Overcrowding`
- `Migration`

Each entry is a `PressureValue`:

- `RawValue`: simulation truth, carried forward month to month
- `EffectiveValue`: gameplay-facing decision value, softened from raw extremes
- `DisplayValue`: player-facing compressed `0..100` value
- `SeverityLabel`: readable band derived from display

Read-only compatibility properties such as `FoodPressure` and `MigrationPressure` now map to `DisplayValue`.

## Core Rules

- Internal pressure is not gameplay-capped at `100`.
- Display pressure remains compressed into `0..100`.
- Pressure persists month to month.
- Each month, pressure decays passively toward zero.
- Decisions should use `EffectiveValue`.
- UI and summaries should use `DisplayValue` and `SeverityLabel`.
- Downstream thresholds are retuned around persistent pressure memory rather than the old fresh-month `0..100` recalculation.

## Definitions

Current definitions are intentionally small and static:

| Pressure | Shape | Curve | Decay | Monthly decay |
| --- | --- | --- | --- | --- |
| Food | OneSided | Persistent | PassiveTowardZero | 1 |
| Water | OneSided | Persistent | PassiveTowardZero | 1 |
| Threat | OneSided | Transient | PassiveTowardZero | 2 |
| Overcrowding | OneSided | Persistent | PassiveTowardZero | 1 |
| Migration | OneSided | Transient | PassiveTowardZero | 2 |

All current pressures are one-sided, but the framework keeps room for signed pressure later.

## Monthly Update Semantics

`PressureCalculationSystem` no longer treats its food, water, threat, overcrowding, and migration calculations as final monthly pressure values.

Instead, each monthly calculation is a contribution:

1. Start from previous `RawValue`
2. Add this month's contribution
3. Apply toward-zero decay
4. Apply safety bounds
5. Recompute `EffectiveValue`
6. Recompute `DisplayValue`
7. Recompute `SeverityLabel`

This keeps causal inputs readable while giving the simulation memory.

## Effective And Display Layers

`RawValue` can climb well beyond the old `0..100` range.

`EffectiveValue` keeps the old decision scale readable by remaining roughly linear through lower values and then softening heavier accumulation.

Gameplay systems should read `EffectiveValue`. This includes migration triggers, law pressure, bloc pressure, governance pressure, and other simulation decisions.

`DisplayValue` compresses the effective value back into a player-readable `0..100` band.

Player-facing summaries should read `DisplayValue` and `SeverityLabel`. Chronicle summaries, urgent warnings, and screen builders should not expose `RawValue` by default.

Severity bands currently map from display as:

- `Calm`
- `Low`
- `Rising`
- `High`
- `Severe`
- `Critical`

## Debugging

`GroupPressureChange` now records, for each pressure:

- prior raw
- monthly contribution
- decay applied
- final raw
- effective
- display
- severity label
- reason text

This is intended for tuning the framework without exposing raw values in the normal player UI.
