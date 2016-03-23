using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.IO;

using Horde_Editor.Common;
using Horde_ClassLibrary;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Scena_Properties_Window.xaml
    /// </summary>
    public partial class Scena_Properties_Window : Window
    {
        public Scena_Properties_Window()
        {
            InitializeComponent();
        }

        public bool closing = false; // флаг - навсегда ли закрывается миникарта? (true когда закрывается главная форма)
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false) // КОСТЫЛЬ - нужен для того что бы крестом миникарта не закрывалась, а вы выключении гл. формы закрывалась
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void Resize_Button_Click(object sender, RoutedEventArgs e)
        {
            int w;
            int h;
            if (!Int32.TryParse(Width_TextBox.Text, out w))
                return;
            if (!Int32.TryParse(Height_TextBox.Text, out h))
                return;
            if (w < 0 || h < 0)
                return;

            // Create a thread
            Thread newWindowThread = Gl.CreateProgressDetWindow("Resizing...");

            Scena scn = Gl.curr_scena;

            Gl.ProgressDetProgressChange(0);
            Gl.ProgressDetStatusChange("Delete excess units..");

            for (int x = 0; x < scn.size_map_x; x++)
                for (int y = 0; y < scn.size_map_y; y++)
                    if (x >= w || y >= h)
                        while (scn.map_units[x, y] != null && scn.map_units[x, y].Count > 0)
                            scn.unit_delete(scn.map_units[x, y][0], false);

            Gl.ProgressDetProgressInc(2);
            Gl.ProgressDetStatusChange("Resizing arrays..");

            scn.size_map_x = w;
            scn.size_map_y = h;
            scn.size_map_x_pixels = w * 32;
            scn.size_map_y_pixels = h * 32;

            scn.map = Utils.ResizeArray<Tile>(scn.map, w, h, scn.tileSet[0]);
            scn.map_res = Utils.ResizeArray<int>(scn.map_res, w, h, 0);
            scn.map_units = Utils.ResizeArray<List<Unit>>(scn.map_units, w, h, null);

            Gl.ProgressDetProgressInc(3);
            Gl.ProgressDetStatusChange("Full update..");

            scn.Close_Scene_without_spr();

            scn.FullUpdate();

            Gl.ProgressDetProgressChange(100);
            Gl.ProgressDetStatusChange("All done!");

            Gl.CloseProgressDetWindow(newWindowThread);
        }

        public void FullUpdate()
        {
            Width_TextBox.Text = Gl.curr_scena.size_map_x.ToString();
            Height_TextBox.Text = Gl.curr_scena.size_map_y.ToString();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void FixWater_Button_Click(object sender, RoutedEventArgs e)
        {
            Gl.curr_scena.FixWaterAutotiles();
            //MessageBox.Show("Water autotiles fixed.", "Fixer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateAutotiles_Button_Click(object sender, RoutedEventArgs e)
        {
            Gl.curr_scena.UpdateAutoTiles();
            //MessageBox.Show("Autotiles was update.", "Updater", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FixLandGrass_Button_Click(object sender, RoutedEventArgs e)
        {
            Gl.curr_scena.FixLandGrassAutotiles();
            //MessageBox.Show("Land-Grass diagonal autotiles fixed.", "Fixer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FixUniversalMarsh_Button_Click(object sender, RoutedEventArgs e)
        {
            Gl.curr_scena.FixMarshTiles();
            //MessageBox.Show("Marsh tiles fixed.", "Fixer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FillMap_autotile_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.palette.autotile_args.type1 == TileType.Unknown)
                return;

            Scena scn = Gl.curr_scena;

            int step = Gl.palette.curr_ground_brush_size;
            int size = Gl.palette.curr_ground_brush_size;

            bool IsOne = step == 1;
            if (IsOne)
                step = 3;

            if (step == 0)
            {
                step = Gl.palette.last_ground_brush_size;
                if (step == 0)
                    step = 1;
            }

            Thread newWindowThread = Gl.CreateProgressDetWindow("Filling...");

            Gl.ProgressDetProgressChange(0);
            Gl.ProgressDetStatusChange("Draw map..");

            #region get rect
            int sx = 0;
            int sy = 0;
            int ex = scn.size_map_x + step;
            int ey = scn.size_map_y + step;

            //if (!(bool)AllMap_toggleButton.IsChecked)
            if (Gl.main.select_land_mode)
            {
                sx = (int)Canvas.GetLeft(Gl.main.rectangle_select_land); //Gl.main.rectangle_select_land. ;
                sy = (int)Canvas.GetTop(Gl.main.rectangle_select_land); ;
                ex = (int)(Gl.main.rectangle_select_land.Width - 1);
                ey = (int)(Gl.main.rectangle_select_land.Height - 1);

                sx /= 32;
                sy /= 32;
                ex /= 32;
                ey /= 32;

                sx += step / 2;
                sy += step / 2;
                ex += sx - step / 2 - step % 2;
                ey += sy - step / 2 - step % 2;
            }
            #endregion get rect

            double dp = (double)100 / (double)(ex - sx) * (double)4;

            for (int i = sx; i < ex; i += step)
            {
                for (int j = sy; j < ey; j += step)
                {
                    scn.set_Land_region_autotile(i, j, size, Gl.palette.autotile_args);

                    if (!IsOne && !Gl.main.select_land_mode)
                    {
                        var timeDiff = DateTime.UtcNow - new DateTime(1970, 1, 1);
                        long totaltime = timeDiff.Ticks;
                        Random rnd1 = new Random(unchecked((int)(i * 1000 + j + totaltime)));
                        timeDiff = DateTime.UtcNow - new DateTime(1970, 1, 1);
                        totaltime = timeDiff.Ticks;
                        Random rnd2 = new Random(unchecked((int)(j * 1000 + i + totaltime)));

                        scn.set_Land_region_autotile(i + rnd1.Next(-5, 5), j + rnd2.Next(-5, 5), size, Gl.palette.autotile_args);
                    }
                }

                if (i % (4 * step) == 0)
                {
                    Gl.ProgressDetProgressInc(dp);
                    if (Gl.main.select_land_mode)
                        Gl.ProgressDetStatusChange("Draw.. (" + (i - sx) + "/" + (ex - sx) + ")");
                    else
                        Gl.ProgressDetStatusChange("Draw.. (" + i + "/" + scn.size_map_x + ")");
                }
            }

            Gl.main.HistoryWrite();

            Gl.CloseProgressDetWindow(newWindowThread);
        }

        private void FillMap_tile_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.palette.selected_sprite < 0)
                return;


            Scena scn = Gl.curr_scena;

            //int step = Gl.palette.curr_ground_brush_size;
            int step = 1;

            //if (step == 0)
            //    return;

            Thread newWindowThread = Gl.CreateProgressDetWindow("Filling...");

            Gl.ProgressDetProgressChange(0);
            Gl.ProgressDetStatusChange("Draw map..");

            #region get rect
            int sx = 0;
            int sy = 0;
            int ex = scn.size_map_x + step;
            int ey = scn.size_map_y + step;

            double dp = (double)100 / (double)scn.size_map_x * (double)4;

            //if (!(bool)AllMap_toggleButton.IsChecked)
            if (Gl.main.select_land_mode)
            {
                sx = (int)Canvas.GetLeft(Gl.main.rectangle_select_land); //Gl.main.rectangle_select_land. ;
                sy = (int)Canvas.GetTop(Gl.main.rectangle_select_land); ;
                ex = (int)(Gl.main.rectangle_select_land.Width - 1);
                ey = (int)(Gl.main.rectangle_select_land.Height - 1);

                sx /= 32;
                sy /= 32;
                ex /= 32;
                ey /= 32;

                sx += step / 2;
                sy += step / 2;
                //ex += sx;
                //ey += sy;

                dp = (double)100 / (double)(ex - sx) * (double)4;
            }
            #endregion get rect


            for (int i = sx; i < ex; i += step)
            {
                for (int j = sy; j < ey; j += step)
                {
                    scn.set_Ground_region_tile(i, j, step, Gl.palette.selected_sprite, true);
                }

                if (i % (4 * step) == 0)
                {
                    Gl.ProgressDetProgressInc(dp);
                    if (Gl.main.select_land_mode)
                        Gl.ProgressDetStatusChange("Draw.. (" + (i - sx) + "/" + (ex - sx) + ")");
                    else
                        Gl.ProgressDetStatusChange("Draw.. (" + i + "/" + scn.size_map_x + ")");
                }
            }

            Gl.main.HistoryWrite();

            Gl.CloseProgressDetWindow(newWindowThread);
        }

        private void FillMap_block_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.palette.selected_block < 0 || Gl.palette.selected_block >= Gl.curr_scena.blocks.Count)
                return;

            Scena scn = Gl.curr_scena;

            int step_x = Gl.palette.block_width;
            int step_y = Gl.palette.block_height;

            if (step_x == 0 || step_y == 0)
                return;

            Thread newWindowThread = Gl.CreateProgressDetWindow("Filling...");

            Gl.ProgressDetProgressChange(0);
            Gl.ProgressDetStatusChange("Draw map..");

            #region get rect
            int sx = step_x / 2;
            int sy = step_y / 2;
            int ex = scn.size_map_x + step_x;
            int ey = scn.size_map_y + step_y;

            //if (!(bool)AllMap_toggleButton.IsChecked)
            if (Gl.main.select_land_mode)
            {
                sx = (int)Canvas.GetLeft(Gl.main.rectangle_select_land); //Gl.main.rectangle_select_land. ;
                sy = (int)Canvas.GetTop(Gl.main.rectangle_select_land); ;
                ex = (int)(Gl.main.rectangle_select_land.Width - 1);
                ey = (int)(Gl.main.rectangle_select_land.Height - 1);

                sx /= 32;
                sy /= 32;
                ex /= 32;
                ey /= 32;

                sx += step_x / 2;
                sy += step_y / 2;
                ex += sx;
                ey += sy;
            }
            #endregion get rect

            double dp = (double)100 / (double)(ex - sx) * (double)4;

            for (int i = sx; i < ex; i += step_x)
            {
                for (int j = sy; j < ey; j += step_y)
                {
                    Gl.curr_scena.set_Ground_block(i, j, step_x, step_y, Gl.palette.selected_block, true);
                }

                if (i % (4 * step_x) == 0)
                {
                    Gl.ProgressDetProgressInc(dp);
                    if (Gl.main.select_land_mode)
                        Gl.ProgressDetStatusChange("Draw.. (" + (i - sx) + "/" + (ex - sx) + ")");
                    else
                        Gl.ProgressDetStatusChange("Draw.. (" + i + "/" + scn.size_map_x + ")");
                }
            }

            Gl.main.HistoryWrite();

            Gl.CloseProgressDetWindow(newWindowThread);
        }

        private void StressTest1_Button_Click(object sender, RoutedEventArgs e)
        {
            /*
             * Нагрузочный тест на Орду
             */

            Scena scn = Gl.curr_scena;
            if ((bool)comboBox_Rows.IsChecked)
            {
                for (int i = 0; i < scn.size_map_x; i++)
                    for (int j = 0; j < scn.size_map_y; j++)
                    {
                        scn.set_Ground_region_tile(j, i, 1, scn.tileSet[(i * scn.size_map_y + j) % scn.tileSet.Count].SpriteId, true);
                    }
            }
            else if ((bool)comboBox_Columns.IsChecked)
            {
                for (int i = 0; i < scn.size_map_x; i++)
                    for (int j = 0; j < scn.size_map_y; j++)
                    {
                        scn.set_Ground_region_tile(i, j, 1, scn.tileSet[(i * scn.size_map_y + j) % scn.tileSet.Count].SpriteId, true);
                    }
            }

            Gl.main.HistoryWrite();
        }

        private void StressTest2_Button_Click(object sender, RoutedEventArgs e)
        {
            /*
             * Нагрузочный тест на Орду
             */

            Scena scn = Gl.curr_scena;

            if ((bool)comboBox_Rows.IsChecked)
            {
                for (int i = 0; i < scn.size_map_x; i++)
                    for (int j = 0; j < scn.size_map_y; j++)
                    {
                        scn.set_Ground_region_tile(j, i, 1, scn.spriteSet[(i * scn.size_map_y + j) % scn.spriteSet.Count].SpriteId, true);
                    }
            }
            else if ((bool)comboBox_Columns.IsChecked)
            {
                for (int i = 0; i < scn.size_map_x; i++)
                    for (int j = 0; j < scn.size_map_y; j++)
                    {
                        scn.set_Ground_region_tile(i, j, 1, scn.spriteSet[(i * scn.size_map_y + j) % scn.spriteSet.Count].SpriteId, true);
                    }
            }

            Gl.main.HistoryWrite();
        }


        private void button_tile_to_block_Click(object sender, RoutedEventArgs e)
        {
            /*
             *  Из тайлов составляет блоки.
             */

            Scena scn = Gl.curr_scena;
            int w = 0;
            int h = 0;

            if (!int.TryParse(Width_tile_to_block.Text, out w))
                return;
            if (!int.TryParse(Height_tile_to_block.Text, out h))
                return;

            if (w == 0 || h == 0)
                return;

            Tile[,] new_block = new Tile[w, h];

            if ((bool)comboBox_Rows.IsChecked)
            {
                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++)
                    {
                        new_block[i, j] = scn.tileSet[Gl.palette.selected_tile + j * w + i];
                    }
            }
            else if ((bool)comboBox_Columns.IsChecked)
            {
                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++)
                    {
                        new_block[i, j] = scn.tileSet[Gl.palette.selected_tile + i * w + j];
                    }
            }

            scn.blocks.Add(new_block);
            Gl.blocks_window.AddBlock(new_block);

            Gl.main.HistoryWrite();
        }


        private void Clear_resources_Button_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;

            //Thread newWindowThread = Gl.CreateProgressWindow("Clearing...");

            #region get rect
            int sx = 0;
            int sy = 0;
            int ex = scn.size_map_x;
            int ey = scn.size_map_y;

            //if (!(bool)AllMap_toggleButton.IsChecked)
            if (Gl.main.select_land_mode)
            {
                sx = (int)Canvas.GetLeft(Gl.main.rectangle_select_land); //Gl.main.rectangle_select_land. ;
                sy = (int)Canvas.GetTop(Gl.main.rectangle_select_land); ;
                ex = (int)(Gl.main.rectangle_select_land.Width - 1);
                ey = (int)(Gl.main.rectangle_select_land.Height - 1);

                sx /= 32;
                sy /= 32;
                ex /= 32;
                ey /= 32;

                ex += sx;
                ey += sy;
            }
            #endregion get rect

            for (int i = sx; i < ex; i++)
                for (int j = sy; j < ey; j++)
                {
                    scn.set_Resource_tile(i, j, 1, 0, true);
                }

            Gl.main.HistoryWrite();

            //newWindowThread.Abort();
        }

        private void Clear_units_Button_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;

            //Thread newWindowThread = Gl.CreateProgressWindow("Clearing...");

            #region get rect
            int sx = 0;
            int sy = 0;
            int ex = scn.size_map_x;
            int ey = scn.size_map_y;

            //if (!(bool)AllMap_toggleButton.IsChecked)
            if (Gl.main.select_land_mode)
            {
                sx = (int)Canvas.GetLeft(Gl.main.rectangle_select_land); //Gl.main.rectangle_select_land. ;
                sy = (int)Canvas.GetTop(Gl.main.rectangle_select_land); ;
                ex = (int)(Gl.main.rectangle_select_land.Width - 1);
                ey = (int)(Gl.main.rectangle_select_land.Height - 1);

                sx /= 32;
                sy /= 32;
                ex /= 32;
                ey /= 32;

                ex += sx;
                ey += sy;
            }
            #endregion get rect

            for (int i = sx; i < ex; i++)
                for (int j = sy; j < ey; j++)
                {
                    while (scn.map_units[i, j] != null && scn.map_units[i, j].Count > 0)
                    {
                        scn.unit_delete(scn.map_units[i, j][0], true);
                    }
                }

            Gl.main.HistoryWrite();

            //newWindowThread.Abort();
        }


        #region universal
        private void Read_universal_indexes()
        {
            ComboBoxItem cbi = (ComboBoxItem)parentScena_comboBox.SelectedItem;
            string filename = Gl.running_dir + "\\resources\\universal\\" + cbi.Content;


            bool result = false;
            Scena scn = Gl.curr_scena;
            StreamReader str = new StreamReader(filename);
            string s = str.ReadLine();
            int id;

            for (int i = 0; i < scn.tileSet.Count; i++)
            {
                result = int.TryParse(s, out id);
                if (result == true)
                {
                    for (int j = 0; j < scn.tileSet[i].FramesCount; j++)
                        scn.spriteSet[scn.tileSet[i].FirstSpriteId + j].TileId_inUniversal = id + j;
                    s = str.ReadLine();
                }
                else
                    break;
            }

            if (s != null)
                result = false;

            if (result == true)
                scn.uni_indexes_readed = filename;
            else
            {
                scn.uni_indexes_readed = "no";
                System.Windows.MessageBox.Show("Bad indexes file.", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private bool uni_indexes_check()
        {
            Scena scn = Gl.curr_scena;
            ComboBoxItem cbi = (ComboBoxItem)parentScena_comboBox.SelectedItem;
            string filename = Gl.running_dir + "\\resources\\universal\\" + cbi.Content;

            if (scn.uni_indexes_readed != filename)
                Read_universal_indexes();
            if (scn.uni_indexes_readed != filename)
                return false;

            return true;
        }

        private void Export_blocks_Click(object sender, RoutedEventArgs e)
        {
            if (!uni_indexes_check())
                return;

            Scena scn = Gl.curr_scena;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = scn.scena_short_name + ".h2b";
            dlg.Title = "Select blocks file";
            dlg.DefaultExt = "*.h2b";
            dlg.Filter = "Blocks file|*.h2b";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == false)
                return;



            FileStream outStream = File.Open(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.Read);


            byte[] spr_index = new byte[4];

            spr_index = Utils.i2b(scn.blocks.Count, 4);
            outStream.WriteByte(spr_index[0]);
            outStream.WriteByte(spr_index[1]);
            outStream.WriteByte(spr_index[2]);
            outStream.WriteByte(spr_index[3]);

            for (int i = 0; i < scn.blocks.Count; i++) // пишем размеры блоков
            {
                outStream.WriteByte((byte)scn.blocks[i].GetLength(0));
                outStream.WriteByte((byte)scn.blocks[i].GetLength(1));

            }
            int cur_w, cur_h;
            for (int i = 0; i < scn.blocks.Count; i++) // пишем сами блоки
            {
                cur_w = scn.blocks[i].GetLength(0);
                cur_h = scn.blocks[i].GetLength(1);

                for (int j2 = 0; j2 < cur_h; j2++)     // y
                    for (int j1 = 0; j1 < cur_w; j1++) // x
                    {
                        if (scn.blocks[i][j1, j2].SpriteId < scn.spriteSet.Count)
                        {
                            spr_index = Utils.i2b(scn.blocks[i][j1, j2].TileId_inUniversal, 4);
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

            System.Windows.MessageBox.Show("Done!", "Export blocks", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Import_blocks_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;
            ComboBoxItem cbi = (ComboBoxItem)parentScena_comboBox.SelectedItem;

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Title = "Select blocks file";
            dlg.DefaultExt = "*.h2b";
            dlg.Filter = "Blocks file|*.h2b";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == false)
                return;

            foreach (string filename in dlg.FileNames.ToList())
            {

                FileStream inStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

                byte[] tmp_size = new byte[64];
                inStream.Read(tmp_size, 0, 4);
                int count = Utils.b2i(tmp_size, 0, 4);
                int tmp_i2;
                byte[,] blocks_sizes = new byte[count, 2]; // 0 - width; 1 - height

                for (int i = 0; i < count; i++) // читаем размеры всех блоков
                {
                    inStream.Read(tmp_size, 0, 2);
                    blocks_sizes[i, 0] = tmp_size[0];
                    blocks_sizes[i, 1] = tmp_size[1];
                }


                byte[] block = new byte[16 * 16 * 4]; // максимальный блок состоит из 16x16 тайлов
                //uint whitespace = 0xFFFFFFFF; // пробел в блоке // вроде это -1
                bool flag = true; // true если блок пустой

                //Gl.blocks_window.Clear();

                int cur_w, cur_h; // размеры текущего блока
                for (int i = 0; i < count; i++)
                {
                    cur_w = blocks_sizes[i, 0];
                    cur_h = blocks_sizes[i, 1];
                    Tile[,] new_block = new Tile[cur_w, cur_h];
                    scn.blocks.Add(new_block); // список блоков в виде номеров тайлов
                    inStream.Read(block, 0, cur_w * cur_h * 4);
                    flag = true;

                    for (int j2 = 0; j2 < cur_h; j2++)     // y
                        for (int j1 = 0; j1 < cur_w; j1++) // x
                        {
                            tmp_i2 = Utils.b2i(block, j2 * 4 * cur_w + j1 * 4, 4);
                            if (tmp_i2 != -1)
                            {
                                new_block[j1, j2] = scn.spriteSet[tmp_i2]; //!!!!!
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
            }

            System.Windows.MessageBox.Show("Done!", "Import blocks", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Clear_Tiles_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;
            int[] count = new int[scn.tileSet.Count];
            int deleted = 0;

            for (int i = 0; i < scn.size_map_x; i++)
                for (int j = 0; j < scn.size_map_y; j++)
                    count[scn.map[i, j].TileId]++;

            for (int i = scn.tileSet.Count - 1; i >= 0; i--)
                if (count[i] == 0)
                {
                    Gl.tiles_window.DeleteTile(i);
                    deleted++;
                }

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            System.Windows.MessageBox.Show("Deleted " + deleted + " tiles.", "Clearing", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Clear_Blocks_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;
            int count = scn.blocks.Count;

            for (int i = 0; i < count; i++)
            {
                Gl.blocks_window.RemoveBlock(0);
            }

            Gl.blocks_window.UpdateBlocks();

            System.Windows.MessageBox.Show("Deleted " + count + " blocks.", "Clearing", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #region Mega Map

        // Загружаем/экспортируем карту соответствующую номеру написанному на кнопке.
        // Причем карта будет браться из той же папки что и уже загруженная.
        // А экспортируется в специальную папку в редакторе.
        private void Map_Export_button_Click(object sender, RoutedEventArgs e)
        {
            int n = Convert.ToInt32(((Button)sender).Content);

            string scena_n = "0" + n;
            if (n == 10 || n == 70)
                scena_n = "" + n;

            string scena_export_name = Gl.running_dir + "\\resources\\universal\\maps\\scena_" + scena_n + ".h2m";

            if (Export_radioButton.IsChecked == true)
            {
                Export_map_in_universal(scena_export_name);

                System.Windows.MessageBox.Show("Done!", "Map export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Import_radioButton.IsChecked == true)
            {
                int x = -1;
                int y = -1;
                if (n == 1)
                { x = 0; y = 0; }
                else if (n == 2)
                { x = 128; y = 0; }
                else if (n == 3)
                { x = 256; y = 0; }
                else if (n == 4)
                { x = 0; y = 128; }
                else if (n == 5)
                { x = 128; y = 128; }
                else if (n == 6)
                { x = 256; y = 128; }
                else if (n == 7)
                { x = 0; y = 256; }
                else if (n == 8)
                { x = 128; y = 256; }
                else if (n == 9)
                { x = 256; y = 256; }
                else if (n == 10)
                { x = 384; y = 128; }
                else if (n == 70)
                { x = 448; y = 320; }

                string scena_load_name = System.IO.Path.GetDirectoryName(Gl.curr_scena.scena_path) + "\\scena_" + scena_n + ".scn";

                Gl.curr_scena.Load_Scene_OverWriting(scena_load_name, scena_export_name, x, y);

                System.Windows.MessageBox.Show("Done!", "Map import", MessageBoxButton.OK, MessageBoxImage.Information);
            }


        }

        private void Export_map_in_universal(string path)
        {
            if (!uni_indexes_check())
                return;

            Scena scn = Gl.curr_scena;


            if (!Directory.Exists(Gl.running_dir + "\\resources\\universal\\maps"))
                Directory.CreateDirectory(Gl.running_dir + "\\resources\\universal\\maps");

            FileStream outStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            byte[] spr_index = new byte[4];

            for (int i = 0; i < scn.size_map_x; i++)     // y
                for (int j = 0; j < scn.size_map_y; j++) // x
                {
                    spr_index = Utils.i2b(scn.map[i, j].TileId_inUniversal, 4);
                    outStream.WriteByte(spr_index[0]);
                    outStream.WriteByte(spr_index[1]);
                    outStream.WriteByte(spr_index[2]);
                    outStream.WriteByte(spr_index[3]);
                }

        }
        private void Divide_Map_button_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;

            string folder = System.IO.Path.GetDirectoryName(scn.scena_path) + "\\";

            int x = -1;
            int y = -1;
            int w = 128;
            int h = 128;

            x = 0; y = 0;
            Export_Part_MegaMap(folder + "scena_01.scn", x, y, w, h);
            x = 128; y = 0;
            Export_Part_MegaMap(folder + "scena_02.scn", x, y, w, h);
            x = 256; y = 0;
            Export_Part_MegaMap(folder + "scena_03.scn", x, y, w, h);
            x = 0; y = 128;
            Export_Part_MegaMap(folder + "scena_04.scn", x, y, w, h);
            x = 128; y = 128;
            Export_Part_MegaMap(folder + "scena_05.scn", x, y, w, h);
            x = 256; y = 128;
            Export_Part_MegaMap(folder + "scena_06.scn", x, y, w, h);
            x = 0; y = 256;
            Export_Part_MegaMap(folder + "scena_07.scn", x, y, w, h);
            x = 128; y = 256;
            Export_Part_MegaMap(folder + "scena_08.scn", x, y, w, h);
            x = 256; y = 256;
            Export_Part_MegaMap(folder + "scena_09.scn", x, y, w, h);
            x = 384; y = 128;
            Export_Part_MegaMap(folder + "scena_10.scn", x, y, w, h);

            System.Windows.MessageBox.Show("MEGA MAP divided", "Divide MEGA MAP", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Export_Part_MegaMap(string path, int x, int y, int w, int h)
        {
            Scena scn = Gl.curr_scena;

            if (File.Exists(path))
            {
                System.Windows.MessageBoxResult res = System.Windows.MessageBox.Show("File \"" + path + "\" is exist. Replace it?", "Divide MEGA MAP", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No)
                    return;
            }

            scn.Save_Scene_Part(path, x, y, w, h);
        }

        #endregion Mega Map

        #endregion universal

    }
}
