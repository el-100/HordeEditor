using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Horde_ClassLibrary;

namespace Horde_Editor.Common
{
    /// <summary>
    /// В классе содержатся списки подобных тайлов одного класса.
    /// 
    /// 
    /// 
    /// </summary>
    class AutoTileDict
    {
        // тип тайла + форма + вариант = тайл
        Dictionary<TileType, Dictionary<string, Dictionary<int, List<Tile>>>> _friends = new Dictionary<TileType, Dictionary<string, Dictionary<int, List<Tile>>>>();

        public Dictionary<TileType, Dictionary<string, Dictionary<int, List<Tile>>>> Friends { get { return _friends; } }

        public void AddFriend(Tile tile, TileType another, string form, int variant)
        {
            if (!_friends.ContainsKey(another))
                _friends.Add(another, new Dictionary<string, Dictionary<int, List<Tile>>>());
            if (!_friends[another].ContainsKey(form))
                _friends[another].Add(form, new Dictionary<int, List<Tile>>());
            if (!_friends[another][form].ContainsKey(variant))
                _friends[another][form].Add(variant, new List<Tile>());

            _friends[another][form][variant].Add(tile);
        }

        public Tile GetRandomTile(TileType another, string form, int variant, int seed)
        {
            var timeDiff = DateTime.UtcNow - new DateTime(1970, 1, 1);
            long totaltime = timeDiff.Ticks;

            Random rnd = new Random(unchecked((int)(seed + totaltime)));

            int n = GetCountTiles(another, form, variant);

            if (n <= 0)
                return null;

            return _friends[another][form][variant][rnd.Next(n)];

        }

        public int GetCountTiles(TileType another, string form, int variant)
        {
            if (!_friends.ContainsKey(another))
                return -1;
            if (!_friends[another].ContainsKey(form))
                return -1;
            if (!_friends[another][form].ContainsKey(variant))
                return -1;
            if (_friends[another][form][variant].Count == 0)
                return -1;

            return _friends[another][form][variant].Count;
        }

        /*public List<Tile> Friends = new List<Tile>();
        public Dictionary<TileType, List<Tile>> Up = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> Down = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> Left = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> Right = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> UpLeft = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> UpRight = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> DownLeft = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> DownRight = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> AntiUpLeft = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> AntiUpRight = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> AntiDownLeft = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> AntiDownRight = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> DiagonalDec = new Dictionary<TileType, List<Tile>>();
        public Dictionary<TileType, List<Tile>> DiagonalInc = new Dictionary<TileType, List<Tile>>();*/

        //Fill,
        //Up, Down,
        //Left, Right,
        //DownRight, UpRight, UpLeft, DownLeft,
        //AntiDownRight, AntiUpRight, AntiUpLeft, AntiDownLeft,
        //DiagonalDec, DiagonalInc,

        public TileType tileType;
        public List<TileType> anotherTypes = new List<TileType>();


        /*public Tile GetRandomTile(TileType another, string side)
        {
            Random rnd = new Random();
            try
            {
                if (side == "Center")
                    return Friends[rnd.Next(Friends.Count)];
                else if (side == "Up")
                    return Up[another][rnd.Next(Up[another].Count)];
                else if (side == "Down")
                    return Down[another][rnd.Next(Down[another].Count)];
                else if (side == "Left")
                    return Left[another][rnd.Next(Left[another].Count)];
                else if (side == "Right")
                    return Right[another][rnd.Next(Right[another].Count)];
                else if (side == "UpLeft")
                    return UpLeft[another][rnd.Next(UpLeft[another].Count)];
                else if (side == "UpRight")
                    return UpRight[another][rnd.Next(UpRight[another].Count)];
                else if (side == "DownLeft")
                    return DownLeft[another][rnd.Next(DownLeft[another].Count)];
                else if (side == "DownRight")
                    return DownRight[another][rnd.Next(DownRight[another].Count)];
                else if (side == "AntiUpLeft")
                    return AntiUpLeft[another][rnd.Next(AntiUpLeft[another].Count)];
                else if (side == "AntiUpRight")
                    return AntiUpRight[another][rnd.Next(AntiUpRight[another].Count)];
                else if (side == "AntiDownLeft")
                    return AntiDownLeft[another][rnd.Next(AntiDownLeft[another].Count)];
                else if (side == "AntiDownRight")
                    return AntiDownRight[another][rnd.Next(AntiDownRight[another].Count)];
                else if (side == "DiagonalDec")
                    return DiagonalDec[another][rnd.Next(DiagonalDec[another].Count)];
                else if (side == "DiagonalInc")
                    return DiagonalInc[another][rnd.Next(DiagonalInc[another].Count)];
                else
                {
                    MessageBox.Show("Wrong side " + side, "GetRandomTileFrom", MessageBoxButton.OK, MessageBoxImage.Information);
                    return Friends[rnd.Next(Friends.Count)];
                }
            }
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
            catch { }

            return null;

        }*/





        static public bool[,] GetTileBoolMask(Tile t, TileType type)
        {
            bool[,] tmp_mask = new bool[2, 2];

            if (t.leftup == (byte)type)
                tmp_mask[0, 0] = true;
            if (t.rightup == (byte)type)
                tmp_mask[1, 0] = true;
            if (t.leftdown == (byte)type)
                tmp_mask[0, 1] = true;
            if (t.rightdown == (byte)type)
                tmp_mask[1, 1] = true;

            return tmp_mask;
        }

        static public string BoolMask2String(bool[,] mask)
        {
            bool lu = mask[0, 0];
            bool ru = mask[1, 0];
            bool ld = mask[0, 1];
            bool rd = mask[1, 1];

            if (lu && ru && ld && rd)
                return "Center";
            else if (lu && ru && ld && !rd)
                return "AntiDownRight";
            else if (lu && ru && !ld && rd)
                return "AntiDownLeft";
            else if (lu && ru && !ld && !rd)
                return "Up";
            else if (lu && !ru && ld && rd)
                return "AntiUpRight";
            else if (lu && !ru && ld && !rd)
                return "Left";
            else if (lu && !ru && !ld && rd)
                return "DiagonalDec";
            else if (lu && !ru && !ld && !rd)
                return "UpLeft";

            else if (!lu && ru && ld && rd)
                return "AntiUpLeft";
            else if (!lu && ru && ld && !rd)
                return "DiagonalInc";
            else if (!lu && ru && !ld && rd)
                return "Right";
            else if (!lu && ru && !ld && !rd)
                return "UpRight";
            else if (!lu && !ru && ld && rd)
                return "Down";
            else if (!lu && !ru && ld && !rd)
                return "DownLeft";
            else if (!lu && !ru && !ld && rd)
                return "DownRight";
            else if (!lu && !ru && !ld && !rd)
                return "Center";
                //return "None";

            return null;
        }

    }


    public class AutoTileArgs
    {
        public bool[,] boolMask = new bool[2, 2];
        public string form = null;
        public TileType type1 = TileType.Unknown;
        public TileType type2 = TileType.Unknown;
        public int variant = 0;
        public bool aggressive = false;
        public bool onlyAll = false;
        public bool onlyInRect = false;
        public bool recursive = true;
        public bool debug = false;

        public AutoTileArgs()
        {


        }

        public void CopyFrom(AutoTileArgs a)
        {
            Array.Copy(a.boolMask, 0, boolMask, 0, a.boolMask.Length); 

            char[] ch = new char[a.form.Length];
            a.form.CopyTo(0,ch,0,a.form.Length);
            form = new string(ch);

            type1 = a.type1;
            type2 = a.type2;
            variant = a.variant;
            aggressive = a.aggressive;
            onlyAll = a.onlyAll;
            onlyInRect = a.onlyInRect;
            recursive = a.recursive;
        }
    }
}
