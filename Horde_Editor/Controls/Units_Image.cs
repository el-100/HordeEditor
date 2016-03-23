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
    class Units_Image : Canvas
    {

        private DrawingGroup _drawingGroup;
        private DrawingGroup _drawingGeometryGroup;

        public bool DrawFrames = true;
        public bool DrawUnits = true;

        public void init()
        {
            _drawingGroup = new DrawingGroup();
            _drawingGeometryGroup = new DrawingGroup();
        }

        public void close()
        {
            if (_drawingGroup != null)
                _drawingGroup.Children.Clear();
            if (_drawingGeometryGroup != null)
                _drawingGeometryGroup.Children.Clear();
            _drawingGroup = null;
            _drawingGeometryGroup = null;
            if (layers != null)
                layers.Clear();
        }

        public ImageDrawing add_unit_img(BitmapImage bi, int x, int y, int z_index)
        {
            int draw_width = Convert.ToInt32(bi.Width);
            int draw_height = Convert.ToInt32(bi.Height);

            //int y_tmp = y + z_index;

            int drawX = x - (draw_width / 2);
            int drawY = y - (draw_height / 2);

            Rect r = new Rect(drawX, drawY, draw_width, draw_height);

            ImageDrawing img_d = new ImageDrawing(bi, r);
            if (layers.ContainsKey(y))
                layers[y]++;
            else
                layers.Add(y, 1);

            int tmp = 0;
            foreach (int q in layers.Keys)
            {
                if (y > q)
                    tmp += layers[q];
            }

            indexes.Add(img_d, y);

            if (tmp > _drawingGroup.Children.Count)
                tmp = _drawingGroup.Children.Count;
            _drawingGroup.Children.Insert(tmp, img_d);

            return img_d;
        }
        Dictionary<int, int> layers = new Dictionary<int, int>();
        Dictionary<ImageDrawing, int> indexes = new Dictionary<ImageDrawing, int>(); // слой в котором отрисовалась картинка
        /*int getLayer(int y)
        {
            if (_drawingGroup.Children.Count == 0)
                return 0;

            int layer = 0;

            for (int i = 0; i < _drawingGroup.Children.Count; i++)
            {


            }

            return layer;
        }*/

        public GeometryDrawing add_unit_frame(int x, int y, int w, int h, Color col)
        {
            Rect r = new Rect(x + 0.5, y + 0.5, w, h);

            RectangleGeometry frame = new RectangleGeometry(r);
            GeometryDrawing gd = new GeometryDrawing(null, new Pen(new SolidColorBrush(col), 1), frame);
            _drawingGeometryGroup.Children.Add(gd);

            return gd;
        }


        public void delete_unit_img(ImageDrawing img_d)
        {
            layers[indexes[img_d]]--;
            indexes.Remove(img_d);
            _drawingGroup.Children.Remove(img_d);
        }

        public void delete_unit_frame(GeometryDrawing uframe)
        {
            _drawingGeometryGroup.Children.Remove(uframe);
        }
        public void unit_frame_update_pos(GeometryDrawing uframe, int x, int y, int w, int h)
        { // anti smooth

            RectangleGeometry rect = uframe.Geometry as RectangleGeometry;
            if (rect == null)
                return;

            if (uframe.Pen.Thickness % 2 == 1)
            {
                rect.Rect = new Rect(x + 0.5, y + 0.5, w, h);
            }
            else
            {
                rect.Rect = new Rect(x, y, w, h);
            }
        }

        public void unit_frame_to_top(GeometryDrawing frame)
        {
            _drawingGeometryGroup.Children.Remove(frame);
            _drawingGeometryGroup.Children.Add(frame);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (DrawUnits)
                drawingContext.DrawDrawing(_drawingGroup);
            if (DrawFrames)
                drawingContext.DrawDrawing(_drawingGeometryGroup);
        }
    }

}




// Version with z_layer

/*









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
    class Units_Image : Canvas
    {

        private DrawingGroup[] _drawingGroups;
        private DrawingGroup _drawingGeometryGroup;

        public bool DrawFrames = true;
        public bool DrawUnits = true;

        const int z_layers = 4;

        public void init()
        {
            _drawingGroups = new DrawingGroup[z_layers];
            _drawingGeometryGroup = new DrawingGroup();
            layers = new Dictionary<int, int>[z_layers];
            indexes = new Dictionary<ImageDrawing, int>[z_layers];
            z_indexes = new Dictionary<ImageDrawing, int>();

            for (int i = 0; i < z_layers; i++)
            {
                _drawingGroups[i] = new DrawingGroup();
                layers[i] = new Dictionary<int, int>();
                indexes[i] = new Dictionary<ImageDrawing, int>();
            }
        }

        public void close()
        {
            for (int i = 0; i < z_layers; i++)
                _drawingGroups[i].Children.Clear();
            _drawingGeometryGroup.Children.Clear();
            _drawingGroups = null;
            _drawingGeometryGroup = null;
            layers = null;
            indexes = null;
            z_indexes = null;
        }

        public ImageDrawing add_unit_img(BitmapImage bi, int x, int y, int z_index)
        {
            int draw_width = Convert.ToInt32(bi.Width);
            int draw_height = Convert.ToInt32(bi.Height);

            int tmp_z_index = z_index;
            if (tmp_z_index > 3)
            {
                tmp_z_index = 3;
                System.Windows.MessageBox.Show("tmp_z_index > 3", "Units_Image", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            int drawX = x - (draw_width / 2);
            int drawY = y - (draw_height / 2);

            Rect r = new Rect(drawX, drawY, draw_width, draw_height);

            ImageDrawing img_d = new ImageDrawing(bi, r);
            if (layers[tmp_z_index].ContainsKey(y))
                layers[tmp_z_index][y]++;
            else
                layers[tmp_z_index].Add(y, 1);

            int tmp = 0;
            foreach (int q in layers[tmp_z_index].Keys)
            {
                if (y > q)
                    tmp += layers[tmp_z_index][q];
            }

            indexes[tmp_z_index].Add(img_d, y);
            z_indexes.Add(img_d, z_index);

            if (tmp > _drawingGroups[tmp_z_index].Children.Count)
                tmp = _drawingGroups[tmp_z_index].Children.Count;
            _drawingGroups[tmp_z_index].Children.Insert(tmp, img_d);

            return img_d;
        }
        Dictionary<int, int>[] layers;
        Dictionary<ImageDrawing, int>[] indexes; // слой в котором отрисовалась картинка
        Dictionary<ImageDrawing, int> z_indexes; // z_index в котором отрисовалась картинка
        //int getLayer(int y)
        //{
        //    if (_drawingGroup.Children.Count == 0)
        //        return 0;
        //
        //    int layer = 0;
        //
        //    for (int i = 0; i < _drawingGroup.Children.Count; i++)
        //    {
        //
        //
        //    }
        //
        //    return layer;
        //}

        public GeometryDrawing add_unit_frame(int x, int y, int w, int h, Color col)
        {
            Rect r = new Rect(x + 0.5, y + 0.5, w, h);

            RectangleGeometry frame = new RectangleGeometry(r);
            GeometryDrawing gd = new GeometryDrawing(null, new Pen(new SolidColorBrush(col), 1), frame);
            _drawingGeometryGroup.Children.Add(gd);

            return gd;
        }


        public void delete_unit_img(ImageDrawing img_d)
        {
            int z = z_indexes[img_d];
            layers[z][indexes[z][img_d]]--;
            indexes[z].Remove(img_d);
            _drawingGroups[z].Children.Remove(img_d);
        }

        public void delete_unit_frame(GeometryDrawing uframe)
        {
            _drawingGeometryGroup.Children.Remove(uframe);
        }
        public void unit_frame_update_pos(GeometryDrawing uframe, int x, int y, int w, int h)
        { // anti smooth

            RectangleGeometry rect = uframe.Geometry as RectangleGeometry;
            if (rect == null)
                return;

            if (uframe.Pen.Thickness % 2 == 1)
            {
                rect.Rect = new Rect(x + 0.5, y + 0.5, w, h);
            }
            else
            {
                rect.Rect = new Rect(x, y, w, h);
            }
        }

        public void unit_frame_to_top(GeometryDrawing frame)
        {
            _drawingGeometryGroup.Children.Remove(frame);
            _drawingGeometryGroup.Children.Add(frame);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (DrawUnits && _drawingGroups!=null)
                for (int i = 0; i < z_layers; i++)
                    drawingContext.DrawDrawing(_drawingGroups[i]);
            if (DrawFrames)
                drawingContext.DrawDrawing(_drawingGeometryGroup);
        }
    }

}


















*/