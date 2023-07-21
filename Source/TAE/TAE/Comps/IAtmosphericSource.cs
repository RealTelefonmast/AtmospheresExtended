using Verse;

namespace TAE
{
    public interface IAtmosphericSource
    {
        public Thing Thing { get; }
        public Room Room { get; }
        public AtmosphericValueDef AtmosphericValueDef { get; }
        bool IsActive { get; }
        int PushInterval { get; }
        int PushAmount { get; }
    }
}
