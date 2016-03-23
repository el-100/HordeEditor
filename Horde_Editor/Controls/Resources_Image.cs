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

    class Resources_Image : Canvas
    {
        private RectangleGeometry[,] fillRects;

        private GeometryGroup _geometryGroup_gold;
        private GeometryGroup _geometryGroup_iron;

        private bool first = true;

        public void init(int x, int y)
        {
            fillRects = new RectangleGeometry[x, y];
            _geometryGroup_gold = new GeometryGroup();
            _geometryGroup_iron = new GeometryGroup();
        }

        public void close()
        {
            if (fillRects != null)
            {
                for (int i = 0; i < fillRects.GetLength(0); i++)
                    for (int j = 0; j < fillRects.GetLength(1); j++)
                        fillRects[i, j] = null;
            }
            fillRects = null;
            _geometryGroup_gold.Children.Clear();
            _geometryGroup_iron.Children.Clear();
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
                    //delete_resource_fillRect(fillRects[(int)r.X / 32, (int)r.Y / 32]);
                    fillRects[(int)r.X / 32, (int)r.Y / 32] = null;
                    break;
                case 1:
                    _geometryGroup_gold.Children.Add(rect);
                    break;
                case 2:
                    _geometryGroup_iron.Children.Add(rect);
                    break;
                default:
                    System.Windows.MessageBox.Show("не известный ресурс(id = " + type + ") в ячейке " + x + "," + y, "exception", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
            }
            fillRects[x, y] = rect;
        }


        public void delete_resource_fillRect(RectangleGeometry rect)
        {
            _geometryGroup_gold.Children.Remove(rect);
            _geometryGroup_iron.Children.Remove(rect);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (first)
            {
                //first = false;

                SolidColorBrush bg = new SolidColorBrush(Color.FromArgb(50, Colors.Gold.R, Colors.Gold.G, Colors.Gold.B));
                SolidColorBrush bi = new SolidColorBrush(Color.FromArgb(50, Colors.DarkGray.R, Colors.DarkGray.G, Colors.DarkGray.B));

                bg.Freeze();
                bi.Freeze();

                drawingContext.DrawGeometry(bg, null, _geometryGroup_gold);
                drawingContext.DrawGeometry(bi, null, _geometryGroup_iron);
            }
        }
    }

}
