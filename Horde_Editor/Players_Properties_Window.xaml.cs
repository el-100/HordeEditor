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

using Horde_Editor.Common;
using Horde_ClassLibrary;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Players_Properties_Window.xaml
    /// </summary>
    public partial class Players_Properties_Window : Window
    {
        public Players_Properties_Window()
        {
            InitializeComponent();
        }

        public void Update()
        {
            //ComboBoxItem cbi = comboBox_Player.SelectedItem as ComboBoxItem;
            //if(cbi == null)
            //{MessageBox.Show("comboBox_Player.SelectedItem == null", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            //    return;}

            int number = comboBox_Player.SelectedIndex;

            if (Gl.curr_scena.players == null)
                return;

            textbox_leader_name.Text = Gl.curr_scena.players[number].Name;
            textbox_town_name.Text = Gl.curr_scena.players[number].TownName;
            combobox_army.SelectedIndex = Gl.curr_scena.players[number].army_type;

            numericUpDown_flag.Value = Gl.curr_scena.players[number].EMBLEM;
            numericUpDown_type_ai.Value = Gl.curr_scena.players[number].CONTROL;

            numericUpDown_gold.Value = Gl.curr_scena.players[number].gold;
            numericUpDown_metal.Value = Gl.curr_scena.players[number].metal;
            numericUpDown_lumber.Value = Gl.curr_scena.players[number].lumber;
            numericUpDown_peoples.Value = Gl.curr_scena.players[number].free_people;

            // Дипломатия
            diplomacyUpdate();

            // добавление шмоток
            listbox_items.Items.Clear();
            items_count = 0;
            List<byte> items = Utils.str2bytearray(Gl.curr_scena.players[number].t_itm_storage);
            for (int i = 0; i < 20; i++)
            {
                if (items[i] != 0)
                {
                    ComboBoxItem cbi = combobox_items.Items[items[i]] as ComboBoxItem;
                    listbox_items.Items.Add(cbi.Content);
                    items_count++;
                }
            }

            // Палитра
            UpdatePlayerPalette();

        }

        private void comboBox_Player_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        #region set fields

        private void combobox_army_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].army_type = combobox_army.SelectedIndex;
        }

        private void numericUpDown_flag_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].EMBLEM = numericUpDown_flag.Value.GetValueOrDefault();
        }

        private void numericUpDown_type_ai_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].CONTROL = numericUpDown_type_ai.Value.GetValueOrDefault();
        }

        private void numericUpDown_gold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].gold = numericUpDown_gold.Value.GetValueOrDefault();
        }

        private void numericUpDown_metal_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].metal = numericUpDown_metal.Value.GetValueOrDefault();
        }

        private void numericUpDown_lumber_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].lumber = numericUpDown_lumber.Value.GetValueOrDefault();
        }

        private void numericUpDown_peoples_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].free_people = numericUpDown_peoples.Value.GetValueOrDefault();
        }

        private void textbox_leader_name_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].Name = textbox_leader_name.Text;
        }

        private void textbox_town_name_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].TownName = textbox_town_name.Text;
        }

        #endregion set fields

        #region diplomacy

        private void diplomacyUpdate()
        {
            int p1 = comboBox_Player.SelectedIndex;
            int p2 = diplomacy_player_comboBox.SelectedIndex;
            string dip = Gl.curr_scena.players[p1].t_dip_status;

            diplomacy_relation_comboBox.SelectionChanged -= diplomacy_relation_comboBox_SelectionChanged;
            warstatus_checkBox.Checked -= warstatus_checkBox_Checked;
            warstatus_checkBox.Unchecked -= warstatus_checkBox_Unchecked;

            diplomacy_relation_comboBox.SelectedIndex = Utils.str2bytearray(dip)[p2];
            warstatus_checkBox.IsChecked = Convert.ToBoolean((Gl.curr_scena.players[p1].WAR_STATUS >> p2) & 1);

            diplomacy_relation_comboBox.SelectionChanged += diplomacy_relation_comboBox_SelectionChanged;
            warstatus_checkBox.Checked += warstatus_checkBox_Checked;
            warstatus_checkBox.Unchecked += warstatus_checkBox_Unchecked;
        }

        private void diplomacy_player_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            diplomacyUpdate();
        }

        private void diplomacy_relation_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int p1 = comboBox_Player.SelectedIndex;
            int p2 = diplomacy_player_comboBox.SelectedIndex;

            setRelation(p1, p2, (byte)diplomacy_relation_comboBox.SelectedIndex);
        }

        private void warstatus_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            int p1 = comboBox_Player.SelectedIndex;
            int p2 = diplomacy_player_comboBox.SelectedIndex;

            setWarStatus(p1, p2, true);
        }

        private void warstatus_checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            int p1 = comboBox_Player.SelectedIndex;
            int p2 = diplomacy_player_comboBox.SelectedIndex;

            setWarStatus(p1, p2, false);
        }


        private void relation_Button_Click(object sender, RoutedEventArgs e)
        {
            int p1 = comboBox_Player.SelectedIndex;
            int p2 = diplomacy_player_comboBox.SelectedIndex;

            setWarStatus(p2, p1, (bool)warstatus_checkBox.IsChecked);
            setRelation(p2, p1, (byte)diplomacy_relation_comboBox.SelectedIndex);

        }

        private void setWarStatus(int p1, int p2, bool val)
        {
            int tmp = Gl.curr_scena.players[p1].WAR_STATUS;
            Utils.BitSet(ref tmp, p2, val);
            Gl.curr_scena.players[p1].WAR_STATUS = tmp;
        }

        private void setRelation(int p1, int p2, byte val)
        {

            string s_dip = Gl.curr_scena.players[p1].t_dip_status;
            byte[] b_dip = Utils.str2bytearray(s_dip).ToArray();

            b_dip[p2] = val;

            Gl.curr_scena.players[p1].t_dip_status = Utils.byteArray2str(b_dip, 0, 16);
        }

        #endregion diplomacy

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comboBox_Player.SelectionChanged += comboBox_Player_SelectionChanged;
            Update();
            combobox_army.SelectionChanged += combobox_army_SelectionChanged;
            numericUpDown_flag.ValueChanged += numericUpDown_flag_ValueChanged;
            numericUpDown_type_ai.ValueChanged += numericUpDown_type_ai_ValueChanged;
            numericUpDown_gold.ValueChanged += numericUpDown_gold_ValueChanged;
            numericUpDown_metal.ValueChanged += numericUpDown_metal_ValueChanged;
            numericUpDown_lumber.ValueChanged += numericUpDown_lumber_ValueChanged;
            numericUpDown_peoples.ValueChanged += numericUpDown_peoples_ValueChanged;
            textbox_leader_name.TextChanged += textbox_leader_name_TextChanged;
            textbox_town_name.TextChanged += textbox_town_name_TextChanged;

            diplomacy_player_comboBox.SelectionChanged += diplomacy_player_comboBox_SelectionChanged;
            diplomacy_relation_comboBox.SelectionChanged += diplomacy_relation_comboBox_SelectionChanged;
            warstatus_checkBox.Checked += warstatus_checkBox_Checked;
            warstatus_checkBox.Unchecked += warstatus_checkBox_Unchecked;

        }

        public bool closing = false; // мб КОСТЫЛЬ  // флаг - навсегда ли закрывается окно? (true когда закрывается главная форма)
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false)
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }


        #region items

        int items_count = 0;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            byte itm_num = (byte)combobox_items.SelectedIndex;
            if (items_count == 20 || itm_num == 0)
                return;

            // добавление в listbox
            items_count++;
            ComboBoxItem cbi = combobox_items.SelectedItem as ComboBoxItem;
            listbox_items.Items.Add(cbi.Content);

            // добавление игроку
            int number = comboBox_Player.SelectedIndex;
            List<byte> bytes = Utils.str2bytearray(Gl.curr_scena.players[number].t_itm_storage);
            bytes[items_count] = itm_num;
            Gl.curr_scena.players[number].t_itm_storage = Utils.byteArray2str(bytes.ToArray(), 0, 20);

        }

        private void removeitem_Click(object sender, RoutedEventArgs e)
        {
            // удаление из листбокса
            //ListBoxItem lbi = listbox_items.SelectedItem as ListBoxItem;
            var lbi = listbox_items.SelectedItem;
            if (lbi == null)
                return;

            int index = listbox_items.SelectedIndex;
            listbox_items.Items.Remove(lbi);
            items_count--;
            if (items_count > index)
                listbox_items.SelectedIndex = index;
            else
                listbox_items.SelectedIndex = items_count;

            // удаление у игрока
            int number = comboBox_Player.SelectedIndex;
            List<byte> bytes = Utils.str2bytearray(Gl.curr_scena.players[number].t_itm_storage);
            bytes.RemoveAt(index);
            bytes.Add(0);
            Gl.curr_scena.players[number].t_itm_storage = Utils.byteArray2str(bytes.ToArray(), 0, 20);
        }

        private void listbox_items_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                removeitem_Click(null, null);
            }
        }

        #endregion items

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        #region palette

        private void change_palette_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Select palette";
            dlg.DefaultExt = "*.bmp";
            dlg.Filter = "BitMap|*.bmp";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == false)
                return;

            BitmapImage bi;

            if (System.IO.File.Exists(dlg.FileName))
                bi = new BitmapImage(new Uri(dlg.FileName, UriKind.Relative));
            else
            {
                System.Windows.MessageBox.Show("Файл не найден");
                throw new Exception("Файл не найден");
            }


            byte[] pixels16 = new byte[2048];
            int b0 = 0;
            int b1 = 0;
            byte[] pixels32 = null;

            WriteableBitmap wBitmap = new WriteableBitmap(bi);
            if (wBitmap.PixelWidth * wBitmap.PixelHeight < 16 * 16)
            {
                System.Windows.MessageBox.Show("Нужна картинка 16x16");
                return;
            }
            pixels32 = new byte[wBitmap.PixelWidth * wBitmap.PixelHeight * 4];
            wBitmap.CopyPixels(pixels32, wBitmap.PixelWidth * 4, 0);

            // в 16 бит
            for (int j = 0; j < 16 * 16; j++)
            {
                Color c = Color.FromArgb(255, pixels32[j * 4 + 2], pixels32[j * 4 + 1], pixels32[j * 4]);
                pixels16[j * 2 + 1] = (byte)((c.R / 8) * 8 + (c.G / 32));
                pixels16[j * 2] = (byte)(((c.G / 4) % 8) * 32 + (c.B / 8));

                b0 += pixels16[j * 2];
                b1 += pixels16[j * 2 + 1];
            }

            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].colors = pixels16;

            UpdatePlayerPalette();
        }

        private void UpdatePlayerPalette()
        {
            Scena scn = Gl.curr_scena;
            int number = comboBox_Player.SelectedIndex;
            Gl.curr_scena.players[number].army_type = combobox_army.SelectedIndex;
            Player p = scn.players[number];
            byte[] pixels16 = p.colors;

            player_palette.Source = bmp16to32bit(16, 16, 8, pixels16);
        }

        private BitmapSource bmp16to32bit(int w, int h, int scale, byte[] pixels16)
        {

            byte[] pixels32 = new byte[w * h * scale * scale * 4];//[16384 * 4];
            int stride = w * scale * 4;

            byte b2;
            byte b1;
            byte b0;

            // 16 в 32 бита
            for (int j = 0; j < w * h; j++)
            {
                b2 = (byte)((pixels16[j * 2 + 1] / 8) * 8);
                b1 = (byte)((pixels16[j * 2 + 1] % 8) * 32 + ((pixels16[j * 2] / 32) * 4));
                //b1 = (byte)((pixels16[j * 2 + 1] % 8) * 32 + (((pixels16[j * 2] / 32) / 2) * 8));
                b0 = (byte)((pixels16[j * 2] % 32) * 8);

                for (int y = 0; y < scale; y++)
                    for (int x = 0; x < scale; x++)
                    {//y * stride
                        pixels32[(j * 4 * scale + x * 4) + (j / w) * stride * (scale - 1) + y * stride + 2] = b2;
                        pixels32[(j * 4 * scale + x * 4) + (j / w) * stride * (scale - 1) + y * stride + 1] = b1;
                        pixels32[(j * 4 * scale + x * 4) + (j / w) * stride * (scale - 1) + y * stride] = b0;
                        pixels32[(j * 4 * scale + x * 4) + (j / w) * stride * (scale - 1) + y * stride + 3] = 255; // непрозрачный.
                    }
            }

            return BitmapSource.Create(w * scale, h * scale, 96d, 96d, PixelFormats.Bgra32, null, pixels32, stride);
        }

        private void player_palette_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // export
            int number = comboBox_Player.SelectedIndex;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "player" + number + "_palette";
            dlg.DefaultExt = ".bmp";
            dlg.Filter = "Bitmaps (.bmp)|*.bmp";
            dlg.Title = "Palette export";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == false)
                return;

            BitmapSource bmp = bmp16to32bit(16, 16, 1, Gl.curr_scena.players[number].colors);

            BitmapEncoder bmpEncoder = new BmpBitmapEncoder();
            bmpEncoder.Frames.Add(BitmapFrame.Create(bmp));

            using (var fs = System.IO.File.OpenWrite(dlg.FileName))
            {
                bmpEncoder.Save(fs);
            }
        }

        #endregion palette

    }
}
