using Species.Domain.Enums;

namespace Species.Domain.Discovery;

public static class MaterialResourceTagResolver
{
    public static bool HasTag(MaterialResource resource, ResourceTag tag)
    {
        return tag switch
        {
            ResourceTag.ToolStone => resource == MaterialResource.Stone,
            _ => false
        };
    }
}
