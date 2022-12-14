using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using Verse;

namespace TAE
{
    public interface IAtmosphericSource
    {
        public Thing Thing { get; }
        public Room Room { get; }
        public AtmosphericDef AtmosphericDef { get; }
        bool IsActive { get; }
        int PushInterval { get; }
        int PushAmount { get; }
    }
}
