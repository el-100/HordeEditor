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
using System.Collections.ObjectModel;
using System.IO;

using Horde_Editor.Common;
using Horde_ClassLibrary;

using System.ComponentModel; // для EventHandlerList и PropertyDescriptor

/* TO DO 
 * 
 * Двойной щелчек мышью по юниту выделяет его и перемещает камеру к нему.
 * 
 * столбик нумерации
 * 
 * возможность изменять любой параметр кроме имени. (требуется отлов изменения игрока и тд..)
 * 
 * добавление юнитов через таблицу
 * 
 * получать имена игроков и автоматически переименовывать вкладки
 * 
 * 
 * 
 */

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Units_Table_Window.xaml
    /// </summary>
    public partial class Units_Table_Window : Window
    {
        public bool closing = false; // мб КОСТЫЛЬ  // флаг - навсегда ли закрывается окно? (true когда закрывается главная форма)

        ObservableCollection<Unit>[] player_units_collection = new ObservableCollection<Unit>[8];
        DataGrid[] dataGrids = new DataGrid[8];

        public Units_Table_Window()
        {
            InitializeComponent();

            dataGrids[0] = player0_dataGrid;
            dataGrids[1] = player1_dataGrid;
            dataGrids[2] = player2_dataGrid;
            dataGrids[3] = player3_dataGrid;
            dataGrids[4] = player4_dataGrid;
            dataGrids[5] = player5_dataGrid;
            dataGrids[6] = player6_dataGrid;
            dataGrids[7] = player7_dataGrid;


            for (int i = 0; i < 8; i++)
            {
                player_units_collection[i] = new ObservableCollection<Unit>();
                dataGrids[i].ItemsSource = player_units_collection[i];

                dataGrids[i].EnableColumnVirtualization = true;
                dataGrids[i].EnableRowVirtualization = true;

                // DataGridTextColumn numberColumn = new DataGridTextColumn();
                // numberColumn.Header = "Number";
                // numberColumn.Binding = new Binding("Number");
                // numberColumn.Binding.StringFormat = "{}{0:N0}";
                // dataGrids[i].Columns.Add(numberColumn);
            }
        }

        public void update_tables()
        {
            for (int i = 0; i < 8; i++)
                player_units_collection[i].Clear();

            int j = 0;

            for (int i = 0; i < 8; i++)
            {

                j = 0;
                foreach (Unit u in Gl.curr_scena.players[i].units)
                {
                    player_units_collection[i].Add(u);

                    //GetCell(dataGrids[i], j, 0).Content = j.ToString();

                    j++;
                }
            }
        }

        #region dataGrid utils
        /*
        public DataGridCell GetCell(DataGrid dg, int row, int column)
        {
            DataGridRow rowContainer = GetRow(dg, row);

            if (rowContainer != null)
            {
                System.Windows.Controls.Primitives.DataGridCellsPresenter presenter = GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(rowContainer);

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public DataGridRow GetRow(DataGrid dg, int index)
        {
            DataGridRow row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                dg.UpdateLayout();
                dg.ScrollIntoView(dg.Items[index]);
                row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
        */
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false)
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void dataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {

            var pd = (PropertyDescriptor)e.PropertyDescriptor;
            var da = (HordeAttribute)pd.Attributes[typeof(HordeAttribute)];

            if (da == null) return;

            var autoGenerateField = da.ShowInDataGrid;
            if (!autoGenerateField)
            {
                e.Cancel = true;
            }

            if (!string.IsNullOrEmpty(da.DisplayName))
            {
                e.Column.Header = da.DisplayName;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Gl.main.saveMap();
            Label_info.Content = "____________________ saved ____________________";
        }
        
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // Get clean

            string name = Gl.curr_scena.scena_short_name + ".scn";
            if (File.Exists(Gl.horde2_folder_Path + @"\" + name))
            {
                File.Copy(Gl.horde2_folder_Path + @"\" + name, Gl.curr_scena.scena_path, true);

                string scena_path;
                scena_path = Gl.curr_scena.scena_path;
                Gl.curr_scena.Close_Scene();
                Gl.curr_scena = new Scena();

                if (!Gl.curr_scena.Load_Scene(scena_path))
                { this.Close(); return; }

                update_tables();
                Label_info.Content = "____________________ get clean done! ____________________";
            }
            else
                Label_info.Content = "____________________ get clean failed ____________________";
        }


        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            string scena_path;
            scena_path = Gl.main.load_scena_dialog();
            if (scena_path != null)
            {
                Gl.curr_scena.Close_Scene();
                Gl.curr_scena = new Scena();

                if (!Gl.curr_scena.Load_Scene(scena_path))
                { this.Close(); return; }

                Gl.main.Title = Gl.curr_scena.scena_short_name + " - Horde Editor " + Gl.Editor_Version;
                update_tables();
                Gl.palette.UpdatePlayerUnitCounter(); // обновим счетчик выбранного на палитре игрока
                Label_info.Content = "____________________ load done! ____________________";
            }
            else
            {
                Label_info.Content = "____________________ load failed! ____________________";
            }

        }


        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            update_tables();
            Label_info.Content = "____________________ refresh done! ____________________";
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
