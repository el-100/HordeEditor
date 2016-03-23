using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;

using Horde_Editor.Common;
using Horde_Editor.Controls;
using Horde_Editor.History;
using Horde_ClassLibrary;


using System.Reflection; // для получения названия метода.

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Tiles_Window.xaml
    /// </summary>
    public partial class Tiles_Window : Window
    {
        bool tileMode = true;
        int _selTile = 0;
        int _selSprite = 0;
        public int SelectedTile
        {
            get
            {
                return _selTile;
            }
            set
            {
                _selTile = value;
                _selSprite = Gl.curr_scena.tileSet[_selTile].SpriteId;
                //tiles_scrollBar.Value = _selTile / collumns_count;
            }
        }
        public int SelectedSprite
        {
            get
            {
                return _selSprite;
            }
            set
            {
                _selSprite = value;
                _selTile = Gl.curr_scena.spriteSet[_selSprite].TileId;
                //tiles_scrollBar.Value = _selTile;
            }
        }
        public Tiles_Window()
        {
            InitializeComponent();
        }

        int _selTile_blue = -1;
        int _selSprite_blue = -1;
        public int SelectedTile_Blue
        {
            get
            {
                return _selTile_blue;
            }
            set
            {
                _selTile_blue = value;
                if (value != -1)
                    _selSprite_blue = Gl.curr_scena.tileSet[_selTile_blue].SpriteId;
                else
                    _selSprite_blue = -1;
            }
        }
        public int SelectedSprite_Blue
        {
            get
            {
                return _selSprite_blue;
            }
            set
            {
                _selSprite_blue = value;
                if (value != -1)
                    _selTile_blue = Gl.curr_scena.spriteSet[_selSprite_blue].TileId;
                else
                    _selTile_blue = -1;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int tmp = ((int)(tiles_border.ActualHeight / 32)) * 32;
            tilesImage.Height = tmp;
            tiles_scrollViewer.Height = tmp;

            tilesImage.Init(1 * 32, 10 * 32);
            //selector.Visibility = System.Windows.Visibility.Hidden;

            #region comboboxes

            List<string> str = H2Strings.tile_types_str;

            foreach (string s in str)
            {
                ComboBoxItem cbi1 = new ComboBoxItem();
                ComboBoxItem cbi2 = new ComboBoxItem();
                ComboBoxItem cbi3 = new ComboBoxItem();
                ComboBoxItem cbi4 = new ComboBoxItem();
                cbi1.Content = s;
                cbi2.Content = s;
                cbi3.Content = s;
                cbi4.Content = s;
                lu_combobox.Items.Add(cbi1);
                ru_combobox.Items.Add(cbi2);
                ld_combobox.Items.Add(cbi3);
                rd_combobox.Items.Add(cbi4);
            }

            str = H2Strings.tile_type_str;
            foreach (string s in str)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = s;
                type_combobox.Items.Add(cbi);
            }

            str = H2Strings.tile_sounds_str;
            foreach (string s in str)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = s;
                sounds_combobox.Items.Add(cbi);
            }

            str = H2Strings.tile_variants_str;
            foreach (string s in str)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = s;
                variant_combobox.Items.Add(cbi);
            }

            #endregion comboboxes

            #region events set

            slowing_numericUpDown.ValueChanged += slowing_numericUpDown_ValueChanged;
            type_combobox.SelectionChanged += type_combobox_SelectionChanged;
            sounds_combobox.SelectionChanged += sounds_combobox_SelectionChanged;
            variant_combobox.SelectionChanged += variant_combobox_SelectionChanged;

            lu_combobox.SelectionChanged += lu_combobox_SelectionChanged;
            ru_combobox.SelectionChanged += ru_combobox_SelectionChanged;
            ld_combobox.SelectionChanged += ld_combobox_SelectionChanged;
            rd_combobox.SelectionChanged += rd_combobox_SelectionChanged;

            sign_numericUpDown.ValueChanged += sign_numericUpDown_ValueChanged;
            first_num_numericUpDown.ValueChanged += first_num_numericUpDown_ValueChanged;
            count_frames_numericUpDown.ValueChanged += count_frames_numericUpDown_ValueChanged;

            minimap_ColorPicker.SelectedColorChanged += minimap_ColorPicker_SelectedColorChanged;
            this.SizeChanged += Window_SizeChanged;

            #endregion

            sign_numericUpDown.IsEnabled = false;
            first_num_numericUpDown.IsEnabled = false;
            count_frames_numericUpDown.IsEnabled = false;

            tiles_scrollViewer.Width = collumns_count * 32 + 16;
            System.Windows.Controls.Primitives.ScrollBar scBar = tiles_scrollViewer.Template.FindName("PART_HorizontalScrollBar", tiles_scrollViewer) as System.Windows.Controls.Primitives.ScrollBar;
            scBar.SmallChange = 1;

            FullUpdate();
        }

        private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            UpdateImage();
            UpdateSelector();
        }

        public bool closing = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false) // нужено для того что бы крестом не закрывалось, а при выключении гл. формы закрывалась
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public void UpdateScroll()
        {
            int rows = (int)Math.Ceiling(tilesImage.Height / 32);

            if (tileMode)
                tiles_scrollBar.Maximum = Gl.curr_scena.tileSet.Count / collumns_count - rows + 1;
            else
                tiles_scrollBar.Maximum = Gl.curr_scena.tileSet.Count - rows + 1;

            int tmp = ((int)(tiles_border.ActualHeight / 32)) * 32;
            if (tmp > 2)
                tiles_scrollBar.Height = tmp - 2;
            else
                tiles_scrollBar.Height = 0;

            tiles_scrollBar.Margin = new Thickness(tiles_border.ActualWidth - 20, 0, 0, 0);
        }

        int collumns_count = 8;
        //int rows_count = ;
        public void UpdateImage()
        {
            double tmp = tilesImage.Width; // костыль (начало)
            collumns_count = (int)Math.Ceiling((tmp - 16) / 32);
            if (collumns_count < 1)
                collumns_count = 1;
            int h = (int)Math.Floor(tilesImage.Height / 32);
            int pos = (int)tiles_scrollBar.Value;
            Scena scn = Gl.curr_scena;

            ImageSource[,] bmp = null;// = new ImageSource[1, h];

            if (tileMode) // тайлы
            {
                bmp = new ImageSource[collumns_count, h];
                pos *= collumns_count; // 8 в ряд

                for (int i = 0; i < h; i++) // строки
                    for (int j = 0; j < collumns_count; j++) // столбцы
                        if (i * collumns_count + j < scn.tileSet.Count - pos)
                            bmp[j, i] = scn.tileSet[i * collumns_count + j + pos].Sprite;
                        else
                            bmp[j, i] = scn.ground_sprites.getBitmapImage(-1, 0);
                //tilesImage.Width = collumns_count * 32 + 16;
            }
            else // спрайты
            {
                // поиск максимального числа кадров
                int w_max = 0;
                for (int i = pos; i < h + pos; i++)
                    if (scn.tileSet.Count > i && w_max < scn.tileSet[i].FramesCount)
                        w_max = scn.tileSet[i].FramesCount;

                bmp = new ImageSource[w_max, h];

                int w = 0;
                for (int i = 0; i < h; i++) // строки
                {
                    if (scn.tileSet.Count - pos > i)
                    {
                        w = scn.tileSet[i + pos].FramesCount;
                        for (int j = 0; j < w_max; j++) // столбцы
                            if (i < scn.tileSet.Count - pos && j < w)
                                bmp[j, i] = scn.spriteSet[scn.tileSet[i + pos].FirstSpriteId + j].Sprite;
                            else
                                bmp[j, i] = scn.ground_sprites.getBitmapImage(-1, 0);
                    }
                }
                //tmp = w_max * 32 + 16;
            }


            tilesImage.ChangeBlock(bmp);
            tilesImage.Width = tmp; // костыль (продолжение)
        }

        bool isSetUp = true;
        public void UpdateInfo()
        {
            isSetUp = true;
            Scena scn = Gl.curr_scena;
            Tile t = getSelectedSprite();

            slowing_numericUpDown.Value = t.slowing;
            type_combobox.SelectedIndex = t.type;
            sounds_combobox.SelectedIndex = t.sounds;
            variant_combobox.SelectedIndex = t.variant;

            lu_combobox.SelectedIndex = t.leftup;
            ru_combobox.SelectedIndex = t.rightup;
            ld_combobox.SelectedIndex = t.leftdown;
            rd_combobox.SelectedIndex = t.rightdown;

            sign_numericUpDown.Value = t.sign;
            first_num_numericUpDown.Value = t.first_num;
            count_frames_numericUpDown.Value = t.count_frames;

            minimap_ColorPicker.SelectedColor = t.MinimapColor;

            // ---------------------------------------------------------

            if (tileMode)
                position_find_TextBox.Text = SelectedTile.ToString();
            else
                position_find_TextBox.Text = SelectedSprite.ToString();

            tile_Count_Label.Content = "Tiles count " + scn.tileSet.Count;
            if (SelectedTile_Blue == -1)
                tile_Sel_Label.Content = "Selected tile " + SelectedTile;
            else
                tile_Sel_Label.Content = "Selected tile " + SelectedTile + ", blue " + SelectedTile_Blue;
            if (SelectedSprite_Blue == -1)
                spr_Sel_Label.Content = "Selected sprite " + SelectedSprite;
            else
                spr_Sel_Label.Content = "Selected sprite " + SelectedSprite + ", blue " + SelectedSprite_Blue;
            spr_Count_Label.Content = "Sprites count " + scn.spriteSet.Count;

            spr_id_Label.Content = "SpriteId " + t.SpriteId;
            tile_id_Label.Content = "TileId " + t.TileId;
            nframe_Label.Content = "NFrame " + t.NFrame;
            frames_count_Label.Content = "FramesCount " + t.FramesCount;
            first_frame_Label.Content = "FirstSpriteId " + t.FirstSpriteId;

            isSetUp = false;
        }

        public void UpdateSelector()
        {
            int h = (int)Math.Ceiling(tilesImage.Height / 32);
            int pos = (int)tiles_scrollBar.Value;

            int y = 0;
            int x = 0;

            Scena scn = Gl.curr_scena;

            int dx = (int)tiles_scrollViewer.HorizontalOffset;

            if (tileMode)
            {
                y = (SelectedTile - pos * collumns_count) / collumns_count;
                x = (SelectedTile - pos * collumns_count) % collumns_count;
            }
            else
            {
                y = SelectedTile - pos;
                x = scn.spriteSet[SelectedSprite].NFrame;
            }

            if (y >= 0 && y < h)
            {
                selector.Margin = new Thickness(1 + x * 32 - dx, 1 + y * 32, 0, 0);
                selector.Visibility = Visibility.Visible;
            }
            else
                selector.Visibility = Visibility.Hidden;

            UpdateSelector_Blue();
        }

        public void UpdateSelector_Blue()
        {
            if (SelectedTile_Blue >= 0 && SelectedSprite_Blue >= 0)
            {
                int h = (int)Math.Ceiling(tilesImage.Height / 32);
                int pos = (int)tiles_scrollBar.Value;

                int y = 0;
                int x = 0;

                Scena scn = Gl.curr_scena;

                int dx = (int)tiles_scrollViewer.HorizontalOffset;

                if (tileMode)
                {
                    y = (SelectedTile_Blue - pos * collumns_count) / collumns_count;
                    x = (SelectedTile_Blue - pos * collumns_count) % collumns_count;
                }
                else
                {
                    y = SelectedTile_Blue - pos;
                    x = scn.spriteSet[SelectedSprite_Blue].NFrame;
                }

                if (y >= 0 && y < h)
                {
                    selector_blue.Margin = new Thickness(1 + x * 32 - dx, 1 + y * 32, 0, 0);
                    selector_blue.Visibility = Visibility.Visible;
                }
                else
                    selector_blue.Visibility = Visibility.Hidden;
            }
            else
                selector_blue.Visibility = Visibility.Hidden;
        }

        public void FullUpdate()
        {
            if (Gl.curr_scena == null)
                return;

            UpdateScroll();
            UpdateImage();
            UpdateInfo();
            UpdateSelector();
        }

        Tile getSelectedSprite()
        {
            Scena scn = Gl.curr_scena;
            Tile t;
            t = scn.spriteSet[SelectedSprite];

            return t;
        }

        private void view_checkBox_Checked(object sender, RoutedEventArgs e)
        { // tiles
            if (Gl.curr_scena == null)
                return;

            tileMode = true;

            FullUpdate();

            UpdateInfoLabels();
        }

        private void view_checkBox_Unchecked(object sender, RoutedEventArgs e)
        { // sprites
            if (Gl.curr_scena == null)
                return;

            tileMode = false;

            FullUpdate();

            UpdateInfoLabels();
        }

        private void UpdateInfoLabels()
        {
            if ((bool)viewmode_checkBox.IsChecked)
            {
                sign_numericUpDown.IsEnabled = false;
                first_num_numericUpDown.IsEnabled = false;
                count_frames_numericUpDown.IsEnabled = false;

                tiles_scrollBar.Value = (SelectedTile - 5 * collumns_count) / collumns_count;
                ScrollBar_Scroll(this, null);

                Insert_new_Button.Content = "new Tile";
                if (SelectedTile_Blue == -1)
                {
                    ChangeSprite_Button.Content = "Change this Tile";
                    Export_Button.Content = "Export this Tile";
                    Delete_Button.Content = "Delete this Tile";
                }
                else
                {
                    ChangeSprite_Button.Content = "Change this Tiles";
                    Export_Button.Content = "Export this Tiles";
                    Delete_Button.Content = "Delete this Tiles";
                }
                findButton.Content = "Find Tile";

                //Union_Button.IsEnabled = true;
                Break_Button.IsEnabled = false;
                //moveTo_Button.IsEnabled = true;
                //position_TextBox.IsEnabled = true;

                Insert_folder_Button.IsEnabled = true;
            }
            else
            {
                sign_numericUpDown.IsEnabled = true;
                first_num_numericUpDown.IsEnabled = true;
                count_frames_numericUpDown.IsEnabled = true;

                tiles_scrollBar.Value = SelectedTile - 5;
                ScrollBar_Scroll(this, null);

                Insert_new_Button.Content = "new Sprite";
                ChangeSprite_Button.Content = "Change this Sprite";
                Export_Button.Content = "Export this Sprite";
                Delete_Button.Content = "Delete this Sprite";
                findButton.Content = "Find Sprite";

                //Union_Button.IsEnabled = false;
                Break_Button.IsEnabled = true;
                //moveTo_Button.IsEnabled = false;
                //position_TextBox.IsEnabled = false;

                Insert_folder_Button.IsEnabled = false;
            }
        }

        // почему 0 references?
        private void tilesImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Keyboard.ClearFocus();

            int mx = (int)e.GetPosition(tilesImage).X;
            int my = (int)e.GetPosition(tilesImage).Y;
            int x = mx / 32;
            int y = my / 32;

            int pos = (int)tiles_scrollBar.Value;

            Scena scn = Gl.curr_scena;

            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
            {
                if (tileMode)
                {
                    if ((pos + y) * collumns_count + x >= scn.tileSet.Count)
                        SelectedTile = scn.tileSet.Count - 1;
                    else
                        SelectedTile = (pos + y) * collumns_count + x;

                    //SelectedSprite = scn.tileSet[SelectedTile].SpriteId;
                }
                else
                {
                    int tmp;
                    if (pos + y < scn.tileSet.Count)
                    {
                        tmp = scn.tileSet[pos + y].SpriteId;
                        if (scn.spriteSet[tmp].FramesCount > x)
                            tmp += x;
                        else
                            tmp += scn.spriteSet[tmp].FramesCount - 1;
                        
                        if (tmp >= scn.spriteSet.Count)
                            SelectedSprite = scn.spriteSet.Count - 1;
                        else
                            SelectedSprite = tmp;
                    }
                    else
                        SelectedSprite = scn.spriteSet.Count - 1;
                    //SelectedTile = scn.spriteSet[SelectedSprite].TileId;
                }

                if (e.ChangedButton == MouseButton.Middle)
                {
                    Gl.palette.selected_tile = SelectedTile;
                    Gl.palette.selected_sprite = SelectedSprite;
                    Gl.palette.UpdateGroundSelector(Gl.palette.selected_tile % 8, Gl.palette.selected_tile / 8);
                    Gl.palette.Is_block_selected = false;
                    Gl.main.CursorsUpdate();
                    Gl.main.selectedInfoUpdate();
                }

            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (tileMode)
                {
                    if (SelectedTile_Blue != (pos + y) * collumns_count + x && SelectedTile != (pos + y) * collumns_count + x)
                    {
                        SelectedTile_Blue = (pos + y) * collumns_count + x;
                        if (SelectedTile_Blue >= scn.tileSet.Count)
                            SelectedTile_Blue = scn.tileSet.Count - 1;
                    }
                    else
                        SelectedTile_Blue = -1;
                }
                else
                {
                    if (SelectedTile_Blue != pos + y && SelectedTile != pos + y)
                    {
                        SelectedTile_Blue = (pos + y);
                        if (SelectedTile_Blue >= scn.tileSet.Count)
                            SelectedTile_Blue = scn.tileSet.Count - 1;
                    }
                    else
                        SelectedTile_Blue = -1;
                }
            }

            UpdateSelector();
            UpdateInfo();
            UpdateInfoLabels();
        }
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            int d = e.Delta;

            e.Handled = true;

            if (d > 0)
            {
                tiles_scrollBar.Value -= 1;
            }
            else if (d < 0)
            {
                tiles_scrollBar.Value += 1;
            }

            ScrollBar_Scroll(this, null);

        }

        #region set fields

        private void setFieldsToAllFrames(int tilenumber, string property, object val)
        {
            FieldInfo fieldinfo = typeof(Tile).GetField(property);

            Scena scn = Gl.curr_scena;

            int index = scn.tileSet[tilenumber].SpriteId;
            int n = index + scn.tileSet[tilenumber].FramesCount;

            for (int i = index; i < n; i++)
                fieldinfo.SetValue(scn.spriteSet[i], val);
        }

        private void slowing_numericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isSetUp)

                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "slowing", (int)slowing_numericUpDown.Value);
                }
                else
                {
                    getSelectedSprite().slowing = (int)slowing_numericUpDown.Value;
                }
        }

        private void type_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "slowing", (int)type_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().type = type_combobox.SelectedIndex;
                }
        }

        private void sounds_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "sounds", (int)sounds_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().sounds = sounds_combobox.SelectedIndex;
                }
        }

        private void variant_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "variant", (int)variant_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().variant = variant_combobox.SelectedIndex;
                }
        }

        private void lu_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "leftup", (int)lu_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().leftup = lu_combobox.SelectedIndex;
                }
        }

        private void ru_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "rightup", (int)ru_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().rightup = ru_combobox.SelectedIndex;
                }
        }

        private void ld_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "leftdown", (int)ld_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().leftdown = ld_combobox.SelectedIndex;
                }
        }

        private void rd_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "rightdown", (int)rd_combobox.SelectedIndex);
                }
                else
                {
                    getSelectedSprite().rightdown = rd_combobox.SelectedIndex;
                }
        }

        private void sign_numericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "sign", (int)sign_numericUpDown.Value);
                }
                else
                {
                    getSelectedSprite().sign = (int)sign_numericUpDown.Value;
                }
        }

        private void first_num_numericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "first_num", (int)first_num_numericUpDown.Value);
                }
                else
                {
                    getSelectedSprite().first_num = (int)first_num_numericUpDown.Value;
                }
        }

        private void count_frames_numericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isSetUp)
                if (tileMode)
                {
                    setFieldsToAllFrames(SelectedTile, "count_frames", (int)count_frames_numericUpDown.Value);
                }
                else
                {
                    getSelectedSprite().count_frames = (int)count_frames_numericUpDown.Value;
                }
        }

        private void minimap_ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            if (!isSetUp)
            {
                Color c = e.NewValue;

                byte[] tmp = new byte[2];
                tmp[1] = (byte)((c.R / 8) * 8 + (c.G / 32));
                //tmp[0] = (byte)((g / 4) * 32 + (b / 8));
                tmp[0] = (byte)(((c.G / 4) % 8) * 32 + (c.B / 8));

                //byte c1 = (byte)((tmp[1] / 8) * 8);
                //byte c2 = (byte)((tmp[1] % 8) * 32 + ((tmp[0] / 32) * 4));
                //byte c3 = (byte)((tmp[0] % 32) * 8);
                //
                //Color col = Color.FromRgb(c1, c2, c3);

                Scena scn = Gl.curr_scena;
                int index;
                int n;
                if (tileMode)
                {
                    index = scn.tileSet[SelectedTile].SpriteId;
                    n = index + scn.tileSet[SelectedTile].FramesCount;
                    setFieldsToAllFrames(SelectedTile, "ScenaMinimapColor", tmp);
                    setFieldsToAllFrames(SelectedTile, "MinimapColor", e.NewValue);
                }
                else
                {
                    index = SelectedSprite;
                    n = SelectedSprite + 1;
                    getSelectedSprite().ScenaMinimapColor = tmp;
                    getSelectedSprite().MinimapColor = e.NewValue;
                }

                for (int x = 0; x < scn.size_map_x; x++)
                    for (int y = 0; y < scn.size_map_y; y++)
                        if (scn.map[x, y].SpriteId >= index && scn.map[x, y].SpriteId < n)
                            Gl.minimap.update_pixel_on_minimap(x, y);
            }
        }

        #endregion set fields

        private void tiles_scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateSelector();
        }

        private void Insert_new_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Multiselect = true;

            dlg.Title = "Select bmp";
            dlg.DefaultExt = "*.bmp";
            dlg.Filter = "BitMap|*.bmp";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == false)
                return;

            int n = dlg.FileNames.Length;

            //dlg.FileNames.Sort

            //BitmapImage[] bitImg = new BitmapImage[n];
            BitmapImage bitImg = null;
            Scena scn = Gl.curr_scena;

            if (tileMode)
            {

                InsertTile(SelectedTile, (bool)insertmode_checkBox.IsChecked, dlg.FileNames.ToList());

            }
            else
            {
                #region spriteMode
                int tmp_selTile = SelectedTile;
                int tmp_selSprite = SelectedSprite;

                int frames_count = scn.tileSet[tmp_selTile].FramesCount; // количество кадров было.
                int first_frame_id = scn.spriteSet[tmp_selSprite].FirstSpriteId; // спрайтовый номер первого кадра.
                int last_frame_id = first_frame_id + frames_count + n - 1; // спрайтовый номер последнего кадра (с учетом вставленного).

                Tile first_frame = scn.spriteSet[tmp_selSprite];

                if ((bool)insertmode_checkBox.IsChecked == false)
                    tmp_selSprite++;

                //n += frames_count; // новые спрайты + которые уже были.

                // вставка спрайтов
                for (int i = 0; i < n; i++)
                {
                    if (System.IO.File.Exists(dlg.FileNames[i]))
                        bitImg = new BitmapImage(new Uri(dlg.FileNames[i], UriKind.Relative));
                    else
                        throw new Exception("Файл не найден");

                    Tile spr = new Tile();
                    scn.spriteSet.Insert(tmp_selSprite + i, spr);

                    /**/

                    // image
                    scn.ground_sprites.changeSpriteImage(spr, bitImg);

                    string name_cfg = System.IO.Path.GetDirectoryName(dlg.FileNames[i]) + "\\" + System.IO.Path.GetFileNameWithoutExtension(dlg.FileNames[i]) + ".tile";
                    if (File.Exists(name_cfg))
                    {
                        using (var inStream = File.Open(name_cfg, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] bytes = new byte[8];
                            inStream.Read(bytes, 0, 8);
                            spr.getFromArray(bytes);
                            inStream.Read(bytes, 0, 2);

                            byte c1 = (byte)((bytes[1] / 8) * 8);
                            byte c2 = (byte)((bytes[1] % 8) * 32 + ((bytes[0] / 32) * 4));
                            byte c3 = (byte)((bytes[0] % 32) * 8);

                            spr.MinimapColor = Color.FromRgb(c1, c2, c3);
                            spr.ScenaMinimapColor = new byte[] { bytes[0], bytes[1] };
                        }
                    }
                    else
                    {
                        // copy filds from first frame
                        spr.CopyAllFieldsFroms(first_frame);
                    }

                }

                Tile tmp_spr;

                // обновление индексов у спрайтов текущего тайла
                for (int i = first_frame_id; i <= last_frame_id; i++)
                {
                    tmp_spr = scn.spriteSet[i];

                    #region scena_fields
                    if (i < first_frame_id + frames_count + n - 1) // не последний кадр
                        tmp_spr.sign = 1;
                    else // последний кадр
                        tmp_spr.sign = 256 - (frames_count + n - 1);

                    if (i == first_frame_id) // первый кадр
                        tmp_spr.first_num = 256;
                    else if (i < first_frame_id + frames_count + n - 1) // средние кадры
                        tmp_spr.first_num = 0;
                    else // последний кадр
                        tmp_spr.first_num = 255;
                    #endregion scena_fields

                    #region my fields
                    tmp_spr.TileId = tmp_selTile;
                    tmp_spr.SpriteId = i;

                    tmp_spr.FirstSpriteId = first_frame_id;
                    tmp_spr.NFrame = i - first_frame_id;
                    if (i == first_frame_id)
                        tmp_spr.FramesCount = frames_count + n;
                    else
                        tmp_spr.FramesCount = 0;
                    #endregion my fields
                }
                //if(scn.spriteSet[first_frame_id].SpriteId<first_frame.sprite
                scn.tileSet[tmp_selTile] = scn.spriteSet[first_frame_id]; // первый кадр мог поменяться

                // обновление индексов у остального спрайтсета.
                for (int i = last_frame_id + 1; i < scn.spriteSet.Count; i++)
                {
                    tmp_spr = scn.spriteSet[i];

                    tmp_spr.SpriteId += n;
                    tmp_spr.FirstSpriteId += n;
                }
                #endregion spriteMode
            }

            // теперь спрайт под этим номером может принадлежать другому тайлу
            _selTile = scn.spriteSet[SelectedSprite].TileId;

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
        }

        private void Insert_folder_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!CommonFileDialog.IsPlatformSupported)
                return;

            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            CommonFileDialogResult result = dlg.ShowDialog();

            if (result != CommonFileDialogResult.Ok)
                return;

            List<string> files = Directory.GetFiles(dlg.FileName).ToList();

            for (int i = files.Count - 1; i >= 0; i--)
            {
                if (System.IO.Path.GetExtension(files[i]).ToLower() != ".bmp")
                {
                    files.RemoveAt(i);
                }
            }


            files.Sort(delegate(string x, string y)
            {
                if (x == null && y == null) return 0;
                else if (x == null) return -1;
                else if (y == null) return 1;
                else
                {
                    string[] s1 = System.IO.Path.GetFileNameWithoutExtension(x).Split('_');
                    string[] s2 = System.IO.Path.GetFileNameWithoutExtension(y).Split('_');

                    int i1, i2;

                    bool b1 = Int32.TryParse(s1[0], out i1);
                    bool b2 = Int32.TryParse(s2[0], out i2);

                    int cmp = -5;

                    if (b1 && b2)
                        cmp = i1.CompareTo(i2);
                    else if (!b1 && !b2)
                        cmp = 0;
                    else if (b1 && !b2)
                        cmp = 1;
                    else if (!b1 && b2)
                        cmp = -1;

                    if (cmp == 0 && s1.Count() == 2 && s2.Count() == 2 && b1 && b2)
                    {
                        b1 = Int32.TryParse(s1[1], out i1);
                        b2 = Int32.TryParse(s2[1], out i2);

                        if (b1 && b2)
                            cmp = i1.CompareTo(i2);
                        else if (!b1 && !b2)
                            cmp = 0;
                        else if (b1 && !b2)
                            cmp = 1;
                        else if (!b1 && b2)
                            cmp = -1;

                        return cmp;
                    }
                    else
                        return cmp;
                }
            });


            int curr_tile = -1;
            int tmp;
            List<List<string>> tiles = new List<List<string>>();

            foreach (string f in files)
            {
                string[] s = System.IO.Path.GetFileNameWithoutExtension(f).Split('_');

                bool b1 = Int32.TryParse(s[0], out tmp);

                if (curr_tile != tmp || !b1)
                {
                    curr_tile = tmp;
                    tiles.Add(new List<string>());
                }

                tiles.Last().Add(f);
            }

            foreach (List<string> list in tiles)
            {
                InsertTile(SelectedTile, (bool)insertmode_checkBox.IsChecked, list);
                SelectedTile++;
            }


            Scena scn = Gl.curr_scena;

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
        }

        private void moveTo_Button_Click(object sender, RoutedEventArgs e)
        {
            int new_pos_tile;
            Scena scn = Gl.curr_scena;
            if (!Int32.TryParse(position_TextBox.Text, out new_pos_tile))
                return;
            if (new_pos_tile < 0 || new_pos_tile >= scn.tileSet.Count)
                return;
            if (SelectedTile == new_pos_tile)
                return;

            int old_pos_tile = SelectedTile;
            int old_pos_sprite = scn.tileSet[SelectedTile].SpriteId;
            int count_sprites = scn.tileSet[SelectedTile].FramesCount;

            int new_pos_sprite = scn.tileSet[new_pos_tile].SpriteId;

            Tile tmp;
            tmp = scn.tileSet[SelectedTile];

            if (old_pos_sprite > new_pos_sprite)
            { // значит двигаем назад
                scn.tileSet.Remove(tmp);
                scn.tileSet.Insert(new_pos_tile, tmp);

                for (int i = 0; i < count_sprites; i++)
                {
                    tmp = scn.spriteSet[old_pos_sprite + i];

                    tmp.SpriteId = new_pos_sprite + i;
                    tmp.TileId = new_pos_tile;
                    tmp.FirstSpriteId = new_pos_sprite;

                    scn.spriteSet.Remove(tmp);
                    scn.spriteSet.Insert(new_pos_sprite + i, tmp);
                }

                int start = new_pos_sprite + count_sprites;
                int end = old_pos_sprite + count_sprites;
                for (int i = start; i < end; i++)
                {
                    tmp = scn.spriteSet[i];

                    tmp.TileId += 1;
                    tmp.SpriteId += count_sprites;
                    tmp.FirstSpriteId += count_sprites;
                }
            }
            else // вперед
            {
                int count_replasing_sprites = scn.tileSet[new_pos_tile].FramesCount; // сколько кадров у тайла, вместо которого хотим стать.
                new_pos_sprite += count_replasing_sprites - 1;

                scn.tileSet.Remove(tmp);
                scn.tileSet.Insert(new_pos_tile, tmp);

                for (int i = 0; i < count_sprites; i++)
                {
                    tmp = scn.spriteSet[old_pos_sprite];

                    tmp.SpriteId = new_pos_sprite - count_sprites + i + 1;
                    tmp.TileId = new_pos_tile;
                    tmp.FirstSpriteId = new_pos_sprite - count_sprites + 1;

                    scn.spriteSet.Remove(tmp);
                    scn.spriteSet.Insert(new_pos_sprite, tmp);
                }

                int start = old_pos_sprite;
                int end = new_pos_sprite - count_sprites + 1;
                for (int i = start; i < end; i++)
                {
                    tmp = scn.spriteSet[i];

                    tmp.TileId -= 1;
                    tmp.SpriteId -= count_sprites;
                    tmp.FirstSpriteId -= count_sprites;
                }
            }


            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
        }


        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;

            if (tileMode)
            {
                if (SelectedTile_Blue == -1)
                {
                    DeleteTile(SelectedTile);
                }
                else
                {
                    int min = Math.Min(SelectedTile, SelectedTile_Blue);
                    int max = Math.Max(SelectedTile, SelectedTile_Blue) + 1;
                    MessageBoxResult res = System.Windows.MessageBox.Show("Delete " + (max - min) + " tiles?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        for (int i = min; i < max; i++)
                            DeleteTile(min); // несколько раз удаляем первый тайл из группы.
                    }
                }
            }
            else
            {
                if (scn.spriteSet.Count == 1)
                    return;

                int dt = 0; // 1 - если были удалены все кадры из анимации
                int frames_count = scn.tileSet[SelectedTile].FramesCount; // количество кадров.
                int first_frame_id = scn.spriteSet[SelectedSprite].FirstSpriteId; // спрайтовый номер первого кадра.
                int last_frame_id = first_frame_id + frames_count - 1; // спрайтовый номер последнего кадра.


                if (last_frame_id == first_frame_id)
                { // у анимации был только один кадр. Значит удаляем тайл
                    scn.tileSet.RemoveAt(SelectedTile);
                    dt = 1;
                }
                else if (last_frame_id == SelectedSprite)
                { // выбран завершающий кадр.
                    scn.spriteSet[first_frame_id].FramesCount--;
                    //if (last_frame_id - 1 != first_frame_id)
                    //{ // кадров останется больше одного
                    scn.spriteSet[SelectedSprite - 1].sign = scn.spriteSet[SelectedSprite].sign + 1;
                    scn.spriteSet[SelectedSprite - 1].first_num = 255;
                    //}
                    //else
                    //{
                    //    scn.spriteSet[SelectedSprite - 1].first_num = 256;
                    //    scn.spriteSet[SelectedSprite - 1].sign = 1;
                    //}
                }
                else
                {
                    if (SelectedSprite == first_frame_id)
                    { // выбран первый кадр.
                        scn.spriteSet[SelectedSprite + 1].first_num = 256;
                        scn.spriteSet[last_frame_id].sign++;
                        scn.spriteSet[first_frame_id + 1].FramesCount = frames_count - 1;

                        // Первый тайл теперь будет другим, поэтому его надо заменить в списке
                        scn.tileSet.RemoveAt(SelectedTile);
                        scn.tileSet.Insert(SelectedTile, scn.spriteSet[first_frame_id + 1]);

                        first_frame_id++; // ВНИМАНИЕ !! это уже "новый" первый кадр
                    }
                    else
                    { // выбран средний кадр.
                        scn.spriteSet[last_frame_id].sign++;
                        scn.spriteSet[first_frame_id].FramesCount--;
                    }
                }

                // вдруг остался один кадр
                if (scn.spriteSet[first_frame_id].FramesCount == 1)
                {
                    scn.spriteSet[first_frame_id].sign = 0;
                    scn.spriteSet[first_frame_id].first_num = 256;
                }

                // обновление индексов у тайлсета.
                for (int i = SelectedSprite; i < scn.spriteSet.Count; i++)
                {
                    if (i > last_frame_id)
                    {
                        scn.spriteSet[i].TileId -= dt;
                        scn.spriteSet[i].FirstSpriteId -= 1;
                    }
                    else
                    {
                        scn.spriteSet[i].NFrame -= 1;
                    }
                    scn.spriteSet[i].SpriteId -= 1;
                }

                // удаляем удаляемое
                scn.spriteSet.RemoveAt(SelectedSprite);

            }

            if (SelectedSprite >= scn.spriteSet.Count || SelectedTile >= scn.tileSet.Count)
            {
                SelectedSprite = scn.spriteSet.Count - 1;
                SelectedTile = scn.spriteSet[SelectedSprite].TileId;
            }

            // теперь спрайт под этим номером может принадлежать другому тайлу
            _selTile = scn.spriteSet[SelectedSprite].TileId;

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
            /*
              

                Scena scn = Gl.curr_scena;
                int index;
                int n;
                if (tileMode)
                {
                    index = scn.tileSet[SelectedTile].SpriteId;
                    n = index + scn.tileSet[SelectedTile].FramesCount;
                    setFieldsToAllFrames(SelectedTile, "ScenaMinimapColor", tmp);
                    setFieldsToAllFrames(SelectedTile, "MinimapColor", e.NewValue);
                }
                else
                {
                    index = SelectedSprite;
                    n = SelectedSprite + 1;
                    getSelectedSprite().ScenaMinimapColor = tmp;
                    getSelectedSprite().MinimapColor = e.NewValue;
                }

                for (int x = 0; x < scn.size_map_x; x++)
                    for (int y = 0; y < scn.size_map_y; y++)
                        if (scn.map[x, y].SpriteId >= index && scn.map[x, y].SpriteId < n)
                            Gl.minimap.update_pixel_on_minimap(x, y);              
             
             */
        }

        private void changeSprite_Button_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("Function deactivated");

            Scena scn = Gl.curr_scena;

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Select bmp";
            dlg.DefaultExt = "*.bmp";
            dlg.Filter = "BitMap|*.bmp";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == false || !System.IO.File.Exists(dlg.FileName))
                return;

            int n = dlg.FileNames.Length;

            BitmapImage bitImg = null;


            if (tileMode)
            {
                for (int i = 0; i < n; i++)
                {
                    if (System.IO.File.Exists(dlg.FileNames[i]))
                        bitImg = new BitmapImage(new Uri(dlg.FileNames[i], UriKind.Relative));
                    else
                        throw new Exception("Файл не найден");
                    Gl.curr_scena.ground_sprites.changeSpriteImage(Gl.curr_scena.spriteSet[SelectedSprite + i], bitImg);
                }
            }
            else
            {
                bitImg = new BitmapImage(new Uri(dlg.FileName, UriKind.Relative));
                Gl.curr_scena.ground_sprites.changeSpriteImage(Gl.curr_scena.spriteSet[SelectedSprite], bitImg);
            }

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
        }

        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            if (tileMode)
            {
                if (SelectedTile_Blue == -1)
                    dlg.FileName = "" + SelectedTile;
                else
                    dlg.FileName = "" + Math.Min(SelectedTile, SelectedTile_Blue);
                dlg.Title = "Export tile";
            }
            else
            {
                dlg.FileName = SelectedTile + "_" + Gl.curr_scena.spriteSet[SelectedSprite].NFrame;
                dlg.Title = "Export sprite";
            }
            dlg.DefaultExt = ".bmp";
            dlg.Filter = "Bitmaps (.bmp)|*.bmp";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == false)
                return;

            string filename = dlg.FileName;

            if (tileMode)
            {
                if (SelectedTile_Blue == -1)
                {
                    string str = System.IO.Path.GetDirectoryName(filename) + "\\" + System.IO.Path.GetFileNameWithoutExtension(filename);
                    ExportTile(str, SelectedTile);
                }
                else
                {
                    int min = Math.Min(SelectedTile, SelectedTile_Blue);
                    int max = Math.Max(SelectedTile, SelectedTile_Blue) + 1;
                    for (int i = min; i < max; i++)
                    {
                        string str = System.IO.Path.GetDirectoryName(filename) + "\\" + i;
                        ExportTile(str, i);
                    }
                }
            }
            else
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)Gl.curr_scena.spriteSet[SelectedSprite].Sprite));

                using (var filestream = new FileStream(filename, FileMode.Create))
                    encoder.Save(filestream);
            }
        }

        public void DeleteTile(int number)
        {
            Scena scn = Gl.curr_scena;

            if (scn.tileSet.Count == 1) // костыль от удаления всех тайлов
                return;

            if (number >= scn.tileSet.Count || number < 0)
                return;

            int index = scn.tileSet[number].SpriteId;
            int n = scn.tileSet[number].FramesCount;

            for (int i = 0; i < n; i++)
                scn.spriteSet.RemoveAt(index);
            scn.tileSet.RemoveAt(number);


            // обновление индексов у тайлсета.
            for (int i = index; i < scn.spriteSet.Count; i++)
            {
                scn.spriteSet[i].TileId -= 1;
                scn.spriteSet[i].SpriteId -= n;
                scn.spriteSet[i].FirstSpriteId -= n;
            }
        }

        private void ExportTile(string filename, int n)
        {
            int spr_num = Gl.curr_scena.tileSet[n].FirstSpriteId;
            for (int i = 0; i < Gl.curr_scena.tileSet[n].FramesCount; i++)
            {
                string name = filename + "_" + i + ".bmp";

                BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)Gl.curr_scena.spriteSet[spr_num + i].Sprite));

                using (var filestream = new FileStream(name, FileMode.Create))
                    encoder.Save(filestream);

                string name_cfg = filename + "_" + i + ".tile";
                using (var filestream = new FileStream(name_cfg, FileMode.Create))
                {
                    filestream.Write(Gl.curr_scena.spriteSet[spr_num + i].composeArray(), 0, 8);
                    filestream.Write(Gl.curr_scena.spriteSet[spr_num + i].ScenaMinimapColor, 0, 2);
                }
            }
        }

        private void InsertTile(int tile_ind, bool before, List<string> list)
        {
            int n = list.Count;
            BitmapImage bitImg = null;
            Scena scn = Gl.curr_scena;

            int spr_ind = scn.tileSet[tile_ind].SpriteId;
            int tmp_selTile = tile_ind;

            if (before == false)
            {
                tmp_selTile++;
                if (tmp_selTile < scn.tileSet.Count)
                    spr_ind = scn.tileSet[tmp_selTile].SpriteId;
                else
                    spr_ind = scn.spriteSet.Count;
            }

            for (int i = 0; i < n; i++)
            {
                if (System.IO.File.Exists(list[i]))
                    bitImg = new BitmapImage(new Uri(list[i], UriKind.Relative));
                else
                {
                    System.Windows.MessageBox.Show("Файл не найден");
                    throw new Exception("Файл не найден");
                }

                Tile spr = new Tile();
                scn.spriteSet.Insert(spr_ind + i, spr);

                // image
                scn.ground_sprites.changeSpriteImage(spr, bitImg);


                #region scena_fields
                string name_cfg = System.IO.Path.GetDirectoryName(list[i]) + "\\" + System.IO.Path.GetFileNameWithoutExtension(list[i]) + ".tile";
                if (File.Exists(name_cfg))
                {
                    using (var inStream = File.Open(name_cfg, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        byte[] bytes = new byte[8];
                        inStream.Read(bytes, 0, 8);
                        spr.getFromArray(bytes);
                        inStream.Read(bytes, 0, 2);

                        byte c1 = (byte)((bytes[1] / 8) * 8);
                        byte c2 = (byte)((bytes[1] % 8) * 32 + ((bytes[0] / 32) * 4));
                        byte c3 = (byte)((bytes[0] % 32) * 8);

                        spr.MinimapColor = Color.FromRgb(c1, c2, c3);
                        spr.ScenaMinimapColor = new byte[] { bytes[0], bytes[1] };
                    }
                }
                else
                {
                    spr.leftdown = (int)TileType.Ice;
                    spr.leftup = (int)TileType.Ice;
                    spr.rightdown = (int)TileType.Ice1;
                    spr.rightup = (int)TileType.Ice;
                }

                if (n == 1) // тайл без анимации
                    spr.sign = 0;
                else // с анимацией
                    if (i < n - 1) // не последний кадр
                        spr.sign = 1;
                    else // последний кадр
                        spr.sign = 256 - (n - 1);

                if (i == 0) // первый кадр
                    spr.first_num = 256;
                else if (i < n - 1) // средние кадры
                    spr.first_num = 0;
                else // последний кадр
                    spr.first_num = 255;

                #endregion scena_fields

                #region my fields
                spr.TileId = tmp_selTile;
                spr.SpriteId = spr_ind + i;

                spr.FirstSpriteId = spr_ind;
                spr.NFrame = i;
                if (i == 0)
                    spr.FramesCount = n;
                else
                    spr.FramesCount = 0;
                #endregion my fields


                spr.ExpectForm();
            }

            // первый спрайт в тайлсет
            scn.tileSet.Insert(tmp_selTile, scn.spriteSet[spr_ind]);

            // обновление индексов у тайлсета.
            for (int i = spr_ind + n; i < scn.spriteSet.Count; i++)
            {
                scn.spriteSet[i].TileId += 1;
                scn.spriteSet[i].SpriteId += n;
                scn.spriteSet[i].FirstSpriteId += n;
            }
        }

        private void Union_Button_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;

            if (SelectedTile >= scn.tileSet.Count - 1)
                return;

            int first_index = scn.tileSet[SelectedTile].SpriteId;
            int last_index = scn.tileSet[SelectedTile + 1].FirstSpriteId + scn.tileSet[SelectedTile + 1].FramesCount;
            int count = last_index - first_index;

            Tile tmp;

            // Update united tiles
            for (int i = first_index; i < last_index; i++)
            {
                tmp = scn.spriteSet[i];

                #region scena_fields
                if (i < first_index + count - 1) // не последний кадр
                    tmp.sign = 1;
                else // последний кадр
                    tmp.sign = 256 - (count - 1);

                if (i == first_index) // первый кадр
                    tmp.first_num = 256;
                else if (i < first_index + count - 1) // средние кадры
                    tmp.first_num = 0;
                else // последний кадр
                    tmp.first_num = 255;
                #endregion scena_fields

                #region my fields
                tmp.FirstSpriteId = first_index;
                tmp.TileId = SelectedTile;
                tmp.NFrame = i - first_index;

                if (i == first_index)
                    tmp.FramesCount = count;
                else
                    tmp.FramesCount = 0;

                #endregion my fields

            }

            scn.tileSet.RemoveAt(SelectedTile + 1);

            // Update other tiles
            for (int i = last_index; i < scn.spriteSet.Count; i++)
                scn.spriteSet[i].TileId -= 1;

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
        }

        private void Break_Button_Click(object sender, RoutedEventArgs e)
        {
            Scena scn = Gl.curr_scena;

            if (tileMode)
                return;

            int first_index = scn.spriteSet[SelectedSprite].FirstSpriteId;                      // первый кадр тайла
            int count = scn.spriteSet[scn.spriteSet[SelectedSprite].FirstSpriteId].FramesCount; // всего кадров в тайле
            int last_index = scn.spriteSet[SelectedSprite].FirstSpriteId + count;               // последний кадр тайла
            int count1 = SelectedSprite - first_index;                                          // количество кадров слева от выделенного
            int count2 = last_index - SelectedSprite;                                           // количество кадров справа от выделенного (+1)

            if (first_index == SelectedSprite)
                return;

            Tile tmp;

            #region first
            // Update first part
            for (int i = first_index; i < SelectedSprite; i++)
            {
                tmp = scn.spriteSet[i];

                #region scena_fields
                if (count1 == 1) // тайл без анимации
                    tmp.sign = 0;
                else // с анимацией
                    if (i < first_index + count1 - 1) // не последний кадр
                        tmp.sign = 1;
                    else // последний кадр
                        tmp.sign = 256 - (count1 - 1);

                if (i == first_index) // первый кадр
                    tmp.first_num = 256;
                else if (i < first_index + count1 - 1) // средние кадры
                    tmp.first_num = 0;
                else // последний кадр
                    tmp.first_num = 255;
                #endregion scena_fields

                #region my fields
                //tmp.FirstSpriteId = first_index;
                //tmp.NFrame = i - first_index;

                if (i == first_index)
                    tmp.FramesCount = count1;
                else
                    tmp.FramesCount = 0;
                #endregion my fields
            }
            #endregion first

            scn.tileSet.Insert(SelectedTile + 1, scn.spriteSet[SelectedSprite]);

            #region second
            // Update second part
            for (int i = SelectedSprite; i < last_index; i++)
            {
                tmp = scn.spriteSet[i];

                #region scena_fields
                if (count2 == 1) // тайл без анимации
                    tmp.sign = 0;
                else // с анимацией
                    if (i < SelectedSprite + count2 - 1) // не последний кадр
                        tmp.sign = 1;
                    else // последний кадр
                        tmp.sign = 256 - (count2 - 1);

                if (i == SelectedSprite) // первый кадр
                    tmp.first_num = 256;
                else if (i < SelectedSprite + count2 - 1) // средние кадры
                    tmp.first_num = 0;
                else // последний кадр
                    tmp.first_num = 255;
                #endregion scena_fields

                #region my fields
                tmp.FirstSpriteId = SelectedSprite;
                tmp.TileId = SelectedTile + 1;
                tmp.NFrame = i - SelectedSprite;

                if (i == SelectedSprite)
                    tmp.FramesCount = count2;
                else
                    tmp.FramesCount = 0;
                #endregion my fields
            }
            #endregion second

            // Update other tiles
            for (int i = last_index; i < scn.spriteSet.Count; i++)
                scn.spriteSet[i].TileId += 1;

            // генерируем палитру.
            Gl.palette.InitLandPalette(scn);

            FullUpdate();
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int tmp = ((int)(tiles_border.ActualHeight / 32)) * 32;
            tilesImage.Height = tmp;
            tiles_scrollViewer.Height = tmp;
            tilesImage.Width = tiles_border.ActualWidth - 16;
            tiles_scrollViewer.Width = tiles_border.ActualWidth - 16;
            UpdateImage();
            UpdateScroll();
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool flag = false;
            if (e.Key == Key.NumPad4)
            {
                if (tileMode && SelectedTile > 0)
                { // тайлы
                    SelectedTile--;
                    flag = true;
                }
                else if (SelectedSprite > 0)
                { // спрайты
                    SelectedSprite--;
                    flag = true;
                }
            }
            if (e.Key == Key.NumPad6)
            {
                if (tileMode && SelectedTile < Gl.curr_scena.tileSet.Count)
                { // тайлы
                    SelectedTile++;
                    flag = true;
                }
                else if (SelectedSprite < Gl.curr_scena.spriteSet.Count)
                { // спрайты
                    SelectedSprite++;
                    flag = true;
                }
            }
            if (e.Key == Key.NumPad8)
            {
                if (tileMode)
                { // тайлы
                    if (SelectedTile < collumns_count)
                        SelectedTile = 0;
                    else
                        SelectedTile -= collumns_count;
                    flag = true;
                }
                else if (SelectedTile > 0)
                { // спрайты
                    SelectedTile--;
                    flag = true;
                }
            }
            if (e.Key == Key.NumPad2)
            {
                if (tileMode)
                { // тайлы
                    if (SelectedTile > Gl.curr_scena.tileSet.Count - collumns_count)
                        SelectedTile = Gl.curr_scena.tileSet.Count - 1;
                    else
                        SelectedTile += collumns_count;
                    flag = true;
                }
                else if (SelectedTile < Gl.curr_scena.tileSet.Count)
                { // спрайты
                    SelectedTile++;
                    flag = true;
                }
            }
            if (flag)
            {
                FullUpdate();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void tilesImage_KeyDown(object sender, KeyEventArgs e)
        {
            //OnKeyDown(e);
        }

        private void tiles_scrollViewer_KeyDown(object sender, KeyEventArgs e)
        {
            //OnKeyDown(e);
        }

        private void tiles_canvas_KeyDown(object sender, KeyEventArgs e)
        {
            //OnKeyDown(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int val;
            Scena scn = Gl.curr_scena;
            if (!Int32.TryParse(position_find_TextBox.Text, out val))
                return;

            if (tileMode)
            {
                SelectedTile = val;
                SelectedSprite = scn.tileSet[val].SpriteId;
                tiles_scrollBar.Value = (SelectedTile - 5 * collumns_count) / collumns_count;
            }
            else
            {
                SelectedTile = scn.spriteSet[val].TileId;
                SelectedSprite = val;
                tiles_scrollBar.Value = SelectedTile - 5;
            }
            ScrollBar_Scroll(this, null);
            UpdateSelector();
            UpdateInfo();
        }

        private void TEST_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = false;

            dlg.Title = "replasing conf";
            dlg.DefaultExt = "*.txt";
            dlg.Filter = "config (*.txt)|*.txt";

            result = dlg.ShowDialog();

            if (result == true)
            {
                StreamReader strd = new StreamReader(dlg.FileName);
                Scena scn = Gl.curr_scena;
                string s = strd.ReadLine();
                int n = 0;
                Dictionary<int, List<string>> diffs = new Dictionary<int, List<string>>();
                int d = 0; // количество тайлов, которые различаются свойствами, но одинаковые картинки
                int q = 0; // количество пройденых тайлов с одинаковыми картинками

                for (int i = 0; s != null; i++)
                {
                    n = Convert.ToInt32(s);
                    if (n + q != i)
                    {
                        q++;
                        diffs.Add(i, new List<string>());
                        if (scn.tileSet[i].slowing != scn.tileSet[n + q].slowing) diffs[i].Add("slowing");
                        if (scn.tileSet[i].type != scn.tileSet[n + q].type) diffs[i].Add("type");
                        if (scn.tileSet[i].sounds != scn.tileSet[n + q].sounds) diffs[i].Add("sounds");
                        if (scn.tileSet[i].variant != scn.tileSet[n + q].variant) diffs[i].Add("variant");

                        /*if (scn.tileSet[i].leftup != scn.tileSet[n +q].leftup) diffs[i].Add("leftup");
                        if (scn.tileSet[i].rightup != scn.tileSet[n  +q].rightup) diffs[i].Add("rightup");
                        if (scn.tileSet[i].leftdown != scn.tileSet[n +q].leftdown) diffs[i].Add("leftdown");
                        if (scn.tileSet[i].rightdown != scn.tileSet[n+q].rightdown) diffs[i].Add("rightdown");*/

                        if (scn.tileSet[i].sign != scn.tileSet[n + q].sign) diffs[i].Add("sign");
                        if (scn.tileSet[i].first_num != scn.tileSet[n + q].first_num) diffs[i].Add("first_num");
                        if (scn.tileSet[i].count_frames != scn.tileSet[n + q].count_frames) diffs[i].Add("count_frames");

                        if (diffs[i].Count != 0)
                            d++;
                    }
                    s = strd.ReadLine();
                }

                MessageBox.Show("d = " + d, "comparing done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }



    }
}



/* TO DO:
 * 
 * 
 * изменение SelectedTile переносить scroll ( мб запилить как свойство?)
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * авто-цвет миникарты и авто-свойства новым тайлам ( вроде работает. Проверить)
 * 
 * 
 
 * BUGs:
 * 
 * keydown focus
 * 
 * 
*/