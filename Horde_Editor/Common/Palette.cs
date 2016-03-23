using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Horde_ClassLibrary;

namespace Horde_Editor.Common
{
    class Palette
    {
        // Палитра - GROUND
        public int palette_size_cell = 32;
        public int selected_tile = -1;
        public int selected_sprite = -1;
        public int last_ground_brush_size = 1;
        public int curr_ground_brush_size = 1;
        public int tiles_collumns_count = 8;

        // блоки
        public bool Is_block_selected = false;
        public int selected_block = -1;
        public int block_width = 0;  // в ячейках
        public int block_height = 0; // в ячейках

        // Палитра автотайлов
        public AutoTileArgs autotile_args = new AutoTileArgs();

        // Палитра юнитов
        public int selected_unit_folder = -1;
        public int selected_unit_index = -1;
        public RenderTargetBitmap img_Player_color_Box; // думаю что это костыль
        public bool one_unit_on_layer = true;
        public bool recursive_units = true;  // заборы и мосты
        // копипаста
        public Dictionary<Tuple<int, int>, List<Unit>> units_in_buffer = null;
        public int units_in_buffer_x_min;
        public int units_in_buffer_x_max;
        public int units_in_buffer_y_min;
        public int units_in_buffer_y_max;

        // Палитра ресурсов
        public int selected_resourse = -1;

        public Palette()
        {
            Canvas.SetLeft(Gl.main.ground_selector, -100);
            InitUnitPalette();
        }

        public void InitLandPalette(Scena scn)
        {
            int w = tiles_collumns_count;
            int h = scn.tileSet.Count / tiles_collumns_count;
            if (scn.tileSet.Count % tiles_collumns_count != 0)
                h++;
            //w *= 32;
            //h *= 32;

            DrawingGroup imageDrawings = new DrawingGroup();
            scn.Mega_bmp = new DrawingImage(imageDrawings);

            ImageDrawing img_d;

            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    if (scn.tileSet.Count > j * tiles_collumns_count + i)
                    {
                        System.Windows.Rect r = new System.Windows.Rect(i * 32, j * 32, 32, 32);

                        img_d = new ImageDrawing(scn.tileSet[j * tiles_collumns_count + i].Sprite, r);
                        img_d.Freeze();

                        imageDrawings.Children.Add(img_d);
                    }
                }
            Gl.main.Ground_Pallete_Image.Source = scn.Mega_bmp;
            Gl.main.Ground_Pallete_Canvas.Height = scn.Mega_bmp.Height;
        }

        public void InitUnitPalette()
        {
            TreeViewItem newItem = new TreeViewItem();
            newItem.Tag = "empty";
            newItem.Header = "empty";
            Gl.main.units_Tree.Items.Add(newItem);

            List<string> folders = H2Strings.armies_str;

            for (int i = 0; i < 5; i++)
            {
                TreeViewItem newItem2 = new TreeViewItem();
                newItem2.Tag = "folder" + i;
                newItem2.Header = folders[i];
                Gl.main.units_Tree.Items.Add(newItem2);
            }

            // Картинка с цветом игрока
            img_Player_color_Box = new RenderTargetBitmap((int)(Gl.main.Player_color_Box.Width), (int)(Gl.main.Player_color_Box.Height), 96, 96, PixelFormats.Default);
            Gl.main.Player_color_Box.Source = img_Player_color_Box;
            UpdatePlayerColor();

            // Заполнение палитры юнитов:
            H2_configs.read_configs();
            for (int i = 0; i < H2_configs.cfgs.Count(); i++)
            {
                for (int j = 0; j < H2_configs.cfgs[i].Count(); j++)
                {
                    TreeViewItem newItem3 = new TreeViewItem();
                    newItem3.Tag = i + "_" + j;
                    newItem3.Header = "[" + i + " " + j + "]   " + H2_configs.cfgs[i][j].Name;

                    // Add items to root node  
                    ((TreeViewItem)Gl.main.units_Tree.Items[i + 1]).Items.Add(newItem3);
                }
            }



        }


        #region land
        public void DeSelectLand() // Убрать выделение тайла из палитры
        {
            selected_tile = -1;
            selected_sprite = -1;
            selected_block = -1;
            Is_block_selected = false;
            //Gl.main.comboBox_Brush.SelectionChanged -= Gl.main.ComboBox_Brush_SelectionChanged;
            //Gl.main.comboBox_Brush.SelectedIndex = 0;
            //Gl.main.comboBox_Brush.SelectionChanged += Gl.main.ComboBox_Brush_SelectionChanged;
            UpdateGroundSelector(-1, -1);
        }

        public void UpdateGroundSelector(int x, int y)
        {
            if (x >= 0 && y >= 0)
            {
                Canvas.SetLeft(Gl.main.ground_selector, x * palette_size_cell);
                Canvas.SetTop(Gl.main.ground_selector, y * palette_size_cell);
            }
            else
            {
                Canvas.SetLeft(Gl.main.ground_selector, -100);
            }
        }

        #endregion land

        #region autotile

        public void UpdateAutoTileForm(bool lu, bool ru, bool ld, bool rd)
        {
            autotile_args.boolMask[0, 0] = lu;
            autotile_args.boolMask[1, 0] = ru;
            autotile_args.boolMask[0, 1] = ld;
            autotile_args.boolMask[1, 1] = rd;

            autotile_args.form = AutoTileDict.BoolMask2String(autotile_args.boolMask);

        }


        #endregion autotile


        #region unit
        public void UpdatePlayerUnitCounter()
        {
            Gl.main.label_PlayerUnits.Content = "has units " + Gl.curr_scena.players[Gl.main.comboBox_Player.SelectedIndex].units_count;
        }

        public void UpdatePlayerColor()
        {

            // Графика
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext;
            drawingContext = drawingVisual.RenderOpen();

            // Вспомогательное
            SolidColorBrush b = new SolidColorBrush();
            b.Color = Gl.getPlayer_color(Gl.main.comboBox_Player.SelectedIndex);
            System.Windows.Rect rect = new System.Windows.Rect(0, 0, (int)(Gl.main.Player_color_Box.Width), (int)(Gl.main.Player_color_Box.Height));
            drawingContext.DrawRectangle(b, null, rect);

            drawingContext.Close();
            img_Player_color_Box.Render(drawingVisual);
        }

        public string SelectedUnitName = "<empty>";
        public UnitConfig SelectedUnitCfg = null;
        public void UpdateUnitPortrait()
        {
            SelectedUnitName = "<empty>";

            if (Gl.main.units_Tree.SelectedItem is TreeViewItem)
            {
                TreeViewItem tvi = Gl.main.units_Tree.SelectedItem as TreeViewItem;
                string tvi_tag = tvi.Tag.ToString();
                int i_sep = tvi_tag.IndexOf("_");
                if (tvi_tag.Contains("folder") == false
                    && tvi_tag.Contains("empty") == false)
                {

                    selected_unit_folder = Convert.ToInt32(tvi_tag.Substring(0, i_sep));
                    selected_unit_index = Convert.ToInt32(tvi_tag.Substring(i_sep + 1, tvi_tag.Length - i_sep - 1));

                    SelectedUnitCfg = H2_configs.cfgs[selected_unit_folder][selected_unit_index];

                    int s_id = SelectedUnitCfg.default_sprite;
                    if (SelectedUnitCfg.HasFlag(UnitFlags.Building)) // проверка на здание.
                        s_id += SelectedUnitCfg.moving_frames;

                    if (selected_unit_folder == 4)
                    {
                        selected_unit_folder = 5;
                    }

                    // Спрайт юнита
                    BitmapImage unit_Portrait = new BitmapImage();

                    // Загружаем картинку
                    unit_Portrait.BeginInit();
                    unit_Portrait.UriSource = new Uri(@"resources\army__0" + selected_unit_folder + @".spr\" + s_id + ".bmp", UriKind.Relative);
                    unit_Portrait.EndInit();
                    unit_Portrait.Freeze();

                    // Меняем
                    Gl.main.image_UnitPortrait.Source = unit_Portrait; // это КОСТЫЛЬ? - каждый раз менять картинку. Затирается ли предыдущая.


                    SelectedUnitName = tvi.Header.ToString();
                }
                else
                    ClearUnitPortrait();
            }
            else
                ClearUnitPortrait();
        }


        public void ClearUnitPortrait()
        {
            selected_unit_index = -1;
            selected_unit_folder = -1;
            // Убираем картинку КОСТЫЛЬ
            BitmapImage unit_Portrait = new BitmapImage();
            unit_Portrait.BeginInit();
            unit_Portrait.UriSource = new Uri(@"Resources/icon.ico", UriKind.Relative);
            unit_Portrait.EndInit();
            Gl.main.image_UnitPortrait.Source = unit_Portrait;

        }



        public void SelectUnit(int x, int y)
        {
            //Gl.main.units_Tree.CollapseAll();
            selected_unit_folder = Gl.curr_scena.map_units[x, y][Gl.curr_scena.map_units[x, y].Count() - 1].folder;
            selected_unit_index = Gl.curr_scena.map_units[x, y][Gl.curr_scena.map_units[x, y].Count() - 1].unit_id;
            if (selected_unit_folder == 5) // Костыль (вожможно от разработчиков орды)
                selected_unit_folder = 4;
            string f = "folder" + selected_unit_folder;
            string s = selected_unit_folder + "_" + selected_unit_index;

            TreeViewItem twiFindFolder = null;
            TreeViewItem twiFind = null;
            foreach (TreeViewItem objTreeviewItem in Gl.main.units_Tree.Items)
            {
                if ((objTreeviewItem.Tag.ToString() == f))
                {
                    twiFindFolder = objTreeviewItem as TreeViewItem;
                    break;
                }
            }
            if (twiFindFolder != null)
            {
                twiFindFolder.IsExpanded = true;
                foreach (TreeViewItem objTreeviewItem in twiFindFolder.Items)
                {
                    if ((objTreeviewItem.Tag.ToString() == s))
                    {
                        twiFind = objTreeviewItem as TreeViewItem;
                        break;
                    }
                }
            }

            TreeViewItem twiSel = Gl.main.units_Tree.SelectedItem as TreeViewItem;
            if (twiSel != null)
                twiSel.IsSelected = false;
            if (twiFind != null)
                twiFind.IsSelected = true;
            //treeView1.Select();
        }
        public void DeSelectUnit() // Убрать выделение юнита в списке юинтов
        {
            //Gl.main.units_Tree.CollapseAll();
            Gl.palette.selected_unit_folder = -1;
            Gl.palette.selected_unit_index = -1;
            TreeViewItem twi = Gl.main.units_Tree.SelectedItem as TreeViewItem;
            if (twi != null)
                twi.IsSelected = false;
            twi = Gl.main.units_Tree.Items[0] as TreeViewItem;
            if (twi != null)
                twi.IsSelected = true;
        }

        #endregion unit

    }
}
