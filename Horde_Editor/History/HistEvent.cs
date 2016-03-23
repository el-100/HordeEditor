using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horde_Editor.History
{
    [Flags]
    public enum EventType
    {
        Land = 0x1, Unit = 0x2, Resource = 0x4, PlayerProperties = 0x8, UnitProperties = 0x10
    }
    //public interface HistEvent
    //{
    //    EventType GetTypeEvent();
    //}
}
