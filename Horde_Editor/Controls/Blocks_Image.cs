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

    class Blocks_Image : Image
    {


        private bool first = false;

        private Rect rect;
        DrawingGroup imageDrawings;
        DrawingImage drawingImageSource;

        private int width = 0;
        private int height = 0;

        public Blocks_Image()
            : base()
        {
            SnapsToDevicePixels = true;
            //this.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            Stretch = System.Windows.Media.Stretch.None;
        }

        public void Init(int x, int y)
        {
            width = x * 32;
            height = y * 32;

            first = true;
            rect = new Rect(0, 0, width, height);

            imageDrawings = new DrawingGroup();
            drawingImageSource = new DrawingImage(imageDrawings);

            Source = drawingImageSource;

            //imgDrawing = new ImageDrawing[x, y];
            //_drawingGroup = new DrawingGroup();

        }

        public void Close()
        {
            imageDrawings = null;
            drawingImageSource = null;
            Source = null;
        }

        public void Clear()
        {
            imageDrawings.Children.Clear();
        }

        public void ChangeBlock(ImageSource[,] tiles)
        {
            if (width == 0 || height == 0)
                return;

            int i_max = tiles.GetLength(0);
            int j_max = tiles.GetLength(1);
            Width = i_max * 32;
            Height = j_max * 32;

            imageDrawings.Children.Clear();

            ImageDrawing[,] img_d = new ImageDrawing[i_max, j_max];

            for (int j = 0; j < j_max; j++)
                for (int i = 0; i < i_max; i++)
                {
                    System.Windows.Rect r = new System.Windows.Rect(i * 32, j * 32, 32, 32);

                    img_d[i, j] = new ImageDrawing(tiles[i, j], r);
                    img_d[i, j].Freeze();

                    imageDrawings.Children.Add(img_d[i, j]);
                }

            for (int j = 0; j < j_max; j++)
                for (int i = 0; i < i_max; i++)
                    img_d[i, j] = null;
            GC.Collect(0);

            //InvalidateVisual();
        }



        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (first)
            {
                first = false;
                //drawingContext.DrawDrawing(_drawingGroup);
                //drawingContext.DrawImage(rtb_curr, rect);
            }
        }

    }

}
