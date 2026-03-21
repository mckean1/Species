using Species.Domain.Enums;

namespace Species.Domain.Models;

// Monthly food accounting is canonical only when all fields describe the same resolved month.
// Stocks are raw stored units; demand / usable-consumed / deficit describe survival coverage.
public sealed class FoodAccountingSnapshot
{
    public int StartingCarriedStores { get; set; }

    public int StartingReserveStores { get; set; }

    public int FoodInflow { get; set; }

    public int FoodConsumption { get; set; }

    public int FoodLosses { get; set; }

    public int NetFoodChange { get; set; }

    public int EndingCarriedStores { get; set; }

    public int EndingReserveStores { get; set; }

    public int MonthlyDemand { get; set; }

    public int UsableFoodConsumed { get; set; }

    public int UnresolvedDeficit { get; set; }

    public float HungerPressure { get; set; }

    public int ShortageMonths { get; set; }

    public FoodStressState FoodStressState { get; set; }

    public int StartingTotalStores => StartingCarriedStores + StartingReserveStores;

    public int EndingTotalStores => EndingCarriedStores + EndingReserveStores;

    public FoodAccountingSnapshot Clone()
    {
        return new FoodAccountingSnapshot
        {
            StartingCarriedStores = StartingCarriedStores,
            StartingReserveStores = StartingReserveStores,
            FoodInflow = FoodInflow,
            FoodConsumption = FoodConsumption,
            FoodLosses = FoodLosses,
            NetFoodChange = NetFoodChange,
            EndingCarriedStores = EndingCarriedStores,
            EndingReserveStores = EndingReserveStores,
            MonthlyDemand = MonthlyDemand,
            UsableFoodConsumed = UsableFoodConsumed,
            UnresolvedDeficit = UnresolvedDeficit,
            HungerPressure = HungerPressure,
            ShortageMonths = ShortageMonths,
            FoodStressState = FoodStressState
        };
    }

    public static FoodAccountingSnapshot CreateInitial(int carriedStores, int reserveStores)
    {
        return new FoodAccountingSnapshot
        {
            StartingCarriedStores = carriedStores,
            StartingReserveStores = reserveStores,
            EndingCarriedStores = carriedStores,
            EndingReserveStores = reserveStores,
            FoodStressState = FoodStressState.FedStable
        };
    }
}
