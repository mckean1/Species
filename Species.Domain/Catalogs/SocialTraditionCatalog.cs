using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class SocialTraditionCatalog
{
    public const string StorageDisciplineId = "storage-discipline";
    public const string SeasonalReturnId = "seasonal-return";
    public const string FrontierAutonomyId = "frontier-autonomy";
    public const string RootedHeartlandId = "rooted-heartland";
    public const string CoordinatedOrderId = "coordinated-order";

    private readonly IReadOnlyDictionary<string, SocialTraditionDefinition> definitionsById;

    public SocialTraditionCatalog()
    {
        var definitions = new[]
        {
            new SocialTraditionDefinition
            {
                Id = StorageDisciplineId,
                Name = "Storage Discipline",
                Summary = "Repeated hardship reinforced careful storage and relief habits.",
                ChronicleLineTemplate = "{0} developed enduring storage customs after repeated hardship."
            },
            new SocialTraditionDefinition
            {
                Id = SeasonalReturnId,
                Name = "Seasonal Return",
                Summary = "Repeated movement and return cycles shaped a recurring mobile identity.",
                ChronicleLineTemplate = "Seasonal return patterns became part of {0}'s identity."
            },
            new SocialTraditionDefinition
            {
                Id = FrontierAutonomyId,
                Name = "Frontier Autonomy",
                Summary = "Peripheral strain hardened expectations of local discretion and self-direction.",
                ChronicleLineTemplate = "Frontier regions of {0} developed stronger autonomy expectations."
            },
            new SocialTraditionDefinition
            {
                Id = RootedHeartlandId,
                Name = "Rooted Heartland",
                Summary = "Long settlement continuity hardened the core into a durable social heartland.",
                ChronicleLineTemplate = "The heartland of {0} hardened into a rooted social core."
            },
            new SocialTraditionDefinition
            {
                Id = CoordinatedOrderId,
                Name = "Coordinated Order",
                Summary = "Repeated successful central coordination reinforced order-oriented expectations.",
                ChronicleLineTemplate = "Repeated coordination pushed {0} toward a stronger order tradition."
            }
        };

        definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<SocialTraditionDefinition> GetAll()
    {
        return definitionsById.Values.OrderBy(definition => definition.Name, StringComparer.Ordinal).ToArray();
    }

    public SocialTraditionDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }
}
