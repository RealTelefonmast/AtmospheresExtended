using System.Xml;
using RimWorld;
using Verse;

namespace TAC;

public class AtmosphericIncidentFilter : Editable
{
    public IncidentDef incidentDef;
    public AtmosphericValueDef AtmosValueDef;
    public float threshold = 0;

    public override void PostLoad()
    {
        base.PostLoad();
        TLog.Message("Postloading");
    }

    // IncidentDef -> >(AtmosDef, 0.5)
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {

    }
}