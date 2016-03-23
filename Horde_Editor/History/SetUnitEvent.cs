using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Horde_ClassLibrary;

namespace Horde_Editor.History
{
    public class SetUnitEvent
    {
        //public int X;
        //public int Y;
        public Unit U; // юнит которого поставили/удалили.
        //List<int[,]> Coords; // координаты на которые поставили/удалили юнита.
        public bool IsAdded; // был удален или поставлен
        public SetUnitEvent(Unit u, bool isadded)
        {
            //X = x;
            //Y = y;
            U = u;
            //Coords = coords;
            IsAdded = isadded;
        }

    }
}
