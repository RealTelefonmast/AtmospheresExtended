using System.Collections.Generic;
using Verse;

namespace TAE.Static;

public static class AtmosphericReferenceCache
{
    private static List<AtmosphericDef> EmptyList = new List<AtmosphericDef>();
    public static readonly Dictionary<string, List<AtmosphericDef>> AtmosphericGroupsByTag;
    
    static AtmosphericReferenceCache()
    {
        AtmosphericGroupsByTag = new();
    }

    public static List<AtmosphericDef> AtmospheresOfTag(string tag)
    {
        if (tag != null && AtmosphericGroupsByTag.TryGetValue(tag, out var group))
        {
            return group;
        }
        return EmptyList;
    }

    public static void RegisterDef(AtmosphericDef def)
    {
        if (def.atmosphericTag == null) return;
        if (!AtmosphericGroupsByTag.TryGetValue(def.atmosphericTag, out var groupList))
        {
            groupList = new List<AtmosphericDef>();
            AtmosphericGroupsByTag.Add(def.atmosphericTag, groupList);
        }
        
        //
        groupList.Add(def);
    }
}