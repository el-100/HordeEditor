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
using System.Timers;
using System.Windows.Threading; // для использования "не таймерских" потоков

using Gma.UserActivityMonitor.GlobalEventProvider; // отлов координат мышки

using CorrectCursorPos;
using Horde_Editor.Common;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для MiniMap.xaml
    /// </summary>
    public partial class MiniMap : Window
    {
        public int pixel_size = 1; // НЕ РАБОТАЕТ // Размер одной клеточки на миникарте(в пикселях)
        public bool closing = false; // флаг - навсегда ли закрывается миникарта? (true когда закрывается главная форма)

        public WriteableBitmap minimap_bmp;

        #region moving
        public bool minimap_move_mode;                          // true, когда нажата ЛКМ на миникарте
        Timer on_left_click_Timer = new Timer();                // таймер запускается при нажатии левой кнопки мыши, для обработки изменений параметров мыши через промежутки времени.

        public Dispatcher _dispatcher;                          // ?диспетчер ?чего?
        delegate void ProbeDelegate_for_leftclick();            //

        GlobalEventProvider _globalEventProvider1 = new Gma.UserActivityMonitor.GlobalEventProvider();
        #endregion

        public MiniMap()
        {
            InitializeComponent();
            //minimap_image.SnapsToDevicePixels = true;


            this.globalEventProvider1.MouseMove += left_click_timer_elapsed;//to listen to mouse move

            on_left_click_Timer.Elapsed += new ElapsedEventHandler(left_click_timer_elapsed);
            on_left_click_Timer.Interval = 1;
            on_left_click_Timer.AutoReset = false;

        }


        public void update_pixel_on_minimap(int x, int y)
        {
            Int32Rect rect = new Int32Rect(x * pixel_size, y * pixel_size, pixel_size, pixel_size);


            if (Gl.curr_scena.map_units[x, y] == null)
            {
                if (Gl.curr_scena.spr2minimap_bmp.Width > Gl.curr_scena.map[x, y] + 1)
                {

                    // Подготавливаемся к вырезанию
                    int stride = Gl.curr_scena.spr2minimap_bmp.PixelWidth * 4;
                    int size = Gl.curr_scena.spr2minimap_bmp.PixelHeight * stride;
                    byte[] pixels = new byte[size];

                    // Вырезаем
                    Gl.curr_scena.spr2minimap_bmp.CopyPixels(pixels, stride, 0);

                    // Вытаскиваем нужный пиксель
                    int index = 0 * stride + 4 * Gl.curr_scena.map[x, y];
                    byte blue = pixels[index];
                    byte green = pixels[index + 1];
                    byte red = pixels[index + 2];
                    //byte alpha = pixels[index + 3];

                    byte[] colorData = { blue, green, red, 255 }; //{blue, green, red, alpha}

                    // Записать 4 байта из массива в растровое изображение.
                    minimap_bmp.WritePixels(rect, colorData, 4, 0);
                }
                else
                {
                    //MessageBox.Show("spr2minimap_bmp слишком мал", "Error DrawToMap(" + x + ", " + y + ", " + cur_spr_id + ")", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    byte[] colorData = { 0, 0, 0, 0 };
                    minimap_bmp.WritePixels(rect, colorData, 4, 0);
                }
            }
            else
            {
                //tmp_minimap_bmp.SetPixel(x, y, getPlayer_color(map_units[x, y].units[0].player));

                Color c = Gl.getPlayer_color(Gl.curr_scena.map_units[x, y][Gl.curr_scena.map_units[x, y].Count() - 1].player);

                byte[] colorData = { c.B, c.G, c.R, 255 };
                minimap_bmp.WritePixels(rect, colorData, 4, 0);

                //minimap_bmp.SetPixel(x, y, getPlayer_color(map_units[x, y].units[map_units[x, y].count - 1].player));
            }

        }


        public void update_rectangle_on_minimap()
        { // пока что КОСТЫЛЬ? - возможны в пикселях ошибки +-1
            double size_zoomCell = 32 * Gl.main.uiScaleSlider.Value;

            double h = (Gl.main.scrollViewer_Map.RenderSize.Height - SystemParameters.HorizontalScrollBarHeight);
            double w = (Gl.main.scrollViewer_Map.RenderSize.Width - SystemParameters.VerticalScrollBarWidth);

            double top = (Gl.main.scrollViewer_Map.VerticalOffset);// -h;
            double left = (Gl.main.scrollViewer_Map.HorizontalOffset);// -w;

            Canvas.SetLeft(view_rect, Math.Floor(left / size_zoomCell));
            Canvas.SetTop(view_rect, Math.Floor(top / size_zoomCell));

            view_rect.Height = Math.Ceiling(h / size_zoomCell) + 1;
            view_rect.Width = Math.Ceiling(w / size_zoomCell) + 1;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false) // КОСТЫЛЬ - нужен для того что бы крестом миникарта не закрывалась, а вы выключении гл. формы закрывалась
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void minimap_image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            minimap_move_mode = true;

            Point p = MouseUtilities.CorrectGetPosition(minimap_image);
            int mx = (int)p.X;
            int my = (int)p.Y;
            int x = mx / pixel_size - (int)view_rect.Width / 2;
            int y = my / pixel_size - (int)view_rect.Height / 2;

            int size_zoomCell = (int)(32 * Gl.main.uiScaleSlider.Value);
            Gl.main.move_scrols_to(x * size_zoomCell, y * size_zoomCell);

            // запускаем таймер обработки мыши
            on_left_click_Timer.AutoReset = true;
            on_left_click_Timer.Start();
            _dispatcher = Dispatcher.CurrentDispatcher; // хз что делается в этой строке
        }

        private void left_click_timer_elapsed(object source, ElapsedEventArgs e)
        {
            _dispatcher.Invoke(new ProbeDelegate_for_leftclick(left_click_Handler));
        }
        private void left_click_Handler()
        {
            if (MouseUtilities.GetAsyncKeyState(MouseUtilities.VK_LEFTBUTTON) != 0)
            {
                #region
                if (minimap_move_mode == true)
                {
                    Point p = MouseUtilities.CorrectGetPosition(minimap_image);
                    int mx = (int)p.X;
                    int my = (int)p.Y;
                    int x = mx / pixel_size - (int)view_rect.Width / 2;
                    int y = my / pixel_size - (int)view_rect.Height / 2;

                    int size_zoomCell = (int)(32 * Gl.main.uiScaleSlider.Value);

                    Gl.main.move_scrols_to(x * size_zoomCell, y * size_zoomCell);
                }
                #endregion
            }
            else
            {
                #region
                on_left_click_Timer.AutoReset = false;
                on_left_click_Timer.Stop();

                minimap_move_mode = false;
                #endregion
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
