using System.Collections.Generic;
using Verse;

namespace TAC;

public class RealmConfig : Editable
{
    public AtmosphericRealm realmType;
    public List<AtmosphericValueDef> requiresAtmospheres;
}