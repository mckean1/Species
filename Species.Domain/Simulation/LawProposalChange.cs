using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public sealed class LawProposalChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string ProposalTitle { get; init; }

    public required LawProposalStatus Status { get; init; }
}
