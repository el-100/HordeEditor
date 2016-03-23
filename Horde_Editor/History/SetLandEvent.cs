using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horde_Editor.History
{
    public class SetLandEvent
    {
        EventType type = EventType.Land;
        public int X;
        public int Y;
        public int Block;
        public int SprFrom; // что заменили?
        public int SprTo;   // на что заменили?

        public SetLandEvent(int x, int y, int block, int from, int to)
        {
            X = x;
            Y = y;
            Block = block;
            SprFrom = from;
            SprTo = to;
        }

        public EventType GetTypeEvent()
        {
            return type;
        }
    }
}
