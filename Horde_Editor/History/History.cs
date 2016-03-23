using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Horde_ClassLibrary;

namespace Horde_Editor.History
{
    public static class History
    {
        // списоки списков нужны, потому что за один клик может случиться многое, например замениться несколько ячеек ландшафта, или имениться параметры отряда.
        //static List<List<HistEvent>> HistEvents = new List<List<HistEvent>>(); // список списков историй событий.
        static List<Dictionary<HistoryPoint, SetLandEvent>> landEvents = new List<Dictionary<HistoryPoint, SetLandEvent>>(); // список списков событий ландшафта.
        static List<List<SetUnitEvent>> unitsEvents = new List<List<SetUnitEvent>>(); // список списков событий юнитов.
        static List<List<SetResourceEvent>> resourcesEvents = new List<List<SetResourceEvent>>(); // список списков событий ресурсов.

        static int ExtractLandLevel = 0; // сколько раз нажали ctrl+z?
        static int ExtractUnitsLevel = 0; // сколько раз нажали ctrl+z?
        static int ExtractResourcesLevel = 0; // сколько раз нажали ctrl+z?

        //static int ExtractLevel = 0; // сколько раз нажали ctrl+z?

        //static List<SetLandEvent> LandTmpEvents = new List<SetLandEvent>(); // список событий. Сначала формируем сбда, а затем //добавляем список в HistEvents
        //public static void AddLandEvent(int x, int y, int from, int to)
        //{
        //    LandTmpEvents.Add(new SetLandEvent(x, y, -1, from, to));
        //}
        //static List<SetUnitEvent> UnitsTmpEvents = new List<SetUnitEvent>();
        //public static void AddUnitsEvent(int x, int y, Unit u, List<int[,]> coords)
        //{
        //    UnitsTmpEvents.Add(new SetUnitEvent(x, y, u, coords));
        //}


        static public void ClearAllHistory()
        {
            landEvents = new List<Dictionary<HistoryPoint, SetLandEvent>>();
            unitsEvents = new List<List<SetUnitEvent>>();
            resourcesEvents = new List<List<SetResourceEvent>>();

            ExtractLandLevel = 0;
            ExtractUnitsLevel = 0;
            ExtractResourcesLevel = 0;
        }


        #region units
        public static void AddUnitsEvent(List<SetUnitEvent> u_sle)
        {
            if (ExtractUnitsLevel != 0)
                unitsEvents.RemoveRange(unitsEvents.Count - ExtractUnitsLevel, ExtractUnitsLevel);
            unitsEvents.Add(u_sle);
            ExtractUnitsLevel = 0;
        }

        // ctrl+z
        public static List<SetUnitEvent> ExtractUndoUnitsEvent()
        {
            if (unitsEvents.Count <= ExtractUnitsLevel)
                return null;

            ExtractUnitsLevel++;
            List<SetUnitEvent> e = unitsEvents[unitsEvents.Count - ExtractUnitsLevel];

            return e;
        }

        // ctrl+y
        public static List<SetUnitEvent> ExtractRedoUnitsEvent()
        {
            if (ExtractUnitsLevel <= 0 || unitsEvents.Count < ExtractUnitsLevel)
                return null;

            List<SetUnitEvent> e = unitsEvents[unitsEvents.Count - ExtractUnitsLevel];
            ExtractUnitsLevel--;

            return e;
        }

        #endregion units


        #region land
        public static void AddLandEvent(Dictionary<HistoryPoint, SetLandEvent> l_sle)
        {
            if (ExtractLandLevel != 0)
                landEvents.RemoveRange(landEvents.Count - ExtractLandLevel, ExtractLandLevel);
            landEvents.Add(l_sle);
            ExtractLandLevel = 0;
        }

        // ctrl+z
        public static Dictionary<HistoryPoint, SetLandEvent> ExtractUndoLandEvent()
        {
            if (landEvents.Count <= ExtractLandLevel)
                return null;

            ExtractLandLevel++;
            Dictionary<HistoryPoint, SetLandEvent> e = landEvents[landEvents.Count - ExtractLandLevel];

            return e;
        }

        // ctrl+y
        public static Dictionary<HistoryPoint, SetLandEvent> ExtractRedoLandEvent()
        {
            if (ExtractLandLevel <= 0 || landEvents.Count < ExtractLandLevel)
                return null;

            Dictionary<HistoryPoint, SetLandEvent> e = landEvents[landEvents.Count - ExtractLandLevel];
            ExtractLandLevel--;

            return e;
        }

        #endregion land


        #region resource
        public static void AddResourceEvent(List<SetResourceEvent> l_e)
        {
            if (ExtractResourcesLevel != 0)
                resourcesEvents.RemoveRange(resourcesEvents.Count - ExtractResourcesLevel, ExtractResourcesLevel);
            resourcesEvents.Add(l_e);
            ExtractResourcesLevel = 0;
        }

        // ctrl+z
        public static List<SetResourceEvent> ExtractUndoResourceEvent()
        {
            if (resourcesEvents.Count <= ExtractResourcesLevel)
                return null;

            ExtractResourcesLevel++;
            List<SetResourceEvent> e = resourcesEvents[resourcesEvents.Count - ExtractResourcesLevel];

            return e;
        }

        // ctrl+y
        public static List<SetResourceEvent> ExtractRedoResourceEvent()
        {
            if (ExtractResourcesLevel <= 0 || resourcesEvents.Count < ExtractResourcesLevel)
                return null;

            List<SetResourceEvent> e = resourcesEvents[resourcesEvents.Count - ExtractResourcesLevel];
            ExtractResourcesLevel--;

            return e;
        }

        #endregion resource



        #region blocks
        static List<SetLandEvent> HistEvents_BlockWindow = new List<SetLandEvent>();
        //static bool BlockChanged = true; // был ли изменен блок после последнего ctrl+z?
        static int ExtractBlockLevel = 0; // сколько раз нажали ctrl+z?

        public static void AddBlockEvent(int x, int y, int block, int from, int to)
        {
            if (ExtractBlockLevel != 0)
                HistEvents_BlockWindow.RemoveRange(HistEvents_BlockWindow.Count - ExtractBlockLevel, ExtractBlockLevel);
            HistEvents_BlockWindow.Add(new SetLandEvent(x, y, block, from, to));
            //BlockChanged = true;
            ExtractBlockLevel = 0;
        }

        // ctrl+z blocks
        public static SetLandEvent ExtractUndoBlockEvent()
        {
            if (HistEvents_BlockWindow.Count <= ExtractBlockLevel)
                return null;

            ExtractBlockLevel++;
            SetLandEvent sle = (SetLandEvent)HistEvents_BlockWindow[HistEvents_BlockWindow.Count - ExtractBlockLevel];
            if (!sle.GetTypeEvent().HasFlag(EventType.Land))
                return null;
            return sle;
            //BlockChanged = false;
        }

        // ctrl+y blocks
        public static SetLandEvent ExtractRedoBlockEvent()
        {
            if (ExtractBlockLevel <= 0 || HistEvents_BlockWindow.Count < ExtractBlockLevel)
                return null;

            SetLandEvent sle = (SetLandEvent)HistEvents_BlockWindow[HistEvents_BlockWindow.Count - ExtractBlockLevel];
            if (!sle.GetTypeEvent().HasFlag(EventType.Land))
                return null;

            ExtractBlockLevel--;

            return sle;
            //BlockChanged = false;
        }
        #endregion blocks
    }


    public class HistoryPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public HistoryPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class HistoryPointEqualityComparer : IEqualityComparer<HistoryPoint>
    {

        public bool Equals(HistoryPoint p1, HistoryPoint p2)
        {
            if (p1.X == p2.X && p1.Y == p2.Y)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public int GetHashCode(HistoryPoint p)
        {
            int hCode = p.X * 1000000 + p.Y; // немного косытляво. Но для этого случая пойдет.
            return hCode.GetHashCode();
        }

    }
}
