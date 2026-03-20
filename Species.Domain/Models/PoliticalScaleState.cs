using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PoliticalScaleState
{
    public PoliticalScaleForm Form { get; set; }

    public int Centralization { get; set; }

    public int IntegrationDepth { get; set; }

    public int AutonomyTolerance { get; set; }

    public int CoordinationStrain { get; set; }

    public int DistanceStrain { get; set; }

    public int CompositeComplexity { get; set; }

    public int OverextensionPressure { get; set; }

    public int FragmentationRisk { get; set; }

    public int ExternalSuccessMonths { get; set; }

    public int IntegrationSuccessMonths { get; set; }

    public int ScaleContinuityMonths { get; set; }

    public string Summary { get; set; } = "A compact local polity.";

    public PoliticalScaleState Clone()
    {
        return new PoliticalScaleState
        {
            Form = Form,
            Centralization = Centralization,
            IntegrationDepth = IntegrationDepth,
            AutonomyTolerance = AutonomyTolerance,
            CoordinationStrain = CoordinationStrain,
            DistanceStrain = DistanceStrain,
            CompositeComplexity = CompositeComplexity,
            OverextensionPressure = OverextensionPressure,
            FragmentationRisk = FragmentationRisk,
            ExternalSuccessMonths = ExternalSuccessMonths,
            IntegrationSuccessMonths = IntegrationSuccessMonths,
            ScaleContinuityMonths = ScaleContinuityMonths,
            Summary = Summary
        };
    }
}
