using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Horde_ClassLibrary;

namespace Horde_Editor.Common
{
    class AutoTiler
    {
        Tile[,] map;
        Scena scn;

        public Dictionary<TileType, AutoTileDict> autoTile_dict = null; // словари с автотайлами.
        public AutoTilePaths Paths = null; // словарь с путями.

        // словарь тайлов по четырем типам и варианту.
        public Dictionary<Tuple<TileType, TileType, TileType, TileType, int>, List<Tile>> Dict_TileFromMask;




        #region modes desc
        /*
         + Aggressive - не обращать внимание на Frozen tiles
         + OnlyAll - рисовать только если все тайлы подобраны. Не все подбираются, когда, например, нет какой-то одной формы перехода, а остальные есть.
         + OnlyInRect - заменять тайлы только внутри прямоугольника, без соседей.
         * Recursive - рекурсивная проверка соседей. Для того чтобы получить двойные переходы и избежать нестыковок.
         */
        #endregion modes desc



        // Установка автотайла 1x1 и его соседей на готовую тайловую маску.
        // [!!Переделать под GetAllQuartersNeightbors(tx, ty); или его аналог!!]
        public void PlaceAutotileToMask(ref TileType[,] mask, int tx, int ty, AutoTileArgs args)
        {
            // аргумент в отдельные переменные
            TileType type1 = (TileType)args.type1;
            TileType type2 = (TileType)args.type2;
            if (type2 == TileType.Unknown)
                type2 = type1;
            bool[,] bool_mask = args.boolMask;

            int mw = scn.size_map_x * 2;
            int mh = scn.size_map_y * 2;

            // те четвертинки, которые можно заменить в центральном квадрате.
            bool[,] allowed_parts = new bool[,]        // Сюда будет входить результат проверки на мороженность
            { { true, true },                          // 
            { true, true } };                          // 

            // те четвертинки, которые заменены в центральном квадрате, будут помечены здесь. Для того чтобы знать что менять соседям.
            bool[,] replaced_parts = new bool[,] { { true, true }, { true, true } };


            /// Проверяем какие четвертинки в центре можно заменять:
            if (!args.aggressive)
            { // был бы аггресив, можно было бы все заменять без проверки.

                // проверим что рядом с ней нет мороженных четвертинок. (с проверкой на границы)
                // (такое нужно чтобы не сбивались переходы к мороженным)
                if (point_in_rect(tx - 1, ty - 1, 0, 0, mw, mh) && Frozen(mask[tx - 1, ty - 1]) ||
                    point_in_rect(tx - 1, ty, 0, 0, mw, mh) && Frozen(mask[tx - 1, ty]) ||
                    point_in_rect(tx, ty - 1, 0, 0, mw, mh) && Frozen(mask[tx, ty - 1]))
                    allowed_parts[0, 0] = false;

                if (point_in_rect(tx - 1, ty + 1, 0, 0, mw, mh) && Frozen(mask[tx - 1, ty + 1]) ||
                    point_in_rect(tx - 1, ty + 2, 0, 0, mw, mh) && Frozen(mask[tx - 1, ty + 2]) ||
                    point_in_rect(tx, ty + 2, 0, 0, mw, mh) && Frozen(mask[tx, ty + 2]))
                    allowed_parts[0, 1] = false;

                if (point_in_rect(tx + 1, ty - 1, 0, 0, mw, mh) && Frozen(mask[tx + 1, ty - 1]) ||
                    point_in_rect(tx + 2, ty - 1, 0, 0, mw, mh) && Frozen(mask[tx + 2, ty - 1]) ||
                    point_in_rect(tx + 2, ty, 0, 0, mw, mh) && Frozen(mask[tx + 2, ty]))
                    allowed_parts[1, 0] = false;

                if (point_in_rect(tx + 2, ty + 1, 0, 0, mw, mh) && Frozen(mask[tx + 2, ty + 1]) ||
                    point_in_rect(tx + 2, ty + 2, 0, 0, mw, mh) && Frozen(mask[tx + 2, ty + 2]) ||
                    point_in_rect(tx + 1, ty + 2, 0, 0, mw, mh) && Frozen(mask[tx + 1, ty + 2]))
                    allowed_parts[1, 1] = false;
            }

            /// Центр:

            // берем у тайла, который описан в палитре, маску и добавляем её в общий массив
            // но только если тут такое можно делать.
            if (allowed_parts[0, 0])
                if (bool_mask[0, 0])
                    mask[tx, ty] = type1;
                else
                    mask[tx, ty] = type2;
            else
                replaced_parts[0, 0] = false;

            if (allowed_parts[0, 1])
                if (bool_mask[0, 1])
                    mask[tx, ty + 1] = type1;
                else
                    mask[tx, ty + 1] = type2;
            else
                replaced_parts[0, 1] = false;

            if (allowed_parts[1, 0])
                if (bool_mask[1, 0])
                    mask[tx + 1, ty] = type1;
                else
                    mask[tx + 1, ty] = type2;
            else
                replaced_parts[1, 0] = false;

            if (allowed_parts[1, 1])
                if (bool_mask[1, 1])
                    mask[tx + 1, ty + 1] = type1;
                else
                    mask[tx + 1, ty + 1] = type2;
            else
                replaced_parts[1, 1] = false;

            /// Соседи:

            if (!args.onlyInRect) // этот режим отключает соседей.
            {
                // заменяем у соседей прилегающие миниквадраты на значения того что поставили.
                // c проверкой на край карты.
                if (replaced_parts[0, 0])
                {
                    if (point_in_rect(tx - 1, ty - 1, 0, 0, mw, mh))
                        mask[tx - 1, ty - 1] = mask[tx, ty];
                    if (point_in_rect(tx, ty - 1, 0, 0, mw, mh))
                        mask[tx, ty - 1] = mask[tx, ty];
                    if (point_in_rect(tx - 1, ty, 0, 0, mw, mh))
                        mask[tx - 1, ty] = mask[tx, ty];
                }

                if (replaced_parts[1, 0])
                {
                    if (point_in_rect(tx + 1, ty - 1, 0, 0, mw, mh))
                        mask[tx + 1, ty - 1] = mask[tx + 1, ty];
                    if (point_in_rect(tx + 2, ty - 1, 0, 0, mw, mh))
                        mask[tx + 2, ty - 1] = mask[tx + 1, ty];
                    if (point_in_rect(tx + 2, ty, 0, 0, mw, mh))
                        mask[tx + 2, ty] = mask[tx + 1, ty];
                }

                if (replaced_parts[1, 1])
                {
                    if (point_in_rect(tx + 2, ty + 1, 0, 0, mw, mh))
                        mask[tx + 2, ty + 1] = mask[tx + 1, ty + 1];
                    if (point_in_rect(tx + 2, ty + 2, 0, 0, mw, mh))
                        mask[tx + 2, ty + 2] = mask[tx + 1, ty + 1];
                    if (point_in_rect(tx + 1, ty + 2, 0, 0, mw, mh))
                        mask[tx + 1, ty + 2] = mask[tx + 1, ty + 1];
                }

                if (replaced_parts[0, 1])
                {
                    if (point_in_rect(tx, ty + 2, 0, 0, mw, mh))
                        mask[tx, ty + 2] = mask[tx, ty + 1];
                    if (point_in_rect(tx - 1, ty + 2, 0, 0, mw, mh))
                        mask[tx - 1, ty + 2] = mask[tx, ty + 1];
                    if (point_in_rect(tx - 1, ty + 1, 0, 0, mw, mh))
                        mask[tx - 1, ty + 1] = mask[tx, ty + 1];
                }
            }
        }

        // Поиск "неправильных" квадратов в маске ВОКРУГ указанного прямоугольника.
        // Проверка на пересечения. (пересекаться можно только внутри тайла)
        public int FindAndFixCrossProblemCells(ref TileType[,] mask, int x, int y, int w, int h)
        {
            int problems = 0;

            int mw = scn.size_map_x;
            int mh = scn.size_map_y;
            int dx = 0, dy = 0;           // смещение, на котором изменяемая четвертинка.
            int tx = 0, ty = 0;           // координаты, на которых образцовая четвертинка.

            int sx = x - 1;       // 
            int sy = y - 1;       // границы сканируемого
            int ex = x + w;   // прямоугольника.
            int ey = y + h;   //

            bool vertical = false;
            bool horizontal = false;


            // цикл по первому внешнему слою указанного прямоугольника. (проверка на пересечения)
            for (int i = sx; i <= ex; i++)
                for (int j = sy; j <= ey; j++)
                    if (!point_in_rect(i, j, x, y, w, h) && // только за прямоугольником.
                        point_in_rect(i, j, 0, 0, mw, mh)) // в границах карты
                    //MaskIsNotFrozen(mask, i * 2, j * 2, 2, 2)
                    //)
                    { // раз уж тут, значит i или j на границе.

                        dx = 0;
                        dy = 0;
                        vertical = false;
                        horizontal = false;

                        if (i == sx) // лево
                        { dx = 1; vertical = true; }
                        else if (i == ex) // право
                        { dx = 0; vertical = true; }

                        if (j == sy) // верх
                        { dy = 1; horizontal = true; }
                        else if (j == ey) // низ
                        { dy = 0; horizontal = true; }

                        // поправка к уже установленной четвертинке.
                        if (vertical)                            // лево право
                            tx = (i - 1 + dx * 2) * 2 + 1 - dx;
                        else
                            tx = i * 2;

                        if (horizontal)                          // низ верх
                            ty = (j - 1 + dy * 2) * 2 + 1 - dy;
                        else
                            ty = j * 2;

                        // пересечений на границе не может быть.
                        if (point_in_rect(tx, ty, 0, 0, mw * 2, mh * 2))
                        {
                            if (MaskIsNotUnknown(mask, tx, ty, 2, 2) &&
                                MaskIsNotFrozen(mask, tx, ty, 2, 2))
                            {
                                if (mask[i * 2 + dx, j * 2 + dy] != mask[tx, ty])
                                    problems++;
                                mask[i * 2 + dx, j * 2 + dy] = mask[tx, ty];
                            }
                            else
                            {
                                problems++;
                            }
                        }

                        if (!vertical || !horizontal) // исключение угловых четвертинок
                        {
                            if (vertical)
                            { dy++; ty++; }
                            else if (horizontal)
                            { dx++; tx++; }

                            if (point_in_rect(tx, ty, 0, 0, mw * 2, mh * 2))
                            {
                                if (MaskIsNotUnknown(mask, tx, ty, 2, 2) &&
                                    MaskIsNotFrozen(mask, tx, ty, 2, 2))
                                {
                                    if (mask[i * 2 + dx, j * 2 + dy] != mask[tx, ty])
                                        problems++;
                                    mask[i * 2 + dx, j * 2 + dy] = mask[tx, ty];
                                }
                                else
                                {
                                    problems++;
                                }
                            }
                        }

                    }



            return problems;
        }


        // Проверка на переходы. (путь должен существовать и быть длиной в 1 переход.)
        public int FindAndFixPathProblemCells(ref TileType[,] mask, int x, int y, int w, int h)
        {
            int problems = 0;

            int mw = scn.size_map_x;
            int mh = scn.size_map_y;
            int dx = 0, dy = 0;           // смещение, на котором изменяемая четвертинка.
            int tx = 0, ty = 0;           // координаты, на которых образцовая четвертинка.

            int sx = x - 1;       // 
            int sy = y - 1;       // границы сканируемого
            int ex = x + w;       // прямоугольника.
            int ey = y + h;       //

            bool vertical = false;
            bool horizontal = false;

            // Для КОТСЫЛЯ от (recursive + части тайлов = фракталы)
            TileType tt1 = TileType.Unknown;
            TileType tt2 = TileType.Unknown;
            int church_tx, church_ty;
            int church_dx, church_dy;


            // цикл по второму внешнему слою указанного прямоугольника. (проверка на пересечения)
            for (int i = sx; i <= ex; i++)
                for (int j = sy; j <= ey; j++)
                    if (!point_in_rect(i, j, x, y, w, h) && // только за прямоугольником.
                        point_in_rect(i, j, 0, 0, mw, mh)) // в границах карты
                    { // раз уж тут, значит i или j на границе.

                        dx = 0;
                        dy = 0;
                        vertical = false;
                        horizontal = false;
                        tt1 = TileType.Unknown;
                        tt2 = TileType.Unknown;

                        if (i == sx) // лево
                        { dx = 0; vertical = true; }
                        else if (i == ex) // право
                        { dx = 1; vertical = true; }

                        if (j == sy) // верх
                        { dy = 0; horizontal = true; }
                        else if (j == ey) // низ
                        { dy = 1; horizontal = true; }


                        // поправка к уже установленной четвертинке.
                        if (vertical)                            // лево право
                            tx = i * 2 + 1 - dx;
                        else
                            tx = i * 2;

                        if (horizontal)                          // низ верх
                            ty = j * 2 + 1 - dy;
                        else
                            ty = j * 2;


                        if (point_in_rect(tx, ty, 0, 0, mw * 2, mh * 2))
                        {
                            if (MaskIsNotUnknown(mask, tx, ty, 2, 2) && // Unknown - мог быть результатом предыдущих шагов.
                                    MaskIsNotFrozen(mask, tx, ty, 2, 2))
                            {
                                // смотрим кол-во переходов
                                int len = Paths.GetPathLenght(mask[i * 2 + dx, j * 2 + dy], mask[tx, ty]);

                                if (len > 1)
                                {
                                    problems++;

                                    // получаем первый переход
                                    tt1 = Paths.GetFirst(mask[tx, ty], mask[i * 2 + dx, j * 2 + dy]);
                                    if (tt1 != TileType.Unknown)
                                        mask[i * 2 + dx, j * 2 + dy] = tt1;

                                }
                                else if (len == 1)
                                {
                                    tt1 = mask[i * 2 + dx, j * 2 + dy];
                                }

                            }
                            else
                            {
                                problems++;
                            }
                        }

                        if (!vertical || !horizontal) // исключение угловых четвертинок
                        {
                            church_tx = tx;
                            church_ty = ty;
                            church_dx = dx;
                            church_dy = dy;
                            if (vertical)
                            { dy++; ty++; }
                            else if (horizontal)
                            { dx++; tx++; }

                            if (point_in_rect(tx, ty, 0, 0, mw * 2, mh * 2))
                            {
                                if (MaskIsNotUnknown(mask, tx, ty, 2, 2) &&
                                        MaskIsNotFrozen(mask, tx, ty, 2, 2))
                                {
                                    int len = Paths.GetPathLenght(mask[i * 2 + dx, j * 2 + dy], mask[tx, ty]);
                                    if (dx == 2 || dy == 2)
                                    { }
                                    if (len > 1)
                                    {
                                        problems++;

                                        tt2 = Paths.GetFirst(mask[tx, ty], mask[i * 2 + dx, j * 2 + dy]);
                                        if (tt2 != TileType.Unknown)
                                            mask[i * 2 + dx, j * 2 + dy] = tt2;

                                    }
                                    else if (len == 1)
                                    {
                                        tt2 = mask[i * 2 + dx, j * 2 + dy];
                                    }
                                }
                                else
                                {
                                    problems++;
                                }
                            }


                            // Костыль от (recursive + части тайлов = фракталы)
                            // смотрим на длину пути измененного до того который уже стоял(т.е. на внутреннем слое)
                            // если далеко(>1) то нужно что-то предпринять.
                            if (tt1 != tt2 && tt1 != TileType.Unknown && tt2 != TileType.Unknown)
                            {
                                // tt1 и tt2 - что поставили на внешний слой.

                                int len1 = Paths.GetPathLenght(mask[tx, ty], tt1);
                                if (len1 > 1)
                                {
                                    problems++;

                                    tt1 = Paths.GetFirst(mask[tx, ty], tt1);
                                    if (tt1 != TileType.Unknown)
                                        mask[i * 2 + church_dx, j * 2 + church_dy] = tt1;
                                }

                                int len2 = Paths.GetPathLenght(mask[church_tx, church_ty], tt2);
                                if (len2 > 1)
                                {
                                    problems++;

                                    tt2 = Paths.GetFirst(mask[church_tx, church_ty], tt2);
                                    if (tt2 != TileType.Unknown)
                                        mask[i * 2 + dx, j * 2 + dy] = tt2;
                                }

                            }

                        }


                    }


            // отдельно доставляем угловые. Отдельно, потому что все зависит от других. 

            // верх лево
            if (point_in_rect(sx * 2 + 1, sy * 2, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(sx * 2 + 2, sy * 2, 0, 0, mw * 2, mh * 2))
                mask[sx * 2 + 1, sy * 2] = mask[sx * 2 + 2, sy * 2];
            if (point_in_rect(sx * 2, sy * 2 + 1, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(sx * 2, sy * 2 + 2, 0, 0, mw * 2, mh * 2))
                mask[sx * 2, sy * 2 + 1] = mask[sx * 2, sy * 2 + 2];

            // верх право
            if (point_in_rect(ex * 2, sy * 2, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(ex * 2 - 1, sy * 2, 0, 0, mw * 2, mh * 2))
                mask[ex * 2, sy * 2] = mask[ex * 2 - 1, sy * 2];
            if (point_in_rect(ex * 2 + 1, sy * 2 + 1, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(ex * 2 + 1, sy * 2 + 2, 0, 0, mw * 2, mh * 2))
                mask[ex * 2 + 1, sy * 2 + 1] = mask[ex * 2 + 1, sy * 2 + 2];


            // низ право
            if (point_in_rect(ex * 2 + 1, ey * 2, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(ex * 2 + 1, ey * 2 - 1, 0, 0, mw * 2, mh * 2))
                mask[ex * 2 + 1, ey * 2] = mask[ex * 2 + 1, ey * 2 - 1];
            if (point_in_rect(ex * 2, ey * 2 + 1, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(ex * 2 - 1, ey * 2 + 1, 0, 0, mw * 2, mh * 2))
                mask[ex * 2, ey * 2 + 1] = mask[ex * 2 - 1, ey * 2 + 1];


            // низ лево
            if (point_in_rect(sx * 2, ey * 2, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(sx * 2, ey * 2 - 1, 0, 0, mw * 2, mh * 2))
                mask[sx * 2, ey * 2] = mask[sx * 2, ey * 2 - 1];
            if (point_in_rect(sx * 2 + 1, ey * 2 + 1, 0, 0, mw * 2, mh * 2) &&
                point_in_rect(sx * 2 + 2, ey * 2 + 1, 0, 0, mw * 2, mh * 2))
                mask[sx * 2 + 1, ey * 2 + 1] = mask[sx * 2 + 2, ey * 2 + 1];



            return problems;
        }



        //#region mask manipulate
        //#endregion mask manipulate


        #region utils

        public bool point_in_rect(int tx, int ty, int x, int y, int w, int h)
        {
            if (tx >= x && tx < x + w &&
                ty >= y && ty < y + h)
                return true;
            else
                return false;
        }



        // Получение списка соседей четвертинки, которые принадлежат другой клеточке.
        public List<Tuple<int, int>> GetQuarterNeightbors(int tx, int ty, int posx, int posy)
        {
            List<Tuple<int, int>> quarter_neightbors = new List<Tuple<int, int>>();

            if (posx == 0 && posy == 0)
            {
                quarter_neightbors.Add(new Tuple<int, int>(tx - 1, ty - 1));
                quarter_neightbors.Add(new Tuple<int, int>(tx, ty - 1));
                quarter_neightbors.Add(new Tuple<int, int>(tx, ty - 1));
            }
            else if (posx == 1 && posy == 0)
            {
                quarter_neightbors.Add(new Tuple<int, int>(tx + 1, ty - 1));
                quarter_neightbors.Add(new Tuple<int, int>(tx + 2, ty - 1));
                quarter_neightbors.Add(new Tuple<int, int>(tx + 2, ty));
            }
            else if (posx == 0 && posy == 1)
            {
                quarter_neightbors.Add(new Tuple<int, int>(tx, ty + 2));
                quarter_neightbors.Add(new Tuple<int, int>(tx - 1, ty + 2));
                quarter_neightbors.Add(new Tuple<int, int>(tx - 1, ty + 1));
            }
            else if (posx == 1 && posy == 1)
            {
                quarter_neightbors.Add(new Tuple<int, int>(tx + 2, ty + 1));
                quarter_neightbors.Add(new Tuple<int, int>(tx + 2, ty + 2));
                quarter_neightbors.Add(new Tuple<int, int>(tx + 1, ty + 2));
            }

            return quarter_neightbors;
        }
        // Получение словаря - [четвертинка],[список соседей-четвертинок, которые принадлежат другой клеточке].
        public Dictionary<Tuple<int, int>, List<Tuple<int, int>>> GetAllQuartersNeightbors(int x, int y)
        {
            Dictionary<Tuple<int, int>, List<Tuple<int, int>>> all_quarters_neightbors = new Dictionary<Tuple<int, int>, List<Tuple<int, int>>>();

            all_quarters_neightbors.Add(new Tuple<int, int>(x, y), GetQuarterNeightbors(x, y, 0, 0));
            all_quarters_neightbors.Add(new Tuple<int, int>(x + 1, y), GetQuarterNeightbors(x, y, 1, 0));
            all_quarters_neightbors.Add(new Tuple<int, int>(x, y + 1), GetQuarterNeightbors(x, y, 0, 1));
            all_quarters_neightbors.Add(new Tuple<int, int>(x + 1, y + 1), GetQuarterNeightbors(x, y, 1, 1));

            return all_quarters_neightbors;
        }


        #region mask
        // Получить тип из маски, с одним переходом до указанного тайла.
        public TileType GetTypeFromMaskWithOneCross(TileType[,] mask, int x, int y, int w, int h, TileType tt)
        {
            foreach (TileType t in GetAllTypesFromMask(mask, x, y, w, h))
            {
                if (Paths.GetPathLenght(tt, t) == 1)
                    return t;
            }
            return TileType.Unknown;
        }
        // получить список типов из маски.
        public List<TileType> GetAllTypesFromMask(TileType[,] mask, int x, int y, int w, int h)
        {
            List<TileType> tmp = new List<TileType>();

            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    if (!tmp.Contains(mask[i, j]))
                        tmp.Add(mask[i, j]);

            return tmp;
        }

        // Получить тайловую маску указанного прямоугольника карты.
        public TileType[,] GetMaskRect(int x, int y, int w, int h)
        {
            TileType[,] mask = new TileType[w * 2, h * 2];

            // зануляем.
            for (int i = 0; i < w * 2; i++)
                for (int j = 0; j < h * 2; j++)
                    mask[i, j] = TileType.Unknown;

            // внутри
            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    if (Gl.point_in_map(i, j) && !(i == x || j == y || i == x + w - 1 || j == y + h - 1))
                        FillTileMaskAt(ref mask, (i - x) * 2, (j - y) * 2, map[i, j]);

            // граница ( соседи)
            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    if (Gl.point_in_map(i, j) && (i == x || j == y || i == x + w - 1 || j == y + h - 1))
                        FillTileMaskAt(ref mask, (i - x) * 2, (j - y) * 2, map[i, j]);

            return mask;
        }

        /// заполняет четвертинки, в большой тайловой маске, принадлежащие указаному тайлу.
        public void FillTileMaskAt(ref TileType[,] mask, int x, int y, Tile t)
        {
            mask[x, y] = (TileType)t.leftup;
            mask[x, y + 1] = (TileType)t.leftdown; //(TileType)t.rightup;
            mask[x + 1, y] = (TileType)t.rightup; //(TileType)t.leftdown;
            mask[x + 1, y + 1] = (TileType)t.rightdown;
        }

        // Залить клетку неизвестным.
        public void FillTileMaskAt(ref TileType[,] mask, int x, int y, TileType tt)
        {
            mask[x, y] = tt;
            mask[x, y + 1] = tt; //(TileType)t.rightup;
            mask[x + 1, y] = tt; //(TileType)t.leftdown;
            mask[x + 1, y + 1] = tt;
        }

        /// пытается найти тайл по указанной маске и типам. (а также и варианту)
        public Tile GetAutoTileByMask(TileType type, TileType another, bool[,] boolmask, int variant, int seed)
        {
            string form = AutoTileDict.BoolMask2String(boolmask);

            int var = variant;
            TileType tmp_type = type;

            // если нет ни одного миниквадрата основной формы, то тупо заменяем основную форму на вторичную.
            if (!boolmask[0, 0] && !boolmask[0, 1] && !boolmask[1, 0] && !boolmask[1, 1])
            {
                tmp_type = another;
                form = "Center";
            }

            // Только центральные тайлы имеют вариант.
            if (form != "Center")
                var = 0;

            Tile tmp = autoTile_dict[tmp_type].GetRandomTile(another, form, var, seed);

            return tmp;
        }

        /// пытается найти многотипный тайл(1-4 типа) по указанной маске. (а также и варианту)
        /// не оптимизированный способ поиска.
        /// можно составлять словарик с уже найдеными значениями.
        public Tile GetAutoTileByMask_ManyTypes(TileType[,] mask, int variant, int seed)
        {
            List<Tile> list = new List<Tile>();

            // проверка на то что тайл центральный.
            // Только центральные тайлы имеют вариант.
            //if (mask[0, 0] != mask[0, 1] ||
            //   mask[0, 0] != mask[1, 0] ||
            //   mask[0, 0] != mask[1, 1])
            //    variant = 0;

            Tuple<TileType, TileType, TileType, TileType, int> tup = new Tuple<TileType, TileType, TileType, TileType, int>(mask[0, 0], mask[1, 0], mask[0, 1], mask[1, 1], variant);

            if (!Dict_TileFromMask.Keys.Contains(tup))
            {
                // составление списка подходящих тайлов.
                foreach (Tile t in scn.tileSet)
                {
                    if (t.leftup == (int)mask[0, 0] &&
                        t.leftdown == (int)mask[0, 1] &&
                        t.rightup == (int)mask[1, 0] &&
                        t.rightdown == (int)mask[1, 1] &&
                        t.variant == variant)
                    {
                        list.Add(t);
                    }
                }

                if (list.Count > 0)
                {
                    Dict_TileFromMask.Add(tup, list);

                    var timeDiff = DateTime.UtcNow - new DateTime(1970, 1, 1);
                    long totaltime = timeDiff.Ticks;

                    Random rnd = new Random(unchecked((int)(seed + totaltime)));
                    return list[rnd.Next(list.Count)];
                }
                else
                {
                    Dict_TileFromMask.Add(tup, null);
                    return null;
                }
            }
            else
            {
                if (Dict_TileFromMask[tup] != null && Dict_TileFromMask[tup].Count > 0)
                {
                    var timeDiff = DateTime.UtcNow - new DateTime(1970, 1, 1);
                    long totaltime = timeDiff.Ticks;

                    Random rnd = new Random(unchecked((int)(seed + totaltime)));
                    return Dict_TileFromMask[tup][rnd.Next(Dict_TileFromMask[tup].Count)];
                }
                else
                    return null;
            }
        }

        /// из "большой" маски тайлов вытаскивает "булеву" маску одного тайла, для указаного типа.
        public bool[,] GetBoolMask(TileType[,] mask, int x, int y, TileType type)
        {
            bool[,] submask = new bool[2, 2];

            submask[0, 0] = (mask[x, y + 0] == type);
            submask[0, 1] = (mask[x, y + 1] == type);
            submask[1, 0] = (mask[x + 1, y + 0] == type);
            submask[1, 1] = (mask[x + 1, y + 1] == type);

            return submask;
        }

        // true - если булева маска состоит из одинаковых элементов.
        public bool BoolMaskIsFill(bool[,] bool_mask)
        {
            if (bool_mask[0, 0] == bool_mask[0, 1] &&
                bool_mask[0, 0] == bool_mask[1, 0] &&
                bool_mask[0, 0] == bool_mask[1, 1])
                return true;
            return false;
        }

        // накладывает mask1 на mask2
        // false - если маски не пересекаются.
        // x1, y1, x2, y2 - координаты соответствующих масок на глобальной карте.
        public bool MergeMasks(TileType[,] mask1, int x1, int y1, ref TileType[,] mask2, int x2, int y2)
        {
            bool cross = false;
            int w1 = mask1.GetLength(0);
            int h1 = mask1.GetLength(1);
            int w2 = mask2.GetLength(0);
            int h2 = mask2.GetLength(1);

            int dx = (x1 - x2) * 2;//Math.Abs(x1 - x2);
            int dy = (y1 - y2) * 2;//Math.Abs(y1 - y2);

            // пересекаются ли маски на глобальной карте.
            if (point_in_rect(x2, y2, x1, y1, w1 / 2, h1 / 2) ||
                point_in_rect(x2 + w2 / 2, y2, x1, y1, w1 / 2, h1 / 2) ||
                point_in_rect(x2, y2 + h2 / 2, x1, y1, w1 / 2, h1 / 2) ||
                point_in_rect(x2 + w2 / 2, y2 + h2 / 2, x1, y1, w1 / 2, h1 / 2))
                cross = true;

            // наложение
            for (int i = 0; i < w2; i++)
                for (int j = 0; j < h2; j++)
                    if (point_in_rect(i - dx, j - dy, 0, 0, w1, h1))
                        mask2[i, j] = mask1[i - dx, j - dy];

            return cross;
        }

        public void DEBUG_PrintMaskInConsole(TileType[,] mask)
        {
            DEBUG_PrintMaskInConsole(mask, 0, 0, mask.GetLength(0), mask.GetLength(1));
        }
        public void DEBUG_PrintMaskInConsole(TileType[,] mask, int x, int y, int ex, int ey)
        {
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);

            for (int j = y; j < ey; j++)
            {
                for (int i = x; i < ex; i++)
                {
                    if (point_in_rect(i, j, 0, 0, w, h) && mask[i, j] <= TileType.Ice1)
                    {
                        if (mask[i, j] != TileType.Ice && mask[i, j] != TileType.Ice1)
                            Console.Write(H2Strings.tile_types_str[(int)mask[i, j]][0] + "");
                        else
                            Console.Write("#");
                        //Console.Write(H2Strings.tile_type_to_short_str(mask[i, j]));
                        if ((int)mask[i, j] % 2 == 1)
                            Console.Write("1 ");
                        else
                            Console.Write("  ");
                    }
                    else
                        Console.Write("   ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        #endregion mask

        #region type in mask

        // поиск второго типа, принадлежащего одной ячейке(2x2) в маске,
        // с наименьшим числом шагов в пути между этими типами.
        // если клетка состоит из одного типа, то он и будет возвращен.
        public TileType GetOtherTypeInMask_MinPath(TileType[,] mask, int tx, int ty, TileType type1)
        {
            TileType tmp = mask[tx, ty];
            int min = 1000;

            for (int i = tx; i < tx + 2; i++)
                for (int j = ty; j < ty + 2; j++)
                {
                    // Внимание!! индекс [0] !!! что-то нужно предпринять. или это потом допилить.
                    if (Paths.GetPathLenght(type1, mask[i, j]) > 0 && Paths.GetPathLenght(type1, mask[i, j]) < min)
                    {
                        tmp = mask[i, j];
                        min = Paths.Dict[new Tuple<TileType, TileType>(type1, mask[i, j])][0].Count;
                    }
                }

            return tmp;
        }



        // Проверяет, содержтся ли type в маске в заданном прямоугольнике.
        public bool TypeInMask(TileType[,] mask, int tx, int ty, int w, int h, TileType type)
        {
            for (int i = tx; i < tx + w; i++)
                for (int j = ty; j < ty + h; j++)
                    if (mask[i, j] == type)
                        return true;

            return false;
        }
        // то же самое для одной ячейки(2x2). (одного тайла)
        public bool TypeInMask(TileType[,] mask, int tx, int ty, TileType type)
        {
            return TypeInMask(mask, tx, ty, 2, 2, type);
        }

        // false если в указанно участке маски имеется Unknown
        public bool MaskIsNotUnknown(TileType[,] mask, int tx, int ty, int w, int h)
        {
            bool flag = true;

            int mw = mask.GetLength(0);
            int mh = mask.GetLength(1);

            for (int i = tx; i < tx + w && i < mw; i++)
            {
                for (int j = ty; j < ty + h && j < mh; j++)
                {
                    if (mask[i, j] == TileType.Unknown)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                    break;
            }

            return flag;
        }

        // false если в указанно участке маски имеется Лед
        public bool MaskIsNotFrozen(TileType[,] mask, int tx, int ty, int w, int h)
        {
            bool flag = true;

            int mw = mask.GetLength(0);
            int mh = mask.GetLength(1);

            for (int i = tx; i < tx + w && i < mw; i++)
            {
                for (int j = ty; j < ty + h && j < mh; j++)
                {
                    if (mask[i, j] == TileType.Ice || mask[i, j] == TileType.Ice1)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                    break;
            }

            return flag;
        }

        #endregion type in mask


        // проверяет есть ли Лед в группе.
        public bool AllNotFrozen(List<Tile> tiles)
        {
            foreach (Tile tile in tiles)
            {
                if (tile.IsFrozen())
                    return false;
            }

            return true;
        }

        public bool Frozen(TileType tt)
        {
            if (tt == TileType.Ice || tt == TileType.Ice1)
                return true;
            else
                return false;
        }

        #endregion utils





        #region update
        public void Update()
        {
            UpdateDict();
            Paths = new AutoTilePaths(this);
            Paths.UpdatePaths();
            Dict_TileFromMask = new Dictionary<Tuple<TileType, TileType, TileType, TileType, int>, List<Tile>>();
        }

        public void UpdateDict()
        {

            autoTile_dict = new Dictionary<TileType, AutoTileDict>();
            int n = Enum.GetNames(typeof(TileType)).Length;
            for (int i = 0; i < n - 2; i++)
                autoTile_dict.Add((TileType)i, new AutoTileDict());

            foreach (Tile tile in scn.tileSet)
            {
                tile.isFrozen = null;

                if (!tile.IsFrozen())
                {

                    if (tile.Form == TileForm.Fill)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "Center", tile.variant);
                    }
                    else if (tile.Form == TileForm.Diagonal)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);
                    }
                    else if (tile.Form == TileForm.LeftDown)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "AntiDownLeft", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DownLeft", tile.variant);
                    }
                    else if (tile.Form == TileForm.RightDown)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "AntiDownRight", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DownRight", tile.variant);
                    }
                    else if (tile.Form == TileForm.Left)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "Left", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "Right", tile.variant);
                    }
                    else if (tile.Form == TileForm.Up)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "Up", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "Down", tile.variant);
                    }
                    else if (tile.Form == TileForm.LeftUp)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "UpLeft", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "AntiUpLeft", tile.variant);
                    }
                    else if (tile.Form == TileForm.RightUp)
                    {
                        autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "AntiUpRight", tile.variant);
                        autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "UpRight", tile.variant);
                    }
                    else
                        System.Windows.MessageBox.Show("Tile has unknown form");

                }
            }
        }

        public void FixWaterAutotiles()
        { // убираем со всех тайлов пометку вода. Кроме первых четырёх.

            System.Windows.MessageBoxResult res;

            if (Gl.CanOpenSecrets == false)
                res = MessageBoxResult.No;
            else
                res = System.Windows.MessageBox.Show("Is tileset universal?", "Water", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (res == MessageBoxResult.No)
            {
                foreach (Tile t in autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0])
                {
                    if (t.TileId >= 4)
                    {
                        t.leftdown = (int)TileType.Ice;
                        t.leftup = (int)TileType.Ice;
                        t.rightdown = (int)TileType.Ice1;
                        t.rightup = (int)TileType.Ice;

                        t.ExpectForm();
                    }
                }

                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Clear();

                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[0]);
                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[1]);
                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[2]);
                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[3]);
            }
            else if (res == MessageBoxResult.Yes)
            {
                foreach (Tile t in autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0])
                {
                    if (t.TileId == 0 || t.TileId >= 5)
                    {
                        t.leftdown = (int)TileType.Ice;
                        t.leftup = (int)TileType.Ice;
                        t.rightdown = (int)TileType.Ice1;
                        t.rightup = (int)TileType.Ice;

                        t.ExpectForm();
                    }
                }

                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Clear();

                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[1]);
                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[2]);
                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[3]);
                autoTile_dict[TileType.Water].Friends[TileType.Water]["Center"][0].Add(scn.tileSet[4]);
            }
        }

        public void FixLandGrassAutotiles()
        { // убираем со всех тайлов пометку вода. Кроме первых четырёх.

            Tile tile;


            System.Windows.MessageBoxResult res;

            if (Gl.CanOpenSecrets == false)
                res = MessageBoxResult.No;
            else
                res = System.Windows.MessageBox.Show("Is tileset universal?", "Water", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (res == MessageBoxResult.No)
            {
                tile = scn.tileSet[88];
                tile.leftup = (int)TileType.Sand;
                tile.leftdown = (int)TileType.Grass;
                tile.rightup = (int)TileType.Grass;
                tile.rightdown = (int)TileType.Sand;
                tile.ExpectForm();
                autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);

                tile = scn.tileSet[113];
                tile.leftup = (int)TileType.Sand;
                tile.leftdown = (int)TileType.Grass;
                tile.rightup = (int)TileType.Grass;
                tile.rightdown = (int)TileType.Sand;
                tile.ExpectForm();
                autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);

                tile = scn.tileSet[102];
                tile.leftup = (int)TileType.Grass;
                tile.leftdown = (int)TileType.Sand;
                tile.rightup = (int)TileType.Sand;
                tile.rightdown = (int)TileType.Grass;
                tile.ExpectForm();
                autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);

            }
            else if (res == MessageBoxResult.Yes)
            {
                tile = scn.tileSet[89];
                tile.leftup = (int)TileType.Sand;
                tile.leftdown = (int)TileType.Grass;
                tile.rightup = (int)TileType.Grass;
                tile.rightdown = (int)TileType.Sand;
                tile.ExpectForm();
                autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);

                tile = scn.tileSet[114];
                tile.leftup = (int)TileType.Sand;
                tile.leftdown = (int)TileType.Grass;
                tile.rightup = (int)TileType.Grass;
                tile.rightdown = (int)TileType.Sand;
                tile.ExpectForm();
                autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);

                tile = scn.tileSet[103];
                tile.leftup = (int)TileType.Grass;
                tile.leftdown = (int)TileType.Sand;
                tile.rightup = (int)TileType.Sand;
                tile.rightdown = (int)TileType.Grass;
                tile.ExpectForm();
                autoTile_dict[tile.Type1].AddFriend(tile, tile.Type2, "DiagonalDec", tile.variant);
                autoTile_dict[tile.Type2].AddFriend(tile, tile.Type1, "DiagonalInc", tile.variant);

            }
        }

        #endregion update




        public AutoTiler(Scena parent)
        {


            scn = parent;
            map = scn.map;
        }

    }
}
