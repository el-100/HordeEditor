using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horde_Editor.History
{
    public class SetResourceEvent
    {
        public int X;
        public int Y;
        public int Block;
        public int ResFrom; // что заменили?
        public int ResTo;   // на что заменили?

        public SetResourceEvent(int x, int y, int block, int from, int to)
        {
            X = x;
            Y = y;
            Block = block;
            ResFrom = from;
            ResTo = to;
        }

    }
}
