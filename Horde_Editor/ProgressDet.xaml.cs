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
using System.Runtime.InteropServices;
using System.Windows.Interop;

using System.Windows.Threading;
using System.Threading;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Progress.xaml
    /// </summary>
    public partial class ProgressDet : Window
    {
        public ProgressDet()
        {
            InitializeComponent();
            Current = this;
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


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        #region updating
        internal static ProgressDet Current;
        //internal double Progress
        //{
        //    get { return progressBar.Value; }
        //}
        
        public void statusChange(object val)
        {
            status_Label.Content = (string)val;
        }

        public void progressChange(object val)
        {
            progressBar.Value = (double)val;
        }

        public void progressInc(object val)
        {
            progressBar.Value += (double)val;
        }

        #endregion updating

    }
}
