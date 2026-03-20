namespace Species.Domain.Simulation;

public sealed class PressureChangeDetail
{
    public required int PriorRaw { get; init; }

    public required int MonthlyContribution { get; init; }

    public required int DecayApplied { get; init; }

    public required int FinalRaw { get; init; }

    public required int Effective { get; init; }

    public required int Display { get; init; }

    public required string SeverityLabel { get; init; }

    public required string ReasonText { get; init; }
}
