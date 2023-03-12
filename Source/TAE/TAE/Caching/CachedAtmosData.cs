using TeleCore;

namespace TAE.Caching
{
    public struct CachedAtmosData
    {
        public int roomID;
        public int numCells;
        public DefValueStack<AtmosphericDef> stack;

        public CachedAtmosData(int roomID, int numCells, RoomComponent_Atmospheric roomComp)
        {
            this.roomID = roomID;
            this.numCells = numCells;
            this.stack = roomComp.Container.ValueStack;
            if (roomComp.IsOutdoors)
            {
                stack += roomComp.OutsideContainer.ValueStack;
            }
        }

        public void Reset()
        {
            roomID = -1;
            numCells = 0;
            stack.Reset();
        }

        public override string ToString()
        {
            return $"[{roomID}][{numCells}][{stack.Empty}]\n{stack}";
        }
    }
}
