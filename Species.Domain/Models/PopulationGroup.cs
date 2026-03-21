using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PopulationGroup
{
    // PopulationGroup remains the ecological and demographic actor that supports a polity.
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string SpeciesId { get; init; } = string.Empty;

    public SpeciesClass SpeciesClass { get; init; } = SpeciesClass.Sapient;

    public string PolityId { get; init; } = string.Empty;

    public string CurrentRegionId { get; set; } = string.Empty;

    public string OriginRegionId { get; init; } = string.Empty;

    public int Population { get; set; }

    public int StoredFood { get; set; }

    public FoodAccountingSnapshot FoodAccounting { get; set; } = new();

    public float HungerPressure { get; set; }

    public int ShortageMonths { get; set; }

    public FoodStressState FoodStressState { get; set; }

    public SubsistencePreference SubsistencePreference { get; set; } = SubsistencePreference.Mixed;

    public SubsistenceMode SubsistenceMode { get; set; }

    public PressureState Pressures { get; set; } = new();

    public string LastRegionId { get; set; } = string.Empty;

    public int MonthsSinceLastMove { get; set; }

    public HashSet<string> KnownRegionIds { get; init; } = new(StringComparer.Ordinal);

    public HashSet<string> KnownDiscoveryIds { get; init; } = new(StringComparer.Ordinal);

    public DiscoveryEvidenceState DiscoveryEvidence { get; set; } = new();

    public HashSet<string> LearnedAdvancementIds { get; init; } = new(StringComparer.Ordinal);

    public AdvancementEvidenceState AdvancementEvidence { get; set; } = new();
}
