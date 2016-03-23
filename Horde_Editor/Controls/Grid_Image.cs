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

    class Grid_Image : Canvas
    {
        private GeometryGroup _geometryGroup;
        private bool first = true;

        public void init(int x, int y)
        {
            _geometryGroup = new GeometryGroup();
        }

        public void close()
        {
            _geometryGroup.Children.Clear();
        }

        public void add_line(Point p1, Point p2)
        {
            LineGeometry line = new LineGeometry(p1, p2);

            line.Freeze();

            _geometryGroup.Children.Add(line);
        }


        public void eraseAllLines()
        {
            _geometryGroup.Children.Clear();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (first)
            {
                //first = false;
                SolidColorBrush b = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
                b.Freeze();
                Pen p = new Pen(b, 1);
                p.Freeze();

                drawingContext.DrawGeometry(null, p, _geometryGroup);
            }
        }
    }

}
