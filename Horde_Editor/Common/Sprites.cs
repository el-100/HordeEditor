using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

using Horde_ClassLibrary;

namespace Horde_Editor.Common
{
    class Ground_Sprites
    {
        // список со спрайтами земли
        public List<ImageSource> groud_sprites = new List<ImageSource>();

        // список тэгов к спрайтам
        // (№ сцены)_(№ спрайта)_(№ кадра)
        public List<string> ground_tags = new List<string>();
        public Dictionary<string, int> ground_dict = new Dictionary<string, int>();

        private int _count;
        public int Count
        {
            get { return _count; }
        }

        ImageSource blankBmp;
        public Ground_Sprites()
        {
            //blankBmp = new BitmapImage(new Uri(@"resources\transparent.ico", UriKind.Relative));

            byte[] buffer = new byte[32 * 32 * 4];
            var width = 32;
            var height = 32;
            var dpiX = 96d;
            var dpiY = 96d;
            var pixelFormat = PixelFormats.Bgra32;
            var bytesPerPixel = (pixelFormat.BitsPerPixel + 7) / 8;
            var stride = bytesPerPixel * width;

            blankBmp = BitmapSource.Create(width, height, dpiX, dpiY,
                                             pixelFormat, null, buffer, stride);

        }

        public void Clear()
        {
            groud_sprites.Clear();
            ground_tags.Clear();
            _count = 0;
        }


        public ImageSource getBitmapImage(int spr, int frame)
        {
            if (spr + frame >= 0 && spr + frame < Gl.curr_scena.spriteSet.Count)
                return Gl.curr_scena.spriteSet[spr + frame].Sprite;
            else
                return blankBmp;

            /*
            string t;
            t = scena + "_" + spr + "_" + frame;

            //int ind = ground_tags.IndexOf(t);
            int ind;
            if (ground_dict.ContainsKey(t))
                ind = ground_dict[t];
            else
                ind = -1;
            if (ind != -1)
            {
                return groud_sprites[ind];
            }
            else
            {
                string path = @"resources\scena__" + scena + @"\" + spr + "_" + frame + ".bmp";
                // загрузка спрайта.
                if (File.Exists(path)) // КОСТЫЛь костылИ - нужно знать у какого спрайта есть анимация!! а не методом тыка
                {
                    groud_sprites.Add(new BitmapImage(new Uri(path, UriKind.Relative)));
                }
                else
                {
                    path = @"resources\scena__" + scena + @"\" + spr + ".bmp";
                    if (File.Exists(path))
                    {
                        groud_sprites.Add(new BitmapImage(new Uri(path, UriKind.Relative)));
                    }
                    else
                    {
                        groud_sprites.Add(blankBmp);
                        //groud_sprites.Add(new BitmapImage(new Uri(@"resources\icon.ico", UriKind.Relative)));
                    }
                }
                ground_tags.Add(t);
                ground_dict.Add(t, _count);
                groud_sprites[_count].Freeze();
                _count++;

                return groud_sprites[_count - 1];
            }*/
        }



        // нужен ИТОГ: изменение картинки у тайла.
        //
        // что должны сделать для этого?
        // 1) конвертировать полученую картинку в массив 16-биного цвета
        // 2) этот массив переделать в картинку, как это делается при загрузки карты.
        // 3) заменить у Tile поля - массив байт и саму картинку.
        // готово!
        public void changeSpriteImage(Tile spr, BitmapImage bi)
        {
            byte[] pixels16 = new byte[2048];
            //byte[] intpixels32 = null;
            byte[] pixels32 = null;//= new byte[4096];

            WriteableBitmap wBitmap = new WriteableBitmap(bi);
            if (wBitmap.PixelWidth * wBitmap.PixelHeight != 32 * 32)
            {
                System.Windows.MessageBox.Show("Нужна картинка 32х32");
                pixels32 = new byte[Math.Max(wBitmap.PixelWidth * wBitmap.PixelHeight * 4, 4096)];
            }
            else
            {
                pixels32 = new byte[4096];
            }
            wBitmap.CopyPixels(pixels32, wBitmap.PixelWidth * 4, 0);

            // for minimap color
            int r = 0;
            int g = 0;
            int b = 0;
            //int b0 = 0;
            //int b1 = 0;

            // в 16 бит
            for (int j = 0; j < 32 * 32; j++)
            {
                Color c = Color.FromArgb(255, pixels32[j * 4 + 2], pixels32[j * 4 + 1], pixels32[j * 4]);
                pixels16[j * 2 + 1] = (byte)((c.R / 8) * 8 + (c.G / 32));
                pixels16[j * 2] = (byte)(((c.G / 4) % 8) * 32 + (c.B / 8));

                //b0 += pixels16[j * 2];
                //b1 += pixels16[j * 2 + 1];
            }

            // 2-й шаг (обратно в 32 бита)
            for (int j = 0; j < 1024; j++)
            {
                pixels32[j * 4 + 2] = (byte)((pixels16[j * 2 + 1] / 8) * 8);
                pixels32[j * 4 + 1] = (byte)((pixels16[j * 2 + 1] % 8) * 32 + ((pixels16[j * 2] / 32) * 4));
                //pixels32[j * 4 + 1] = (byte)((pixels16[j * 2 + 1] % 8) * 32 + (((pixels16[j * 2] / 32) / 2) * 8));
                pixels32[j * 4] = (byte)((pixels16[j * 2] % 32) * 8);
                pixels32[j * 4 + 3] = 255; // непрозрачный.

                r += pixels32[j * 4 + 2];
                g += pixels32[j * 4 + 1];
                b += pixels32[j * 4];
            }

            // 3
            spr.Sprite = BitmapSource.Create(32, 32, 96d, 96d, PixelFormats.Bgra32, null, pixels32, 128);
            spr.ScenaSprite = pixels16;

            // цвет миникарты ( среднее по всем цветам)
            //spr.ScenaMinimapColor = new byte[] { (byte)(b0 / 1024), (byte)(b1 / 1024) };
            //spr.MinimapColor = Color.FromArgb(255, (byte)((b0 % 32) * 8), (byte)((b1 % 8) * 32 + ((b0 / 32) * 4)), (byte)((b1 / 8) * 8));
            r /= 1024;
            g /= 1024;
            b /= 1024;
            spr.ScenaMinimapColor = new byte[] { (byte)(((g / 4) % 8) * 32 + (b / 8)), (byte)((r / 8) * 8 + (g / 32)) };
            spr.MinimapColor = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }
    }

    static class Unit_Sprites
    {
        // массив спрайтов юнитов. Заполняется по мере надобности(при обращении к новому спрайту.
        static List<BitmapImage> unit_sprites = new List<BitmapImage>();

        /*// конфиг юнита + номер игрока = битмап
        static Dictionary<UnitConfig, List<WriteableBitmap>> unit_sprites_dict = new Dictionary<UnitConfig, List<WriteableBitmap>>();

        static Dictionary<int, List<Color>> players_palettes = new Dictionary<int, List<Color>>();

        static public WriteableBitmap getBitmapImage(UnitConfig u_cfg, int frame, int player)
        {
            if (!unit_sprites_dict.ContainsKey(u_cfg))
            {
                unit_sprites_dict.Add(u_cfg, new List<WriteableBitmap>());

                // загрузка спрайта юнита.
                BitmapImage bi = new BitmapImage(new Uri(Gl.running_dir + @"\resources\army__0" + u_cfg.Folder + @".spr\" + (u_cfg.UnitID + frame) + ".bmp", UriKind.Relative));
                unit_sprites_dict[u_cfg].Add(new WriteableBitmap(bi));
                unit_sprites.Last().Freeze();

                for (int i = 0; i < Gl.curr_scena.players.Count(); i++)
                {
                    unit_sprites_dict[u_cfg].Add(new WriteableBitmap(bi));
                    //unit_sprites.Last().Palette = new BitmapPalette(players_palettes[player]);
                    unit_sprites.Last().Freeze();
                }
            }

            return null;
        }

        static public void UpdatePlayerPalette(int player)
        {


        }*/


        // тэги загруженых спрайтов
        // (№ папки)_(№ юнита)_(№ кадра)
        static List<string> tags = new List<string>();

        static public BitmapImage getBitmapImage(int folder, int id, int frame)
        {
            string t;
            t = folder + "_" + id + "_" + frame;

            int type;

            if (tags.Contains(t))
            {
                return unit_sprites[tags.IndexOf(t)];
            }
            else
            {

                type = folder;
                if (type == 4)
                {
                    type = 5;
                }

                // загрузка спрайта юнита.
                unit_sprites.Add(new BitmapImage(new Uri(Gl.running_dir + @"\resources\army__0" + type + @".spr\" + (id + frame) + ".bmp", UriKind.Relative)));
                tags.Add(t);
                unit_sprites.Last().Freeze();

                return unit_sprites[unit_sprites.Count - 1];
            }
        }

    }

}
