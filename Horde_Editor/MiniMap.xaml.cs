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
    /// Логика взаимодействия для MiniMap.xaml
    /// </summary>
    public partial class MiniMap : Window
    {
        public int pixel_size = 1; // НЕ РАБОТАЕТ // Размер одной клеточки на миникарте(в пикселях)
        public bool closing = false; // флаг - навсегда ли закрывается миникарта? (true когда закрывается главная форма)

        #region moving
        public bool minimap_move_mode;                          // true, когда нажата ЛКМ на миникарте

        #endregion

        #region graphics
        private WriteableBitmap writeableBmp;
        #endregion


        public MiniMap()
        {
            InitializeComponent();
            //minimap_image.SnapsToDevicePixels = true;



        }

        public void initmap()
        {
            //canvas_minimap.init(Gl.curr_scena.size_map_x, Gl.curr_scena.size_map_y);
            //writeableBmp = BitmapFactory.New(Gl.curr_scena.size_map_x, Gl.curr_scena.size_map_y);
            writeableBmp = new WriteableBitmap(Gl.curr_scena.size_map_x, Gl.curr_scena.size_map_y, 96, 96, PixelFormats.Bgra32, null);

            minimap_image.Width = Gl.curr_scena.size_map_x;
            minimap_image.Height = Gl.curr_scena.size_map_y;
            canvas_minimap.Width = Gl.curr_scena.size_map_x;
            canvas_minimap.Height = Gl.curr_scena.size_map_y;

            minimap_image.Source = writeableBmp;

            //this.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            //this.UpdateDefaultStyle();
            //this.UpdateLayout();
            //this.SizeToContent = System.Windows.SizeToContent.Manual;
            //writeableBmp.GetBitmapContext();

        }

        public void closemap()
        {
            //writeableBmp.Dispose();
            writeableBmp = null;
            //canvas_minimap.close();
        }

        public void update_pixel_on_minimap(int x, int y)
        {
            Int32Rect rect = new Int32Rect(x, y, 1, 1);

            byte[] pixels = new byte[4];

            //writeableBmp.Lock();
            //IntPtr buff = writeableBmp.BackBuffer;
            //int Stride = writeableBmp.BackBufferStride;

            if (Gl.curr_scena.map_units[x, y] != null && Gl.main.unitsView_checkBox.IsChecked)
            {
                Color c = Gl.getPlayer_color(Gl.curr_scena.map_units[x, y][Gl.curr_scena.map_units[x, y].Count() - 1].player);

                pixels[0] = c.B;   // Blue
                pixels[1] = c.G;   // Green
                pixels[2] = c.R;   // Red
                pixels[3] = c.A;   // Alpha
                writeableBmp.WritePixels(rect, pixels, pixel_size * 4, 0);
            }
            else if (Gl.main.landView_checkBox.IsChecked)
            {
                Tile tile = Gl.curr_scena.map[x, y];

                pixels[0] = tile.MinimapColor.B;   // Blue
                pixels[1] = tile.MinimapColor.G;   // Green
                pixels[2] = tile.MinimapColor.R;   // Red
                pixels[3] = tile.MinimapColor.A;   // Alpha
                writeableBmp.WritePixels(rect, pixels, pixel_size * 4, 0);

            }
            else
            {

                pixels[0] = 0;   // Blue
                pixels[1] = 0;   // Green
                pixels[2] = 0;   // Red
                pixels[3] = 255; // Alpha
                writeableBmp.WritePixels(rect, pixels, pixel_size * 4, 0);
            }
            //writeableBmp.AddDirtyRect(rect);
            //writeableBmp.Unlock();

            //writeableBmp.Invalidate();
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

            Point p = Mouse.GetPosition(canvas_minimap);
            int mx = (int)p.X;
            int my = (int)p.Y;
            int x = mx / pixel_size - (int)view_rect.Width / 2;
            int y = my / pixel_size - (int)view_rect.Height / 2;

            int size_zoomCell = (int)(32 * Gl.main.uiScaleSlider.Value);
            Gl.main.move_scrols_to(x * size_zoomCell, y * size_zoomCell);

        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                #region
                if (minimap_move_mode == true)
                {
                    this.CaptureMouse();
                    Point p = Mouse.GetPosition(canvas_minimap);
                    int mx = (int)p.X;
                    int my = (int)p.Y;
                    int x = mx / pixel_size - (int)view_rect.Width / 2;
                    int y = my / pixel_size - (int)view_rect.Height / 2;

                    int size_zoomCell = (int)(32 * Gl.main.uiScaleSlider.Value);

                    Gl.main.move_scrols_to(x * size_zoomCell, y * size_zoomCell);
                }
                #endregion
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                this.ReleaseMouseCapture();
                minimap_move_mode = false;
                Gl.main.Focus();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }



        internal void Update()
        {
            for (int i = 0; i < Gl.curr_scena.size_map_x; i++)
                for (int j = 0; j < Gl.curr_scena.size_map_y; j++)
                {
                    update_pixel_on_minimap(i, j);
                }
        }

    }
}
