using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PoliticalBloc
{
    public ProposalBackingSource Source { get; init; }

    public int Influence { get; set; }

    public int Satisfaction { get; set; }

    public PoliticalBloc Clone()
    {
        return new PoliticalBloc
        {
            Source = Source,
            Influence = Influence,
            Satisfaction = Satisfaction
        };
    }
}
