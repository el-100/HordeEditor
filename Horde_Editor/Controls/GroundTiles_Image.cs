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

    class GroundTiles_Image : Canvas
    {

        //private DrawingGroup _drawingGroup;
        //private ImageDrawing[,] imgDrawing;
        //private RenderTargetBitmap rtb;

        private bool first = false;

        private Rect[,] rects;
        private DrawingVisual dv = new DrawingVisual();
        private DrawingContext dc;
        RenderTargetBitmap[,] rtbs;

        private int x_count, y_count;
        private const int cells_in_rect_x = 8;
        private const int cells_in_rect_y = 8;

        public void init(int x, int y)
        {
            first = true;
            //imgDrawing = new ImageDrawing[x, y];
            //_drawingGroup = new DrawingGroup();

            x_count = x / cells_in_rect_x;
            if (x_count % cells_in_rect_x != 0)
                x_count++;
            y_count = y / cells_in_rect_y;
            if (y_count % cells_in_rect_y != 0)
                y_count++;


            rtbs = new RenderTargetBitmap[x_count, y_count];
            rects = new Rect[x_count, y_count];
            for (int i = 0; i < x_count; i++)
                for (int j = 0; j < y_count; j++)
                {
                    rtbs[i, j] = new RenderTargetBitmap(cells_in_rect_x * 32, cells_in_rect_y * 32, 96, 96, PixelFormats.Default);
                    rects[i, j] = new Rect(i * 32 * cells_in_rect_x, j * 32 * cells_in_rect_y, cells_in_rect_x * 32, cells_in_rect_y * 32);
                }
        }

        public void close()
        {
            //if (imgDrawing != null)
            //{
            //    for (int i = 0; i < imgDrawing.GetLength(0); i++)
            //        for (int j = 0; j < imgDrawing.GetLength(1); j++)
            //            imgDrawing[i, j] = null;
            //}
            //imgDrawing = null;
            //_drawingGroup.Children.Clear();

            rtbs = null;
            rects = null;

        }

        public void add_ground_img(ImageSource bi, Rect r)
        {
            int x = (int)r.X / 32 / cells_in_rect_x;
            int y = (int)r.Y / 32 / cells_in_rect_y;
            r.X -= x * 32 * cells_in_rect_x;
            r.Y -= y * 32 * cells_in_rect_y;

            ImageDrawing img_d = new ImageDrawing(bi, r);
            img_d.Freeze();

            dc = dv.RenderOpen();
            dc.DrawDrawing(img_d);
            dc.Close();
            rtbs[x, y].Render(dv);
            img_d = null;
            //GC.Collect(0);

        }

        public void delete_ground_img(ImageDrawing img_d)
        {
            //_drawingGroup.Children.Remove(img_d);
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (first)
            {
                first = false;
                //drawingContext.DrawDrawing(_drawingGroup);
                for (int i = 0; i < x_count; i++)
                    for (int j = 0; j < y_count; j++)
                        drawingContext.DrawImage(rtbs[i, j], rects[i, j]);
            }
        }
    }

}
