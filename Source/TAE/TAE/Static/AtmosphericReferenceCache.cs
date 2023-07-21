using System.Collections.Generic;

namespace TAE.Static;

public static class AtmosphericReferenceCache
{
    private static List<AtmosphericValueDef> EmptyList = new List<AtmosphericValueDef>();
    public static readonly Dictionary<string, List<AtmosphericValueDef>> AtmosphericGroupsByTag;
    
    static AtmosphericReferenceCache()
    {
        AtmosphericGroupsByTag = new();
    }

    public static List<AtmosphericValueDef> AtmospheresOfTag(string tag)
    {
        if (tag != null && AtmosphericGroupsByTag.TryGetValue(tag, out var group))
        {
            return group;
        }
        return EmptyList;
    }

    public static void RegisterDef(AtmosphericValueDef valueDef)
    {
        if (valueDef.atmosphericTag == null) return;
        if (!AtmosphericGroupsByTag.TryGetValue(valueDef.atmosphericTag, out var groupList))
        {
            groupList = new List<AtmosphericValueDef>();
            AtmosphericGroupsByTag.Add(valueDef.atmosphericTag, groupList);
        }
        
        //
        groupList.Add(valueDef);
    }
}