using Species.Domain.Simulation;

namespace Species.Domain.Diagnostics;

public static class SimulationTickFormatter
{
    public static string Format(SimulationTickResult tickResult)
    {
        var lines = new List<string>
        {
            $"Tick Result: Year {tickResult.World.CurrentYear}, Month {tickResult.World.CurrentMonth}",
            "Flora Changes:"
        };

        foreach (var change in tickResult.FloraChanges)
        {
            lines.Add(
                $"{change.RegionId} | {change.RegionName} | {change.FloraSpeciesId} ({change.FloraSpeciesName}) | {change.PreviousPopulation} -> {change.NewPopulation} | Target={change.TargetPopulation} | Outcome={change.Outcome} | WaterSupported={change.WaterSupported} | CoreBiomeFit={change.CoreBiomeFit} | FertilityFit={change.FertilityFit:0.00} | Cause={change.PrimaryCause}");
        }

        lines.Add(string.Empty);
        lines.Add("Fauna Changes:");

        foreach (var change in tickResult.FaunaChanges)
        {
            lines.Add(
                $"{change.RegionId} | {change.RegionName} | {change.FaunaSpeciesId} ({change.FaunaSpeciesName}) | {change.PreviousPopulation} -> {change.NewPopulation} | Births={change.Births} | Deaths={change.Deaths} | Needed={change.FoodNeeded:0.00} | Consumed={change.FoodConsumed:0.00} | Shortfall={change.FoodShortfall:0.00} | Fulfillment={change.FulfillmentRatio:0.00} | FoodState={change.FoodStressState} | Hunger={change.HungerPressure:0.00}/{change.ShortageMonths}m | Habitat={change.HabitatSupport:0.00} | MigrationPressure={change.MigrationPressure:0.00} | Migrated={change.MigratedOut} | Outcome={change.Outcome} | FloraConsumed=[{change.ConsumedFloraSummary}] | FaunaConsumed=[{change.ConsumedFaunaSummary}] | Cause={change.PrimaryCause}");
        }

        lines.Add(string.Empty);
        lines.Add("Group Pressures:");

        foreach (var change in tickResult.GroupPressureChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Region={change.CurrentRegionId} ({change.CurrentRegionName}) | Population={change.Population} | StoredFood={change.StoredFood} | Food={Describe(change.Food)} | Water={Describe(change.Water)} | Threat={Describe(change.Threat)} | Overcrowding={Describe(change.Overcrowding)} | Migration={Describe(change.Migration)} | FoodReason={change.FoodPressureReason} | ThreatReason={change.ThreatPressureReason} | MigrationReason={change.MigrationPressureReason}");
        }

        lines.Add(string.Empty);
        lines.Add("Group Survival:");

        foreach (var change in tickResult.GroupSurvivalChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Region={change.CurrentRegionId} ({change.CurrentRegionName}) | Preference={change.SubsistencePreference} | Mode={change.SubsistenceMode} | Plan={change.ExtractionPlan} | Population={change.StartingPopulation}->{change.FinalPopulation} | Need={change.MonthlyFoodNeed} | KnownSupport=Gather:{change.KnownGatheringSupport}/Hunt:{change.KnownHuntingSupport} | Primary={change.PrimaryAction}:{change.PrimaryFoodGained} | Fallback={change.FallbackAction}:{change.FallbackFoodGained} | Acquired={change.TotalFoodAcquired} | Usable={change.UsableFoodConsumed} | StoredFood={change.StoredFoodBefore}->{change.StoredFoodAfter} | Hardship=FoodEff:{change.FoodPressureEffective}/WaterEff:{change.WaterPressureEffective}/{change.HardshipSeverityLabel} | FoodState={change.FoodStressState} | Hunger={change.HungerPressure:0.00}/{change.ShortageMonths}m | Shortage={change.Shortage} | StarvationLoss={change.StarvationLoss} | Outcome={change.Outcome} | PrimarySummary={change.PrimarySummary} | FallbackSummary={change.FallbackSummary} | Reason={change.SurvivalReason}");
        }

        lines.Add(string.Empty);
        lines.Add("Migration:");

        foreach (var change in tickResult.MigrationChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Region={change.CurrentRegionId} ({change.CurrentRegionName}) | MigrationPressure=display:{change.MigrationPressure}/eff:{change.MigrationEffectivePressure}/{change.MigrationSeverityLabel} | StoredFood={change.StoredFood} | Considered={change.ConsideredMigration} | RequiredMargin={change.RequiredMoveMargin:0.0} | CurrentScore={change.CurrentRegionScore:0.0} | Neighbors=[{change.NeighborScoresSummary}] | Winner={change.WinningRegionId} ({change.WinningRegionName})={change.WinningRegionScore:0.0} | Moved={change.Moved} | NewRegion={change.NewRegionId} ({change.NewRegionName}) | LastRegionId={change.LastRegionId} | MonthsSinceLastMove={change.MonthsSinceLastMove} | Reason={change.DecisionReason}");
        }

        lines.Add(string.Empty);
        lines.Add("Discoveries:");

        foreach (var change in tickResult.DiscoveryChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Known=[{change.KnownDiscoveriesSummary}] | Evidence={change.EvidenceSummary} | Checks={change.CheckSummary} | Unlocked=[{change.UnlockedDiscoveriesSummary}] | Effect={change.DecisionEffectSummary}");
        }

        lines.Add(string.Empty);
        lines.Add("Advancements:");

        foreach (var change in tickResult.AdvancementChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | RelevantDiscoveries=[{change.RelevantDiscoveriesSummary}] | Learned=[{change.LearnedAdvancementsSummary}] | Evidence={change.EvidenceSummary} | Checks={change.CheckSummary} | Unlocked=[{change.UnlockedAdvancementsSummary}] | Effect={change.PracticalEffectSummary}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Describe(PressureChangeDetail detail)
    {
        return $"prior={detail.PriorRaw}, +={detail.MonthlyContribution}, decay={detail.DecayApplied}, raw={detail.FinalRaw}, eff={detail.Effective}, display={detail.Display}, band={detail.SeverityLabel}";
    }
}
