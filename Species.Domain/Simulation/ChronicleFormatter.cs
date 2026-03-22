using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// Chronicle entries intentionally use canonical short templates and typed tokens so entity classes can be colored consistently.
public static class ChronicleFormatter
{
    public static bool CanFormat(ChronicleEventCandidate candidate)
    {
        return TryFormatTokens(candidate) is not null;
    }

    public static string Format(ChronicleEventCandidate candidate)
    {
        return string.Concat(FormatTokens(candidate).Select(token => token.Text));
    }

    public static IReadOnlyList<ChronicleTextToken> FormatTokens(ChronicleEventCandidate candidate)
    {
        return TryFormatTokens(candidate) ??
               throw new InvalidOperationException($"No Chronicle formatter template exists for {candidate.Category}:{candidate.EventType}:{candidate.TriggerKind}.");
    }

    private static IReadOnlyList<ChronicleTextToken>? TryFormatTokens(ChronicleEventCandidate candidate)
    {
        return candidate.Category switch
        {
            ChronicleCandidateCategory.Discovery => FormatDiscovery(candidate),
            ChronicleCandidateCategory.Advancement => FormatAdvancement(candidate),
            ChronicleCandidateCategory.Settlement => FormatSettlement(candidate),
            ChronicleCandidateCategory.Politics => FormatPolitics(candidate),
            ChronicleCandidateCategory.Polity => FormatPolity(candidate),
            ChronicleCandidateCategory.Territory => FormatTerritory(candidate),
            ChronicleCandidateCategory.Survival => FormatSurvival(candidate),
            ChronicleCandidateCategory.Environment => FormatEnvironment(candidate),
            ChronicleCandidateCategory.Conflict => FormatConflict(candidate),
            ChronicleCandidateCategory.Demography => FormatDemography(candidate),
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatDiscovery(ChronicleEventCandidate candidate)
    {
        if (candidate.TriggerKind == ChronicleTriggerKind.Encountered &&
            !string.IsNullOrWhiteSpace(candidate.OtherPartyName))
        {
            if (candidate.IsScoutSourced && !string.IsNullOrWhiteSpace(candidate.RegionName))
            {
                return
                [
                    Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                    Token(ChronicleTextTokenType.NeutralText, " scouts encountered "),
                    Token(ChronicleTextTokenType.Sapient, candidate.OtherPartyName),
                    Token(ChronicleTextTokenType.NeutralText, " in "),
                    Token(ChronicleTextTokenType.Region, candidate.RegionName),
                    Token(ChronicleTextTokenType.NeutralText, ".")
                ];
            }

            return
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " encountered "),
                Token(ChronicleTextTokenType.Sapient, candidate.OtherPartyName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ];
        }

        if (string.IsNullOrWhiteSpace(candidate.DiscoveryName))
        {
            return null;
        }

        if (candidate.IsScoutSourced && !string.IsNullOrWhiteSpace(candidate.RegionName))
        {
            return
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " scouts discovered "),
                Token(ChronicleTextTokenType.Discovery, candidate.DiscoveryName),
                Token(ChronicleTextTokenType.NeutralText, " in "),
                Token(ChronicleTextTokenType.Region, candidate.RegionName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ];
        }

        return
        [
            Token(ChronicleTextTokenType.Polity, candidate.PolityName),
            Token(ChronicleTextTokenType.NeutralText, " discovered "),
            Token(ChronicleTextTokenType.Discovery, candidate.DiscoveryName),
            Token(ChronicleTextTokenType.NeutralText, ".")
        ];
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatAdvancement(ChronicleEventCandidate candidate)
    {
        return string.IsNullOrWhiteSpace(candidate.AdvancementName)
            ? null
            :
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " learned "),
                Token(ChronicleTextTokenType.Advancement, candidate.AdvancementName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ];
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatSettlement(ChronicleEventCandidate candidate)
    {
        if (candidate.EventType == "settlement-vulnerable" && !string.IsNullOrWhiteSpace(candidate.SettlementName))
        {
            return
            [
                Token(ChronicleTextTokenType.Settlement, candidate.SettlementName),
                Token(ChronicleTextTokenType.NeutralText, " is vulnerable.")
            ];
        }

        if (string.IsNullOrWhiteSpace(candidate.SettlementName))
        {
            return null;
        }

        return candidate.TriggerKind switch
        {
            ChronicleTriggerKind.Founded =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " founded "),
                Token(ChronicleTextTokenType.Settlement, candidate.SettlementName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            ChronicleTriggerKind.Abandoned =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " abandoned "),
                Token(ChronicleTextTokenType.Settlement, candidate.SettlementName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            ChronicleTriggerKind.Secured =>
            [
                Token(ChronicleTextTokenType.Settlement, candidate.SettlementName),
                Token(ChronicleTextTokenType.NeutralText, " became the capital of "),
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            ChronicleTriggerKind.Migrated =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " moved its capital to "),
                Token(ChronicleTextTokenType.Settlement, candidate.SettlementName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatPolitics(ChronicleEventCandidate candidate)
    {
        if (candidate.EventType == "law" && !string.IsNullOrWhiteSpace(candidate.LawName))
        {
            return candidate.TriggerKind switch
            {
                ChronicleTriggerKind.Secured =>
                [
                    Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                    Token(ChronicleTextTokenType.NeutralText, " passed "),
                    Token(ChronicleTextTokenType.NeutralText, candidate.LawName),
                    Token(ChronicleTextTokenType.NeutralText, ".")
                ],
                ChronicleTriggerKind.Lost =>
                [
                    Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                    Token(ChronicleTextTokenType.NeutralText, " vetoed "),
                    Token(ChronicleTextTokenType.NeutralText, candidate.LawName),
                    Token(ChronicleTextTokenType.NeutralText, ".")
                ],
                _ => null
            };
        }

        return null;
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatPolity(ChronicleEventCandidate candidate)
    {
        return candidate.TriggerKind switch
        {
            ChronicleTriggerKind.Founded =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " was founded.")
            ],
            ChronicleTriggerKind.Started when !string.IsNullOrWhiteSpace(candidate.GovernmentFormName) =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " became a "),
                Token(ChronicleTextTokenType.GovernmentForm, candidate.GovernmentFormName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            ChronicleTriggerKind.Split => BuildPolityKeyword(candidate.PolityName, " split", ChronicleTextTokenType.NegativeKeyword),
            ChronicleTriggerKind.Unified => BuildPolityKeyword(candidate.PolityName, " unified", ChronicleTextTokenType.PositiveKeyword),
            ChronicleTriggerKind.Collapsed => BuildPolityKeyword(candidate.PolityName, " collapsed", ChronicleTextTokenType.NegativeKeyword),
            ChronicleTriggerKind.Stabilized => BuildPolityKeyword(candidate.PolityName, " stabilized", ChronicleTextTokenType.PositiveKeyword),
            ChronicleTriggerKind.BecameUnstable => BuildPolityKeyword(candidate.PolityName, " became unstable", ChronicleTextTokenType.NegativeKeyword),
            ChronicleTriggerKind.Fractured => BuildPolityKeyword(candidate.PolityName, " fractured", ChronicleTextTokenType.NegativeKeyword),
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatTerritory(ChronicleEventCandidate candidate)
    {
        if (candidate.EventType == "migration-pressure")
        {
            return
            [
                Token(ChronicleTextTokenType.NeutralText, "Migration pressure is rising.")
            ];
        }

        if (string.IsNullOrWhiteSpace(candidate.RegionName))
        {
            return null;
        }

        return candidate.TriggerKind switch
        {
            ChronicleTriggerKind.Migrated => BuildPolityRegion(candidate.PolityName, " migrated to ", candidate.RegionName),
            ChronicleTriggerKind.Settled => BuildPolityRegion(candidate.PolityName, " settled in ", candidate.RegionName),
            ChronicleTriggerKind.Recovered => BuildPolityRegion(candidate.PolityName, " retreated from ", candidate.RegionName),
            ChronicleTriggerKind.Started => BuildPolityRegion(candidate.PolityName, " expanded into ", candidate.RegionName),
            ChronicleTriggerKind.Lost => BuildPolityRegion(candidate.PolityName, " lost ", candidate.RegionName),
            ChronicleTriggerKind.Secured => BuildPolityRegion(candidate.PolityName, " secured ", candidate.RegionName),
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatSurvival(ChronicleEventCandidate candidate)
    {
        return candidate.TriggerKind switch
        {
            ChronicleTriggerKind.Started when candidate.EventType == "low-food-stores" =>
            [
                Token(ChronicleTextTokenType.NeutralText, "Food stores are low.")
            ],
            ChronicleTriggerKind.Started when candidate.EventType == "shortage-active" =>
            [
                Token(ChronicleTextTokenType.NeutralText, "Shortage remains active.")
            ],
            ChronicleTriggerKind.Escalated when candidate.EventType == "famine-active" =>
            [
                Token(ChronicleTextTokenType.NeutralText, "Famine remains active.")
            ],
            ChronicleTriggerKind.Started when candidate.EventType == "shortage" => BuildPolityKeyword(candidate.PolityName, " entered shortage", ChronicleTextTokenType.NegativeKeyword),
            ChronicleTriggerKind.Escalated when candidate.EventType == "famine" => BuildPolityKeyword(candidate.PolityName, " entered famine", ChronicleTextTokenType.NegativeKeyword),
            ChronicleTriggerKind.Recovered when candidate.EventType == "shortage" => BuildPolityKeyword(candidate.PolityName, " recovered from shortage", ChronicleTextTokenType.PositiveKeyword),
            ChronicleTriggerKind.Recovered when candidate.EventType == "famine" => BuildPolityKeyword(candidate.PolityName, " recovered from famine", ChronicleTextTokenType.PositiveKeyword),
            ChronicleTriggerKind.Collapsed when candidate.EventType == "losses" => BuildPolityKeyword(candidate.PolityName, " suffered heavy losses", ChronicleTextTokenType.NegativeKeyword),
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatEnvironment(ChronicleEventCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.RegionName))
        {
            return null;
        }

        return candidate.EventType switch
        {
            "drought" =>
            [
                Token(ChronicleTextTokenType.NegativeKeyword, "Drought"),
                Token(ChronicleTextTokenType.NeutralText, " struck "),
                Token(ChronicleTextTokenType.Region, candidate.RegionName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            "harsh-winter" =>
            [
                Token(ChronicleTextTokenType.NegativeKeyword, "Harsh winter"),
                Token(ChronicleTextTokenType.NeutralText, " struck "),
                Token(ChronicleTextTokenType.Region, candidate.RegionName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            "prey-collapsed" => BuildKeywordRegion("Prey collapsed in ", candidate.RegionName, ChronicleTextTokenType.NegativeKeyword),
            "flora-collapsed" => BuildKeywordRegion("Flora collapsed in ", candidate.RegionName, ChronicleTextTokenType.NegativeKeyword),
            "prey-recovered" => BuildKeywordRegion("Prey recovered in ", candidate.RegionName, ChronicleTextTokenType.PositiveKeyword),
            "flora-recovered" => BuildKeywordRegion("Flora recovered in ", candidate.RegionName, ChronicleTextTokenType.PositiveKeyword),
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatConflict(ChronicleEventCandidate candidate)
    {
        if (candidate.EventType == "conflict-active")
        {
            return
            [
                Token(ChronicleTextTokenType.NeutralText, "Conflict remains active.")
            ];
        }

        if (string.IsNullOrWhiteSpace(candidate.OtherPartyName))
        {
            return null;
        }

        return candidate.EventType switch
        {
            "hostility" =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NeutralText, " encountered hostility from "),
                Token(ChronicleTextTokenType.Polity, candidate.OtherPartyName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            "raid" =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NegativeKeyword, " raided "),
                Token(ChronicleTextTokenType.Polity, candidate.OtherPartyName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            "war" =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.NegativeKeyword, " went to war with "),
                Token(ChronicleTextTokenType.Polity, candidate.OtherPartyName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            "peace" =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.PositiveKeyword, " made peace with "),
                Token(ChronicleTextTokenType.Polity, candidate.OtherPartyName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            "alliance" =>
            [
                Token(ChronicleTextTokenType.Polity, candidate.PolityName),
                Token(ChronicleTextTokenType.PositiveKeyword, " allied with "),
                Token(ChronicleTextTokenType.Polity, candidate.OtherPartyName),
                Token(ChronicleTextTokenType.NeutralText, ".")
            ],
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken>? FormatDemography(ChronicleEventCandidate candidate)
    {
        return candidate.EventType switch
        {
            "decline" => BuildPolityKeyword(candidate.PolityName, " suffered heavy losses", ChronicleTextTokenType.NegativeKeyword),
            "losses" => BuildPolityKeyword(candidate.PolityName, " suffered heavy losses", ChronicleTextTokenType.NegativeKeyword),
            _ => null
        };
    }

    private static IReadOnlyList<ChronicleTextToken> BuildPolityKeyword(string polityName, string phrase, ChronicleTextTokenType keywordType)
    {
        return
        [
            Token(ChronicleTextTokenType.Polity, polityName),
            Token(keywordType, phrase),
            Token(ChronicleTextTokenType.NeutralText, ".")
        ];
    }

    private static IReadOnlyList<ChronicleTextToken> BuildPolityRegion(string polityName, string phrase, string regionName)
    {
        return
        [
            Token(ChronicleTextTokenType.Polity, polityName),
            Token(ChronicleTextTokenType.NeutralText, phrase),
            Token(ChronicleTextTokenType.Region, regionName),
            Token(ChronicleTextTokenType.NeutralText, ".")
        ];
    }

    private static IReadOnlyList<ChronicleTextToken> BuildKeywordRegion(string phrase, string regionName, ChronicleTextTokenType keywordType)
    {
        return
        [
            Token(keywordType, phrase),
            Token(ChronicleTextTokenType.Region, regionName),
            Token(ChronicleTextTokenType.NeutralText, ".")
        ];
    }

    private static ChronicleTextToken Token(ChronicleTextTokenType type, string text)
    {
        return new ChronicleTextToken
        {
            Type = type,
            Text = text
        };
    }
}
