using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace Horde_Editor.Common
{

    // В этом классе взаимосвязь между различными классами,
    // и другие параметры не зависящие от текущей сцены.

    static class Gl
    {
        public static bool CanOpenSecrets = true;
        public static bool NewFormat = true;
        public static string Editor_Version = "v1.0f";

        public static string running_dir;
        public static string running_file;

        public static MainWindow main; // ссылка на главную форму
        public static MiniMap minimap;
        public static Scena curr_scena;
        public static Palette palette;
        public static Unit_Properties_Window unit_properties_window;
        public static Units_Table_Window units_table_window;
        public static Players_Properties_Window players_properties_window;
        //public static Progress progress_wnd;
        public static Blocks_Window blocks_window;
        public static Tiles_Window tiles_window;
        public static Scena_Properties_Window scena_prop_window;

        public static int size_cell = 32;          // размер клетки в изображении карты
        public static string horde2_exe_Path;
        public static string horde2_folder_Path;

        // DEBUG
        #region DEBUG
        public static Stopwatch stopWatch1 = new Stopwatch();
        public static Stopwatch stopWatch2 = new Stopwatch();

        public static string DEBUG_stopWatch1_value()
        {
            TimeSpan ts = stopWatch1.Elapsed;
            return String.Format("{0:00}:{1:000}", ts.Seconds, ts.Milliseconds);
        }
        public static string DEBUG_stopWatch2_value()
        {
            TimeSpan ts = stopWatch2.Elapsed;
            return String.Format("{0:00}:{1:000}", ts.Seconds, ts.Milliseconds);
        }
        #endregion

        public static Color getPlayer_color(int i)
        {
            switch (i)
            {
                case 0:
                    return Colors.Red;
                case 1:
                    return Colors.Blue;
                case 2:
                    return Colors.Aqua;
                case 3:
                    return Colors.Green;
                case 4:
                    return Colors.White;
                case 5:
                    return Colors.Orange;
                case 6:
                    return Colors.Yellow;
                case 7:
                    return Colors.Brown;
            }

            MessageBox.Show("Игрок " + i + " не найден.", "Error getPlayer_color(" + i + ")", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return Colors.Black;
        }

        public static bool point_in_map(int x, int y)
        {

            if (x >= 0 && x < curr_scena.size_map_x && y >= 0 && y < curr_scena.size_map_y)
            {
                return true;
            }
            return false;
        }

        public static int ceil(int d1, int d2)
        {
            return (int)Math.Ceiling((decimal)d1 / (decimal)d2);
        }
        public static int floor(int d1, int d2)
        {
            return (int)Math.Floor((decimal)d1 / (decimal)d2);
        }

        #region progress
        public static Thread CreateProgressWindow(string title)
        {

            // Create a thread
            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                // Create and show the Window
                Progress tempWindow = new Progress();
                tempWindow.closing = true;
                tempWindow.Title = title;
                tempWindow.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();

            return newWindowThread;
        }

        public static Thread CreateProgressDetWindow(string title)
        {
            ProgressDet.Current = null;

            // Create a thread
            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                // Create and show the Window
                ProgressDet tempWindow = new ProgressDet();
                tempWindow.closing = true;
                tempWindow.Title = title;
                tempWindow.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();

            while (ProgressDet.Current == null) { }

            return newWindowThread;
        }

        public static void ProgressDetStatusChange(string str)
        {
            ProgressDet.Current.Dispatcher.Invoke(DispatcherPriority.Send, new ParameterizedThreadStart(ProgressDet.Current.statusChange), str);
        }

        public static void ProgressDetProgressChange(double val)
        {
            ProgressDet.Current.Dispatcher.Invoke(DispatcherPriority.Send, new ParameterizedThreadStart(ProgressDet.Current.progressChange), val);
        }

        public static void ProgressDetProgressInc(double val)
        {

            ProgressDet.Current.Dispatcher.Invoke(DispatcherPriority.Send, new ParameterizedThreadStart(ProgressDet.Current.progressInc), val);
        }


        public static void CloseProgressDetWindow(Thread th)
        {

            if (ProgressDet.Current.Dispatcher.CheckAccess())
                ProgressDet.Current.Close();
            else
                ProgressDet.Current.Dispatcher.Invoke(DispatcherPriority.Send, new ThreadStart(ProgressDet.Current.Close));

            ProgressDet.Current = null;

            th.Abort();

        }

        #endregion progress

    }



}
