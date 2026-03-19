namespace Species.Domain.Knowledge;

public sealed record RegionKnowledgeSnapshot(
    string RegionId,
    string RegionName,
    bool IsKnownRegion,
    bool IsCurrentRegion,
    KnowledgeLevel OverallKnowledge,
    KnowledgeLevel FloraKnowledge,
    KnowledgeLevel FaunaKnowledge,
    KnowledgeLevel WaterKnowledge,
    KnowledgeLevel ConditionsKnowledge,
    KnowledgeLevel RouteKnowledge,
    float GatheringPotentialFood,
    float HuntingPotentialFood,
    float WaterSupport,
    float ThreatPressure,
    int MonthsSpent,
    int GatheringEvidenceMonths,
    int HuntingEvidenceMonths,
    int WaterEvidenceMonths);
