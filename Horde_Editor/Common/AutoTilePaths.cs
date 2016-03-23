using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Horde_ClassLibrary;

namespace Horde_Editor.Common
{
    class AutoTilePaths
    {
        /// <summary>
        /// key - от кого, к кому.
        /// value - список возможных путей. Может быть несколько, только если все одинаковой длины.
        /// 
        /// Путь - список ТайлТипов в порядке перехода.
        /// </summary>
        public Dictionary<Tuple<TileType, TileType>, List<List<TileType>>> Dict = null; // словарь переходов



        #region utils

        public int GetPathLenght(TileType type1, TileType type2)
        {
            Tuple<TileType, TileType> tuple = new Tuple<TileType, TileType>(type1, type2);

            if (!Dict.ContainsKey(tuple) || Dict[tuple].Count == 0)
                return -1;

            return Dict[new Tuple<TileType, TileType>(type1, type2)][0].Count;
        }


        // первый переходный тип в пути от type1 к type2
        public TileType GetFirst(TileType type1, TileType type2)
        {

            Tuple<TileType, TileType> tuple = new Tuple<TileType, TileType>(type1, type2);

            if (type1 == type2)
                return type1;

            if (!Dict.ContainsKey(tuple) ||
                Dict[tuple].Count == 0 ||
                Dict[tuple][0].Count == 0)
                return TileType.Unknown;


            return Dict[new Tuple<TileType, TileType>(type1, type2)][0][0];
        }


        #endregion utils



        #region update paths
        public void UpdatePaths()
        {
            Dict = new Dictionary<Tuple<TileType, TileType>, List<List<TileType>>>();

            //////////
            // Этап 1. Первичные пути.

            foreach (TileType tt1 in autotiler.autoTile_dict.Keys) // по всем ключам словарика.
            {
                // по всем "правильным" переходам из текущего типа.
                foreach (TileType tt2 in get_all_valid_tiletypes_path_from_dict(autotiler.autoTile_dict[tt1]))
                {
                    // составляем все пути с одним переходом.
                    if (tt1 != tt2)
                    {
                        Tuple<TileType, TileType> tmp = new Tuple<TileType, TileType>(tt1, tt2);
                        Dict.Add(tmp, new List<List<TileType>>());
                        Dict[tmp].Add(new List<TileType>() { tt2 }); // путь состоит из одного элемента.
                    }

                }
            }

            //////////
            // Этап 2. Всевозможные пути.

            // словарик с путями, которые добавили нв текущей итерации.
            Dictionary<Tuple<TileType, TileType>, List<List<TileType>>> tmpPaths = new Dictionary<Tuple<TileType, TileType>, List<List<TileType>>>();
            int n = 0; // количество путей добавленых за одну итерацию.

            while (true)
            {
                foreach (Tuple<TileType, TileType> tup0 in Dict.Keys) // по всем ключам словаря путей. (внешний цикл)
                {
                    TileType tt1 = tup0.Item1; // исходная точка.
                    TileType tt2 = tup0.Item2; // конечная точка.

                    foreach (Tuple<TileType, TileType> tup2 in Dict.Keys) // по всем ключам словаря путей. (внутренний цикл)
                    {
                        TileType tt3 = tup2.Item1; // исходная точка.
                        TileType tt4 = tup2.Item2; // конечная точка.

                        // пути можно слить, если конечная точка одного пути совпадает с начальной точкой другого пути.
                        // главное, чтобы начальная точка первого пути не совпадала с конечной второго.
                        if (tt2 == tt3 && tt1 != tt4)
                        {
                            List<TileType> new_path;

                            // все пути [tt1 to tt2] сливаем со всеми путями [tt3 to tt4]
                            for (int i = 0; i < Dict[tup0].Count; i++)
                                for (int j = 0; j < Dict[tup2].Count; j++)
                                {
                                    // соединяем
                                    new_path = Utils.ListMerger<TileType>(Dict[tup0][i], Dict[tup2][j]);

                                    // добавляем во временный список.
                                    path_add(new_path, tt1, tt4, tmpPaths);
                                }
                        }

                    }
                }

                // переносим все новые пути в главный словарь.
                foreach (Tuple<TileType, TileType> tup0 in tmpPaths.Keys)
                {
                    foreach (List<TileType> list in tmpPaths[tup0])
                    {
                        // добавляем в главный словарь, с проверкой пути.
                        bool result = path_add_to_main_dict(list, tup0.Item1, tup0.Item2);

                        if (result)
                            n++;
                    }

                }

                // закончились пути
                if (n == 0)
                    break;

                // Очистить временное.
                tmpPaths.Clear();
                n = 0;
            }

            //////////
            // Этап 3. Проверка, что нету два путя для одной пары.
            //
            //foreach (Tuple<TileType, TileType> pair in autoTilePaths.Keys)
            //{
            //    if (autoTilePaths[pair].Count != 1)
            //    {
            //        string msg = "Found " + autoTilePaths[pair].Count + " paths from " + H2Strings.tile_types_str[(int)pair.Item1] +
            //            " to " + H2Strings.tile_types_str[(int)pair.Item2];
            //        System.Windows.MessageBox.Show(msg, "Tile paths", MessageBoxButton.OK, MessageBoxImage.Information);
            //    }
            //}

        }

        // добавление пути в главный словарь.
        // остаются только наименьшие пути, или одинаковые, по числу шагов.
        bool path_add_to_main_dict(List<TileType> new_path, TileType type1, TileType type2)
        {
            bool result = false;
            Tuple<TileType, TileType> tuple = new Tuple<TileType, TileType>(type1, type2);

            if (!Dict.ContainsKey(tuple)) // был ли вообще такой переход
            { // не было такого перехода.
                Dict.Add(tuple, new List<List<TileType>>());
                Dict[tuple].Add(new_path); // добавляем путь.
                result = true;
            }
            else
            { // был такой переход.
                if (Dict[tuple].Count > 0) // есть ли что-нибудь внутри. Скорее всего есть.
                {
                    // проверяем по длине.
                    if (Dict[tuple][0].Count > new_path.Count)
                    { // имеющиеся пути - больше. Убираем их и ставим новый.
                        Dict[tuple] = new List<List<TileType>>();
                        Dict[tuple].Add(new_path); // добавляем путь.
                        result = true;
                    }
                    else if (Dict[tuple][0].Count == new_path.Count)
                    { // новый такой же как и старые. Сравниваем его со старыми.

                        bool equal = true;

                        foreach (List<TileType> list in Dict[tuple])
                        {
                            equal = true;

                            for (int i = 0; i < list.Count; i++)
                                if (list[i] != new_path[i])
                                {
                                    equal = false;       // есть отличие.
                                    break;
                                }

                            if (equal) // нашелся такой же путь.
                                break;
                        }

                        if (equal == false) // было хотя бы одно отличие со всеми тайлами.
                        {
                            Dict[tuple].Add(new_path); // добавляем путь.
                            result = true;
                        }
                    }
                }
                else
                { // пусто.
                    Dict[tuple].Add(new_path); // добавляем путь.
                    result = true;
                }
            }
            return result;
        }

        // добавление пути во временный словарик. Тут без проверки на длину.
        void path_add(List<TileType> new_path, TileType type1, TileType type2, Dictionary<Tuple<TileType, TileType>, List<List<TileType>>> dict)
        {
            Tuple<TileType, TileType> tuple = new Tuple<TileType, TileType>(type1, type2);

            if (!dict.ContainsKey(tuple)) // был ли вообще такой переход
                dict.Add(tuple, new List<List<TileType>>());

            dict[tuple].Add(new_path); // добавляем путь.
        }


        // составляет список с "правильными" переходами указанного словаря.
        List<TileType> get_all_valid_tiletypes_path_from_dict(AutoTileDict dict)
        {
            List<TileType> valids = new List<TileType>();

            foreach (TileType tt in dict.Friends.Keys)
            {
                if (it_valid_crossing(dict, tt))
                    valids.Add(tt);

            }
            return valids;
        }

        // true если словарь содержит все нужные тайлы перехода на определенный тип
        // Нужные:
        //Up, Down,
        //Left, Right,
        //DownRight, UpRight, UpLeft, DownLeft,
        //AntiDownRight, AntiUpRight, AntiUpLeft, AntiDownLeft,
        bool it_valid_crossing(AutoTileDict dict, TileType another)
        {
            bool valid = true;

            if (dict.GetCountTiles(another, "Up", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "Down", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "Left", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "Right", 0) == 0)
                valid = false;

            if (dict.GetCountTiles(another, "DownRight", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "UpRight", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "UpLeft", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "DownLeft", 0) == 0)
                valid = false;

            if (dict.GetCountTiles(another, "AntiDownRight", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "AntiUpRight", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "AntiUpLeft", 0) == 0)
                valid = false;
            if (dict.GetCountTiles(another, "AntiDownLeft", 0) == 0)
                valid = false;

            return valid;
        }


        #endregion update paths



        AutoTiler autotiler;
        public AutoTilePaths(AutoTiler a_t)
        {
            autotiler = a_t;
        }
    }
}
