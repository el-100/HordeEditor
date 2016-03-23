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

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Progress.xaml
    /// </summary>
    public partial class Progress : Window
    {
        public Progress()
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
    }
}
