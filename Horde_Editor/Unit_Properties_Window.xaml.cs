using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using System.IO; // for export

using Xceed.Wpf.Toolkit;
using Horde_Editor.Common;
using Horde_ClassLibrary;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Unit_Properties_Window.xaml
    /// </summary>
    public partial class Unit_Properties_Window : Window
    {
        Unit model = new Unit(); // если будет открыто несколько сцен одновременно в одном редакторе, то model нужно для каждой сцены разный.
        public bool closing = false; // мб КОСТЫЛЬ  // флаг - навсегда ли закрывается окно? (true когда закрывается главная форма)



        public Unit_Properties_Window()
        {
            InitializeComponent();
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false)
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Button_Ok_Click(this, null);
            }

            if (e.Key == Key.Escape)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        #region properties
        public void update_properties()
        {
            //black_list.Clear();
            List<string> black_list = new List<string>();

            model.CopyFrom(Gl.curr_scena.selected_units[0]);
            model.mapX = Gl.curr_scena.selected_units[0].mapX;
            model.mapY = Gl.curr_scena.selected_units[0].mapY;
            model.map_width = Gl.curr_scena.selected_units[0].map_width;
            model.map_height = Gl.curr_scena.selected_units[0].map_height;

            foreach (Unit u in Gl.curr_scena.selected_units)
            {
                #region black_list_creating
                if (u.mapX != model.mapX && black_list.Contains("mapX") == false)
                    black_list.Add("mapX");
                if (u.mapY != model.mapY && black_list.Contains("mapY") == false)
                    black_list.Add("mapY");
                if (u.map_width != model.map_width && black_list.Contains("map_width") == false)
                    black_list.Add("map_width");
                if (u.map_height != model.map_height && black_list.Contains("map_height") == false)
                    black_list.Add("map_height");
                if (u.folder != model.folder && black_list.Contains("type") == false)
                    black_list.Add("type");
                if (u.x != model.x && black_list.Contains("x") == false)
                    black_list.Add("x");
                if (u.z != model.z && black_list.Contains("z") == false)
                    black_list.Add("z");
                if (u.y != model.y && black_list.Contains("y") == false)
                    black_list.Add("y");
                if (u.spr_number != model.spr_number && black_list.Contains("spr_number") == false)
                    black_list.Add("spr_number");
                if (u.player != model.player && black_list.Contains("player") == false)
                    black_list.Add("player");
                if (u.layer != model.layer && black_list.Contains("layer") == false)
                    black_list.Add("layer");
                if (u.unit_id != model.unit_id && black_list.Contains("unit_id") == false)
                    black_list.Add("unit_id");
                if (u.allowed_orders != model.allowed_orders && black_list.Contains("allowed_orders") == false)
                    black_list.Add("allowed_orders");
                if (u.health != model.health && black_list.Contains("health") == false)
                    black_list.Add("health");
                if (u.old_health != model.old_health && black_list.Contains("old_health") == false)
                    black_list.Add("old_health");

                if (u.experience != model.experience && black_list.Contains("experience") == false)
                    black_list.Add("experience");
                if (u.curr_com != model.curr_com && black_list.Contains("curr_com") == false)
                    black_list.Add("curr_com");
                if (u.com_stage != model.com_stage && black_list.Contains("com_stage") == false)
                    black_list.Add("com_stage");
                if (u.exchange != model.exchange && black_list.Contains("exchange") == false)
                    black_list.Add("exchange");
                if (u.SQUAD != model.SQUAD && black_list.Contains("SQUAD") == false)
                    black_list.Add("SQUAD");
                if (u.target_obj != model.target_obj && black_list.Contains("target_obj") == false)
                    black_list.Add("target_obj");
                if (u.visible != model.visible && black_list.Contains("visible") == false)
                    black_list.Add("visible");


                if (u.size != model.size && black_list.Contains("size") == false)
                {
                    black_list.Add("size");
                    black_list.Add("information");
                }

                if (black_list.Contains("size") == false)
                {
                    if (u.t_UPropInformation != model.t_UPropInformation && black_list.Contains("information") == false)
                    { black_list.Add("information"); }
                }

                if (u.cfg.health_max != model.cfg.health_max && black_list.Contains("health_max") == false)
                    black_list.Add("health_max");

                #endregion
            }

            //listBox1.Items[1] = "black_list.Count()   " + black_list.Count();

            #region print_other
            if (black_list.Contains("mapX") == false)
                cbi_mapX.Content = "mapX   " + model.mapX;
            else
                cbi_mapX.Content = "mapX   ---";

            if (black_list.Contains("mapY") == false)
                cbi_mapY.Content = "mapY   " + model.mapY;
            else
                cbi_mapY.Content = "mapY   ---";

            if (black_list.Contains("map_width") == false)
                cbi_map_width.Content = "map_width   " + model.map_width;
            else
                cbi_map_width.Content = "map_width   ---";

            if (black_list.Contains("map_height") == false)
                cbi_map_height.Content = "map_height   " + model.map_height;
            else
                cbi_map_height.Content = "map_height   ---";

            if (black_list.Contains("x") == false)
                cbi_x.Content = "x   " + model.x;
            else
                cbi_x.Content = "x   ---";

            if (black_list.Contains("z") == false)
                cbi_z.Content = "z   " + model.z;
            else
                cbi_z.Content = "z   ---";

            if (black_list.Contains("y") == false)
                cbi_y.Content = "y   " + model.y;
            else
                cbi_y.Content = "y   ---";

            if (black_list.Contains("size") == false)
                cbi_size.Content = "size   " + model.size;
            else
                cbi_size.Content = "size   ---";

            if (black_list.Contains("type") == false)
                cbi_type.Content = "type   " + model.folder;
            else
                cbi_type.Content = "type   ---";

            if (black_list.Contains("unit_id") == false)
                cbi_unit_id.Content = "unit_id   " + model.unit_id;
            else
                cbi_unit_id.Content = "unit_id   ---";

            if (black_list.Contains("unit_id") == false && black_list.Contains("type") == false)
                cbi_Name_unit.Content = model.Name;
            else
                cbi_Name_unit.Content = "Name_unit   ---";

            if (black_list.Contains("curr_com") == false)
                cbi_curr_com.Content = "curr_com   " + model.curr_com;
            else
                cbi_curr_com.Content = "curr_com   ---";

            if (black_list.Contains("com_stage") == false)
                cbi_com_stage.Content = "com_stage   " + model.com_stage;
            else
                cbi_com_stage.Content = "com_stage   ---";

            if (black_list.Contains("exchange") == false)
                cbi_exchange.Content = "exchange   " + model.exchange;
            else
                cbi_exchange.Content = "exchange   ---";

            if (black_list.Contains("health_max") == false)
                label_HealthMax.Content = "Max " + model.cfg.health_max;
            else
                label_HealthMax.Content = "Max ---";


            #endregion
            // ---

            #region controls_fill
            if (black_list.Contains("spr_number") == false)
                numericUpDown_spr_id.Value = model.spr_number;
            else
                numericUpDown_spr_id.Value = -1;

            if (black_list.Contains("player") == false)
                comboBox_owner.SelectedIndex = model.player;
            else
            {
                comboBox_owner.SelectedIndex = -1;
                comboBox_owner.Text = "---";
            }

            if (black_list.Contains("health") == false)
                numericUpDown_health.Value = model.health;
            else
                numericUpDown_health.Value = -1;

            if (black_list.Contains("old_health") == false)
                numericUpDown_old_health.Value = model.old_health;
            else
                numericUpDown_old_health.Value = -1;

            if (black_list.Contains("layer") == false)
                numericUpDown_layer.Value = model.layer;
            else
                numericUpDown_layer.Value = -1;

            if (black_list.Contains("allowed_orders") == false)
                numericUpDown_orders.Value = model.allowed_orders;
            else
                numericUpDown_orders.Value = -1;

            if (black_list.Contains("experience") == false)
                numericUpDown_experience.Value = model.experience;
            else
                numericUpDown_experience.Value = -1;

            if (black_list.Contains("SQUAD") == false)
                numericUpDown_SQUAD.Value = model.SQUAD;
            else
                numericUpDown_SQUAD.Value = -1;

            if (black_list.Contains("target_obj") == false)
                numericUpDown_target_obj.Value = model.target_obj;
            else
                numericUpDown_target_obj.Value = -1;

            if (black_list.Contains("visible") == false)
                numericUpDown_visible.Value = model.visible;
            else
                numericUpDown_visible.Value = -1;



            if (black_list.Contains("information") == false)
            {
                textBox_information.Text = model.t_UPropInformation;
            }
            else
                textBox_information.Text = "---";
            #endregion
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();

            List<byte> tmp;
            byte[] nulls3 = new byte[3];
            byte[] hz = new byte[7];
            byte[] ending_6 = new byte[6];
            byte[] information = new byte[model.size - 33];

            bool all_OK = true;

            if (textBox_information.Text != "---")
            {
                tmp = Utils.str2bytearray(textBox_information.Text);
                if (tmp.Count() == model.size - 33)
                    information = tmp.ToArray();
                else
                {
                    System.Windows.MessageBox.Show("information(len = size - 33) ", "Bad argument", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    textBox_information.Text = model.t_UPropInformation;
                    all_OK = false;
                }
            }

            if (all_OK)
            {
                foreach (Unit tu in Gl.curr_scena.selected_units)
                {
                    if (numericUpDown_spr_id.Value != -1)
                        tu.spr_number = Convert.ToUInt16(numericUpDown_spr_id.Value);

                    if (comboBox_owner.SelectedIndex != -1)
                        Gl.curr_scena.unit_changePlayer(tu, comboBox_owner.SelectedIndex);

                    if (numericUpDown_health.Value != -1)
                        tu.health = Convert.ToByte(numericUpDown_health.Value);

                    if (numericUpDown_old_health.Value != -1)
                        tu.old_health = Convert.ToByte(numericUpDown_old_health.Value);

                    if (numericUpDown_layer.Value != -1)
                        tu.layer = Convert.ToByte(numericUpDown_layer.Value);

                    if (numericUpDown_orders.Value != -1)
                        tu.allowed_orders = Convert.ToByte(numericUpDown_orders.Value);

                    if (numericUpDown_experience.Value != -1)
                        tu.experience = Convert.ToByte(numericUpDown_experience.Value);

                    if (numericUpDown_SQUAD.Value != -1)
                        tu.SQUAD = Convert.ToByte(numericUpDown_SQUAD.Value);

                    if (numericUpDown_target_obj.Value != -1)
                        tu.target_obj = Convert.ToByte(numericUpDown_target_obj.Value);

                    if (numericUpDown_visible.Value != -1)
                        tu.visible = Convert.ToByte(numericUpDown_visible.Value);

                }
                label_info.Content = "info";
                this.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                label_info.Content = "bad args";
            }
        }
        #endregion properties

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {

            Unit[,] tmp_units = new Unit[5, 100];
            int[] units_c = new int[5];

            foreach (Unit u in Gl.curr_scena.selected_units)
            {
                tmp_units[u.folder, u.unit_id] = u;
                units_c[u.folder]++;
            }

            Unit tmp_u;
            for (int i = 0; i < 5; i++)
            {
                if (units_c[i] > 0)
                {
                    StreamWriter strw = new StreamWriter("enter_output" + i + ".txt");
                    for (int j = 0; j < units_c[i]; j++)
                    {
                        tmp_u = tmp_units[i, j];
                        strw.Write(String.Format("{0,20} | {1}\n", tmp_u.cfg.Name, Utils.byteArray2str(tmp_u.RepresentToArray(), 0, tmp_u.size)));
                    }
                    strw.Close();
                }
            }
            MessageBoxResult msgb = System.Windows.MessageBox.Show("Export units done!", "Export units", MessageBoxButton.OK, MessageBoxImage.Information);

            //Unit u = Gl.curr_scena.selected_units[0];
            //Clipboard.SetText(Utils.byteArray2str(u.RepresentToArray(), 0, u.size));

            //MessageBoxResult msgb = System.Windows.MessageBox.Show("Export unit[0] to clipboard done!", "Export unit[0]", MessageBoxButton.OK, MessageBoxImage.Information);
        }


    }
}
