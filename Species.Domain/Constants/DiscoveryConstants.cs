namespace Species.Domain.Constants;

public static class DiscoveryConstants
{
    public const int LocalFloraGatheringMonthsRequired = 2;
    public const int LocalFaunaHuntingMonthsRequired = 2;
    public const int LocalRegionResidenceMonthsRequired = 2;
    public const int LocalWaterExposureMonthsRequired = 2;
    public const int RouteTraversalCountRequired = 2;
    public const float KnownLocalFloraConfidence = 1.00f;
    public const float UnknownLocalFloraConfidence = 0.75f;
    public const float KnownLocalFaunaConfidence = 1.00f;
    public const float UnknownLocalFaunaConfidence = 0.75f;
    public const float KnownLocalWaterConfidence = 1.00f;
    public const float UnknownLocalWaterConfidence = 0.80f;
    public const float LocalRegionConditionsBonus = 6.0f;
    public const float UnknownRoutePenalty = 5.0f;
    public const float KnownRouteBonus = 4.0f;
}
