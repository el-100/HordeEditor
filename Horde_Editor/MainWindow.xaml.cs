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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Diagnostics;

using Horde_Editor.Common;
using Horde_Editor.History;
using Horde_ClassLibrary;
using Misc;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool paint_mode;                      // true, когда нажата ЛКМ на canvas
        public bool drag_mode;                       // true, когда нажата ПКМ на canvas
        public bool select_unit_mode;                // true, когда есть рамка выделения юнитов
        public bool _select_land_mode;               // true, когда есть рамка выделения земли

        public bool select_land_mode
        {
            get { return _select_land_mode; }
            set
            {
                if (Gl.scena_prop_window != null)
                {
                    if (value == true)
                    {
                        Gl.scena_prop_window.autotile_fill_button.Content = "Fill region selected autotile";
                        Gl.scena_prop_window.tile_fill_button.Content = "Fill region selected tile";
                        Gl.scena_prop_window.block_fill_button.Content = "Fill region selected block";
                        Gl.scena_prop_window.resources_clear_button.Content = "Clear resources in region";
                        Gl.scena_prop_window.units_clear_button.Content = "Clear units in region";
                    }
                    else
                    {
                        Gl.scena_prop_window.autotile_fill_button.Content = "Fill map selected autotile";
                        Gl.scena_prop_window.tile_fill_button.Content = "Fill map selected tile";
                        Gl.scena_prop_window.block_fill_button.Content = "Fill map selected block";
                        Gl.scena_prop_window.resources_clear_button.Content = "Clear all resources";
                        Gl.scena_prop_window.units_clear_button.Content = "Clear all units";
                    }
                }
                _select_land_mode = value;
            }
        }
        public bool _paste_units_mode = false;       // true, при вставке юнитов на ctrl+v
        public bool paste_units_mode
        {
            get { return _paste_units_mode; }
            set
            {
                units_Tree.IsEnabled = !value;
                _paste_units_mode = value;
            }
        }

        Microsoft.Win32.OpenFileDialog dlg;

        #region clicks
        public Point lkm_click = new Point();                      // запомним координаты, где была нажата ЛКМ
        public Point pkm_click = new Point();                      // запомним координаты, где была нажата ПКМ
        public Point pkm_click_scrollPos = new Point();            // запомним скроллинг, тогда когда была нажата ПКМ

        #endregion

        // История 
        static HistoryPointEqualityComparer HistoryPointEqC = new HistoryPointEqualityComparer();
        public Dictionary<HistoryPoint, SetLandEvent> LandTmpEvents = new Dictionary<HistoryPoint, SetLandEvent>(HistoryPointEqC);
        public List<SetResourceEvent> ResourceTmpEvents = new List<SetResourceEvent>();
        public List<SetUnitEvent> UnitsTmpEvents = new List<SetUnitEvent>();

        public MainWindow()
        {
            InitializeComponent();

        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.Title = "Horde Editor " + Gl.Editor_Version;

            Gl.running_dir = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            Gl.running_file = System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0]);

            // Блок загрузки настроек:
            bool? result;
            string horde_path = "";

            #region set up conf

            #region open config

            Dictionary<string, string> settings = new Dictionary<string, string>();
            string config = "";
            string settings_path = Gl.running_dir + @"\configs\Settings.cfg";
            string settings_dir = Gl.running_dir + @"\configs";

            if (File.Exists(settings_path))
            {
                StreamReader str = new StreamReader(settings_path);
                config = str.ReadToEnd();

                settings = JSON.ParseJson(config);
                str.Close();
            }
            else
            {
                Directory.CreateDirectory(settings_dir);
                var file = File.Create(settings_path);
                file.Close();
            }

            #endregion open config

            #region horde2.exe
            if (settings.ContainsKey("horde2_path") && File.Exists(settings["horde2_path"]))
            {
                horde_path = settings["horde2_path"];
            }
            else
            {
                #region get horde2.exe


                dlg = new Microsoft.Win32.OpenFileDialog();

                dlg.Title = "Select path to horde2.exe";
                dlg.DefaultExt = "horde2.exe";
                dlg.Filter = "horde2.exe|horde2.exe";

                result = dlg.ShowDialog();


                if (result == true)
                {
                    horde_path = dlg.FileName;
                }
                else if (result == false)
                {
                    this.Close();
                    return;
                }

                #endregion get horde.exe

                if (settings.ContainsKey("horde2_path"))
                    settings["horde2_path"] = horde_path;
                else
                    settings.Add("horde2_path", horde_path);
            }

            Gl.horde2_exe_Path = horde_path;
            #endregion horde2.exe
            //Gl.horde2_exe_Path = Gl.running_dir + @"\resources\horde2.exe"; // Костыль, от моего перекомпиленого экзешника.
            Gl.horde2_folder_Path = new DirectoryInfo(horde_path).Parent.FullName;

            #region associate
            bool need_restart = false;
            // ассоциацию
            if (settings.ContainsKey("FileAssociation") && settings["FileAssociation"] == "Yes")
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

                if (isElevated)
                {
                    if (FileAssociation.IsAssociated)
                        FileAssociation.Remove();
                    FileAssociation.Associate("Horde scena file", "resources\\scn_file.ico");
                }
            }
            else
            {
                MessageBoxResult res = MessageBox.Show("Do you want set association Horde Editor with *.scn files?", "File association", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    settings["FileAssociation"] = "Yes";

                    WindowsIdentity identity = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    if (isElevated)
                    {
                        if (FileAssociation.IsAssociated)
                            FileAssociation.Remove();
                        FileAssociation.Associate("Horde scena file", "resources\\scn_file.ico");
                    }
                    else
                    {
                        res = MessageBox.Show("Need administrator rights to associate it.\r\n Restart Horde Editor with administrator rights?", "Need administrator rights", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (res == MessageBoxResult.Yes)
                            need_restart = true;
                    }
                }
                else
                    settings["FileAssociation"] = "No";
            }
            #endregion associate

            //if (settings.ContainsKey("last_scena_path"))
            //{
            //    scena_path = settings["last_scena_path"];
            //}
            //else
            //{

            //}

            config = JSON.WriteJson(settings);

            StreamWriter stw = new StreamWriter(settings_path);
            stw.Write(config);
            stw.Close();

            // загрузиться с правами админа для ассоциации.
            if (need_restart)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(Gl.running_file);
                startInfo.Verb = "runas";
                Process.Start(startInfo);

                this.Close();
                return;
            }

            #endregion set up conf
            // Блок загрузки настроек^

            // Устанавливаем глобальные переменные
            Gl.main = this;
            Gl.minimap = new MiniMap();


            #region images

            cursor_Block.Init(4, 4);

            /*// Сдвигаем все image_Mega к верхнему левому углу
            Canvas.SetLeft(image_Mega, 0);
            Canvas.SetTop(image_Mega, 0);
            image_Mega.Stretch = Stretch.None;

            // Привязываем к верхнему левому углу
            image_Mega.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            image_Mega.VerticalAlignment = System.Windows.VerticalAlignment.Top;*/

            // Сдвигаем все Images к верхнему левому углу и растягиваем по canvas_Map
            Canvas.SetLeft(image_Ground, 0);
            Canvas.SetTop(image_Ground, 0);
            Canvas.SetLeft(image_Resources, 0);
            Canvas.SetTop(image_Resources, 0);
            Canvas.SetLeft(image_Units, 0);
            Canvas.SetTop(image_Units, 0);
            Canvas.SetLeft(image_Grid, 0);
            Canvas.SetTop(image_Grid, 0);
            Canvas.SetLeft(cursor_Block, 0);
            Canvas.SetTop(cursor_Block, 0);
            //image_Ground.Stretch = Stretch.None;
            //image_Resources.Stretch = Stretch.None;
            //image_Units.Stretch = Stretch.None;
            //image_Grid.Stretch = Stretch.None;

            // Привязываем к верхнему левому углу
            image_Ground.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            image_Ground.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            image_Resources.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            image_Resources.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            image_Units.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            image_Units.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            image_Grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            image_Grid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            cursor_Block.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            cursor_Block.VerticalAlignment = System.Windows.VerticalAlignment.Top;

            //RenderOptions.SetBitmapScalingMode(Ground_Pallete_Image, BitmapScalingMode.Linear);
            //Ground_Pallete_Image.Stretch = Stretch.None;
            //RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

            #endregion

            // читаем строки из horde2.exe
            H2Strings.read_strings();

            // Инициализация палитры
            Gl.palette = new Palette();
            //((TreeViewItem)units_Tree.Items[0]).IsSelected = true;

            // Инициализация окошка параметров юнитов
            Gl.unit_properties_window = new Unit_Properties_Window();
            Gl.unit_properties_window.Visibility = Visibility.Hidden;

            // Инициализация окошка таблицы юнитов
            Gl.units_table_window = new Units_Table_Window();
            Gl.units_table_window.Visibility = Visibility.Hidden;

            // Инициализация окошка блоков
            Gl.blocks_window = new Blocks_Window();
            Gl.blocks_window.Width = canvas_Map.Width;
            Gl.blocks_window.Visibility = Visibility.Hidden;

            // Инициализация окна тайлов
            Gl.tiles_window = new Tiles_Window();
            Gl.tiles_window.Visibility = System.Windows.Visibility.Hidden;

            // Инициализация окна свойств сцены
            Gl.scena_prop_window = new Scena_Properties_Window();
            Gl.scena_prop_window.Visibility = System.Windows.Visibility.Hidden;

            // Инициализация окошка прогресса загрузки
            //Gl.progress_wnd = new Progress();
            //Gl.progress_wnd.Visibility = Visibility.Hidden;

            // Инициализация окна игроков
            Gl.players_properties_window = new Players_Properties_Window();
            Gl.players_properties_window.Visibility = Visibility.Hidden;

            // убираем кнопку Secret если надо.
            // и другое
            if (!Gl.CanOpenSecrets)
            {
                secret_Button.Visibility = System.Windows.Visibility.Hidden;
                Gl.scena_prop_window.universal_Canvas.Visibility = System.Windows.Visibility.Hidden;
                Gl.scena_prop_window.Fix_Marsh_In_Universal_Button.Visibility = System.Windows.Visibility.Hidden;
                debug_button.Visibility = System.Windows.Visibility.Hidden;
                Gl.tiles_window.TEST_Button.Visibility = System.Windows.Visibility.Hidden;
                Gl.scena_prop_window.Resize_Button.IsEnabled = false;
            }

        }
        private void Horde_Editor_Loaded(object sender, RoutedEventArgs e)
        {
            #region images2
            /*// Делаем привязку к пикселям
            Ground_Pallete_Image.SnapsToDevicePixels = true;
            image_Mega.SnapsToDevicePixels = true;

            // привязываем BitmapImage к Image
            image_Mega.Source = map_bmp;*/

            // Делаем привязку к пикселям
            Ground_Pallete_Image.SnapsToDevicePixels = true;
            image_Ground.SnapsToDevicePixels = true;
            image_Resources.SnapsToDevicePixels = true;
            image_Units.SnapsToDevicePixels = true;
            image_Grid.SnapsToDevicePixels = true;

            // привязываем BitmapImage к Image (wtf)
            //image_Ground.Source = map_bmp;
            //image_Resources.Source = map_res_bmp;
            //image_Units.Source = map_units_bmp;
            //image_Grid.Source = map_grid_bmp;
            #endregion


            #region get and load scena

            string scena_path;
            if (Environment.GetCommandLineArgs().Count() > 1)
                scena_path = Environment.GetCommandLineArgs()[1];
            else
                scena_path = load_scena_dialog();

            if (scena_path == null)
            {
                this.Close();
                return;
            }


            // создаём сцену
            Gl.curr_scena = new Scena();

            // Инициализация карты(загружаем выбранную)
            if (!Gl.curr_scena.Load_Scene(scena_path))
            { this.Close(); return; }
            Gl.main.Title = Gl.curr_scena.scena_short_name + " - Horde Editor " + Gl.Editor_Version;
            Gl.palette.UpdatePlayerUnitCounter(); // обновим счетчик выбранного на палитре игрока

            #endregion get and load scena

            // Перемещаем и показываем миникарту и прямоугольник на ней
            Point relativePoint = tabControl_Palette.TransformToAncestor(this).Transform(new Point(0, 0));
            Gl.minimap.Left = relativePoint.X + this.Left - Gl.minimap.Width - 10;
            Gl.minimap.Top = relativePoint.Y + this.Top + 25;
            Gl.minimap.Show();
            this.Focus();

            // Перемещаем окно блоков
            Gl.blocks_window.Left = this.Left + 10;
            Gl.blocks_window.Top = this.Top + this.Height - Gl.blocks_window.Height + 60;
            Gl.blocks_window.Width = scrollViewer_Map.Width;

            // Убираем прямоугольник выделения и прямоугольник-курсор
            rectangle_select.Visibility = System.Windows.Visibility.Hidden;
            rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
            rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
            Gl.main.rectangle_cursor.Width = 0;
            Gl.main.rectangle_cursor.Height = 0;

            // инфо
            selectedInfoUpdate();

            // Палитра автотайлов

            #region fill comboboxes

            List<string> str = H2Strings.tile_types_str;

            ComboBoxItem cbi1 = new ComboBoxItem();
            ComboBoxItem cbi2 = new ComboBoxItem();
            cbi1.Content = "<нет>";
            cbi2.Content = "<нет>";
            autoTile_Type1_comboBox.Items.Add(cbi1);
            autoTile_Type2_comboBox.Items.Add(cbi2);

            foreach (string s in str)
            {
                cbi1 = new ComboBoxItem();
                cbi2 = new ComboBoxItem();
                cbi1.Content = s;
                cbi2.Content = s;
                autoTile_Type1_comboBox.Items.Add(cbi1);
                autoTile_Type2_comboBox.Items.Add(cbi2);
            }

            str = H2Strings.tile_variants_str;
            ComboBoxItem cbi;
            foreach (string s in str)
            {
                cbi = new ComboBoxItem();
                cbi.Content = s;
                autoTile_variant_comboBox.Items.Add(cbi);
            }


            str = new List<string> { "нет", "1x1", "2x2", "3x3", "4x4", "5x5", "6x6", "7x7", "8x8" };
            ComboBoxItem cbi3;
            foreach (string s in str)
            {
                cbi1 = new ComboBoxItem();
                cbi2 = new ComboBoxItem();
                cbi3 = new ComboBoxItem();
                cbi1.Content = s;
                cbi2.Content = s;
                cbi3.Content = s;
                comboBox_Brush.Items.Add(cbi1);
                comboBox_autoBrush.Items.Add(cbi2);
                comboBox_resBrush.Items.Add(cbi3);
            }
            #endregion fill comboboxes



            // DEBUG
            //Gl.dbg = new DEBUG_Window();
            //relativePoint = tabControl_Palette.TransformToAncestor(this).Transform(new Point(0, 0));
            //Gl.dbg.Left = relativePoint.X + this.Left + tabControl_Palette.Width + 10;
            //Gl.dbg.Top = relativePoint.Y + this.Top + 50;
            //Gl.dbg.Show();
            //Console.WindowLeft = (int)(Gl.dbg.Left + this.Width);
            //Console.WindowTop = (int)(Gl.dbg.Top + Gl.dbg.Height);
        }

        #region common
        void print_info_cursor(int x, int y, int mx, int my)
        {
            Scena scn = Gl.curr_scena;

            string selected_land_size = "";
            string selected_units_size = "";
            if (select_land_mode)
                selected_land_size = " [" + ((int)rectangle_select_land.Width / 32) + "x" + ((int)rectangle_select_land.Height / 32) + "]";
            if (select_unit_mode)
                selected_units_size = " [" + (int)rectangle_select.Width + "x" + (int)rectangle_select.Height + "]";
            label_coord1.Content = mx.ToString() + ", " + my.ToString() + selected_land_size;
            label_coord2.Content = "(" + x.ToString() + ", " + y.ToString() + selected_units_size + ")";
            switch (tabControl_Palette.SelectedIndex)
            {
                case 0:
                    label_spr_id.Content = "№ " + scn.map[x, y].SpriteId.ToString() + " (" + scn.map[x, y].TileId.ToString() + ")"; break;
                case 1:
                    Tile t = scn.map[x, y];
                    label_spr_id.Content = "Type " + scn.map[x, y].TypeToString();
                    type1_cursor_info_label.Content = "Type1 " + H2Strings.tile_type_to_str(t.Type1);
                    type2_cursor_info_label.Content = "Type2 " + H2Strings.tile_type_to_str(t.Type2);
                    variant_cursor_info_label.Content = "Variant " + t.VariantToString();

                    #region get form
                    if (!t.IsFrozen())
                    {
                        lu_cursor_info_toggleButton.IsChecked = scn.main_mask[x * 2, y * 2] == t.Type1;
                        ru_cursor_info_toggleButton.IsChecked = scn.main_mask[x * 2 + 1, y * 2] == t.Type1;
                        ld_cursor_info_toggleButton.IsChecked = scn.main_mask[x * 2, y * 2 + 1] == t.Type1;
                        rd_cursor_info_toggleButton.IsChecked = scn.main_mask[x * 2 + 1, y * 2 + 1] == t.Type1;

                        //lu_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2, y * 2]);
                        //ru_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2 + 1, y * 2]);
                        //ld_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2, y * 2 + 1]);
                        //rd_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2 + 1, y * 2 + 1]);
                    }
                    else
                    {
                        lu_cursor_info_toggleButton.IsChecked = false;
                        ru_cursor_info_toggleButton.IsChecked = false;
                        ld_cursor_info_toggleButton.IsChecked = false;
                        rd_cursor_info_toggleButton.IsChecked = false;
                        //lu_cursor_info_toggleButton.Content = "";
                        //ru_cursor_info_toggleButton.Content = "";
                        //ld_cursor_info_toggleButton.Content = "";
                        //rd_cursor_info_toggleButton.Content = "";
                    }

                    lu_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2, y * 2]);
                    ru_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2 + 1, y * 2]);
                    ld_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2, y * 2 + 1]);
                    rd_cursor_info_toggleButton.Content = H2Strings.tile_type_to_short_str(scn.main_mask[x * 2 + 1, y * 2 + 1]);
                    #endregion get form


                    break;
                case 2:
                    if (scn.map_units[x, y] != null)
                    {
                        Unit u = scn.map_units[x, y][scn.map_units[x, y].Count() - 1];
                        label_spr_id.Content = "unit  " + H2Strings.army_to_str(u.folder) + ", " + u.Name + " [" + u.folder.ToString() + ", " + u.unit_id.ToString() + "]";
                    }
                    else if (paste_units_mode)
                        label_spr_id.Content = "paste units";
                    else
                        label_spr_id.Content = "null";
                    break;
                case 3:
                    label_spr_id.Content = scn.map_res[x, y].ToString(); break;
            }
            //label_info.Content = "";
        }

        // Двигаем скроллы на основной форме. Координаты измеряем в пикселях
        // В функции выполняются проверки на выход координат за пределы
        public void move_scrols_to(int x, int y)
        {
            if (x != scrollViewer_Map.HorizontalOffset)
            {
                int new_horOffset = x;

                if (new_horOffset > 0 && new_horOffset < scrollViewer_Map.ScrollableWidth)
                {
                    scrollViewer_Map.ScrollToHorizontalOffset(new_horOffset);
                }
                else if (new_horOffset <= 0)
                {
                    scrollViewer_Map.ScrollToHorizontalOffset(0);
                }
                else
                {
                    scrollViewer_Map.ScrollToHorizontalOffset(scrollViewer_Map.ScrollableWidth);
                }
            }

            if (y != scrollViewer_Map.VerticalOffset)
            {
                int new_vertOffset = y;

                if (new_vertOffset > 0 && new_vertOffset < scrollViewer_Map.ScrollableHeight)
                {
                    scrollViewer_Map.ScrollToVerticalOffset(new_vertOffset);
                }
                else if (new_vertOffset <= 0)
                {
                    scrollViewer_Map.ScrollToVerticalOffset(0);
                }
                else
                {
                    scrollViewer_Map.ScrollToVerticalOffset(scrollViewer_Map.ScrollableHeight);
                }

            }
        }

        public string load_scena_dialog()
        {

            string scena_path = "";

            Nullable<bool> result = false;

            dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Select path to any scena or save";
            if (Gl.curr_scena == null)
                dlg.FileName = "scena_01.scn";
            else
                dlg.FileName = Gl.curr_scena.scena_short_name + ".scn";
            dlg.DefaultExt = ".scn";
            dlg.Filter = "Scena Files (*.scn)|*.scn|Save Files(don't work!) (*.sav)|*.sav|Supported Files(*.scn;*.sav)|*.scn;*.sav";

            result = dlg.ShowDialog();


            if (result == true)
            {
                scena_path = dlg.FileName;
            }
            else if (result == false
                || scena_path.EndsWith(".sav") // Заглушка
                || !File.Exists(scena_path))
            {
                return null;
            }
            return scena_path;
        }

        #endregion

        #region mouse_and_keys
        private void canvas_Map_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rectangle_cursor.Visibility = System.Windows.Visibility.Visible; // немного костыль, но избавляет от одного минибага

            if (e.ChangedButton == MouseButton.Left)
            {
                #region down left
                //this.CaptureMouse();
                int mx = (int)e.GetPosition(canvas_Map).X;
                int my = (int)e.GetPosition(canvas_Map).Y;
                int x = mx / 32;
                int y = my / 32;

                lkm_click.X = mx;
                lkm_click.Y = my;
                mcell_click_x = x;
                mcell_click_y = y;

                switch (tabControl_Palette.SelectedIndex)
                {
                    case 0:
                        if (Gl.palette.selected_sprite > -1 && Gl.palette.Is_block_selected == false)
                        {
                            //select_land_mode = false;
                            //rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
                            //Canvas.SetLeft(rectangle_select_land, -100);
                            //Canvas.SetTop(rectangle_select_land, -100);

                            paint_mode = true;
                            Gl.curr_scena.set_Ground_region_tile(x, y, Gl.palette.curr_ground_brush_size, Gl.palette.selected_sprite, true);

                        }
                        else if (Gl.palette.Is_block_selected == true && Gl.palette.selected_block > -1 &&
                            Gl.palette.selected_block < Gl.curr_scena.blocks.Count)
                        {
                            //select_land_mode = false;
                            //rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
                            //Canvas.SetLeft(rectangle_select_land, -100);
                            //Canvas.SetTop(rectangle_select_land, -100);

                            paint_mode = true;

                            Gl.curr_scena.set_Ground_block(x, y, Gl.palette.block_width, Gl.palette.block_height, Gl.palette.selected_block, true);
                        }
                        else
                        {
                            paint_mode = false;
                            select_land_mode = true;

                            rectangle_select_land.Visibility = System.Windows.Visibility.Visible;
                            rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;

                            Canvas.SetLeft(rectangle_select_land, (int)(lkm_click.X / 32) * 32);
                            Canvas.SetTop(rectangle_select_land, (int)(lkm_click.Y / 32) * 32);
                            rectangle_select_land.Width = 32 + 1;
                            rectangle_select_land.Height = 32 + 1;
                        }
                        break;
                    case 1:
                        if (Gl.palette.autotile_args.type1 != TileType.Unknown)
                        {
                            //select_land_mode = false;
                            //rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
                            //Canvas.SetLeft(rectangle_select_land, -100);
                            //Canvas.SetTop(rectangle_select_land, -100);

                            paint_mode = true;
                            Gl.curr_scena.set_Land_region_autotile(x, y, Gl.palette.curr_ground_brush_size, Gl.palette.autotile_args);
                        }
                        break;
                    case 2:
                        if (Gl.unit_properties_window.Visibility == Visibility.Hidden)
                        {
                            if (Gl.palette.selected_unit_folder != -1 && Gl.palette.selected_unit_index != -1 && !paste_units_mode)
                            {
                                paint_mode = true;
                                Gl.curr_scena.unit_create(x, y, Gl.palette.selected_unit_folder, Gl.palette.selected_unit_index, comboBox_Player.SelectedIndex, true);
                                Gl.palette.UpdatePlayerUnitCounter();
                            }
                            else if (paste_units_mode)
                            {
                                Palette palette = Gl.palette;

                                foreach (Tuple<int, int> tup in palette.units_in_buffer.Keys)
                                {
                                    foreach (Unit u in palette.units_in_buffer[tup])
                                    {
                                        Unit new_u = new Unit();
                                        new_u.CopyFrom(u);

                                        int w = palette.units_in_buffer_x_max - palette.units_in_buffer_x_min;
                                        int h = palette.units_in_buffer_y_max - palette.units_in_buffer_y_min;
                                        int ux = tup.Item1 + x - w / 2;
                                        int uy = tup.Item2 + y - h / 2;

                                        Gl.curr_scena.unit_create(ux, uy, new_u, true);
                                    }
                                }

                                label_units_count.Content = palette.units_in_buffer.Count + " units pasted";
                            }
                            else
                            {
                                select_unit_mode = true;

                                rectangle_select.Visibility = System.Windows.Visibility.Visible;
                                rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;

                                Canvas.SetLeft(rectangle_select, lkm_click.X);
                                Canvas.SetTop(rectangle_select, lkm_click.Y);
                                rectangle_select.Width = 1;
                                rectangle_select.Height = 1;
                            }
                        }
                        break;
                    case 3:
                        if (Gl.palette.selected_resourse > -1)
                        {
                            paint_mode = true;
                            Gl.curr_scena.set_Resource_tile(x, y, Gl.palette.curr_ground_brush_size, Gl.palette.selected_resourse, true);
                        }
                        break;

                }
                #endregion
            }
            if (e.ChangedButton == MouseButton.Middle)
            {
                #region down middle
                int mx = (int)e.GetPosition(canvas_Map).X;
                int my = (int)e.GetPosition(canvas_Map).Y;
                int x = mx / 32;
                int y = my / 32;

                switch (tabControl_Palette.SelectedIndex)
                {
                    case 0:
                        //Gl.palette.selected_tile = Gl.curr_scena.tileId_to_spriteId.IndexOf(Gl.curr_scena.map[x, y]);
                        Gl.palette.selected_tile = Gl.curr_scena.map[x, y].TileId;
                        Gl.palette.selected_sprite = Gl.curr_scena.map[x, y].SpriteId;
                        Gl.palette.UpdateGroundSelector(Gl.palette.selected_tile % Gl.palette.tiles_collumns_count, Gl.palette.selected_tile / Gl.palette.tiles_collumns_count);
                        Gl.palette.Is_block_selected = false;
                        CursorsUpdate();

                        // Окно тайлов.
                        Gl.tiles_window.SelectedTile = Gl.palette.selected_tile;
                        Gl.tiles_window.SelectedSprite = Gl.palette.selected_sprite;
                        Gl.tiles_window.FullUpdate();

                        break;
                    case 1:
                        Tile t = Gl.curr_scena.map[x, y];
                        if (!t.IsFrozen())
                        {
                            autoTile_Type1_comboBox.SelectedIndex = (int)t.Type1 + 1;
                            autoTile_Type2_comboBox.SelectedIndex = (int)t.Type2 + 1;
                            autoTile_variant_comboBox.SelectedIndex = (int)t.variant;
                            #region get form
                            bool[,] tmp_bool = AutoTileDict.GetTileBoolMask(t, t.Type1);
                            lu_toggleButton.IsChecked = tmp_bool[0, 0];
                            ru_toggleButton.IsChecked = tmp_bool[1, 0];
                            ld_toggleButton.IsChecked = tmp_bool[0, 1];
                            rd_toggleButton.IsChecked = tmp_bool[1, 1];
                            #endregion get form

                            CursorsUpdate();
                            autoTile_toggleButtons_Handle();
                        }
                        break;
                    case 2:
                        if (Gl.curr_scena.map_units[x, y] != null)
                        {
                            Gl.palette.SelectUnit(x, y);
                        }
                        else
                        {
                            Gl.palette.DeSelectUnit();
                        }
                        break;
                    case 3:
                        comboBox_Resources.SelectedIndex = Gl.curr_scena.map_res[x, y] + 1;
                        CursorsUpdate();
                        break;
                }
                selectedInfoUpdate();
                #endregion
            }
            if (e.ChangedButton == MouseButton.Right)
            {
                #region down right
                pkm_click.X = (int)e.GetPosition(scrollViewer_Map).X;
                pkm_click.Y = (int)e.GetPosition(scrollViewer_Map).Y;
                pkm_click_scrollPos.X = scrollViewer_Map.HorizontalOffset;
                pkm_click_scrollPos.Y = scrollViewer_Map.VerticalOffset;
                drag_mode = true;

                // меняем курсор, если кнопка все ещё нажата ( иначе в отладке иногда проскакивает)
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    //Mouse.SetCursor(Cursors.Hand);
                    Cursor = Cursors.ScrollAll;
                }
                #endregion
            }
            if (paint_mode || select_unit_mode || drag_mode)
            {

                if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                {
                    this.CaptureMouse();
                    //e.Handled = true;
                }
            }
        }

        #region MoveMouseHooks
        // запомним координаты последней клеточки(32х32) в которой был курсор при нажатой мыши
        int mcell_x = -1;    // нужно для проверки перехода мышки на следующую клеточку
        int mcell_y = -1;    // 
        // запомним координаты клеточки(32х32) в которой был НАЖАТ курсор
        int mcell_click_x;
        int mcell_click_y;
        // запомним координаты клеточки(32х32) в которой был ОТПУЩЕН курсор
        //int mcell_up_x;
        //int mcell_up_y;

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            Point p = Mouse.GetPosition(canvas_Map);//MouseUtilities.CorrectGetPosition(canvas_Map);
            int mx = (int)p.X;
            int my = (int)p.Y;
            int x = mx / 32;
            int y = my / 32;

            if (x >= 0 && x < Gl.curr_scena.size_map_x && y >= 0 && y < Gl.curr_scena.size_map_y)
            {
                print_info_cursor(x, y, mx, my);
                CursorsUpdate();
            }
            /*if (!(e.OriginalSource is System.Windows.Controls.Primitives.Thumb) &&      // Don't block the scrollbars
                !(e.OriginalSource is System.Windows.Controls.Primitives.ButtonBase) && // && buttons
                !(e.OriginalSource is System.Windows.Controls.Primitives.Selector))   */
            if (paint_mode || select_unit_mode || select_land_mode)
            {

                #region leftmouse
                if (e.LeftButton == MouseButtonState.Pressed)//MouseUtilities.GetAsyncKeyState(MouseUtilities.VK_LEFTBUTTON) != 0)
                {
                    #region left_button pressed
                    //Point p = Mouse.GetPosition(canvas_Map);

                    if ((TabItem)tabControl_Palette.SelectedItem == Unit_Palette_Tab &&
                        (Gl.palette.selected_unit_index == -1 || // мышка сдвинулась на пиксель
                        Gl.palette.selected_unit_folder == -1))  // нас интересует лишь выделение юнитов
                    {
                        LeftMouse_DownAndMove_Handler1(mx, my);
                    }

                    if (mcell_x == -1 && mcell_y == -1) // мышка только что была нажата
                    {
                        mcell_x = x;
                        mcell_y = y;
                    }
                    else if (mcell_x != x || mcell_y != y) // мышка сдвинулась на клеточку
                    {
                        mcell_x = x;
                        mcell_y = y;
                        LeftMouse_DownAndMove_Handler32(x, y);
                    }
                    #endregion
                }
                #endregion
            }

            if (drag_mode)
            {
                #region rightmouse
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    #region right_button pressed

                    p = Mouse.GetPosition(scrollViewer_Map);//MouseUtilities.CorrectGetPosition(canvas_Map);
                    mx = (int)p.X;
                    my = (int)p.Y;
                    x = mx / 32;
                    y = my / 32;

                    //if (Gl.point_in_map(x, y))
                    {
                        if (drag_mode == true)
                        {
                            //drag_mode = false;
                            int dx = mx - (int)pkm_click.X;
                            int dy = my - (int)pkm_click.Y;

                            // DEBUG
                            //label_selected_spr.Content = "dxdy " + dx + ", " + dy;

                            move_scrols_to((int)pkm_click_scrollPos.X - dx, (int)pkm_click_scrollPos.Y - dy);

                        }
                    }
                    #endregion
                }
                #endregion

            }
        }

        private void LeftMouse_DownAndMove_Handler32(int x, int y)
        {
            #region  left_button pressed

            switch (tabControl_Palette.SelectedIndex)
            {
                case 0:
                    if (Gl.point_in_map(x, y))
                        if (paint_mode == true &&                       //
                            Gl.palette.selected_sprite > -1 &&          // установка тайлов
                            Gl.palette.Is_block_selected == false)      //
                        {

                            Gl.curr_scena.set_Ground_region_tile(x, y, Gl.palette.curr_ground_brush_size, Gl.palette.selected_sprite, true);

                        }
                        else if (paint_mode == true &&                  //
                            Gl.palette.selected_block > -1 &&           // установка блоков
                            Gl.palette.Is_block_selected == true)       //
                        {
                            Gl.curr_scena.set_Ground_block(x, y, Gl.palette.block_width, Gl.palette.block_height, Gl.palette.selected_block, true);
                        }
                        else if (select_land_mode == true)              // режим выделения области земли
                        {
                            // обработка прямоугольника выделения
                            int w = x * 32 - mcell_click_x * 32;
                            int h = y * 32 - mcell_click_y * 32;
                            //if (w != 0 && h != 0)
                            {
                                if (w > 0)
                                {
                                    Canvas.SetLeft(rectangle_select_land, mcell_click_x * 32);
                                    rectangle_select_land.Width = w + 32 + 1;
                                }
                                else
                                {
                                    Canvas.SetLeft(rectangle_select_land, x * 32);
                                    rectangle_select_land.Width = -w + 32 + 1;
                                }
                                if (h > 0)
                                {
                                    Canvas.SetTop(rectangle_select_land, mcell_click_y * 32);
                                    rectangle_select_land.Height = h + 32 + 1;
                                }
                                else
                                {
                                    Canvas.SetTop(rectangle_select_land, y * 32);
                                    rectangle_select_land.Height = -h + 32 + 1;
                                }
                            }
                        }
                    break;
                case 1:
                    if (paint_mode == true && Gl.palette.autotile_args.type1 != TileType.Unknown)
                    {
                        Gl.curr_scena.set_Land_region_autotile(x, y, Gl.palette.curr_ground_brush_size, Gl.palette.autotile_args);
                    }
                    break;
                case 2:
                    #region place units
                    if (paint_mode == true && Gl.palette.selected_unit_folder != -1 &&
                        Gl.palette.selected_unit_index != -1 &&
                        Gl.point_in_map(x, y))
                    {
                        Gl.curr_scena.unit_create(x, y, Gl.palette.selected_unit_folder, Gl.palette.selected_unit_index, comboBox_Player.SelectedIndex, true);
                        Gl.palette.UpdatePlayerUnitCounter();
                    }
                    #endregion place units
                    break;
                case 3:
                    if (paint_mode == true &&
                        Gl.palette.selected_resourse > -1 &&
                        Gl.point_in_map(x, y))
                    {
                        Gl.curr_scena.set_Resource_tile(x, y, Gl.palette.curr_ground_brush_size, Gl.palette.selected_resourse, true);
                    }
                    break;
            }
            #endregion
        }

        private void LeftMouse_DownAndMove_Handler1(int mx, int my)
        {
            // обработка прямоугольника выделения
            int w = mx - (int)lkm_click.X;
            int h = my - (int)lkm_click.Y;
            if (w != 0 && h != 0)
            {
                if (w > 0)
                {
                    Canvas.SetLeft(rectangle_select, lkm_click.X);
                    rectangle_select.Width = w;
                }
                else
                {
                    Canvas.SetLeft(rectangle_select, mx);
                    rectangle_select.Width = -w;
                }
                if (h > 0)
                {
                    Canvas.SetTop(rectangle_select, lkm_click.Y);
                    rectangle_select.Height = h;
                }
                else
                {
                    Canvas.SetTop(rectangle_select, my);
                    rectangle_select.Height = -h;
                }
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(canvas_Map);//MouseUtilities.CorrectGetPosition(canvas_Map);
            int mx = (int)p.X;
            int my = (int)p.Y;
            int x = mx / 32;
            int y = my / 32;

            #region leftmouse
            if (e.LeftButton == MouseButtonState.Released &&
                    (select_unit_mode || paint_mode || select_land_mode))
            {
                paint_mode = false;

                #region left_button release

                mcell_x = -1;
                mcell_y = -1;
                //if (select_land_mode)
                //{
                //    mcell_up_x = x;
                //    mcell_up_y = y;
                //    if (mcell_up_x < 0) mcell_up_x = 0;
                //    if (mcell_up_x >= Gl.curr_scena.size_map_x) mcell_up_x = Gl.curr_scena.size_map_x - 1;
                //    if (mcell_up_y < 0) mcell_up_y = 0;
                //    if (mcell_up_y >= Gl.curr_scena.size_map_y) mcell_up_y = Gl.curr_scena.size_map_y - 1;
                //    select_land_mode = false;
                //}

                switch (tabControl_Palette.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        if (//unit_properties_form.Visible == false &&
                            (Gl.palette.selected_unit_folder == -1 || Gl.palette.selected_unit_index == -1) &&
                            select_unit_mode == true)
                        {
                            //Point p = Mouse.GetPosition(canvas_Map);//MouseUtilities.CorrectGetPosition(canvas_Map);
                            //int mx = (int)p.X;
                            //int my = (int)p.Y;

                            // Убираем прямоугольник выделения с глаз и показываем курсор
                            rectangle_select.Visibility = System.Windows.Visibility.Hidden;
                            rectangle_cursor.Visibility = System.Windows.Visibility.Visible;

                            int x1, x2, y1, y2;

                            // Границы выбора
                            x1 = Gl.floor((int)Math.Min(lkm_click.X, mx), 32);
                            x2 = Gl.ceil((int)Math.Max(lkm_click.X, mx), 32);
                            y1 = Gl.floor((int)Math.Min(lkm_click.Y, my), 32);
                            y2 = Gl.ceil((int)Math.Max(lkm_click.Y, my), 32);

                            // Что бы не вышло за границы карты
                            x1 = Math.Max(x1, 0);
                            x2 = Math.Min(x2, Gl.curr_scena.size_map_x);
                            y1 = Math.Max(y1, 0);
                            y2 = Math.Min(y2, Gl.curr_scena.size_map_y);

                            // ctrl будет удалять юнитов из выборки
                            bool ctrl = (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                                Keyboard.IsKeyDown(Key.RightCtrl));
                            // shift будет добавлять новых юнитов к уже выделенным
                            bool shift = (Keyboard.IsKeyDown(Key.LeftShift) ||
                                Keyboard.IsKeyDown(Key.RightShift));

                            if (!shift && !ctrl)
                            {
                                // очищаем предыдущий выбор, закрасив рамку в нужный цвет
                                foreach (Unit u in Gl.curr_scena.selected_units)
                                {
                                    Gl.curr_scena.update_unit_frame_to_player_color(u);
                                }
                                Gl.curr_scena.selected_units.Clear();
                            }

                            List<Unit> added_units = new List<Unit>();
                            List<Unit> removed_units = new List<Unit>();

                            if (!ctrl || shift)
                            { // добавляем
                                for (int i = x1; i < x2; i++)
                                    for (int j = y1; j < y2; j++)
                                        if (Gl.curr_scena.map_units[i, j] != null)
                                            foreach (Unit u in Gl.curr_scena.map_units[i, j])
                                                if (!Gl.curr_scena.selected_units.Contains(u))
                                                {
                                                    Gl.curr_scena.selected_units.Add(u);
                                                    added_units.Add(u);
                                                }
                                foreach (Unit u in added_units)
                                {
                                    Gl.curr_scena.update_selected_unit_frame(u);
                                }
                            }

                            if (ctrl)
                            { // убираем выделенных юнитов из списка
                                for (int i = x1; i < x2; i++)
                                    for (int j = y1; j < y2; j++)
                                        if (Gl.curr_scena.map_units[i, j] != null)
                                            foreach (Unit u in Gl.curr_scena.map_units[i, j])
                                                if (Gl.curr_scena.selected_units.Contains(u) &&
                                                    !added_units.Contains(u))
                                                {
                                                    Gl.curr_scena.selected_units.Remove(u);
                                                    removed_units.Add(u);
                                                }
                                foreach (Unit u in removed_units)
                                {
                                    Gl.curr_scena.update_unit_frame_to_player_color(u);
                                }
                            }


                            label_units_count.Content = "selected units count " + Gl.curr_scena.selected_units.Count();
                            select_unit_mode = false;
                        }
                        break;
                    case 3:
                        break;
                }
                #endregion

            }
            #endregion

            HistoryWrite();

            #region rightmouse
            if (e.RightButton == MouseButtonState.Released)
            {
                #region right_button release

                Cursor = Cursors.Arrow;

                drag_mode = false;
                #endregion
            }
            #endregion

            if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
            {
                this.ReleaseMouseCapture();
            }
        }

        #region zoom

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (mouse_on_canvas && this.IsActive)
            {
                //bool alt = (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                bool ctrl = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
                bool shift = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

                int md = e.Delta;


                if (!ctrl && !shift)
                {
                    e.Handled = true;

                    double d = 0;

                    if (md > 0)
                        d = uiScaleSlider.TickFrequency;
                    else if (md < 0)
                        d = -uiScaleSlider.TickFrequency;

                    uiScaleSlider.Value += d;

                    Gl.minimap.update_rectangle_on_minimap();
                }
                else if (ctrl && !shift)
                {
                    e.Handled = true;

                    if (e.Delta > 0)
                        scrollViewer_Map.LineLeft();
                    else
                        scrollViewer_Map.LineRight();
                }
            }
        }

        private void uiScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;
            if (!this.IsLoaded)
            {
                //slider.Loaded += (s, le) => uiScaleSlider_ValueChanged(sender, e);
                return;
            }

            var scale = slider.Value;

            double sw = (int)SystemParameters.VerticalScrollBarWidth;
            double sh = (int)SystemParameters.HorizontalScrollBarHeight;

            //var relativeMiddle = new Point((scrollViewer_Map.ActualWidth + sw) / 2, (scrollViewer_Map.ActualHeight + sh) / 2);
            //var oldLocation = Canvas_Map_scaleTransform.Transform(canvas_Map.PointFromScreen(relativeMiddle));
            //ScaleChanged(scale, Canvas_Map_scaleTransform);
            //var newLocation = Canvas_Map_scaleTransform.Transform(canvas_Map.PointFromScreen(relativeMiddle));

            var relativeMouse = Mouse.GetPosition(scrollViewer_Map);
            var oldLocation = Canvas_Map_scaleTransform.Transform(canvas_Map.PointFromScreen(relativeMouse));
            ScaleChanged(scale, Canvas_Map_scaleTransform);
            var newLocation = Canvas_Map_scaleTransform.Transform(canvas_Map.PointFromScreen(relativeMouse));

            var shift = newLocation - oldLocation;

            int offset_x = (int)(scrollViewer_Map.HorizontalOffset + shift.X);
            int offset_y = (int)(scrollViewer_Map.VerticalOffset + shift.Y);

            //double offset_x = scrollViewer_Map.HorizontalOffset + shift.X;
            //double offset_y = scrollViewer_Map.VerticalOffset + shift.Y;

            scrollViewer_Map.ScrollToVerticalOffset(offset_y);
            scrollViewer_Map.ScrollToHorizontalOffset(offset_x);

            //lblScale.Content = scale.ToString("P1").Replace(".0", string.Empty);
        }

        private static void ScaleChanged(double scale, ScaleTransform st)
        {
            st.ScaleX = scale;
            st.ScaleY = scale;
        }

        #endregion zoom

        #endregion MoveMouseHooks

        bool mouse_on_canvas = false;
        private void canvas_Map_MouseEnter(object sender, MouseEventArgs e)
        {
            mouse_on_canvas = true;
            switch (tabControl_Palette.SelectedIndex)
            {
                case 0:
                    rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                    if (Gl.palette.Is_block_selected && Gl.palette.selected_block > -1)
                    {
                        cursor_Block.Visibility = System.Windows.Visibility.Visible;
                    }
                    break;
                case 1:
                    rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 2:
                    if ((Gl.palette.selected_unit_index != -1 && Gl.palette.selected_unit_folder != -1) ||
                        Gl.palette.selected_sprite != -1 || Gl.palette.selected_resourse != -1)
                        rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 3:
                    if (Gl.palette.selected_resourse > -1)
                    {
                        rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                    }
                    break;
            }
        }

        private void canvas_Map_MouseLeave(object sender, MouseEventArgs e)
        {
            mouse_on_canvas = false;
            if (!paint_mode)
            {
                rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
                cursor_Block.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        private void canvas_Map_MouseMove(object sender, MouseEventArgs e)
        {
            //Point p = Mouse.GetPosition(this);

            //Gl.stopWatch1.Stop();
            //string s1 = Gl.DEBUG_stopWatch1_value();
            //Console.WriteLine(s1);


            //Gl.stopWatch1.Restart();
        }

        public void CallKeyUp(KeyEventArgs e)
        {
            OnKeyUp(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            bool alt = (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
            bool ctrl = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            bool shift = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
            Palette palette = Gl.palette;

            /*if (Gl.blocks_window.Visibility == System.Windows.Visibility.Visible &&  // палитра блоков видна
                (Gl.blocks_window.lastBlock_image != -1 || (TabItem)(tabControl_Palette.SelectedItem) == Ground_Palette_Tab) && // выделений блок виден или открыта палитра ландшафта
                Gl.blocks_window.lastBlock != -1 && // был ли какой-нибудь блок выделен
                /*!alt && !shift * / && ctrl) // нажат ли ctrl
            {
                e.Handled = true;
                Gl.blocks_window.CallKeyUp(e); // ээм, и зачем это? мб для esc?
            }*/

            switch (tabControl_Palette.SelectedIndex)
            {
                case 0:

                    if (!alt && !shift && ctrl && e.Key == Key.Z && this.IsActive)
                        LandHistoryUndo();
                    if (!alt && !shift && ctrl && e.Key == Key.Y && this.IsActive)
                        LandHistoryRedo();

                    #region copy
                    if (!alt && !shift && ctrl && e.Key == Key.C && select_land_mode)
                    {
                        Blocks_Window bw = Gl.blocks_window;
                        Scena scn = Gl.curr_scena;

                        int left = (int)Canvas.GetLeft(rectangle_select_land);
                        int top = (int)Canvas.GetTop(rectangle_select_land);

                        int w = (int)(rectangle_select_land.Width - 1);
                        int h = (int)(rectangle_select_land.Height - 1);

                        left /= 32;
                        top /= 32;
                        w /= 32;
                        h /= 32;

                        if (w > 0 && h > 0)
                        {
                            w = Math.Min(w, bw.max_columns);
                            h = Math.Min(h, bw.max_rows);

                            Tile[,] block = new Tile[w, h];

                            scn.blocks.Add(block);

                            //bw.add_block_button_Click(this, null);

                            for (int i = 0; i < w; i++)
                            {
                                for (int j = 0; j < h; j++)
                                {
                                    if (scn.map[left + i, top + j].SpriteId != -1)
                                        block[i, j] = scn.map[left + i, top + j];
                                    else
                                        block[i, j] = bw.blank_Tile;
                                }
                            }

                            Gl.blocks_window.AddBlock(block);

                            Gl.blocks_window.UpdateBlocks();

                            //MessageBox.Show(String.Format("Block from ({0};{1}) to ({2};{3}) saved with number {4}", left, top, left + w - 1, top + h - 1, scn.blocks.Count - 1), "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    #endregion copy

                    #region move selector
                    if (palette.selected_tile >= 0)
                    {
                        bool flag = false;
                        if (e.Key == Key.NumPad4)
                        {
                            if (palette.selected_tile > 0)
                            {
                                palette.selected_tile--;
                                flag = true;
                            }
                        }
                        if (e.Key == Key.NumPad6)
                        {
                            if (palette.selected_tile < Gl.curr_scena.tileSet.Count)
                            {
                                palette.selected_tile++;
                                flag = true;
                            }
                        }
                        if (e.Key == Key.NumPad8)
                        {
                            if (palette.selected_tile < palette.tiles_collumns_count)
                                palette.selected_tile = 0;
                            else
                                palette.selected_tile -= palette.tiles_collumns_count;
                            flag = true;
                        }
                        if (e.Key == Key.NumPad2)
                        {
                            if (palette.selected_tile > Gl.curr_scena.tileSet.Count - palette.tiles_collumns_count)
                                palette.selected_tile = Gl.curr_scena.tileSet.Count - 1;
                            else
                                palette.selected_tile += palette.tiles_collumns_count;
                            flag = true;
                        }
                        if (flag)
                        {
                            Gl.palette.selected_sprite = Gl.curr_scena.tileSet[palette.selected_tile].SpriteId;
                            Gl.palette.UpdateGroundSelector(Gl.palette.selected_tile % palette.tiles_collumns_count, Gl.palette.selected_tile / palette.tiles_collumns_count);
                        }
                    }

                    #endregion move selector

                    #region change brush size

                    if (e.Key == Key.D0)
                        comboBox_Brush.SelectedIndex = 0;
                    if (e.Key == Key.D1)
                        comboBox_Brush.SelectedIndex = 1;
                    if (e.Key == Key.D2)
                        comboBox_Brush.SelectedIndex = 2;
                    if (e.Key == Key.D3)
                        comboBox_Brush.SelectedIndex = 3;
                    if (e.Key == Key.D4)
                        comboBox_Brush.SelectedIndex = 4;
                    if (e.Key == Key.D5)
                        comboBox_Brush.SelectedIndex = 5;
                    if (e.Key == Key.D6)
                        comboBox_Brush.SelectedIndex = 6;
                    if (e.Key == Key.D7)
                        comboBox_Brush.SelectedIndex = 7;
                    if (e.Key == Key.D8)
                        comboBox_Brush.SelectedIndex = 8;

                    #endregion change brush size

                    if (e.Key == Key.Escape)
                    {
                        Gl.palette.DeSelectLand();
                        Gl.blocks_window.DeSelectBlock();
                        if (select_land_mode)
                        {
                            select_land_mode = false;
                            rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
                            Canvas.SetLeft(rectangle_select_land, -100);
                            Canvas.SetTop(rectangle_select_land, -100);
                        }

                        selectedInfoUpdate();
                        CursorsUpdate();
                    }

                    if (Gl.palette.selected_sprite > -1)
                    {

                    }
                    break;
                case 1:

                    if (!alt && !shift && ctrl && e.Key == Key.Z && this.IsActive)
                        LandHistoryUndo();
                    if (!alt && !shift && ctrl && e.Key == Key.Y && this.IsActive)
                        LandHistoryRedo();

                    if (e.Key == Key.Escape)
                    {
                        autoTile_Type1_comboBox.SelectedIndex = 0;
                        autoTile_Type2_comboBox.SelectedIndex = 0;
                        autoTile_variant_comboBox.SelectedIndex = 0;
                        lu_toggleButton.IsChecked = false;
                        ru_toggleButton.IsChecked = false;
                        ld_toggleButton.IsChecked = false;
                        rd_toggleButton.IsChecked = false;

                        selectedInfoUpdate();
                        CursorsUpdate();
                    }

                    #region change brush size

                    if (e.Key == Key.D0)
                        comboBox_Brush.SelectedIndex = 0;
                    if (e.Key == Key.D1)
                        comboBox_Brush.SelectedIndex = 1;
                    if (e.Key == Key.D2)
                        comboBox_Brush.SelectedIndex = 2;
                    if (e.Key == Key.D3)
                        comboBox_Brush.SelectedIndex = 3;
                    if (e.Key == Key.D4)
                        comboBox_Brush.SelectedIndex = 4;
                    if (e.Key == Key.D5)
                        comboBox_Brush.SelectedIndex = 5;
                    if (e.Key == Key.D6)
                        comboBox_Brush.SelectedIndex = 6;
                    if (e.Key == Key.D7)
                        comboBox_Brush.SelectedIndex = 7;
                    if (e.Key == Key.D8)
                        comboBox_Brush.SelectedIndex = 8;

                    #endregion change brush size

                    break;
                case 2:
                    if (Gl.unit_properties_window.Visibility == Visibility.Hidden)
                    {

                        #region units history
                        if (ctrl && e.Key == Key.Z && this.IsActive) // Undo
                        {
                            List<SetUnitEvent> l_sue = History.History.ExtractUndoUnitsEvent();
                            SetUnitEvent set_u_ev;
                            Unit u;
                            //Unit new_u;

                            if (l_sue != null) // && l_sle.Count > 0)
                            {
                                for (int i = l_sue.Count - 1; i >= 0; i--)
                                {
                                    set_u_ev = l_sue[i];
                                    u = set_u_ev.U;
                                    if (u != null)
                                    {
                                        if (set_u_ev.IsAdded)
                                        {
                                            Gl.curr_scena.unit_delete(u, false);
                                        }
                                        else
                                        {
                                            //new_u = Gl.curr_scena.unit_create(u.mapX, u.mapY, u.folder, u.unit_id, u.player, false);
                                            //if (new_u != null)
                                            //    new_u.CopyFrom(u);
                                            //else
                                            //    MessageBox.Show("new_u==null", "Undo fail!", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            //set_u_ev.U = new_u;

                                            Gl.curr_scena.players[u.player].add_unit(u);
                                            Gl.curr_scena.add_Unit_at_tile(u, true);

                                        }
                                    }
                                    else
                                        MessageBox.Show("u==null", "Undo fail!", MessageBoxButton.OK, MessageBoxImage.Warning);

                                }
                                Gl.palette.UpdatePlayerUnitCounter();
                            }
                        }
                        if (ctrl && e.Key == Key.Y && this.IsActive) // Redo
                        {
                            List<SetUnitEvent> l_sue = History.History.ExtractRedoUnitsEvent();
                            SetUnitEvent set_u_ev;
                            Unit u;
                            //Unit new_u;

                            if (l_sue != null) // && l_sle.Count > 0)
                            {
                                for (int i = l_sue.Count - 1; i >= 0; i--)
                                {
                                    set_u_ev = l_sue[i];
                                    u = set_u_ev.U;
                                    if (u != null)
                                    {
                                        if (set_u_ev.IsAdded)
                                        {
                                            //new_u = Gl.curr_scena.unit_create(u.mapX, u.mapY, u.folder, u.unit_id, u.player, false);
                                            //if (new_u != null)
                                            //    new_u.CopyFrom(u);
                                            //else
                                            //    MessageBox.Show("new_u==null", "Redo fail!", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            //set_u_ev.U = new_u;

                                            Gl.curr_scena.players[u.player].add_unit(u);
                                            Gl.curr_scena.add_Unit_at_tile(u, true);

                                        }
                                        else
                                        {
                                            Gl.curr_scena.unit_delete(u, false);
                                        }
                                    }
                                    else
                                        MessageBox.Show("u==null", "Redo fail!", MessageBoxButton.OK, MessageBoxImage.Warning);

                                }
                            }
                        }
                        #endregion units history

                        #region copy paste delete
                        if (ctrl && (e.Key == Key.C || e.Key == Key.X) && // copy or cut
                            !alt && !shift &&
                            Gl.curr_scena.selected_units != null &&
                            Gl.curr_scena.selected_units.Count > 0)
                        {
                            Scena scn = Gl.curr_scena;
                            Gl.palette.units_in_buffer = new Dictionary<Tuple<int, int>, List<Unit>>();

                            int x_min = int.MaxValue;
                            int x_max = -1;
                            int y_min = int.MaxValue;
                            int y_max = -1;

                            foreach (Unit u in scn.selected_units)
                            {
                                if (x_min > u.mapX - u.cfg.width_cells / 2)
                                    x_min = u.mapX - u.cfg.width_cells / 2;
                                if (x_max < u.mapX + u.cfg.width_cells / 2 - 1 + u.cfg.width_cells % 2)
                                    x_max = u.mapX + u.cfg.width_cells / 2 - 1 + u.cfg.width_cells % 2;
                                if (y_min > u.mapY - u.cfg.height_cells / 2)
                                    y_min = u.mapY - u.cfg.height_cells / 2;
                                if (y_max < u.mapY + u.cfg.height_cells / 2 - 1 + u.cfg.height_cells % 2)
                                    y_max = u.mapY + u.cfg.height_cells / 2 - 1 + u.cfg.height_cells % 2;
                            }
                            //Gl.palette.units_in_buffer_x_min = x_min;
                            //Gl.palette.units_in_buffer_x_max = x_max;
                            //Gl.palette.units_in_buffer_y_min = y_min;
                            //Gl.palette.units_in_buffer_y_max = y_max;
                            Gl.palette.units_in_buffer_x_min = 0;
                            Gl.palette.units_in_buffer_x_max = x_max - x_min + 1;
                            Gl.palette.units_in_buffer_y_min = 0;
                            Gl.palette.units_in_buffer_y_max = y_max - y_min + 1;

                            units_in_buffer_image.close();
                            units_in_buffer_image.init();

                            foreach (Unit u in scn.selected_units)
                            {
                                System.Drawing.Rectangle rect = Gl.curr_scena.UnitRect(u);
                                rect.X -= x_min;
                                rect.Y -= y_min;

                                Tuple<int, int> tup = new Tuple<int, int>(rect.X, rect.Y);
                                if (Gl.palette.units_in_buffer.Keys.Contains(tup))
                                    Gl.palette.units_in_buffer[tup].Add(u);
                                else
                                    Gl.palette.units_in_buffer.Add(tup, new List<Unit> { u });

                                // Отрисовка курсора
                                BitmapImage bmp_i = Unit_Sprites.getBitmapImage(u.folder, u.spr_number, 0);

                                int ux = tup.Item1 * 32 + 32 * u.cfg.width_cells / 2;
                                int uy = tup.Item2 * 32 + 32 * u.cfg.height_cells / 2;
                                int uw = u.cfg.width_cells * 32;
                                int uh = u.cfg.height_cells * 32;
                                int d = 2;

                                units_in_buffer_image.add_unit_img(bmp_i, ux, uy, u.cfg.z_level);
                                units_in_buffer_image.add_unit_frame(ux - uw / 2 + d, uy - uh / 2 + d, uw - d * 2, uh - d * 2, Gl.getPlayer_color(u.player));
                            }

                            units_in_buffer_image.InvalidateVisual();

                            label_units_count.Content = scn.selected_units.Count + " units copied";
                        }

                        if (ctrl && e.Key == Key.V &&               // paste
                            !alt && !shift &&
                            Gl.palette.units_in_buffer != null &&
                            Gl.palette.units_in_buffer.Count > 0 &&
                            paste_units_mode == false)
                        {
                            units_in_buffer_image.Visibility = System.Windows.Visibility.Visible;
                            paste_units_mode = true;
                            CursorsUpdate();

                            label_units_count.Content = Gl.palette.units_in_buffer.Count + " units in buffer";
                        }

                        if (e.Key == Key.Delete || (!alt && !shift && ctrl && e.Key == Key.X)) // delete or cut
                        {
                            foreach (Unit u in Gl.curr_scena.selected_units)
                            {
                                Gl.curr_scena.unit_delete(u, true);
                            }
                            Gl.curr_scena.selected_units.Clear();
                            label_units_count.Content = "selected units deleted";

                            if (UnitsTmpEvents.Count > 0)
                            {
                                History.History.AddUnitsEvent(UnitsTmpEvents);
                                UnitsTmpEvents = new List<SetUnitEvent>();
                            }
                        }
                        #endregion copy paste delete


                        #region move selector
                        int dy = 0;
                        int dx = 0;

                        if (e.Key == Key.NumPad4)
                            dx = -1;
                        if (e.Key == Key.NumPad6)
                            dx = 1;
                        if (e.Key == Key.NumPad8)
                            dy = -1;
                        if (e.Key == Key.NumPad2)
                            dy = 1;

                        if (dx != 0)
                        {
                            if (palette.selected_unit_index != -1)
                            {
                                if (dx == 1)
                                    dy = dx; // перемещаем вверх/вниз
                                else if (dx == -1)
                                {
                                    TreeViewItem twi_unit = (TreeViewItem)units_Tree.SelectedItem;
                                    TreeViewItem twi_folder = (TreeViewItem)twi_unit.Parent;

                                    if (twi_folder.IsSelected)
                                        twi_folder.IsExpanded = false;
                                    twi_folder.IsSelected = true;

                                }
                            }
                            else
                            {
                                TreeViewItem twi_folder = (TreeViewItem)units_Tree.SelectedItem;
                                if (twi_folder != null && twi_folder != units_Tree.Items[0]) // <empty> не имеет item'ы
                                {
                                    int f_index = units_Tree.Items.IndexOf(twi_folder);

                                    if (dx == -1)
                                    {
                                        if (twi_folder.IsExpanded == false)
                                            foreach (TreeViewItem twi in units_Tree.Items)
                                                twi.IsExpanded = false;
                                        else
                                            twi_folder.IsExpanded = false;
                                    }
                                    else if (dx == 1)
                                    {
                                        if (twi_folder.IsExpanded == true)
                                            ((TreeViewItem)twi_folder.Items[0]).IsSelected = true;
                                        else
                                            twi_folder.IsExpanded = true;
                                    }
                                }
                            }
                        }

                        if (dy != 0)
                        {
                            if (palette.selected_unit_index != -1)
                            {
                                TreeViewItem twi_unit = (TreeViewItem)units_Tree.SelectedItem;
                                TreeViewItem twi_folder = (TreeViewItem)twi_unit.Parent;
                                int u_index = twi_folder.Items.IndexOf(twi_unit);
                                int f_index = units_Tree.Items.IndexOf(twi_folder);

                                if (twi_folder.Items.Count > u_index + dy && u_index + dy >= 0)
                                    ((TreeViewItem)twi_folder.Items[u_index + dy]).IsSelected = true;
                                else if (u_index + dy == -1)
                                    twi_folder.IsSelected = true;
                                else if (u_index + dy == twi_folder.Items.Count)
                                    if (f_index + dy < units_Tree.Items.Count)
                                        ((TreeViewItem)units_Tree.Items[f_index + dy]).IsSelected = true;
                            }
                            else
                            { // перемещение по папкам
                                TreeViewItem twi_folder = (TreeViewItem)units_Tree.SelectedItem; // текущая папка
                                if (twi_folder != null)
                                {
                                    int f_index = units_Tree.Items.IndexOf(twi_folder); // индекс текущей папки

                                    if (units_Tree.Items.Count > f_index + dy && f_index + dy >= 0) // проверяем индекс следующей папки
                                    {
                                        TreeViewItem twi_folder_next = (TreeViewItem)units_Tree.Items[f_index + dy];// следущая папка

                                        if (dy == -1) // двигаемся вверх
                                        {
                                            if (twi_folder_next.IsExpanded) // папка раскрыта - переходим в последний индекс
                                                ((TreeViewItem)twi_folder_next.Items[twi_folder_next.Items.Count - 1]).IsSelected = true;
                                            else
                                                twi_folder_next.IsSelected = true;
                                        }
                                        else if (dy == 1) // двигаемся вниз
                                        {

                                            if (twi_folder.IsExpanded) // текущая папка раскрыта - значит нужно зайти
                                                ((TreeViewItem)twi_folder.Items[0]).IsSelected = true;
                                            else
                                                twi_folder_next.IsSelected = true;
                                        }

                                    }
                                }
                            }
                        }

                        #endregion move selector

                        if (e.Key == Key.Enter) // enter
                        {
                            if (Gl.curr_scena.selected_units.Count() > 0)
                            {
                                Gl.unit_properties_window.update_properties();
                                Gl.unit_properties_window.Visibility = System.Windows.Visibility.Visible;
                            }
                        }

                        if (e.Key == Key.Escape)
                        {

                            // очищаем выбор юнитов, закрасив рамку в нужный цвет
                            foreach (Unit u in Gl.curr_scena.selected_units)
                                Gl.curr_scena.update_unit_frame_to_player_color(u);
                            Gl.curr_scena.selected_units.Clear();

                            paste_units_mode = false;
                            units_in_buffer_image.Visibility = System.Windows.Visibility.Hidden;

                            Gl.palette.DeSelectUnit();
                            selectedInfoUpdate();
                        }

                        #region change player

                        if (e.Key == Key.D1)
                            comboBox_Player.SelectedIndex = 0;
                        if (e.Key == Key.D2)
                            comboBox_Player.SelectedIndex = 1;
                        if (e.Key == Key.D3)
                            comboBox_Player.SelectedIndex = 2;
                        if (e.Key == Key.D4)
                            comboBox_Player.SelectedIndex = 3;
                        if (e.Key == Key.D5)
                            comboBox_Player.SelectedIndex = 4;
                        if (e.Key == Key.D6)
                            comboBox_Player.SelectedIndex = 5;
                        if (e.Key == Key.D7)
                            comboBox_Player.SelectedIndex = 6;
                        if (e.Key == Key.D8)
                            comboBox_Player.SelectedIndex = 7;

                        #endregion change player
                    }

                    break;
                case 3:
                    #region resourses history
                    if (!alt && !shift && ctrl && e.Key == Key.Z && this.IsActive)
                    {
                        List<SetResourceEvent> l_sle = History.History.ExtractUndoResourceEvent();
                        SetResourceEvent sle;

                        if (l_sle != null) // && l_sle.Count > 0)
                        {
                            for (int i = l_sle.Count - 1; i >= 0; i--)
                            {
                                sle = l_sle[i];
                                Gl.curr_scena.set_Resource_tile(sle.X, sle.Y, 1, sle.ResFrom, false);
                            }
                        }
                    }
                    if (!alt && !shift && ctrl && e.Key == Key.Y && this.IsActive)
                    {
                        List<SetResourceEvent> l_sle = History.History.ExtractRedoResourceEvent();
                        SetResourceEvent sle;

                        if (l_sle != null) // && l_sle.Count > 0)
                        {
                            for (int i = l_sle.Count - 1; i >= 0; i--)
                            {
                                sle = l_sle[i];
                                Gl.curr_scena.set_Resource_tile(sle.X, sle.Y, 1, sle.ResTo, false);
                            }
                        }
                    }
                    #endregion resourses history

                    #region move selector
                    if (e.Key == Key.NumPad8)
                    {
                        if (comboBox_Resources.SelectedIndex < comboBox_Resources.Items.Count - 1)
                            comboBox_Resources.SelectedIndex++;
                        else
                            comboBox_Resources.SelectedIndex = 0;
                    }
                    if (e.Key == Key.NumPad2)
                    {
                        if (comboBox_Resources.SelectedIndex > 0)
                            comboBox_Resources.SelectedIndex--;
                        else
                            comboBox_Resources.SelectedIndex = comboBox_Resources.Items.Count - 1;
                    }
                    #endregion move selector

                    #region change brush size

                    if (e.Key == Key.D0)
                        comboBox_Brush.SelectedIndex = 0;
                    if (e.Key == Key.D1)
                        comboBox_Brush.SelectedIndex = 1;
                    if (e.Key == Key.D2)
                        comboBox_Brush.SelectedIndex = 2;
                    if (e.Key == Key.D3)
                        comboBox_Brush.SelectedIndex = 3;
                    if (e.Key == Key.D4)
                        comboBox_Brush.SelectedIndex = 4;
                    if (e.Key == Key.D5)
                        comboBox_Brush.SelectedIndex = 5;
                    if (e.Key == Key.D6)
                        comboBox_Brush.SelectedIndex = 6;
                    if (e.Key == Key.D7)
                        comboBox_Brush.SelectedIndex = 7;
                    if (e.Key == Key.D8)
                        comboBox_Brush.SelectedIndex = 8;

                    #endregion change brush size

                    if (e.Key == Key.Escape)
                        comboBox_Resources.SelectedIndex = 0;

                    break;
            }

            if (!alt && ctrl && !shift && e.Key == Key.S)
                saveMap();
            if (!alt && ctrl && !shift && e.Key == Key.O)
                loadScena_Button_Click(this, null);
            if (!alt && ctrl && shift && e.Key == Key.S)
                saveAs_Button_Click(this, null);
            if (!alt && ctrl && shift && e.Key == Key.O)
                reloadScena_Button_Click(this, null);

            // windows
            if (!alt && ctrl && !shift && e.Key == Key.L)
                showPalette_Button_Click(this, null);
            if (!alt && ctrl && !shift && e.Key == Key.M)
                showMinimap_Button_Click(this, null);
            if (!alt && ctrl && !shift && e.Key == Key.U)
                showUnitsTable_Button_Click(this, null);
            if (!alt && ctrl && !shift && e.Key == Key.P)
                showPlayers_Button_Click(this, null);
            if (!alt && ctrl && !shift && e.Key == Key.B)
                showBlocks_Button_Click(this, null);
            if (!alt && ctrl && !shift && e.Key == Key.T)
                showTiles_Button_Click(this, null);
            if (!alt && ctrl && !shift && e.Key == Key.E)
                scenaProperties_Button_Click(this, null);

            // palette
            if (!alt && !ctrl && !shift && e.Key == Key.L)
                tabControl_Palette.SelectedItem = Ground_Palette_Tab;
            if (!alt && !ctrl && !shift && e.Key == Key.A)
                tabControl_Palette.SelectedItem = AutoTiles_Palette_Tab;
            if (!alt && !ctrl && !shift && e.Key == Key.U)
                tabControl_Palette.SelectedItem = Unit_Palette_Tab;
            if (!alt && !ctrl && !shift && e.Key == Key.R)
                tabControl_Palette.SelectedItem = Resources_Palette_Tab;

            // view
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (alt && !ctrl && !shift && key == Key.L)
                landView_checkBox.IsChecked = !landView_checkBox.IsChecked;
            if (alt && !ctrl && !shift && key == Key.U)
                unitsView_checkBox.IsChecked = !unitsView_checkBox.IsChecked;
            if (alt && !ctrl && !shift && key == Key.F)
                unitsFramesView_checkBox.IsChecked = !unitsFramesView_checkBox.IsChecked;
            if (alt && !ctrl && !shift && key == Key.C)
                unitsFramesColorView_checkBox.IsChecked = !unitsFramesColorView_checkBox.IsChecked;
            if (alt && !ctrl && !shift && key == Key.R)
                resourcesView_checkBox.IsChecked = !resourcesView_checkBox.IsChecked;

            if (alt && !ctrl && !shift && key == Key.G)
            {
                if (comboBox_Grid.SelectedIndex < comboBox_Grid.Items.Count - 1)
                    comboBox_Grid.SelectedIndex++;
                else
                    comboBox_Grid.SelectedIndex = 0;
            }
        }


        // Клик по палитре
        private void Ground_Pallete_Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                int mx = (int)e.GetPosition(Ground_Pallete_Canvas).X;
                int my = (int)e.GetPosition(Ground_Pallete_Canvas).Y;
                int x = mx / Gl.palette.palette_size_cell;
                int y = my / Gl.palette.palette_size_cell;

                Gl.palette.selected_tile = y * Gl.palette.tiles_collumns_count + x;
                if (Gl.palette.selected_tile >= Gl.curr_scena.tileSet.Count || Gl.palette.selected_tile < 0)
                {
                    Gl.palette.selected_tile = -1;
                    Gl.palette.selected_sprite = -1;
                }
                else
                    Gl.palette.selected_sprite = Gl.curr_scena.tileSet[Gl.palette.selected_tile].SpriteId;

                Gl.palette.Is_block_selected = false;

                Gl.palette.UpdateGroundSelector(x, y);

                // Окно тайлов.
                if (Gl.palette.selected_tile < Gl.curr_scena.tileSet.Count && Gl.palette.selected_tile >= 0)
                {
                    Gl.tiles_window.SelectedTile = Gl.palette.selected_tile;
                    Gl.tiles_window.SelectedSprite = Gl.palette.selected_sprite;
                    Gl.tiles_window.FullUpdate();
                }

            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                Gl.palette.DeSelectLand();
            }
            CursorsUpdate();
            selectedInfoUpdate();
        }
        #endregion mouse_and_keys


        #region ground history
        private void LandHistoryUndo()
        {
            Dictionary<HistoryPoint, SetLandEvent> l_sle = History.History.ExtractUndoLandEvent();
            SetLandEvent sle;

            if (l_sle != null) // && l_sle.Count > 0)
            {
                /*for (int i = l_sle.Count - 1; i >= 0; i--)
                {
                    sle = l_sle[i];
                    Gl.curr_scena.set_Ground_region_tile(sle.X, sle.Y, 1, sle.SprFrom, false);
                }*/
                foreach (HistoryPoint point in l_sle.Keys)
                {
                    sle = l_sle[point];
                    Gl.curr_scena.set_Ground_region_tile(sle.X, sle.Y, 1, sle.SprFrom, false);
                }
            }


        }
        private void LandHistoryRedo()
        {
            Dictionary<HistoryPoint, SetLandEvent> l_sle = History.History.ExtractRedoLandEvent();
            SetLandEvent sle;

            if (l_sle != null) // && l_sle.Count > 0)
            {
                //for (int i = l_sle.Count - 1; i >= 0; i--)
                //{
                //    sle = l_sle[i];
                //    Gl.curr_scena.set_Ground_region_tile(sle.X, sle.Y, 1, sle.SprTo, false);
                //}

                foreach (HistoryPoint point in l_sle.Keys)
                {
                    sle = l_sle[point];
                    Gl.curr_scena.set_Ground_region_tile(sle.X, sle.Y, 1, sle.SprTo, false);
                }
            }

        }
        #endregion ground history



        #region controls
        private void Horde_Editor_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Gl.minimap != null)
            {
                Gl.minimap.closing = true;
                Gl.minimap.Close();
            }
            if (Gl.unit_properties_window != null)
            {
                Gl.unit_properties_window.closing = true;
                Gl.unit_properties_window.Close();
            }
            if (Gl.units_table_window != null)
            {
                Gl.units_table_window.closing = true;
                Gl.units_table_window.Close();
            }
            if (Gl.players_properties_window != null)
            {
                Gl.players_properties_window.closing = true;
                Gl.players_properties_window.Close();
            }
            if (Gl.blocks_window != null)
            {
                Gl.blocks_window.closing = true;
                Gl.blocks_window.Close();
            }
            if (Gl.tiles_window != null)
            {
                Gl.tiles_window.closing = true;
                Gl.tiles_window.Close();
            }
            if (Gl.scena_prop_window != null)
            {
                Gl.scena_prop_window.closing = true;
                Gl.scena_prop_window.Close();
            }
            //if (Gl.progress_wnd != null)
            //{
            //    Gl.progress_wnd.closing = true;
            //    Gl.progress_wnd.Close();
            //}

        }

        //double scroll_dx;
        //double scroll_dy;
        double scroll_lastpos_x = -33;
        double scroll_lastpos_y = -33;
        private void scrollViewer_Map_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Gl.curr_scena == null)
                return;

            if (!(Math.Abs(scrollViewer_Map.HorizontalOffset - scroll_lastpos_x) >= 32 ||
                  Math.Abs(scrollViewer_Map.VerticalOffset - scroll_lastpos_y) >= 32))
                return;

            scroll_lastpos_x = scrollViewer_Map.HorizontalOffset;
            scroll_lastpos_y = scrollViewer_Map.VerticalOffset;

            Gl.minimap.update_rectangle_on_minimap();

            //int x = (int)(scrollViewer_Map.HorizontalOffset / 32);
            //int y = (int)(scrollViewer_Map.VerticalOffset / 32);
            //int w = (int)(scrollViewer_Map.RenderSize.Width / 32);
            //int h = (int)(scrollViewer_Map.RenderSize.Height / 32);
            //
            //if (x > 5)
            //    x -= 5;
            //else
            //    x = 0;
            //
            //if (y > 5)
            //    y -= 5;
            //else
            //    y = 0;
            //
            //if (x + w <= Gl.curr_scena.size_map_x - 5)
            //    w += 5;
            //else
            //    w = Gl.curr_scena.size_map_x - x;
            //
            //if (y + h <= Gl.curr_scena.size_map_y - 5)
            //    h += 5;
            //else
            //    h = Gl.curr_scena.size_map_y - y;
            //
            //
            //image_Ground.UpdateVisibleImgs(x, y, w, h);

        }

        #region show Windows
        private void showUnitsTable_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.units_table_window.Visibility == System.Windows.Visibility.Visible)
            {
                Gl.units_table_window.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                Gl.units_table_window.update_tables();
                Gl.units_table_window.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void showPlayers_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.players_properties_window.Visibility == System.Windows.Visibility.Visible)
            {
                Gl.players_properties_window.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                Gl.players_properties_window.Update();
                Gl.players_properties_window.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void showBlocks_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.blocks_window.Visibility == System.Windows.Visibility.Visible)
            {
                Gl.blocks_window.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                //Gl.blocks_window.Update();
                Gl.blocks_window.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void showMinimap_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.minimap.Visibility == System.Windows.Visibility.Visible)
            {
                Gl.minimap.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                Gl.minimap.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void showTiles_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.tiles_window.Visibility == System.Windows.Visibility.Visible)
            {
                Gl.tiles_window.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                Gl.tiles_window.FullUpdate();
                Gl.tiles_window.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void scenaProperties_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.scena_prop_window.Visibility == System.Windows.Visibility.Visible)
            {
                Gl.scena_prop_window.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                Gl.scena_prop_window.FullUpdate();
                Gl.scena_prop_window.Visibility = System.Windows.Visibility.Visible;
            }
        }

        #endregion show Windows

        private void units_Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Gl.palette.UpdateUnitPortrait();
            CursorsUpdate();
            selectedInfoUpdate();
        }

        private void comboBox_Player_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                Gl.palette.UpdatePlayerColor();
                Gl.palette.UpdatePlayerUnitCounter();
            }
        }

        private void comboBox_Resources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gl.palette != null)
            {
                Gl.palette.selected_resourse = comboBox_Resources.SelectedIndex - 1;

                selectedInfoUpdate();
                CursorsUpdate();
            }
        }

        private void ComboBox_Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                int step;
                ComboBoxItem cbi = comboBox_Grid.SelectedItem as ComboBoxItem;
                int i = comboBox_Grid.SelectedIndex;
                if (i > 0)
                    step = Convert.ToInt32(cbi.Content);
                else
                    step = 0;
                Gl.curr_scena.update_grid(step);
            }
        }

        private void loadScena_Button_Click(object sender, RoutedEventArgs e)
        {
            Gl.minimap.Topmost = false;
            Gl.unit_properties_window.Topmost = false;
            Gl.blocks_window.Topmost = false;
            Gl.tiles_window.Topmost = false;
            Gl.scena_prop_window.Topmost = false;

            string scena_path;
            scena_path = load_scena_dialog();
            if (scena_path != null)
            {
                //Gl.progress_wnd = new Progress();

                Gl.curr_scena.Close_Scene();
                Gl.curr_scena = new Scena();

                if (!Gl.curr_scena.Load_Scene(scena_path))
                { this.Close(); return; }

                Gl.main.Title = Gl.curr_scena.scena_short_name + " - Horde Editor " + Gl.Editor_Version;
                if (Gl.units_table_window.Visibility == System.Windows.Visibility.Visible)
                    Gl.units_table_window.update_tables();
                Gl.palette.UpdatePlayerUnitCounter(); // обновим счетчик выбранного на палитре игрока

                //Gl.progress_wnd.closing = true;
                //Gl.progress_wnd.Close();
            }

            Gl.minimap.Topmost = true;
            Gl.unit_properties_window.Topmost = true;
            Gl.blocks_window.Topmost = true;
            Gl.tiles_window.Topmost = true;
            Gl.scena_prop_window.Topmost = true;
        }

        private void reloadScena_Button_Click(object sender, RoutedEventArgs e)
        {
            string scena_path = Gl.curr_scena.scena_path;
            if (File.Exists(scena_path))
            {
                MessageBoxResult res = MessageBox.Show("Rly reload?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    Gl.curr_scena.Close_Scene();
                    Gl.curr_scena = new Scena();

                    if (!Gl.curr_scena.Load_Scene(scena_path))
                    { this.Close(); return; }
                }
            }
            else
                MessageBox.Show(scena_path, "File not exist! wtf", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void saveAs_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                Nullable<bool> result = false;

                dlg.Title = "Save Scena";
                dlg.DefaultExt = "*.scn";
                dlg.FileName = Gl.curr_scena.scena_short_name + ".scn";
                dlg.Filter = "Scena files (*.scn)|*.scn";

                result = dlg.ShowDialog();

                if (result == true)
                {
                    bool res = Gl.curr_scena.Save_Scene(dlg.FileName);

                    if (res)
                        MessageBox.Show("Save done!", "Saving", MessageBoxButton.OK, MessageBoxImage.Information);

                    Gl.main.Title = Gl.curr_scena.scena_short_name + " - Horde Editor " + Gl.Editor_Version;
                }
            }
        }

        internal void saveMap()
        {
            bool res = Gl.curr_scena.Save_Scene(Gl.curr_scena.scena_path);

            if (res)
                MessageBox.Show("Save done!", "Saving", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        public void HistoryWrite()
        {
            #region history write
            if (LandTmpEvents.Count > 0)
            {
                History.History.AddLandEvent(LandTmpEvents);
                LandTmpEvents = new Dictionary<HistoryPoint, SetLandEvent>(HistoryPointEqC);
            }
            if (UnitsTmpEvents.Count > 0)
            {
                History.History.AddUnitsEvent(UnitsTmpEvents);
                UnitsTmpEvents = new List<SetUnitEvent>();
            }
            if (ResourceTmpEvents.Count > 0)
            {
                History.History.AddResourceEvent(ResourceTmpEvents);
                ResourceTmpEvents = new List<SetResourceEvent>();
            }
            #endregion history write
        }

        private void save_Button_Click(object sender, RoutedEventArgs e)
        {
            saveMap();
        }
        private void saveImage_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                double x = scrollViewer_Map.HorizontalOffset;
                double y = scrollViewer_Map.VerticalOffset;

                double zoom = uiScaleSlider.Value;

                if ((int)x != 0 || y != 0 || zoom != 1)
                {
                    MessageBox.Show("At first scroll to (0;0) and scale 1", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {

                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    Nullable<bool> result = false;

                    dlg.Title = "Save image";
                    dlg.DefaultExt = "*.png";
                    dlg.FileName = "_export_image_" + Gl.curr_scena.scena_short_name + ".png";
                    dlg.Filter = "PNG files (*.png)|*.png";

                    result = dlg.ShowDialog();

                    if (result == true)
                    {
                        // Create a thread
                        Thread newWindowThread = new Thread(new ThreadStart(() =>
                        {
                            // Create and show the Window
                            Progress tempWindow = new Progress();
                            tempWindow.closing = true;
                            tempWindow.Show();
                            tempWindow.Title = "Export image";
                            System.Windows.Threading.Dispatcher.Run();
                        }));
                        newWindowThread.SetApartmentState(ApartmentState.STA);
                        newWindowThread.IsBackground = true;
                        newWindowThread.Start();

                        RenderTargetBitmap rtb = new RenderTargetBitmap((int)canvas_Map.RenderSize.Width, (int)canvas_Map.RenderSize.Height, 96d, 96d, System.Windows.Media.PixelFormats.Default);
                        rtb.Render(canvas_Map);

                        BitmapEncoder pngEncoder = new PngBitmapEncoder();
                        pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

                        using (var fs = System.IO.File.OpenWrite(dlg.FileName))
                        {
                            pngEncoder.Save(fs);
                        }


                        // minimap
                        RenderTargetBitmap rtb_mini = new RenderTargetBitmap((int)Gl.minimap.minimap_image.RenderSize.Width, (int)Gl.minimap.minimap_image.RenderSize.Height, 96d, 96d, System.Windows.Media.PixelFormats.Default);
                        rtb_mini.Render(Gl.minimap.minimap_image);

                        BitmapEncoder pngEncoder_mini = new PngBitmapEncoder();
                        pngEncoder_mini.Frames.Add(BitmapFrame.Create(rtb_mini));

                        string filename = System.IO.Path.GetDirectoryName(dlg.FileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + "_minimap.png";

                        using (var fs = System.IO.File.OpenWrite(filename))
                        {
                            pngEncoder_mini.Save(fs);
                        }

                        newWindowThread.Abort();
                    }
                }
            }
        }

        private void showPalette_Button_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl_Palette.Visibility == System.Windows.Visibility.Visible)
            {
                tabControl_Palette.Visibility = System.Windows.Visibility.Hidden;
                scrollViewer_Map.Margin = new Thickness(scrollViewer_Map.Margin.Left,
                    scrollViewer_Map.Margin.Top,
                    0,
                    scrollViewer_Map.Margin.Bottom);
            }
            else
            {
                tabControl_Palette.Visibility = System.Windows.Visibility.Visible;
                scrollViewer_Map.Margin = new Thickness(scrollViewer_Map.Margin.Left,
                    scrollViewer_Map.Margin.Top,
                    tabControl_Palette.RenderSize.Width,
                    scrollViewer_Map.Margin.Bottom);
            }
        }

        private void tabControl_Palette_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem ti = e.AddedItems[0] as TabItem;
            if (ti == null) return;
            if (Gl.curr_scena == null) return;

            if (ti == Unit_Palette_Tab)
            {
                Gl.palette.UpdateUnitPortrait();
            }

            CursorsUpdate();
            selectedInfoUpdate();
        }
        public void selectedInfoUpdate()
        {
            switch (tabControl_Palette.SelectedIndex)
            {
                case 0:
                    if (Gl.palette.selected_sprite > -1 && Gl.palette.Is_block_selected == false)
                        label_selected_spr.Content = "Выбранный спрайт(тайл): " + Gl.palette.selected_sprite + " (" + Gl.palette.selected_tile + ")";
                    else if (Gl.palette.selected_block > -1 && Gl.palette.Is_block_selected == true)
                        label_selected_spr.Content = "Выбранный блок: " + Gl.palette.selected_block;
                    else
                        label_selected_spr.Content = "Спрайт не выбран";
                    break;
                case 1:
                    label_selected_spr.Content = "AutoTile";
                    break;
                case 2:
                    label_selected_spr.Content = "Выбранный юнит: " + Gl.palette.SelectedUnitName;
                    break;
                case 3:

                    switch (Gl.palette.selected_resourse)
                    {
                        case -1:
                            label_selected_spr.Content = "Выбранный ресурс: ---";
                            break;
                        case 0:
                            label_selected_spr.Content = "Выбранный ресурс: Убрать";
                            break;
                        case 1:
                            label_selected_spr.Content = "Выбранный ресурс: золото";
                            break;
                        case 2:
                            label_selected_spr.Content = "Выбранный ресурс: зелезо";
                            break;
                    }
                    break;
            }
        }

        public void BrushUpdate()
        {
            TabItem ti = (TabItem)tabControl_Palette.SelectedItem;

            if (Gl.palette.selected_tile != -1 && ti == Ground_Palette_Tab ||
                Gl.palette.autotile_args.type1 != TileType.Unknown && ti == AutoTiles_Palette_Tab ||
                Gl.palette.selected_resourse != -1 && ti == Resources_Palette_Tab)
            {
                if (comboBox_Brush.SelectedIndex != Gl.palette.last_ground_brush_size)
                {
                    comboBox_Brush.SelectionChanged -= ComboBox_Brush_SelectionChanged;
                    comboBox_Brush.SelectedIndex = Gl.palette.last_ground_brush_size;
                    Gl.palette.curr_ground_brush_size = comboBox_Brush.SelectedIndex;
                    comboBox_Brush.SelectionChanged += ComboBox_Brush_SelectionChanged;
                }
            }
            else if (comboBox_Brush.SelectedIndex != 0)
            {
                comboBox_Brush.SelectionChanged -= ComboBox_Brush_SelectionChanged;
                Gl.palette.last_ground_brush_size = comboBox_Brush.SelectedIndex;
                comboBox_Brush.SelectedIndex = 0;
                Gl.palette.curr_ground_brush_size = 0;
                comboBox_Brush.SelectionChanged += ComboBox_Brush_SelectionChanged;

            }
        }

        public void CursorsUpdate()
        {
            Point p = Mouse.GetPosition(canvas_Map);//MouseUtilities.CorrectGetPosition(canvas_Map);
            int mx = (int)p.X;
            int my = (int)p.Y;
            int x = mx / 32;
            int y = my / 32;
            int d = 0;

            int w;
            int h;

            if (Gl.palette == null)
                return;

            switch (tabControl_Palette.SelectedIndex)
            {
                case 0:
                    BrushUpdate();
                    d = -1;
                    if (Gl.palette.Is_block_selected)
                    {
                        //select_land_mode = false;

                        //cursor_Block.Visibility = System.Windows.Visibility.Visible;
                        //rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
                        w = Gl.palette.block_width;
                        h = Gl.palette.block_height;


                        Canvas.SetLeft(rectangle_cursor, (x - w / 2) * 32 + d);
                        Canvas.SetTop(rectangle_cursor, (y - h / 2) * 32 + d);
                        Canvas.SetLeft(cursor_Block, (x - w / 2) * 32);
                        Canvas.SetTop(cursor_Block, (y - h / 2) * 32);

                        rectangle_cursor.Width = Gl.palette.block_width * 32 + 3;
                        rectangle_cursor.Height = Gl.palette.block_height * 32 + 3;
                    }
                    else if (Gl.palette.selected_tile != -1)
                    {
                        //select_land_mode = false;

                        cursor_Block.Visibility = System.Windows.Visibility.Hidden;
                        //rectangle_select_land.Visibility = System.Windows.Visibility.Hidden;
                        //comboBox_Brush.SelectedIndex - 1

                        w = Gl.palette.curr_ground_brush_size;
                        h = Gl.palette.curr_ground_brush_size;

                        Canvas.SetLeft(rectangle_cursor, (x - w / 2) * 32 + d);
                        Canvas.SetTop(rectangle_cursor, (y - h / 2) * 32 + d);

                        rectangle_cursor.Width = w * 32 + 3;
                        rectangle_cursor.Height = h * 32 + 3;
                    }
                    else if (select_land_mode)
                    {
                        //rectangle_select_land.Visibility = System.Windows.Visibility.Visible;
                        cursor_Block.Visibility = System.Windows.Visibility.Hidden;
                        rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        cursor_Block.Visibility = System.Windows.Visibility.Hidden;
                        rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
                    }
                    break;
                case 1:
                    BrushUpdate();
                    d = -1;
                    w = Gl.palette.curr_ground_brush_size;
                    h = Gl.palette.curr_ground_brush_size;

                    rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetLeft(rectangle_cursor, (x - w / 2) * 32 + d);
                    Canvas.SetTop(rectangle_cursor, (y - h / 2) * 32 + d);

                    rectangle_cursor.Width = w * 32 + 3;
                    rectangle_cursor.Height = h * 32 + 3;
                    break;
                case 2:
                    if (Gl.palette.selected_unit_folder != -1 && Gl.palette.selected_unit_index != -1 && paste_units_mode == false)
                    {
                        d = 1;

                        rectangle_cursor.Visibility = System.Windows.Visibility.Visible;

                        Canvas.SetLeft(rectangle_cursor, x * 32 + d);
                        Canvas.SetTop(rectangle_cursor, y * 32 + d);

                        // Изменяем размеры курсора.
                        Gl.main.rectangle_cursor.Width = Gl.palette.SelectedUnitCfg.width_cells * 32 - 1;
                        Gl.main.rectangle_cursor.Height = Gl.palette.SelectedUnitCfg.height_cells * 32 - 1;
                    }
                    else
                    {
                        // Изменяем размеры курсора.
                        Gl.main.rectangle_cursor.Width = 0;
                        Gl.main.rectangle_cursor.Height = 0;

                        if (paste_units_mode)
                        {
                            Scena scn = Gl.curr_scena;
                            Palette palette = Gl.palette;

                            w = palette.units_in_buffer_x_max - palette.units_in_buffer_x_min;
                            h = palette.units_in_buffer_y_max - palette.units_in_buffer_y_min;

                            //if (w > 0)
                            units_in_buffer_image.Width = w * 32;
                            //else
                            //    units_in_buffer_image.Width = 32;
                            //
                            //if (h > 0)
                            units_in_buffer_image.Height = h * 32;
                            //else
                            //    units_in_buffer_image.Height = 32;

                            Canvas.SetLeft(units_in_buffer_image, (x - w / 2) * 32);
                            Canvas.SetTop(units_in_buffer_image, (y - h / 2) * 32);
                            //Canvas.SetLeft(units_in_buffer_image, x * 32);
                            //Canvas.SetTop(units_in_buffer_image, y * 32);
                        }
                    }
                    break;
                case 3:
                    d = -1;
                    BrushUpdate();
                    w = Gl.palette.curr_ground_brush_size;
                    h = Gl.palette.curr_ground_brush_size;

                    rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetLeft(rectangle_cursor, (x - w / 2) * 32 + d);
                    Canvas.SetTop(rectangle_cursor, (y - h / 2) * 32 + d);

                    rectangle_cursor.Width = w * 32 + 3;
                    rectangle_cursor.Height = h * 32 + 3;
                    break;
            }


        }

        private void scrollViewer_Map_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Gl.minimap.update_rectangle_on_minimap();
        }

        private void Horde_Editor_Activated(object sender, EventArgs e)
        {
            Gl.minimap.Topmost = true;
            Gl.unit_properties_window.Topmost = true;
            Gl.blocks_window.Topmost = true;
            Gl.tiles_window.Topmost = true;
            Gl.scena_prop_window.Topmost = true;
            Gl.players_properties_window.Topmost = true;

        }

        private void Horde_Editor_Deactivated(object sender, EventArgs e)
        {
            Gl.minimap.Topmost = false;
            Gl.unit_properties_window.Topmost = false;
            Gl.blocks_window.Topmost = false;
            Gl.tiles_window.Topmost = false;
            Gl.scena_prop_window.Topmost = false;
            Gl.players_properties_window.Topmost = false;
        }

        public void ComboBox_Brush_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gl.palette == null)
                return;

            if (comboBox_Brush.SelectedIndex != 0)
                Gl.palette.last_ground_brush_size = comboBox_Brush.SelectedIndex;

            Gl.palette.curr_ground_brush_size = comboBox_Brush.SelectedIndex;
            CursorsUpdate();
        }

        private void showHelp_Button_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show(@"https://vk.com/horde_editor", "Horde Editor vk group", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        #region checkBoxes view
        private void landView_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            image_Ground.Visibility = System.Windows.Visibility.Visible;
            if (Gl.curr_scena != null)
                Gl.minimap.Update();
        }

        private void unitsView_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            //image_Units.Visibility = System.Windows.Visibility.Visible;
            image_Units.DrawUnits = true;
            image_Units.InvalidateVisual();
            if (Gl.curr_scena != null)
                Gl.minimap.Update();
        }

        private void unitsFramesView_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            image_Units.DrawFrames = true;
            image_Units.InvalidateVisual();
        }

        private void unitsFramesColorView_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            unitFramesColor_Handler();
        }

        private void resourcesView_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            image_Resources.Visibility = System.Windows.Visibility.Visible;
        }

        private void landView_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            image_Ground.Visibility = System.Windows.Visibility.Hidden;
            if (Gl.curr_scena != null)
                Gl.minimap.Update();
        }

        private void unitsView_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //image_Units.Visibility = System.Windows.Visibility.Hidden;
            image_Units.DrawUnits = false;
            image_Units.InvalidateVisual();
            if (Gl.curr_scena != null)
                Gl.minimap.Update();
        }

        private void unitsFramesView_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            image_Units.DrawFrames = false;
            image_Units.InvalidateVisual();
        }

        private void unitsFramesColorView_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            unitFramesColor_Handler();
        }

        private void resourcesView_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            image_Resources.Visibility = System.Windows.Visibility.Hidden;
        }

        private void unitFramesColor_Handler()
        {
            Scena scn = Gl.curr_scena;
            if (scn == null)
                return;

            foreach (Player p in scn.players)
                foreach (Unit u in p.units)
                {
                    if (scn.selected_units.Contains(u))
                        Gl.curr_scena.update_selected_unit_frame(u);
                    else
                        Gl.curr_scena.update_unit_frame_to_player_color(u);
                }
        }

        #endregion checkBoxes view

        #region autotiles

        private void select_all_toggles_button_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)lu_toggleButton.IsChecked ||
                !(bool)ru_toggleButton.IsChecked ||
                !(bool)ld_toggleButton.IsChecked ||
                !(bool)rd_toggleButton.IsChecked)
            {
                lu_toggleButton.IsChecked = true;
                ru_toggleButton.IsChecked = true;
                ld_toggleButton.IsChecked = true;
                rd_toggleButton.IsChecked = true;
            }
            else
            {
                lu_toggleButton.IsChecked = false;
                ru_toggleButton.IsChecked = false;
                ld_toggleButton.IsChecked = false;
                rd_toggleButton.IsChecked = false;
            }
        }

        private void invert_toggles_button_Click(object sender, RoutedEventArgs e)
        {
            lu_toggleButton.IsChecked = !lu_toggleButton.IsChecked;
            ru_toggleButton.IsChecked = !ru_toggleButton.IsChecked;
            ld_toggleButton.IsChecked = !ld_toggleButton.IsChecked;
            rd_toggleButton.IsChecked = !rd_toggleButton.IsChecked;
        }

        private void cursor_info_toggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void AutoTile_checkBox_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                Gl.palette.autotile_args.aggressive = (bool)Aggressive_AutoTile_checkBox.IsChecked;
                Gl.palette.autotile_args.onlyAll = (bool)OnlyAll_AutoTile_checkBox.IsChecked;
                Gl.palette.autotile_args.onlyInRect = (bool)OnlyInRect_AutoTile_checkBox.IsChecked;
                Gl.palette.autotile_args.recursive = (bool)Recursive_AutoTile_checkBox.IsChecked;
            }
        }

        private void autoTile_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                if (autoTile_Type1_comboBox.SelectedIndex - 1 >= 0)
                    Gl.palette.autotile_args.type1 = (TileType)autoTile_Type1_comboBox.SelectedIndex - 1;
                else
                    Gl.palette.autotile_args.type1 = TileType.Unknown;

                if (autoTile_Type2_comboBox.SelectedIndex - 1 >= 0)
                    Gl.palette.autotile_args.type2 = (TileType)autoTile_Type2_comboBox.SelectedIndex - 1;
                else
                    Gl.palette.autotile_args.type2 = TileType.Unknown;

                autoTile_toggleButtons_Handle();
            }
        }
        private void autoTile_variant_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gl.curr_scena != null)
                Gl.palette.autotile_args.variant = autoTile_variant_comboBox.SelectedIndex;
        }

        private void autoTile_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            autoTile_toggleButtons_Handle();
        }

        private void autoTile_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            autoTile_toggleButtons_Handle();
        }

        private void autoTile_toggleButtons_Handle()
        {
            Gl.palette.UpdateAutoTileForm(
                (bool)lu_toggleButton.IsChecked,
                (bool)ru_toggleButton.IsChecked,
                (bool)ld_toggleButton.IsChecked,
                (bool)rd_toggleButton.IsChecked);

            //Text
            //char ch1 = ' ';
            //char ch2 = ' ';
            string ch1 = "";
            string ch2 = "";

            if (Gl.palette.autotile_args.type1 != TileType.Unknown &&
                Gl.palette.autotile_args.type1 < (TileType)16)          // костыль
            {
                ch1 = H2Strings.tile_types_str[(int)Gl.palette.autotile_args.type1][0] + "";
                if ((int)Gl.palette.autotile_args.type1 % 2 == 1)
                    ch1 += 1;
            }
            if (Gl.palette.autotile_args.type2 != TileType.Unknown &&
                Gl.palette.autotile_args.type2 < (TileType)16)          // костыль
            {
                ch2 = H2Strings.tile_types_str[(int)Gl.palette.autotile_args.type2][0] + "";
                if ((int)Gl.palette.autotile_args.type2 % 2 == 1)
                    ch2 += 1;
            }

            bool[,] tmp_bool = Gl.palette.autotile_args.boolMask;
            if (tmp_bool[0, 0])
                lu_toggleButton.Content = ch1;
            else
                lu_toggleButton.Content = ch2;

            if (tmp_bool[1, 0])
                ru_toggleButton.Content = ch1;
            else
                ru_toggleButton.Content = ch2;

            if (tmp_bool[0, 1])
                ld_toggleButton.Content = ch1;
            else
                ld_toggleButton.Content = ch2;

            if (tmp_bool[1, 1])
                rd_toggleButton.Content = ch1;
            else
                rd_toggleButton.Content = ch2;

        }

        private void autoTile_SwapTypes_button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.curr_scena != null)
            {
                int tmp = autoTile_Type1_comboBox.SelectedIndex;
                autoTile_Type1_comboBox.SelectedIndex = autoTile_Type2_comboBox.SelectedIndex;
                autoTile_Type2_comboBox.SelectedIndex = tmp;
            }
        }

        private void debug_button_Click(object sender, RoutedEventArgs e)
        {
            if (Gl.curr_scena != null)
                Gl.palette.autotile_args.debug = (bool)debug_button.IsChecked;

        }


        #endregion autotiles



        private void Find_SelectedTile_button_Click(object sender, RoutedEventArgs e)
        {
            Land_scrollViewer.ScrollToVerticalOffset((Gl.palette.selected_tile - 5 * Gl.palette.tiles_collumns_count) * 4);
        }


        private void OnlyOneUnit_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            Gl.palette.one_unit_on_layer = (bool)OnlyOneUnit_toggleButton.IsChecked;
        }

        private void Recursive_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            Gl.palette.recursive_units = (bool)Recursive_toggleButton.IsChecked;
        }

        private void old_format_Button_Checked(object sender, RoutedEventArgs e)
        {
            Gl.NewFormat = false;
        }

        private void old_format_Button_Unchecked(object sender, RoutedEventArgs e)
        {
            Gl.NewFormat = true;
        }

        #endregion controls












    }
}




