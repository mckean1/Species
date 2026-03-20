using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed record SocialIdentityResult(
    World World,
    IReadOnlyList<SocialIdentityChange> Changes);
