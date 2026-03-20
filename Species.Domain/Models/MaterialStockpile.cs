using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class MaterialStockpile
{
    public int Timber { get; set; }

    public int Stone { get; set; }

    public int Fiber { get; set; }

    public int Clay { get; set; }

    public int Hides { get; set; }

    public int Total => Timber + Stone + Fiber + Clay + Hides;

    public int Get(MaterialResource resource)
    {
        return resource switch
        {
            MaterialResource.Timber => Timber,
            MaterialResource.Stone => Stone,
            MaterialResource.Fiber => Fiber,
            MaterialResource.Clay => Clay,
            MaterialResource.Hides => Hides,
            _ => 0
        };
    }

    public void Set(MaterialResource resource, int value)
    {
        var clamped = Math.Max(0, value);
        switch (resource)
        {
            case MaterialResource.Timber:
                Timber = clamped;
                break;
            case MaterialResource.Stone:
                Stone = clamped;
                break;
            case MaterialResource.Fiber:
                Fiber = clamped;
                break;
            case MaterialResource.Clay:
                Clay = clamped;
                break;
            case MaterialResource.Hides:
                Hides = clamped;
                break;
        }
    }

    public void Add(MaterialResource resource, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        Set(resource, Get(resource) + amount);
    }

    public MaterialStockpile Clone()
    {
        return new MaterialStockpile
        {
            Timber = Timber,
            Stone = Stone,
            Fiber = Fiber,
            Clay = Clay,
            Hides = Hides
        };
    }

    public IReadOnlyDictionary<MaterialResource, int> AsDictionary()
    {
        return new Dictionary<MaterialResource, int>
        {
            [MaterialResource.Timber] = Timber,
            [MaterialResource.Stone] = Stone,
            [MaterialResource.Fiber] = Fiber,
            [MaterialResource.Clay] = Clay,
            [MaterialResource.Hides] = Hides
        };
    }
}
