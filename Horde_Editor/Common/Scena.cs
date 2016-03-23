using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;

using System.Threading;
using System.Windows.Threading;
//using System.Diagnostics;
//using System.Security.Principal;

using Horde_ClassLibrary;
using Horde_Editor.History;

namespace Horde_Editor.Common
{
    // В классе Scena глобальные переменные относящиеся напрямую к КОНКРЕТНОЙ сцене,
    // и методы для создания, сохранения или загрузки сцены.

    class Scena
    {
        // Инфа из файла сцены
        public Header head;
        public MyHeader myhead;
        public bool original = true; // редактиловась ли эта карта. Или нет(влияет на способ кодирования блоков)
        public Player[] players;

        public Tile[,] map;                       // карта(ландшафт)
        public TileType[,] main_mask;             // маска типов.
        public int[,] map_res;                    // карта(тут ресурс)
        public List<Unit>[,] map_units;   // карта(тут в каждой клетке может быть несколько юнитов) это ярлыки на юнитов которые находятся у каждого игрока

        public List<Unit> selected_units = new List<Unit>();         // юниты выделенные квадратом выделения

        public int size_map_x;        // размер карты в клеточках и равен обычно 128
        public int size_map_x_pixels; // размер карты в пикселях и равен size_map * 32
        public int size_map_y;        // 
        public int size_map_y_pixels; // 

        public List<Tile> spriteSet; // список с инфой о каждом спрайте из сцены + "друзья" тайлов/спрайтов
        public List<Tile> tileSet;   // неполный дубляж предыдущего списка. Сюда входят только тайлы. Т.е. спрайты с которых начинается анимация
        public List<Tile[,]> blocks = new List<Tile[,]>();

        public Ground_Sprites ground_sprites = new Ground_Sprites(); // тут все спрайты для этой карты

        //public BitmapImage Giga_bmp = new BitmapImage();
        public ImageSource Mega_bmp = new BitmapImage();

        // Name scena
        public string scena_path;  // путь к текущей сцене(для сохранения)
        public string scena_short_name;  // имя файла текущей сцены(для лейбла)

        public string uni_indexes_readed = "no"; // были ли прочитаны индексы в универсальном тайлсете? (через кнопку на Scena_Properties_Window)

        public Scena()
        {
            // Конструктор
        }

        // Дозагрузка карты в уже текущуюю - для создания мега-карты
        // Карта берется из зараннее созданного файла в папке редактора "\\universal\\original_maps\\scena_0x.h2m"
        // Остальное грузится из отсюда.
        public void Load_Scene_OverWriting(string path, string land_file, int offset_x, int offset_y)
        {
            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show("File \"" + path + "\" not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!File.Exists(land_file))
            {
                System.Windows.MessageBox.Show("File \"" + land_file + "\" not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FileStream inStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            FileStream inStream2 = File.Open(land_file, FileMode.Open, FileAccess.Read, FileShare.Read);


            Header head = new Header();
            head.read(inStream);

            byte[] tmp = new byte[4];
            int ind = -1;

            // Land из файла
            // и ресы из карты
            inStream.Position = 72;
            for (int i = 0; i < head.width; i++)
                for (int j = 0; j < head.height; j++)
                {
                    inStream2.Read(tmp, 0, 4);
                    ind = Utils.b2i(tmp, 0, 4);
                    if (i + offset_x < size_map_x && j + offset_y < size_map_y)
                        set_Ground_tile(i + offset_x, j + offset_y, ind, false);


                    inStream.Read(tmp, 0, 2);
                    inStream.Read(tmp, 0, 2);
                    if (i + offset_x < size_map_x && j + offset_y < size_map_y)
                        set_Resource_tile(j + offset_x, i + offset_y, 1, (int)((tmp[1] << 8) | tmp[0]), false);
                }

            #region players and units
            // читаем игроков и юнитов
            inStream.Position = head.offset2 + 20;
            for (int n = 0; n < 8; n++)
            {
                Player player = new Player();
                byte[] player_head = new byte[114];
                inStream.Read(player_head, 0, 114);
                player.ReadHead(player_head);

                byte[] player_cut = new byte[player.OBJECTS];
                inStream.Read(player_cut, 0, player.OBJECTS - 1);

                // юниты
                int j = 0;
                int tmp_size = 0;
                while (j < player.OBJECTS - 1) // минус один, т.к. последний байт не нужно читать
                {
                    tmp_size = Utils.b2i(player_cut, j + 16, 2);
                    byte[] unit_cut = Utils.SubArray(player_cut, j, tmp_size);

                    Unit u = new Unit();
                    u.ComposeFromArray(unit_cut);

                    if (u.mapX + offset_x < size_map_x && u.mapY + offset_y < size_map_y)
                        unit_create(u.mapX + offset_x - u.cfg.width_cells / 2, u.mapY + offset_y - u.cfg.height_cells / 2, u, false);

                    j += tmp_size;
                }

                inStream.Position = head.offset2 + 20 + (n + 1) * (114 + 65536) + 512 * n;

                inStream.Position += 512;
                //outStream.Close();
            }
            #endregion players and units

        }

        public bool Load_Scene(string path)
        {
            // Create a thread
            Thread newWindowThread = Gl.CreateProgressDetWindow("Load Scena");

            Gl.ProgressDetProgressChange(0);
            Gl.ProgressDetStatusChange("Start loading..");

            FileStream inStream = null;
            bool result = false;

            //try
            {
                scena_path = path;
                //file_number = path.Substring(path.Length - 6, 2);
                scena_short_name = Path.GetFileNameWithoutExtension(path);

                inStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                head = new Header();
                players = new Player[8];
                head.read(inStream);

                size_map_x = head.width;
                size_map_y = head.height;
                size_map_x_pixels = size_map_x * 32;
                size_map_y_pixels = size_map_y * 32;

                /*pictureBox2.Image = new Bitmap(size_map_x_pixels, size_map_y_pixels);
                pictureBox3.Image = new Bitmap(size_map_x_pixels, size_map_y_pixels);
                pictureBox_Rectangle.Image = new Bitmap(size_map_x_pixels, size_map_y_pixels);*/

                int n;                               // текущий номер ячейки
                int n_max = size_map_x * size_map_y; // ячеек всего
                int x, y;                            // координаты

                map = new Tile[size_map_x, size_map_y];
                main_mask = new TileType[size_map_x * 2, size_map_y * 2];
                map_res = new int[size_map_x, size_map_y];
                map_units = new List<Unit>[size_map_x, size_map_y];

                byte[] tmp = { 0, 0 };

                #region myheader
                // читаем мой заголовок(если он есть)
                inStream.Position = head.offset2 - 52;
                myhead = new MyHeader();
                myhead.read(inStream);

                if (myhead.vers == unchecked((int)ScenaVerses.v1))
                {
                    original = false;
                    Gl.main.secret_Button.IsChecked = false;
                }
                else if (myhead.vers == unchecked((int)ScenaVerses.secret))
                {
                    if (Gl.CanOpenSecrets)
                    {
                        original = false;
                        Gl.main.secret_Button.IsChecked = true;
                    }
                    else
                        throw new Exception("Instruction at referenced memory could not be read");
                    //Gl.main.Close();
                }
                else
                {
                    original = true;
                    Gl.main.secret_Button.IsChecked = false;
                }
                #endregion myheader


                // Чистим палитру спрайтов и загружаем заного
                //tileId_to_spriteId.Clear();

                // читаем графику.
                #region graphics

                // читаем тайлы и инфу о них
                inStream.Position = 72 + size_map_x * size_map_y * 4 + head.sprites_count * 2;

                byte[] tmp2 = new byte[8];
                Tile tmp_tile = new Tile();
                spriteSet = new List<Tile>();
                tileSet = new List<Tile>();

                int nframe = 0; // количество кадров в анимации
                int fist_frame = 0; // номер первого кадра в списке всех спрайтов
                int tile_pos = 0;
                int spr_pos = 0;
                for (int i = 0; i < head.sprites_count; i++) // номер спрайта
                {
                    inStream.Read(tmp2, 0, 8);
                    spriteSet.Add(new Tile());
                    spriteSet.Last().getFromArray(tmp2);
                    spriteSet.Last().FirstSpriteId = fist_frame;
                    spriteSet.Last().TileId = tile_pos;
                    spriteSet.Last().NFrame = nframe;
                    spriteSet.Last().SpriteId = spr_pos;
                    if (spriteSet.Last().leftup == (int)TileType.Ice &&            //
                        spriteSet.Last().leftup == spriteSet.Last().rightup &&     // это не обязательно.
                        spriteSet.Last().leftup == spriteSet.Last().rightdown &&   // но если встретился тайл полностью изо льда,
                        spriteSet.Last().leftup == spriteSet.Last().leftdown)      // то заменим одну четверть на лед1. Так принято в Орде.
                        spriteSet.Last().rightdown = (int)TileType.Ice1;           //
                    spriteSet.Last().ExpectForm();

                    nframe++; // номер кадра
                    spr_pos++; // номер спрайта

                    if (spriteSet.Last().sign != 1)
                    {
                        // добавить в список тайлов
                        tileSet.Add(spriteSet[fist_frame]);

                        //for (int j = fist_frame; j < fist_frame + nframe; j++)
                        //    spriteSet[j].FramesCount = nframe;
                        spriteSet[fist_frame].FramesCount = nframe;

                        nframe = 0;
                        fist_frame = i + 1; // устанавливаем номер первого кадра для следующей анимации
                        tile_pos++; // номер тайла
                    }
                }

                // цвета на миникарте
                byte c1, c2, c3;
                inStream.Position = 72 + size_map_x * size_map_y * 4;
                for (int i = 0; i < head.sprites_count; i++)
                {
                    inStream.Read(tmp, 0, 2);

                    c1 = (byte)((tmp[1] / 8) * 8);
                    //c2 = (byte)((tmp[1] % 8) * 32 + (((tmp[0] / 32) / 2) * 8));
                    c2 = (byte)((tmp[1] % 8) * 32 + ((tmp[0] / 32) * 4));
                    c3 = (byte)((tmp[0] % 32) * 8);

                    spriteSet[i].MinimapColor = Color.FromRgb(c1, c2, c3);
                    spriteSet[i].ScenaMinimapColor = (byte[])tmp.Clone();
                }

                // сами спрайты
                inStream.Position = 72 + size_map_x * size_map_y * 4 + head.sprites_count * 10;
                n = 0; // контроль пройденых спрайтов без учета анимации(все кадры)
                byte[] pixels16 = new byte[2048]; // сюда будем считывать данные о цветах и потом перерабатывать в 32-х битный цвет.
                byte[] pixels32 = new byte[4096]; // цвета 32-х битного изображения.
                for (int i = 0; i < spriteSet.Count; i++) // i - номер каждой отдельной анимации
                {
                    // Этот цикл выполняется пока не пройдем все кадры текущего тайла.
                    for (int k = 0; k < spriteSet[i].FramesCount; k++)
                    {
                        // читаем пиксели одного кадра
                        inStream.Read(pixels16, 0, 2048);
                        for (int j = 0; j < 1024; j++)
                        {
                            pixels32[j * 4 + 2] = (byte)((pixels16[j * 2 + 1] / 8) * 8);
                            pixels32[j * 4 + 1] = (byte)((pixels16[j * 2 + 1] % 8) * 32 + ((pixels16[j * 2] / 32) * 4));
                            //pixels32[j * 4 + 1] = (byte)((pixels16[j * 2 + 1] % 8) * 32 + (((pixels16[j * 2] / 32) / 2) * 8));
                            pixels32[j * 4] = (byte)((pixels16[j * 2] % 32) * 8);
                            pixels32[j * 4 + 3] = 255; // непрозрачный.
                        }

                        spriteSet[n].Sprite = BitmapSource.Create(32, 32, 96d, 96d, PixelFormats.Bgra32, null, pixels32, 128);
                        spriteSet[n].ScenaSprite = (byte[])pixels16.Clone();
                        n++; // инкремент на каждом спрайте
                    }
                }
                #endregion graphics


                #region ground
                // читаем Ground и ресурсы
                x = 0;
                y = 0;
                int tmp_i = 0;
                inStream.Position = 72;
                for (n = 0; n < n_max; n++)
                {
                    // читаем и заносим на карту
                    inStream.Read(tmp, 0, 2);
                    tmp_i = (int)((tmp[1] << 8) | tmp[0]);
                    map[x, y] = spriteSet[tmp_i];
                    inStream.Read(tmp, 0, 2);
                    map_res[x, y] = (int)((tmp[1] << 8) | tmp[0]);

                    x++;
                    if (x == size_map_x)
                    {
                        x = 0;
                        y += 1;
                    }
                }
                #endregion ground


                #region players and units
                // читаем игроков и юнитов
                inStream.Position = head.offset2 + 20;
                for (n = 0; n < 8; n++)
                {
                    players[n] = new Player();
                    byte[] player_head = new byte[114];
                    inStream.Read(player_head, 0, 114);
                    players[n].ReadHead(player_head);

                    byte[] player_cut = new byte[players[n].OBJECTS];
                    inStream.Read(player_cut, 0, players[n].OBJECTS - 1);

                    // юниты
                    int j = 0;
                    int t = 0;
                    int tmp_size = 0;
                    while (j < players[n].OBJECTS - 1) // минус один, т.к. последний байт не нужно читать
                    {
                        tmp_size = Utils.b2i(player_cut, j + 16, 2);
                        byte[] unit_cut = Utils.SubArray(player_cut, j, tmp_size);

                        players[n].units.Add(new Unit());
                        players[n].units[t].ComposeFromArray(unit_cut);
                        // здесь можно заносить юнитов на карту
                        players[n].units_count++;

                        j += tmp_size;

                        //players[i].units[t].dumpunit(outStream);
                        t++;
                    }

                    inStream.Position = head.offset2 + 20 + (n + 1) * (114 + 65536) + 512 * n;

                    inStream.Read(players[n].colors, 0, 512);
                    //outStream.Close();
                }
                #endregion players and units

                Gl.ProgressDetProgressChange(5);
                Gl.ProgressDetStatusChange("Main information readed. Full update..");

                FullUpdate();

                Gl.ProgressDetStatusChange("Drawing complete. Update palette..");

                #region palette and tiles window
                // обновляем палитру тайлов земли
                //RenderOptions.SetEdgeMode(Gl.main.Ground_Pallete_Image, EdgeMode.Aliased);
                // генерируем палитру.
                Gl.palette.InitLandPalette(this);
                Gl.palette.selected_tile = -1;
                Gl.palette.selected_sprite = -1;
                Canvas.SetLeft(Gl.main.ground_selector, -100);

                // апдейт окна тайлов, т.к. изменилось кол-во тайлов
                Gl.tiles_window.SelectedSprite = 0;
                Gl.tiles_window.SelectedTile = 0;
                Gl.tiles_window.FullUpdate();
                #endregion palette and tiles window

                Gl.ProgressDetProgressInc(5);
                Gl.ProgressDetStatusChange("Reading blocks..");

                //// Читаем блоки и формируем списки-друзей тайлов
                #region blocks
                int tmp_i2;
                if (original)
                {
                    #region original blocks
                    inStream.Position = head.offset2 - 16384;

                    byte[] block = new byte[64]; // блок состоит из 16-ти тайлов
                    //int[,] tiles = new int[4, 4];// блок в виде номеров тайлов
                    //uint whitespace = 0xFFFFFFFF; // пробел в блоке
                    bool flag = true; // true если блок пустой

                    Gl.blocks_window.Clear();

                    for (int i = 0; i < 256; i++)
                    {
                        Tile[,] new_block = new Tile[4, 4];
                        inStream.Read(block, 0, 64);
                        flag = true;

                        for (int j2 = 0; j2 < 4; j2++)     // y
                            for (int j1 = 0; j1 < 4; j1++) // x
                            {
                                tmp_i2 = Utils.b2i(block, j2 * 16 + j1 * 4, 4);
                                if (tmp_i2 != -1)
                                {
                                    new_block[j1, j2] = spriteSet[tmp_i2];
                                    flag = false;
                                }
                                else
                                    new_block[j1, j2] = Gl.blocks_window.blank_Tile;
                            }

                        // Если используем старый формат блоков, то пустые блоки тоже нужно записывать.
                        if (!Gl.NewFormat)
                            flag = false;

                        //if (i < 7)
                        if (!flag)
                        {
                            blocks.Add(new_block);
                            Gl.blocks_window.AddBlock(new_block);

                            #region friends
                            /*
                            int tx, ty;
                            for (int j2 = 0; j2 < 4; j2++)     // y
                                for (int j1 = 0; j1 < 4; j1++) // x
                                {
                                    if ((uint)new_block[j1, j2].SpriteId != whitespace)
                                    {
                                        tx = j1;
                                        ty = j2 - 1;     // верх
                                        if (ty >= 0 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsUp.Add(tileSet[new_block[tx, ty].TileId]);
                                        // что произошло в этой строке?
                                        // спрайту с индексом new_block[j1, j2] добавили "друга сверху" new_block[tx, ty]

                                        tx = j1;
                                        ty = j2 + 1;     // низ
                                        if (ty < 4 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsDown.Add(tileSet[new_block[tx, ty].TileId]);

                                        tx = j1 - 1;     // лево 
                                        ty = j2;
                                        if (tx >= 0 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsLeft.Add(tileSet[new_block[tx, ty].TileId]);

                                        tx = j1 + 1;     // право
                                        ty = j2;
                                        if (tx < 4 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsRight.Add(tileSet[new_block[tx, ty].TileId]);



                                        tx = j1 - 1;     // лево
                                        ty = j2 - 1;     // верх
                                        if (tx >= 0 && ty >= 0 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsUpLeft.Add(tileSet[new_block[tx, ty].TileId]);

                                        tx = j1 + 1;     // право
                                        ty = j2 - 1;     // верх
                                        if (tx < 4 && ty >= 0 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsUpRight.Add(tileSet[new_block[tx, ty].TileId]);

                                        tx = j1 - 1;     // лево
                                        ty = j2 + 1;     // низ
                                        if (tx >= 0 && ty < 4 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsDownLeft.Add(tileSet[new_block[tx, ty].TileId]);

                                        tx = j1 + 1;     // право
                                        ty = j2 + 1;     // низ
                                        if (tx < 4 && ty < 4 && (uint)new_block[tx, ty].SpriteId != whitespace)
                                            spriteSet[new_block[j1, j2].SpriteId].FriendsDownRight.Add(tileSet[new_block[tx, ty].TileId]);




                                    }
                                }

                            */
                            #endregion friends
                        }

                    }
                    #endregion original blocks
                }
                else
                {
                    #region my blocks
                    inStream.Position = myhead.blocks_offset;

                    byte[,] blocks_sizes = new byte[myhead.blocks_count, 2]; // 0 - width; 1 - height
                    byte[] tmp_size = new byte[64];
                    for (int i = 0; i < myhead.blocks_count; i++) // читаем размеры всех блоков
                    {
                        inStream.Read(tmp_size, 0, 2);
                        blocks_sizes[i, 0] = tmp_size[0];
                        blocks_sizes[i, 1] = tmp_size[1];
                    }


                    byte[] block; // сюда читаем блок из сцены
                    //uint whitespace = 0xFFFFFFFF; // пробел в блоке // вроде это -1
                    bool flag = true; // true если блок пустой

                    Gl.blocks_window.Clear();

                    int cur_w, cur_h; // размеры текущего блока
                    for (int i = 0; i < myhead.blocks_count; i++)
                    {
                        cur_w = blocks_sizes[i, 0];
                        cur_h = blocks_sizes[i, 1];
                        Tile[,] new_block = new Tile[cur_w, cur_h];
                        blocks.Add(new_block); // список блоков в виде номеров тайлов
                        block = new byte[cur_w * cur_h * 4];
                        inStream.Read(block, 0, cur_w * cur_h * 4);
                        flag = true;

                        for (int j2 = 0; j2 < cur_h; j2++)     // y
                            for (int j1 = 0; j1 < cur_w; j1++) // x
                            {
                                tmp_i2 = Utils.b2i(block, j2 * 4 * cur_w + j1 * 4, 4);
                                if (tmp_i2 != -1)
                                {
                                    new_block[j1, j2] = spriteSet[tmp_i2];
                                    flag = false;
                                }
                                else
                                    new_block[j1, j2] = Gl.blocks_window.blank_Tile;
                            }

                        //if (i < 7)
                        if (!flag)
                        {
                            Gl.blocks_window.AddBlock(new_block);

                        }

                    }
                    #endregion my blocks
                }
                Gl.blocks_window.UpdateBlockImages();
                #endregion blocks

                Gl.ProgressDetProgressInc(5);
                Gl.ProgressDetStatusChange("Compose autotiles..");

                // автотайлы
                UpdateAutoTiles();

                Gl.ProgressDetProgressChange(100);
                Gl.ProgressDetStatusChange("All done!");


                result = true;
            }
            //catch (Exception ex)
            {
               // System.Windows.MessageBox.Show(ex.Message, "exception", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                //App.Current.Shutdown();
                //Gl.main.Close();
            }
            //finally
            {
                if (inStream != null)
                    inStream.Close();

                Gl.CloseProgressDetWindow(newWindowThread);
            }

            return result;
        }


        public bool Save_Scene(string path)
        {
            // такой вот прокси-костыль для экспорта карт из Мега-Карты
            return Save_Scene_Part(path, 0, 0, int.MaxValue, int.MaxValue);
        }

        public bool Save_Scene_Part(string path, int offset_x, int offset_y, int save_width, int save_height)
        {
            FileStream outStream = null;

            bool success = false;

            try
            {
                // такое нужно для того чтобы в обычной карте можно было бы записать войска за края карты, а при делении мегакарты нет.
                int size_x = size_map_x;
                int size_y = size_map_y;
                if (save_width != int.MaxValue)
                    size_x = save_width;
                if (save_height != int.MaxValue)
                    size_y = save_height;


                int n;                               // текущий номер ячейки
                int n_max = size_x * size_y; // ячеек всего
                int x, y;                            // координаты

                outStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

                scena_path = Path.GetFullPath(path);
                scena_short_name = Path.GetFileNameWithoutExtension(path);

                // Пишем Ground
                outStream.Position = 72;
                x = 0;
                y = 0;
                for (n = 0; n < n_max; n++)
                {
                    if (map[x + offset_x, y + offset_y].SpriteId < spriteSet.Count)
                    {
                        outStream.WriteByte(Convert.ToByte(map[x + offset_x, y + offset_y].SpriteId & 0xFF));
                        outStream.WriteByte(Convert.ToByte(map[x + offset_x, y + offset_y].SpriteId >> 8));
                    }
                    else
                    {
                        outStream.WriteByte(0);
                        outStream.WriteByte(0);
                    }
                    outStream.WriteByte(Convert.ToByte(map_res[x + offset_x, y + offset_y] & 0xFF));
                    outStream.WriteByte(Convert.ToByte(map_res[x + offset_x, y + offset_y] >> 8));
                    x++;
                    if (x == size_x)
                    {
                        x = 0;
                        y += 1;
                    }
                }

                // обновляем количество спрайтов
                head.sprites_count = spriteSet.Count;

                // пишем цвета на миникарте
                outStream.Position = 72 + size_x * size_y * 4;
                for (int i = 0; i < head.sprites_count; i++)
                    outStream.Write(spriteSet[i].ScenaMinimapColor, 0, 2);

                // пишем инфу о спрайтах
                outStream.Position = 72 + size_x * size_y * 4 + head.sprites_count * 2;
                for (int i = 0; i < head.sprites_count; i++) // номер спрайта
                    outStream.Write(spriteSet[i].composeArray(), 0, 8);

                // пишем графон спрайтов
                outStream.Position = 72 + size_x * size_y * 4 + head.sprites_count * 10;
                for (int i = 0; i < head.sprites_count; i++)
                    outStream.Write(spriteSet[i].ScenaSprite, 0, 2048);


                // после изменения количества спрайтов надо поменять оффсеты
                //head.offset2 = 72 + size_x * size_y * 4 + head.sprites_count * 2058;

                // Пишем блоки
                if (!Gl.NewFormat)
                {
                    #region original blocks
                    // Пишем старые блоки
                    outStream.Position = 72 + size_x * size_y * 4 + head.sprites_count * 2058;//head.offset2 - 16384;
                    byte[] spr_index = new byte[4];
                    for (int i = 0; i < 256; i++)
                    {
                        for (int j2 = 0; j2 < 4; j2++)     // y
                            for (int j1 = 0; j1 < 4; j1++) // x
                            {
                                if (i < blocks.Count && blocks[i].GetLength(0) > j1 && blocks[i].GetLength(1) > j2)
                                    spr_index = Utils.i2b(blocks[i][j1, j2].SpriteId, 4);
                                else
                                    spr_index = Utils.i2b(unchecked((int)0xFFFFFFFF), 4);
                                outStream.WriteByte(spr_index[0]);
                                outStream.WriteByte(spr_index[1]);
                                outStream.WriteByte(spr_index[2]);
                                outStream.WriteByte(spr_index[3]);
                            }
                    }
                    #endregion original blocks
                }
                else
                {
                    #region new blocks
                    // Пишем новые блоки
                    //if (original)
                    //myhead.blocks_offset = head.offset2 - 16384;
                    myhead.blocks_offset = (int)outStream.Position;
                    outStream.Position = myhead.blocks_offset;
                    byte[] spr_index = new byte[4];
                    for (int i = 0; i < blocks.Count; i++) // пишем размеры блоков
                    {
                        outStream.WriteByte((byte)blocks[i].GetLength(0));
                        outStream.WriteByte((byte)blocks[i].GetLength(1));

                    }
                    int cur_w, cur_h;
                    for (int i = 0; i < blocks.Count; i++) // пишем сами блоки
                    {
                        cur_w = blocks[i].GetLength(0);
                        cur_h = blocks[i].GetLength(1);

                        for (int j2 = 0; j2 < cur_h; j2++)     // y
                            for (int j1 = 0; j1 < cur_w; j1++) // x
                            {
                                if (blocks[i][j1, j2].SpriteId < spriteSet.Count)
                                {
                                    spr_index = Utils.i2b(blocks[i][j1, j2].SpriteId, 4);
                                    outStream.WriteByte(spr_index[0]);
                                    outStream.WriteByte(spr_index[1]);
                                    outStream.WriteByte(spr_index[2]);
                                    outStream.WriteByte(spr_index[3]);
                                }
                                else
                                {
                                    outStream.WriteByte(0);
                                    outStream.WriteByte(0);
                                    outStream.WriteByte(0);
                                    outStream.WriteByte(0);
                                }
                            }
                    }
                    #endregion new blocks
                }

                // заполняем заголовки
                if (Gl.NewFormat)
                {
                    myhead.blocks_count = blocks.Count;
                    head.offset2 = (int)outStream.Position + 52;
                }
                else
                {
                    head.offset2 = (int)outStream.Position;
                }


                // Пишем заголовок
                head.offset1 = head.offset2 - 52; // неведомый сдвиг(я его принял как за сдвиг своего заголовка)
                head.width = size_x;
                head.height = size_y;
                outStream.Position = 0;
                head.write(outStream);

                // Пишем мой заголовок
                if (Gl.NewFormat)
                {
                    outStream.Position = head.offset1;
                    if (Gl.main.secret_Button.IsChecked == true)
                        myhead.vers = unchecked((int)ScenaVerses.secret);
                    else
                        myhead.vers = unchecked((int)ScenaVerses.v1);
                    myhead.write(outStream);
                }

                // Пишем игроков и юнитов
                outStream.Position = head.offset2;
                byte[] hz = new byte[20];
                hz[0] = 8;
                hz[1] = 5;
                outStream.Write(hz, 0, 20);

                int units_size;   // сколько байт потратили на юнитов
                bool large_data;  // было ли переполнение уже? (для MessageBox)
                int tmp_pl_count; // сколько редактор насчитал байт на юнитов (без ограничений)

                for (n = 0; n < 8; n++)
                {
                    // пишем юнитов
                    units_size = 0;
                    large_data = false;
                    tmp_pl_count = players[n].OBJECTS;
                    players[n].OBJECTS = 1;
                    outStream.Position = head.offset2 + 20 + (n + 1) * (114) + n * (512 + 65536);
                    for (int i = 0; i < players[n].units_count; i++)
                    {
                        units_size += players[n].units[i].size;
                        if (players[n].units[i].x >= offset_x * 32 && players[n].units[i].x < (offset_x + save_width) * 32 &&
                            players[n].units[i].y >= offset_y * 32 && players[n].units[i].y < (offset_y + save_height) * 32
                            || save_width == int.MaxValue)
                        {
                            players[n].units[i].x -= offset_x * 32;
                            players[n].units[i].y -= offset_y * 32;
                            if (units_size < 65536)
                            {
                                outStream.Write(players[n].units[i].RepresentToArray(), 0, players[n].units[i].size);
                                players[n].OBJECTS += players[n].units[i].size; // подсчитываем кол-во юнитов которое записали.
                            }
                            else
                            {
                                if (!large_data)
                                {
                                    MessageBox.Show("Units limit reached, but map will save.", "Player " + n, MessageBoxButton.OK, MessageBoxImage.Warning);
                                    large_data = true;
                                }
                            }
                            players[n].units[i].x += offset_x * 32;
                            players[n].units[i].y += offset_y * 32;
                        }
                    }

                    // пишем поля игрока
                    outStream.Position = head.offset2 + 20 + n * (512 + 65536 + 114);
                    byte[] player_head = players[n].ComposeHeadArray();
                    outStream.Write(player_head, 0, 114);
                    players[n].OBJECTS = tmp_pl_count; // записываем обратно (т.к. редактор без ограничений)

                    // пишем палитру
                    //outStream.Position = head.offset2 + 20 + (n + 1) * (114 + 65536 + 512);
                    outStream.Position = head.offset2 + 20 + (n + 1) * (114 + 65536) + n * 512;
                    outStream.Write(players[n].colors, 0, 512);
                }

                success = true;

                Gl.main.label_PlayerUnits.Content = "has units " + players[Gl.main.comboBox_Player.SelectedIndex].units_count;

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "exception", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                if (outStream != null)
                    outStream.Close();
            }
            return success;
        }

        public void Close_Scene()
        {
            ground_sprites.Clear();
            Close_Scene_without_spr();
        }
        public void Close_Scene_without_spr()
        { // Костыль нужно для изменения размера карты

            //Gl.main.image_Mega.close();
            Gl.main.image_Ground.close();
            Gl.main.image_Resources.close();
            Gl.main.image_Units.close();
            Gl.main.image_Grid.close();

            Gl.minimap.closemap();


            History.History.ClearAllHistory();
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        public void FullUpdate()
        {
            int n_max = size_map_x * size_map_y; // ячеек всего

            Gl.ProgressDetStatusChange("Mask update..");

            MainMaskUpdate();

            Gl.ProgressDetProgressInc(1);
            Gl.ProgressDetStatusChange("Setup canvas..");

            setupDraw();

            Gl.ProgressDetProgressInc(1);
            Gl.ProgressDetStatusChange("Draw map..");
            //double dp = (90 - ProgressDet.Current.Progress) / size_map_y * 4;
            double dp = (double)80 / (double)size_map_y * (double)4;

            for (int y = 0; y < size_map_y; y++)
            {
                for (int x = 0; x < size_map_x; x++)
                {
                    // отрисовка карты.
                    DrawToMap(x, y, ground_sprites.getBitmapImage(map[x, y].SpriteId, 0));
                    if (map_res[x, y] != 0)
                        DrawToResourcesMap(x, y, map_res[x, y]);


                    // чистим карту юнитов
                    if (map_units[x, y] != null)
                    {
                        map_units[x, y].Clear();
                    }
                    map_units[x, y] = null; // Говнокод?

                }

                if (y % 4 == 0)
                {
                    Gl.ProgressDetProgressInc(dp);
                    Gl.ProgressDetStatusChange("Draw map.. (" + y + "/" + size_map_y + ")");
                }
            }

            Gl.ProgressDetProgressInc(1);
            Gl.ProgressDetStatusChange("Draw units..");

            // Рисуем юнитов
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < players[i].units_count; j++)
                {
                    add_Unit_at_tile(players[i].units[j], true);
                }
            }

            Gl.ProgressDetProgressInc(1);
            Gl.ProgressDetStatusChange("Invalidate all..");

            Gl.minimap.update_rectangle_on_minimap();
            Gl.minimap.minimap_image.InvalidateVisual();
            //Gl.main.image_Mega.InvalidateVisual();
            Gl.main.image_Ground.InvalidateVisual();
            Gl.main.image_Units.InvalidateVisual();
            Gl.main.image_Resources.InvalidateVisual();
            //Gl.main.image_Grid.InvalidateVisual();
        }

        public void MainMaskUpdate()
        {
            main_mask = new TileType[size_map_x * 2, size_map_y * 2];
            MainMaskPartUpdate(0, 0, size_map_x, size_map_y);
        }
        public void MainMaskPartUpdate(int x, int y, int w, int h)
        {
            for (int tx = x; tx < x + w; tx++)
                for (int ty = y; ty < y + h; ty++)
                {
                    if (tx >= 0 && tx < size_map_x && ty >= 0 && ty < size_map_y)
                    {
                        main_mask[tx * 2, ty * 2] = (TileType)map[tx, ty].leftup;
                        main_mask[tx * 2 + 1, ty * 2] = (TileType)map[tx, ty].rightup;
                        main_mask[tx * 2, ty * 2 + 1] = (TileType)map[tx, ty].leftdown;
                        main_mask[tx * 2 + 1, ty * 2 + 1] = (TileType)map[tx, ty].rightdown;
                    }
                }
        }
        #region editor

        // установить тайл в клетку.
        // !!! без проверки на края карты !!!
        private void set_Ground_tile(int x, int y, int spr_id, bool write_to_history)
        {
            if (map[x, y].SpriteId != spr_id)
            {
                if (write_to_history)
                    toLandHistory(x, y, -1, map[x, y].SpriteId, spr_id);

                // изменяем массив карты
                map[x, y] = spriteSet[spr_id];                                 // заносим id спрайта в массив карты
                main_mask[x * 2, y * 2] = (TileType)map[x, y].leftup;
                main_mask[x * 2 + 1, y * 2] = (TileType)map[x, y].rightup;
                main_mask[x * 2, y * 2 + 1] = (TileType)map[x, y].leftdown;
                main_mask[x * 2 + 1, y * 2 + 1] = (TileType)map[x, y].rightdown;

                //Gl.stopWatch1.Restart();
                DrawToMap(x, y, ground_sprites.getBitmapImage(spr_id, 0));
                //Gl.stopWatch1.Stop();
                //string s1 = Gl.DEBUG_stopWatch1_value();
                //Console.WriteLine(s1 + " - " + i + ", " + j);


                // рекурсивная замена тайлов
                //checked_cells.Add((x + i) + ":" + (y + j));
                //recursiveSetGround(x + i, y + j, spr_id);
                //checked_cells = new List<string>(); // зануляем для следующей рекурсии
            }
        }


        // записать действие в историю. LandHistory
        private void toLandHistory(int x, int y, int blocks, int spr_id_from, int spr_id_to)
        {
            Dictionary<HistoryPoint, SetLandEvent> LandTmpEvents = Gl.main.LandTmpEvents;

            HistoryPoint point = new HistoryPoint(x, y);

            if (!LandTmpEvents.ContainsKey(point))
                LandTmpEvents.Add(point, new SetLandEvent(x, y, blocks, spr_id_from, spr_id_to));
            else
                LandTmpEvents[point].SprTo = spr_id_to;
        }



        // заполнить регион тайлами.
        public void set_Ground_region_tile(int x, int y, int size, int spr_id, bool write_to_history)
        {
            if (spr_id >= spriteSet.Count)
                return;
            if (size == 0)
                return;

            int sq_radius = size / 2; // наименьшее расстояние от центра квадрата до его стороны
            int even = 1 - size % 2; // поправка для четных размеров

            for (int i = -(sq_radius); i <= sq_radius - even; i++)
            {
                for (int j = -(sq_radius); j <= sq_radius - even; j++)
                {
                    if (x + i < size_map_x && x + i >= 0 && y + j < size_map_y && y + j >= 0)
                    {
                        set_Ground_tile(x + i, y + j, spr_id, write_to_history);

                    }
                }
            }
        }

        // Нарисовать блок тайлов
        public void set_Ground_block(int x, int y, int w, int h, int n, bool write_to_history)
        {
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    int tmp = Gl.curr_scena.blocks[n][i, j].SpriteId;
                    if (tmp != -1)
                        Gl.curr_scena.set_Ground_region_tile(x - w / 2 + i, y - h / 2 + j, 1, tmp, true);
                }
        }



        #region autotile

        AutoTiler autotiler;


        public void UpdateAutoTiles()
        {
            autotiler = new AutoTiler(this);

            autotiler.Update();
        }


        public void FixWaterAutotiles()
        { // убираем со всех тайлов пометку вода. Кроме первых четырёх.

            autotiler.FixWaterAutotiles();
        }


        public void FixLandGrassAutotiles()
        { // убираем со всех тайлов пометку вода. Кроме первых четырёх.

            autotiler.FixLandGrassAutotiles();
        }

        // в универсальном тайлсете нужно разделять траву и болото.
        public void FixMarshTiles()
        {
            // 8089 - # tile
            for (int i = 31443; i < spriteSet.Count; i++)
            {
                if (spriteSet[i].leftup == (int)TileType.Grass)
                    spriteSet[i].leftup = (int)TileType.Marsh;
                if (spriteSet[i].leftup == (int)TileType.Grass1)
                    spriteSet[i].leftup = (int)TileType.Marsh1;

                if (spriteSet[i].leftdown == (int)TileType.Grass)
                    spriteSet[i].leftdown = (int)TileType.Marsh;
                if (spriteSet[i].leftdown == (int)TileType.Grass1)
                    spriteSet[i].leftdown = (int)TileType.Marsh1;

                if (spriteSet[i].rightup == (int)TileType.Grass)
                    spriteSet[i].rightup = (int)TileType.Marsh;
                if (spriteSet[i].rightup == (int)TileType.Grass1)
                    spriteSet[i].rightup = (int)TileType.Marsh1;

                if (spriteSet[i].rightdown == (int)TileType.Grass)
                    spriteSet[i].rightdown = (int)TileType.Marsh;
                if (spriteSet[i].rightdown == (int)TileType.Grass1)
                    spriteSet[i].rightdown = (int)TileType.Marsh1;
            }
        }


        // тут скоро будет все правильно.
        public void set_Land_region_autotile(int x, int y, int size, AutoTileArgs args)
        {
            // аргумент в отдельные переменные
            TileType type1 = (TileType)args.type1;
            TileType type2 = (TileType)args.type2;
            if (type2 == TileType.Unknown || autotiler.BoolMaskIsFill(args.boolMask))
                type2 = type1;
            int variant = args.variant;

            if (size == 0)
                return;
            // ничего не делаем если типы разные и путь между ними больше 1-го
            if (autotiler.Paths.GetPathLenght(type1, type2) != 1 && type1 != type2)
                return;
            // ничего не делаем если варианта нет такого.
            if (autotiler.autoTile_dict[type1].GetCountTiles(type2, args.form, variant) < 1)
                return;

            int sq_radius = size / 2; // наименьшее расстояние от центра квадрата до его стороны
            int even = 1 - size % 2; // поправка для четных размеров

            // переход от центрального к лево-верхнему прямоугольнику
            int x_start = x - sq_radius;
            int y_start = y - sq_radius;
            int x_end = x + sq_radius - even;
            int y_end = y + sq_radius - even;
            int width = x_end - x_start + 1;
            int height = y_end - y_start + 1;

            int b_size = 1; // "толщина" границы

            int tx, ty;      // 
            int gx, gy;      // временные координаты
            int tw, th;      // 

            // Составляем всевозможные пары координат (x;y)
            List<Tuple<int, int>> pairs = new List<Tuple<int, int>>();

            for (int i = x_start; i <= x_end; i++)
                for (int j = y_start; j <= y_end; j++)
                    if (Gl.point_in_map(i, j))
                    {
                        pairs.Add(new Tuple<int, int>(i, j));
                    }

            // Вытаскиваем рандомом случайную координату, и делаем на ней автотайл
            Random rnd = new Random();
            while (pairs.Count != 0)
            {
                Tuple<int, int> tmp_tuple = pairs[rnd.Next(pairs.Count)];
                tx = tmp_tuple.Item1 * 2;
                ty = tmp_tuple.Item2 * 2;
                gx = tmp_tuple.Item1;
                gy = tmp_tuple.Item2;

                // проверка на "мороженность"
                List<Tile> inners = new List<Tile> { map[gx, gy] };

                if (autotiler.AllNotFrozen(inners) || args.aggressive)
                { // выбранный тайл не мороженный ( или агрессивный режим)
                    autotiler.PlaceAutotileToMask(ref main_mask, tx, ty, args);
                }
                pairs.Remove(tmp_tuple);

                var timeDiff = DateTime.UtcNow - new DateTime(1970, 1, 1);
                long totaltime = timeDiff.Ticks;
                rnd = new Random(unchecked((int)(gx * 1000 + gy + totaltime)));
            }


            //////////
            // Проверка того что наставили.


            if (!args.onlyInRect && // этот режим заведомо разрешает "неправильные" тайлы.
                args.recursive) // этот режим собственно виновник проверок
            {

                // тут можно было бы сделать список клеток в которые были внесены изменения, и затем проверять только их.

                int problems_count = 0; // количество найденых проблемных тайлов. Для того чтобы определить - продолжать проверку или нет.


                tx = (x_start);
                ty = (y_start);
                tw = (x_end - x_start + 1);
                th = (y_end - y_start + 1);

                bool bool_DEBUG = args.debug;

                if (bool_DEBUG)
                {
                    Console.WriteLine("before:");
                    autotiler.DEBUG_PrintMaskInConsole(main_mask, (tx - 1) * 2, (ty - 1) * 2, (tx + tw) * 2 + 2, (ty + th) * 2 + 2);
                }

                // проверка внешнего слоя, внутри прямоугольника из палитры.
                problems_count = autotiler.FindAndFixPathProblemCells(ref main_mask, tx, ty, tw, th);

                if (bool_DEBUG)
                {
                    Console.WriteLine("after (problems " + problems_count + "):");
                    autotiler.DEBUG_PrintMaskInConsole(main_mask, (tx - 1) * 2, (ty - 1) * 2, (tx + tw) * 2 + 2, (ty + th) * 2 + 2);
                }

                problems_count = 0;
                for (int q = 0; q < 3; q++)
                {
                    tx = (x_start - b_size);
                    ty = (y_start - b_size);
                    tw = (x_end - x_start + 2 * b_size + 1);
                    th = (y_end - y_start + 2 * b_size + 1);

                    if (bool_DEBUG)
                    {
                        Console.WriteLine("before_" + q + ":");
                        autotiler.DEBUG_PrintMaskInConsole(main_mask, (tx - 1) * 2, (ty - 1) * 2, (tx + tw) * 2 + 2, (ty + th) * 2 + 2);
                    }

                    problems_count += autotiler.FindAndFixCrossProblemCells(ref main_mask, tx, ty, tw, th);
                    problems_count += autotiler.FindAndFixPathProblemCells(ref main_mask, tx, ty, tw, th);

                    if (bool_DEBUG)
                    {
                        Console.WriteLine("after_" + q + " (problems " + problems_count + "):");
                        autotiler.DEBUG_PrintMaskInConsole(main_mask, (tx - 1) * 2, (ty - 1) * 2, (tx + tw) * 2 + 2, (ty + th) * 2 + 2);
                    }

                    if (problems_count == 0)
                        break;

                    b_size++; // расширяем границы для проверки.
                    problems_count = 0;
                }

                if (problems_count > 0 && args.onlyAll)
                {
                    MainMaskPartUpdate(x_start - b_size, y_start - b_size, width + b_size * 2, height + b_size * 2);
                    return;
                }

            }


            //////////
            // Получение непосредственно тайлов.

            // (перенести в метод автотайлера)

            // Пригодится:
            tx = 0; ty = 0;                         // координаты клетки в маске
            int var_currtile = variant;             // вариант текущего тайла
            Tile[,] tiles = new Tile[width + b_size * 2, height + b_size * 2];     // то что будем ставить
            TileType[,] tmp_mask = new TileType[2, 2];
            Tile tmp_tile;

            for (int i = x_start - b_size; i <= x_end + b_size; i++)
                for (int j = y_start - b_size; j <= y_end + b_size; j++)
                    if (Gl.point_in_map(i, j) &&
                        (!args.onlyInRect || (i != x_start - b_size && j != y_start - b_size && i != x_end + b_size && j != y_end + b_size))) // проверка на соседство, если режим "без соседей"
                    {
                        tx = i * 2;    // координаты клетки в маске
                        ty = j * 2;    // 

                        if (autotiler.MaskIsNotUnknown(main_mask, tx, ty, 2, 2) &&  // проверка, на то что маска подобрана
                            autotiler.MaskIsNotFrozen(main_mask, tx, ty, 2, 2))     // для этого тайла. ( и не ЛЕд)
                        {

                            // Проверка на то что тайл был изменен. (Только для тех кто за пределами прямоугольника из палитры)
                            autotiler.FillTileMaskAt(ref tmp_mask, 0, 0, map[i, j]);
                            if (!autotiler.point_in_rect(i, j, x_start, y_start, width, height) && // сейчас находимся снаружи прямоуголька.
                                main_mask[tx, ty] == tmp_mask[0, 0] &&         //
                                main_mask[tx + 1, ty] == tmp_mask[1, 0] &&     // и маска полностью совпадает.
                                main_mask[tx, ty + 1] == tmp_mask[0, 1] &&     //
                                main_mask[tx + 1, ty + 1] == tmp_mask[1, 1])   //                                
                            {
                                // тогда оставляем тайл старым.
                                //tmp_tile = map[i, j];
                                tmp_tile = null;
                            }
                            else // иначе подбираем новый.
                            {
                                // возвращаем вариант из палитры(т.к. мог измениться)
                                //var_currtile = variant;


                                // надо оставить стрый вариант тайла, если на карте стоит тайл типа, отличного от указанного в палитре. Т.е. например, выбрана трава вариант 2, не надо ставить лесу вариант 2.
                                //if (type1_currtile != type1)
                                //    var_currtile = map[i, j].variant;

                                // не надо изменять вариант "соседям"
                                //if (i < x_start || j < y_start || i > x_end || j > y_end)
                                //    var_currtile = map[i, j].variant;

                                TileType[,] sub_mask = new TileType[,] { { main_mask[tx, ty], main_mask[tx, ty + 1] }, { main_mask[tx + 1, ty], main_mask[tx + 1, ty + 1] } };
                                tmp_tile = autotiler.GetAutoTileByMask_ManyTypes(sub_mask, variant, i * 1000 + j);
                                if (tmp_tile == null) // если не нашли с тем вариантом, то поищем "обычный"
                                    tmp_tile = autotiler.GetAutoTileByMask_ManyTypes(sub_mask, 0, i * 1000 + j);

                                // КОСТЫЛЬ, для (рекурсии с установкой тайла из двух типов).
                                // плохая ситуация может возникнуть, когда после "фиксов" на маске образовались
                                // тайлы с тремя типами, а такого тайла не существует в тайлсете.
                                // В таком случае заменим "неправильные" четвертинки type2 из палитры.
                                /*if (tmp_tile == null && args.recursive && !args.onlyAll && !args.onlyInRect)
                                {
                                    //TileType tmp_type = autotiler.GetTypeFromMaskWithOneCross(main_mask, tx, ty, 2, 2, args.type1);
                                    TileType tmp_type = autotiler.GetOtherTypeInMask_MinPath(main_mask, tx, ty, args.type1);
                                    
                                    if (tmp_type != TileType.Unknown)
                                    {
                                        if (sub_mask[0, 0] != tmp_type)
                                            sub_mask[0, 0] = args.type1;
                                        if (sub_mask[0, 1] != tmp_type)
                                            sub_mask[0, 1] = args.type1;
                                        if (sub_mask[1, 0] != tmp_type)
                                            sub_mask[1, 0] = args.type1;
                                        if (sub_mask[1, 1] != tmp_type)
                                            sub_mask[1, 1] = args.type1;
                                        tmp_tile = autotiler.GetAutoTileByMask_ManyTypes(sub_mask, var_currtile, i * 1000 + j);
                                    }
                                }*/

                                // проверка на то что тайл найден
                                if (tmp_tile == null)
                                { // не смогли подобрать тайл, поэтому восстановим маску
                                    autotiler.FillTileMaskAt(ref main_mask, tx, ty, map[i, j]);

                                    // если режим - ТолькоВсе, то если тайл не найден, возвращаем маску и прерываем.
                                    if (args.onlyAll)
                                    {
                                        MainMaskPartUpdate(x_start - b_size, y_start - b_size, width + b_size * 2, height + b_size * 2);
                                        return;
                                    }
                                }
                            }

                            // найденый тайл - в базу и обнуляем.
                            tiles[i - x_start + b_size, j - y_start + b_size] = tmp_tile;
                            tmp_tile = null;
                        }
                        else
                        { // маска могла содержать Unknown, поэтому восстановим её
                            autotiler.FillTileMaskAt(ref main_mask, tx, ty, map[i, j]);

                            // если режим - ТолькоВсе, то если тайл не найден, возвращаем маску и прерываем.
                            if (args.onlyAll)
                            {
                                MainMaskPartUpdate(x_start - b_size, y_start - b_size, width + b_size * 2, height + b_size * 2);
                                return;
                            }
                        }
                    }


            //////////
            // Установка, полученных тайлов.


            for (int i = x_start - b_size; i <= x_end + b_size; i++)
                for (int j = y_start - b_size; j <= y_end + b_size; j++)
                    if (Gl.point_in_map(i, j))
                        if (tiles[i - x_start + b_size, j - y_start + b_size] != null) // проверка на то что тайл найден
                            set_Ground_tile(i, j, tiles[i - x_start + b_size, j - y_start + b_size].SpriteId, true); // ставим!


        }



        #endregion autotile



        public void add_Unit_at_tile(Unit unit, bool rewrite)
        {
            int x = unit.mapX;
            int y = unit.mapY;

            System.Drawing.Rectangle rect = UnitRect(unit);

            //string s1 = ""; // timing
            for (int i = rect.X; i < rect.X + rect.Width; i++)
                for (int j = rect.Y; j < rect.Y + rect.Height; j++)
                {
                    if (map_units[i, j] == null)
                        map_units[i, j] = new List<Unit>();
                    map_units[i, j].Add(unit);

                    //Gl.stopWatch1.Restart();
                    Gl.minimap.update_pixel_on_minimap(i, j);
                    //Gl.stopWatch1.Stop();
                    //s1 = Gl.DEBUG_stopWatch1_value();
                }


            // Графика
            BitmapImage bmp_i = Unit_Sprites.getBitmapImage(unit.folder, unit.spr_number, 0);

            int ux = unit.x;
            int uy = unit.y;
            int uw = unit.cfg.width_cells * 32;
            int uh = unit.cfg.height_cells * 32;


            //Gl.stopWatch2.Restart();
            //unit.link_to_sprite = Gl.main.image_Mega.add_unit_img(bmp_i, x, y);
            //unit.link_to_frame = Gl.main.image_Mega.add_unit_frame(ux - 32 / 2, uy - 32 / 2, 1 * 32, 1 * 32);
            unit.link_to_sprite = Gl.main.image_Units.add_unit_img(bmp_i, ux, uy, unit.cfg.z_level);

            int d = 2;

            if (Gl.main.unitsFramesColorView_checkBox.IsChecked)
                unit.link_to_frame = Gl.main.image_Units.add_unit_frame(ux - uw / 2 + d, uy - uh / 2 + d, uw - d * 2, uh - d * 2, Gl.getPlayer_color(unit.player));
            else
                unit.link_to_frame = Gl.main.image_Units.add_unit_frame(ux - uw / 2 + d, uy - uh / 2 + d, uw - d * 2, uh - d * 2, Colors.White);

            //Gl.stopWatch2.Stop();
            //string s2 = Gl.DEBUG_stopWatch2_value();
            //Console.WriteLine(s1 + "   |   " + s2);
        }

        public void set_Resource_tile(int x, int y, int size, int spr_id, bool write_to_history)
        {
            if (size == 0)
                return;

            int sq_radius = size / 2; // наименьшее расстояние от центра квадрата до его стороны
            int even = 1 - size % 2; // поправка для четных размеров

            for (int i = -(sq_radius); i <= sq_radius - even; i++)
            {
                for (int j = -(sq_radius); j <= sq_radius - even; j++)
                {
                    if (x + i < size_map_x && x + i >= 0 && y + j < size_map_y && y + j >= 0)
                    {
                        if (map_res[x + i, y + j] != spr_id)
                        {
                            if (write_to_history)
                                Gl.main.ResourceTmpEvents.Add(new SetResourceEvent(x + i, y + j, -1, map_res[x + i, y + j], spr_id));

                            // изменяем массив карты
                            map_res[x + i, y + j] = spr_id;                                 // заносим id спрайта в массив карты

                            DrawToResourcesMap(x + i, y + j, spr_id);
                        }

                    }
                }
            }
        }


        #endregion editor




        #region drawing
        public void setupDraw()
        {
            // Устанавливаем размеры всего на чем будем рисовать.
            Gl.main.canvas_Map.Width = size_map_x_pixels;
            Gl.main.canvas_Map.Height = size_map_y_pixels;

            /*Gl.main.image_Mega.Width = size_map_x_pixels;
            Gl.main.image_Mega.Height = size_map_y_pixels;*/
            Gl.main.image_Ground.Width = size_map_x_pixels;
            Gl.main.image_Ground.Height = size_map_y_pixels;
            Gl.main.image_Resources.Width = size_map_x_pixels;
            Gl.main.image_Resources.Height = size_map_y_pixels;
            Gl.main.image_Units.Width = size_map_x_pixels;
            Gl.main.image_Units.Height = size_map_y_pixels;
            Gl.main.image_Grid.Width = size_map_x_pixels;
            Gl.main.image_Grid.Height = size_map_y_pixels;

            Gl.minimap.initmap();

            //RenderOptions.SetEdgeMode(Gl.main.image_Mega, EdgeMode.Aliased);

            //Gl.main.image_Mega.init(size_map_x, size_map_y);

            Gl.main.image_Ground.init(size_map_x, size_map_y);
            Gl.main.image_Resources.init(size_map_x, size_map_y);
            Gl.main.image_Units.init();
            Gl.main.image_Grid.init(size_map_x, size_map_y);
        }


        public void DrawToMap(int x, int y, ImageSource bi)
        {
            System.Windows.Rect r = new System.Windows.Rect(x * 32, y * 32, 32, 32);
            //Gl.main.image_Mega.add_ground_img(ground_sprites.getBitmapImage(file_number, spr_id, 0), r);
            Gl.main.image_Ground.add_ground_img(bi, r);
            Gl.minimap.update_pixel_on_minimap(x, y);
        }


        public void update_unit_frame(Unit u, Color c, int s)
        {
            u.link_to_frame.Pen.Brush = new SolidColorBrush(c);
            u.link_to_frame.Pen.Thickness = s;
            u.link_to_frame.Pen.Brush.Freeze();
        }

        public void update_unit_frame_to_player_color(Unit u)
        {

            if (Gl.main.unitsFramesColorView_checkBox.IsChecked)
                update_unit_frame(u, Gl.getPlayer_color(u.player), 1);
            else
                update_unit_frame(u, Colors.White, 1);

            unit_frame_update_pos(u, false);
        }

        private void unit_frame_update_pos(Unit u, bool selected)
        {
            int ux = u.x;
            int uy = u.y;
            int uw = u.cfg.width_cells * 32;
            int uh = u.cfg.height_cells * 32;

            int d = 2;
            if (selected)
                d = 1;

            Gl.main.image_Units.unit_frame_update_pos(u.link_to_frame, ux - uw / 2 + d, uy - uh / 2 + d, uw - d * 2, uh - d * 2);
        }

        public void update_selected_unit_frame(Unit u)
        {
            if (Gl.main.unitsFramesColorView_checkBox.IsChecked)
                update_unit_frame(u, Color.FromArgb(255, 0, 255, 0), 2);
            else
                update_unit_frame(u, Color.FromArgb(255, 0, 255, 0), 1);
            //Gl.main.image_Units.unit_frame_to_top(u.link_to_frame);

            unit_frame_update_pos(u, true);
        }

        public void DrawToResourcesMap(int x, int y, int spr_id)
        {
            System.Windows.Rect rect = new System.Windows.Rect(x * 32, y * 32, 32, 32);

            // рисуем
            //Gl.main.image_Mega.add_resource_fillrect(rect, spr_id);
            Gl.main.image_Resources.add_resource_fillrect(rect, spr_id);

        }

        public void update_grid(int step)
        {

            System.Windows.Point p1 = new System.Windows.Point();
            System.Windows.Point p2 = new System.Windows.Point();

            int n = Math.Max(size_map_x, size_map_y);

            // очищаем
            //Gl.main.image_Mega.eraseAllLines();
            Gl.main.image_Grid.eraseAllLines();

            for (int i = 0; i < n; i++)
            {
                if (step != 0 && i % step == 0)
                {
                    p1.X = i * 32 + 0.5; // 0.5 поправка на толщину
                    p1.Y = 0.5;
                    p2.X = i * 32 + 0.5;
                    p2.Y = size_map_y * 32 + 0.5;
                    //Gl.main.image_Mega.add_line(p1, p2);
                    Gl.main.image_Grid.add_line(p1, p2);

                    p1.X = 0.5;
                    p1.Y = i * 32 + 0.5;
                    p2.X = size_map_x * 32 + 0.5;
                    p2.Y = i * 32 + 0.5;
                    //Gl.main.image_Mega.add_line(p1, p2);
                    Gl.main.image_Grid.add_line(p1, p2);
                }
            }
            //Gl.main.image_Mega.InvalidateVisual();
            Gl.main.image_Grid.InvalidateVisual();
        }

        #endregion








        #region units

        // True если можно поставить юнита с конфигом cfg в клетку x;y
        public bool MayPlaceUnitInCell(UnitConfig cfg, int x, int y)
        {
            if (map_units[x, y] != null)
                foreach (Unit u in map_units[x, y])
                {
                    if (u.cfg.z_level == cfg.z_level)
                        return false;
                }
            return true;
        }

        public System.Drawing.Rectangle UnitRect(Unit unit)
        {

            int left = unit.mapX - unit.cfg.width_cells / 2;
            int widht = unit.cfg.width_cells;
            int up = unit.mapY - unit.cfg.height_cells / 2;
            int height = unit.cfg.height_cells;

            if (left < 0)
                left = 0;
            if (up < 0)
                up = 0;
            if (left + widht > size_map_x)
                widht = size_map_x - left;
            if (up + height > size_map_y)
                height = size_map_y - up;

            return new System.Drawing.Rectangle(left, up, widht, height);
        }

        public Unit unit_create(int x, int y, int u_t, int u_id, int p, bool write_to_history)
        {
            bool isEmpty = true;

            if (u_t == 5)
                u_t = 4;

            int widht = H2_configs.cfgs[u_t][u_id].width_cells;
            int height = H2_configs.cfgs[u_t][u_id].height_cells;

            if (x + widht > size_map_x || x < 0)
                return null;
            if (y + height > size_map_y || y < 0)
                return null;

            //string s1 = ""; // timing
            for (int i = 0; i < widht; i++)
                for (int j = 0; j < height; j++)
                    //if (map_units[x + i, y + j] != null)
                    if (!MayPlaceUnitInCell(H2_configs.cfgs[u_t][u_id], x + i, y + j) && Gl.palette.one_unit_on_layer)
                        isEmpty = false;

            if (isEmpty)
            {
                Unit tmp_u = new Unit();

                // в массив карты
                //map_units[x, y] = new List<Unit>(); // объявляем т.к. клетка была пустая
                //map_units[x, y].Add(tmp_u);

                tmp_u.InitNew(x, y, u_t, u_id, p);
                players[tmp_u.player].add_unit(tmp_u);

                add_Unit_at_tile(tmp_u, true);



                if (tmp_u != null)
                {
                    if (write_to_history)
                        Gl.main.UnitsTmpEvents.Add(new SetUnitEvent(tmp_u, true));

                    if (tmp_u.folder == 2 && tmp_u.unit_id == 5)
                    {
                        fence_updated.Clear();
                        recursive_fence_update(tmp_u, !Gl.palette.recursive_units);
                    }
                    else if (tmp_u.folder == 2 && tmp_u.unit_id == 7)
                    {
                        bridges_updated.Clear();
                        recursive_bridge_update(tmp_u, !Gl.palette.recursive_units);

                    }
                }
                if (p == Gl.main.comboBox_Player.SelectedIndex)
                    Gl.main.label_PlayerUnits.Content = "has units " + players[p].units_count;

                return tmp_u;
            }
            return null;
        }

        public Unit unit_create(int x, int y, Unit u, bool write_to_history)
        {

            bool isEmpty = true;

            int widht = u.cfg.width_cells;
            int height = u.cfg.height_cells;

            if (x + widht > size_map_x || x < 0)
                return null;
            if (y + height > size_map_y || y < 0)
                return null;

            for (int i = 0; i < widht; i++)
                for (int j = 0; j < height; j++)
                    if (!MayPlaceUnitInCell(u.cfg, x + i, y + j) && Gl.palette.one_unit_on_layer)
                        isEmpty = false;

            if (isEmpty)
            {
                players[u.player].add_unit(u);
                u.x = x * 32 + 32 * widht / 2;
                u.y = y * 32 + 32 * height / 2;
                u.update_coordinates();
                u.mapX = u.x / 32;
                u.mapY = u.y / 32;

                add_Unit_at_tile(u, true);

                if (u != null)
                {
                    if (write_to_history)
                        Gl.main.UnitsTmpEvents.Add(new SetUnitEvent(u, true));
                }
                if (u.player == Gl.main.comboBox_Player.SelectedIndex)
                    Gl.main.label_PlayerUnits.Content = "has units " + players[u.player].units_count;

                return u;
            }
            return null;
        }

        List<Unit> fence_updated = new List<Unit>();
        public void recursive_fence_update(Unit tmp_u, bool onlyone)
        {

            if (fence_updated.Contains(tmp_u))
                return;
            fence_updated.Add(tmp_u);

            int x = tmp_u.x / 32;
            int y = tmp_u.y / 32;

            if (tmp_u != null)
            {
                if (tmp_u.folder == 2 && tmp_u.unit_id == 5)
                {

                    Unit left_u;
                    Unit right_u;
                    Unit up_u;
                    Unit down_u;
                    bool left = false;
                    bool right = false;
                    bool up = false;
                    bool down = false;

                    if (x > 0 && map_units[x - 1, y] != null)
                    {
                        left_u = map_units[x - 1, y][0];
                        left = left_u.folder == 2 && left_u.unit_id == 5;
                    }
                    else
                        left_u = null;

                    if (x < size_map_x - 1 && map_units[x + 1, y] != null)
                    {
                        right_u = map_units[x + 1, y][0];
                        right = right_u.folder == 2 && right_u.unit_id == 5;
                    }
                    else
                        right_u = null;

                    if (y > 0 && map_units[x, y - 1] != null)
                    {
                        up_u = map_units[x, y - 1][0];
                        up = up_u.folder == 2 && up_u.unit_id == 5;
                    }
                    else
                        up_u = null;

                    if (y < size_map_y - 1 && map_units[x, y + 1] != null)
                    {
                        down_u = map_units[x, y + 1][0];
                        down = down_u.folder == 2 && down_u.unit_id == 5;
                    }
                    else
                        down_u = null;

                    int tmp_spr_number = tmp_u.spr_number;

                    if ((left || right))// && !up && !down)
                    {
                        tmp_u.spr_number = 951;
                        tmp_u.t_UPropInformation = "0 0";
                    }

                    if ((up || down))// && !left && !right)
                    {
                        tmp_u.spr_number = 986;
                        tmp_u.t_UPropInformation = "0 5";
                    }

                    if (left && down && !right && !up)
                    {
                        tmp_u.spr_number = 958;
                        tmp_u.t_UPropInformation = "0 1";
                    }

                    if (left && up && !right && !down)
                    {
                        tmp_u.spr_number = 965;
                        tmp_u.t_UPropInformation = "0 2";
                    }

                    if (right && up && !left && !down)
                    {
                        tmp_u.spr_number = 972;
                        tmp_u.t_UPropInformation = "0 3";
                    }

                    if (right && down && !left && !up)
                    {
                        tmp_u.spr_number = 979;
                        tmp_u.t_UPropInformation = "0 4";
                    }


                    if (tmp_spr_number != tmp_u.spr_number)
                    {
                        Gl.main.image_Units.delete_unit_img(tmp_u.link_to_sprite);
                        BitmapImage bmp_i = Unit_Sprites.getBitmapImage(tmp_u.folder, tmp_u.spr_number, 0);
                        int ux = tmp_u.x;
                        int uy = tmp_u.y;
                        tmp_u.link_to_sprite = Gl.main.image_Units.add_unit_img(bmp_i, ux, uy, tmp_u.cfg.z_level);
                    }

                    if (!onlyone)
                    {
                        if (left_u != null)
                            recursive_fence_update(left_u, false);
                        if (right_u != null)
                            recursive_fence_update(right_u, false);
                        if (up_u != null)
                            recursive_fence_update(up_u, false);
                        if (down_u != null)
                            recursive_fence_update(down_u, false);
                    }

                }
            }
        }

        List<Unit> bridges_updated = new List<Unit>();
        public void recursive_bridge_update(Unit tmp_u, bool onlyone)
        {

            if (bridges_updated.Contains(tmp_u))
                return;
            bridges_updated.Add(tmp_u);

            int x = tmp_u.x / 32;
            int y = tmp_u.y / 32;

            if (tmp_u != null)
            {
                if (tmp_u.folder == 2 && tmp_u.unit_id == 7)
                {

                    Unit left_u;
                    Unit right_u;
                    Unit up_u;
                    Unit down_u;
                    bool left = false;
                    bool right = false;
                    bool up = false;
                    bool down = false;

                    if (x > 1 && map_units[x - 2, y] != null)
                    {
                        left_u = map_units[x - 2, y][0];
                        left = left_u.folder == 2 && left_u.unit_id == 7;
                    }
                    else
                        left_u = null;

                    if (x < size_map_x - 1 && map_units[x + 1, y] != null)
                    {
                        right_u = map_units[x + 1, y][0];
                        right = right_u.folder == 2 && right_u.unit_id == 7;
                    }
                    else
                        right_u = null;

                    if (y > 1 && map_units[x, y - 2] != null)
                    {
                        up_u = map_units[x, y - 2][0];
                        up = up_u.folder == 2 && up_u.unit_id == 7;
                    }
                    else
                        up_u = null;

                    if (y < size_map_y - 1 && map_units[x, y + 1] != null)
                    {
                        down_u = map_units[x, y + 1][0];
                        down = down_u.folder == 2 && down_u.unit_id == 7;
                    }
                    else
                        down_u = null;

                    int tmp_spr_number = tmp_u.spr_number;

                    if (left && right)
                    {
                        tmp_u.spr_number = 1128;
                        tmp_u.t_UPropInformation = "0 5";
                    }

                    if (up && down)
                    {
                        tmp_u.spr_number = 1053;
                        tmp_u.t_UPropInformation = "0 0";
                    }

                    if (left && !(up || down) && !right)
                    {
                        tmp_u.spr_number = 1098;
                        tmp_u.t_UPropInformation = "0 3";
                    }

                    if (right && !(up || down) && !left)
                    {
                        tmp_u.spr_number = 1113;
                        tmp_u.t_UPropInformation = "0 4";
                    }

                    if (up && !down)
                    {
                        tmp_u.spr_number = 1083;
                        tmp_u.t_UPropInformation = "0 2";
                    }

                    if (down && !up)
                    {
                        tmp_u.spr_number = 1068;
                        tmp_u.t_UPropInformation = "0 1";
                    }


                    if (tmp_spr_number != tmp_u.spr_number)
                    {
                        Gl.main.image_Units.delete_unit_img(tmp_u.link_to_sprite);
                        BitmapImage bmp_i = Unit_Sprites.getBitmapImage(tmp_u.folder, tmp_u.spr_number, 0);
                        int ux = tmp_u.x;
                        int uy = tmp_u.y;
                        tmp_u.link_to_sprite = Gl.main.image_Units.add_unit_img(bmp_i, ux, uy, tmp_u.cfg.z_level);
                    }

                    if (!onlyone)
                    {
                        if (left_u != null)
                            recursive_bridge_update(left_u, false);
                        if (right_u != null)
                            recursive_bridge_update(right_u, false);
                        if (up_u != null)
                            recursive_bridge_update(up_u, false);
                        if (down_u != null)
                            recursive_bridge_update(down_u, false);
                    }
                }
            }
        }

        public void unit_changePlayer(Unit u, int p)
        {
            players[u.player].remove_unit(u);
            players[p].add_unit(u);
            u.change_player((byte)p);

            System.Drawing.Rectangle rect = UnitRect(u);

            for (int i = rect.X; i < rect.X + rect.Width; i++)
                for (int j = rect.Y; j < rect.Y + rect.Height; j++)
                {
                    Gl.minimap.update_pixel_on_minimap(i, j);
                }

            if (u.player == Gl.main.comboBox_Player.SelectedIndex || p == Gl.main.comboBox_Player.SelectedIndex)
                Gl.main.label_PlayerUnits.Content = "has units " + players[u.player].units_count;
        }
        public void unit_delete(Unit u, bool write_to_history)
        {
            System.Drawing.Rectangle rect = UnitRect(u);

            for (int i = rect.X; i < rect.X + rect.Width; i++)
                for (int j = rect.Y; j < rect.Y + rect.Height; j++)
                {
                    map_units[i, j].Remove(u);      // удаляем с массива карты
                    if (map_units[i, j].Count() == 0) // удаляем клетку карты юнитов если это был единственный юнит
                        map_units[i, j] = null;

                    Gl.minimap.update_pixel_on_minimap(i, j);
                }

            // Удаляем картинку
            //Gl.main.image_Mega.delete_unit_img(unit.link_to_sprite);
            //Gl.main.image_Mega.delete_unit_frame(unit.link_to_frame);
            Gl.main.image_Units.delete_unit_img(u.link_to_sprite);
            Gl.main.image_Units.delete_unit_frame(u.link_to_frame);
            //Gl.minimap.update_pixel_on_minimap(u.mapX, u.mapY);

            players[u.player].remove_unit(u); // удаляем юнита у игрока

            if (write_to_history)
                Gl.main.UnitsTmpEvents.Add(new SetUnitEvent(u, false));

            if (u.player == Gl.main.comboBox_Player.SelectedIndex)
                Gl.main.label_PlayerUnits.Content = "has units " + players[u.player].units_count;
        }
        #endregion

    }



    public class Header
    {
        public string signature;
        public int offset1;
        public int l2; //
        public int offset2;
        public int bytes_to_players;
        public string str2; //
        public int height;
        public int width;
        public int sprites_count;
        public int l6; //
        public int l7; //

        public void read(FileStream instr)
        {
            byte[] buff = new byte[24];

            instr.Read(buff, 0, 12);
            signature = Encoding.ASCII.GetString(buff, 0, 12);

            instr.Read(buff, 0, 4);
            offset1 = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            l2 = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            offset2 = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            bytes_to_players = Utils.b2i(buff, 0, 4);

            instr.Read(buff, 0, 24);
            str2 = Encoding.ASCII.GetString(buff, 0, 24);

            instr.Read(buff, 0, 4);
            height = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            width = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            sprites_count = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            l6 = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            l7 = Utils.b2i(buff, 0, 4);

        }


        public void write(FileStream outstr)
        {
            outstr.Write(Encoding.ASCII.GetBytes(signature.ToCharArray()), 0, 12);

            outstr.Write(Utils.i2b(offset1, 4), 0, 4);
            outstr.Write(Utils.i2b(l2, 4), 0, 4);
            outstr.Write(Utils.i2b(offset2, 4), 0, 4);
            outstr.Write(Utils.i2b(bytes_to_players, 4), 0, 4);

            outstr.Write(Encoding.ASCII.GetBytes(str2.ToCharArray()), 0, 24);

            outstr.Write(Utils.i2b(height, 4), 0, 4);
            outstr.Write(Utils.i2b(width, 4), 0, 4);
            outstr.Write(Utils.i2b(sprites_count, 4), 0, 4);
            outstr.Write(Utils.i2b(l6, 4), 0, 4);
            outstr.Write(Utils.i2b(l7, 4), 0, 4);
        }
    }

    // встроенный мною заголовок. Будет находиться по адресу head.offset1
    public class MyHeader
    {
        public int vers; // версия, а за одно и сигнатура
        public int blocks_count;
        public int blocks_offset;
        public byte[] other = new byte[40];

        public void read(FileStream instr)
        {
            byte[] buff = new byte[40];

            instr.Read(buff, 0, 4);
            vers = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            blocks_count = Utils.b2i(buff, 0, 4);
            instr.Read(buff, 0, 4);
            blocks_offset = Utils.b2i(buff, 0, 4);

            instr.Read(other, 0, 40);

        }


        public void write(FileStream outstr)
        {
            outstr.Write(Utils.i2b(vers, 4), 0, 4);
            outstr.Write(Utils.i2b(blocks_count, 4), 0, 4);
            outstr.Write(Utils.i2b(blocks_offset, 4), 0, 4);

            outstr.Write(other, 0, 40);
        }
    }

    enum ScenaVerses
    {
        v1 = unchecked((int)0xfefefefe),
        secret = unchecked((int)0xcafbbfac)
    }
}
