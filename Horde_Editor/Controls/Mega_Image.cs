using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

using Horde_Editor.Common;

namespace Horde_Editor.Controls
{

    class Mega_Image : Image
    {
        // General
        private DrawingGroup _drawingGroup;
        private DrawingGroup _drawingGroup_last;
        private bool is_dirt = false;
        //private bool is_dirt_last = false;

        // Ground
        private ImageDrawing[,] ground_imgDrawing; // здесь содержатся ячейки земли

        // Resources
        private RectangleGeometry[,] fillRects;
        private GeometryGroup _geometryGroup_gold;
        private GeometryGroup _geometryGroup_iron;
        private bool _resources_is_dirt = false;

        //Grid
        private GeometryGroup _geometryGroup_lines;
        private bool _lines_is_dirt = false;

        public void init(int x, int y)
        {
            _drawingGroup = new DrawingGroup();
            _drawingGroup_last = new DrawingGroup();

            ground_imgDrawing = new ImageDrawing[x, y];

            fillRects = new RectangleGeometry[x, y];
            _geometryGroup_gold = new GeometryGroup();
            _geometryGroup_iron = new GeometryGroup();

            _geometryGroup_lines = new GeometryGroup();
        }

        public void close()
        {
            // Ground
            /*if (ground_imgDrawing != null)
            {
                for (int i = 0; i < ground_imgDrawing.GetLength(0); i++)
                    for (int j = 0; j < ground_imgDrawing.GetLength(1); j++)
                        ground_imgDrawing[i, j] = null;
            }*/
            ground_imgDrawing = null;

            // Resources
            /*if (fillRects != null)
            {
                for (int i = 0; i < fillRects.GetLength(0); i++)
                    for (int j = 0; j < fillRects.GetLength(1); j++)
                        fillRects[i, j] = null;
            }*/
            fillRects = null;
            _geometryGroup_gold.Children.Clear();
            _geometryGroup_iron.Children.Clear();
            _resources_is_dirt = true;

            // General
            _drawingGroup.Children.Clear();
            _drawingGroup_last.Children.Clear();

            // Grid
            _geometryGroup_lines.Children.Clear();

            _resources_is_dirt = true;
            _lines_is_dirt = true;
            is_dirt = true;

        }

        #region add images
        public ImageDrawing add_unit_img(BitmapImage bi, int x, int y)
        {
            int draw_width = Convert.ToInt32(bi.Width);
            int draw_height = Convert.ToInt32(bi.Height);

            int drawX = x - (draw_width / 2);
            int drawY = y - (draw_height / 2);

            Rect r = new Rect(drawX, drawY, draw_width, draw_height);

            ImageDrawing img_d = new ImageDrawing(bi, r);
            _drawingGroup.Children.Add(img_d);
            /*if (is_dirt_last == false)
                _drawingGroup_last.Children.Clear();
            _drawingGroup_last.Children.Add(img_d);
            is_dirt_last = true;*/

            is_dirt = true;

            return img_d;
        }

        public void add_ground_img(BitmapImage bi, Rect r)
        {
            int x = (int)r.X / 32;
            int y = (int)r.Y / 32;
            ImageDrawing img_d = new ImageDrawing(bi, r);

            img_d.Freeze();

            if (ground_imgDrawing[x, y] != null)
            {
                delete_ground_img(ground_imgDrawing[x, y]);
            }
            _drawingGroup.Children.Add(img_d);
            ground_imgDrawing[x, y] = img_d;
            is_dirt = true;
        }
        #endregion

        #region add geometry
        public GeometryDrawing add_unit_frame(int x, int y, int w, int h)
        {
            Rect r = new Rect(x + 0.5, y + 0.5, w, h);

            RectangleGeometry frame = new RectangleGeometry(r);
            GeometryDrawing gd = new GeometryDrawing(null, new Pen(Brushes.White, 1), frame);
            _drawingGroup.Children.Add(gd);

            is_dirt = true;

            return gd;
        }

        public void add_resource_fillrect(Rect r, int type) // type - тип ресурса(0 - убрать, 1 - золото, 2 - железо)
        {
            int x = (int)r.X / 32;
            int y = (int)r.Y / 32;

            RectangleGeometry rect = new RectangleGeometry(r);

            if (fillRects[x, y] != null)
            {
                delete_resource_fillRect(fillRects[x, y]);
            }

            switch (type)
            {
                case 0:
                    delete_resource_fillRect(fillRects[(int)r.X / 32, (int)r.Y / 32]);
                    fillRects[(int)r.X / 32, (int)r.Y / 32] = null;
                    break;
                case 1:
                    _geometryGroup_gold.Children.Add(rect);
                    break;
                case 2:
                    _geometryGroup_iron.Children.Add(rect);
                    break;
                default:
                    System.Windows.MessageBox.Show("не известный ресурс(id = " + type + ") в ячейке " + x + "," + y, "exceptioin", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
            }
            fillRects[x, y] = rect;
            is_dirt = true;
        }

        public void add_line(Point p1, Point p2)
        {
            LineGeometry line = new LineGeometry(p1, p2);

            line.Freeze();

            _geometryGroup_lines.Children.Add(line);
            _lines_is_dirt = true;
        }
        #endregion

        #region delete images
        public void delete_unit_img(ImageDrawing img_d)
        {
            is_dirt = true;

            _drawingGroup.Children.Remove(img_d);
        }

        public void delete_ground_img(ImageDrawing img_d)
        {
            _drawingGroup.Children.Remove(img_d);
        }

        #endregion

        #region delete geometry

        public void delete_unit_frame(GeometryDrawing frame)
        {
            is_dirt = true;

            _drawingGroup.Children.Remove(frame);
        }

        public void delete_resource_fillRect(RectangleGeometry rect)
        {
            _geometryGroup_gold.Children.Remove(rect);
            _geometryGroup_iron.Children.Remove(rect);
        }

        public void eraseAllLines()
        {
            _geometryGroup_lines.Children.Clear();
            _lines_is_dirt = true;
        }
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            /*if (is_dirt_last == true)
            {
                is_dirt_last = false;
                drawingContext.DrawDrawing(_drawingGroup_last);
                //_drawingGroup_last.Children.Clear();
            }*/

            if (is_dirt == true)
            {
                is_dirt = false;
                drawingContext.DrawDrawing(_drawingGroup);
            }

            if (_resources_is_dirt == true)
            {
                _resources_is_dirt = false;

                SolidColorBrush bg = new SolidColorBrush(Color.FromArgb(50, Colors.Gold.R, Colors.Gold.G, Colors.Gold.B));
                SolidColorBrush bi = new SolidColorBrush(Color.FromArgb(50, Colors.DarkGray.R, Colors.DarkGray.G, Colors.DarkGray.B));

                bg.Freeze();
                bi.Freeze();

                drawingContext.DrawGeometry(bg, null, _geometryGroup_gold);
                drawingContext.DrawGeometry(bi, null, _geometryGroup_iron);
            }

            if (_lines_is_dirt == true)
            {
                _lines_is_dirt = false;

                SolidColorBrush b = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
                b.Freeze();
                Pen p = new Pen(b, 1);
                p.Freeze();

                drawingContext.DrawGeometry(null, p, _geometryGroup_lines);
            }
        }
    }

}
