using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public enum PolityConditionSeverity
{
    Stable,
    Strained,
    Critical,
    Collapse
}

public enum PolityIntegrityBand
{
    Coherent,
    Strained,
    Unstable,
    NearCollapse
}

public enum GovernanceConditionBand
{
    Functional,
    Strained,
    Failing,
    Collapsing
}

public sealed record MaterialSurvivalAssessment(
    PolityConditionSeverity FoodCondition,
    string FoodConditionReason,
    PolityConditionSeverity WaterCondition,
    PolityConditionSeverity ThreatCondition,
    PolityConditionSeverity CrowdingCondition,
    PolityConditionSeverity MigrationCondition,
    PolityConditionSeverity MaterialFragilityCondition,
    string MaterialFragilityReason,
    PolityConditionSeverity OverallSeverity,
    bool HasCriticalFoodWater,
    bool HasExtremeMigration,
    bool HasMaterialShortage,
    bool NonFoodMaterialWeaknessAffectsOverall);

public sealed record SpatialStabilityAssessment(
    PolityAnchoringKind AnchoringKind,
    bool HasValidSeat,
    bool HasCorePresence,
    bool HasStableBase,
    bool IsDisplaced,
    PolityConditionSeverity StabilitySeverity,
    string Summary);

public sealed record PolityIntegrityAssessment(
    int IntegrityScore,
    PolityIntegrityBand Band,
    PoliticalScaleForm ScaleForm,
    string Summary);

public sealed record GovernanceConditionAssessment(
    GovernanceState Governance,
    GovernanceConditionBand Band,
    string Summary,
    int LegitimacyDelta,
    int CohesionDelta,
    int AuthorityDelta,
    int GovernabilityDelta,
    IReadOnlyList<string> MajorCauses);

public sealed record PolityConditionSnapshot(
    string PolityId,
    GovernmentForm GovernmentForm,
    PoliticalScaleForm ScaleForm,
    PolityAnchoringKind AnchoringKind,
    MaterialSurvivalAssessment MaterialSurvival,
    SpatialStabilityAssessment SpatialStability,
    PolityIntegrityAssessment Integrity,
    GovernanceConditionAssessment Governance,
    IReadOnlyList<string> CurrentIssues,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Problems,
    IReadOnlyList<string> GovernanceNotes,
    IReadOnlyList<string> ScaleNotes,
    string Summary);
