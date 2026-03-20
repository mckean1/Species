namespace Species.Domain.Models;

public sealed class BiologicalPressureMemory
{
    public int ColdStress { get; set; }

    public int HeatStress { get; set; }

    public int DroughtStress { get; set; }

    public int ScarcityPressure { get; set; }

    public int CompetitionPressure { get; set; }

    public int PredatorPressure { get; set; }

    public int TerrainPressure { get; set; }

    public BiologicalPressureMemory Clone()
    {
        return new BiologicalPressureMemory
        {
            ColdStress = ColdStress,
            HeatStress = HeatStress,
            DroughtStress = DroughtStress,
            ScarcityPressure = ScarcityPressure,
            CompetitionPressure = CompetitionPressure,
            PredatorPressure = PredatorPressure,
            TerrainPressure = TerrainPressure
        };
    }
}
