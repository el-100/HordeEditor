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
using Horde_Editor.Controls;
using Horde_Editor.History;

namespace Horde_Editor
{
    /// <summary>
    /// Логика взаимодействия для Blocks_Window.xaml
    /// </summary>
    public partial class Blocks_Window : Window
    {


        List<Blocks_Image> blocksList = new List<Blocks_Image>();
        List<ColumnDefinition> gridCols = new List<ColumnDefinition>();
        List<int> widths = new List<int>();  // размеры контролов(в ячейках)
        List<int> heights = new List<int>(); // размеры контролов(в ячейках)

        int space = 3; // расстояние между изображениями блоков


        //ContextMenu context_menu = new ContextMenu(); // меню клика правой кнопкой мыши по блоку

        //Blocks_Image bi;
        public Tile blank_Tile = new Tile();
        List<ImageSource[,]> bitmaps = new List<ImageSource[,]>();

        public Blocks_Window()
        {
            blank_Tile.SpriteId = -1;
            InitializeComponent();
        }

        private void BlocksGrid_Initialized(object sender, EventArgs e)
        {
            rectangle_cursor.Visibility = System.Windows.Visibility.Hidden; // тайл
            rectangle_select.Visibility = System.Windows.Visibility.Hidden; // блок
            UpdateBlocks();
        }

        public void Clear()
        {
            widths.Clear();
            heights.Clear();
            bitmaps.Clear();
        }

        // добавление блока(контрол в окне)
        public void AddBlockImage(int x, int y)
        {

            ColumnDefinition gridCol = new ColumnDefinition();
            gridCol.Width = new GridLength(x * 32 + space, GridUnitType.Pixel);
            BlocksGrid.ColumnDefinitions.Add(gridCol);
            gridCols.Add(gridCol);

            Blocks_Image bi = new Blocks_Image();

            bi.Init(x * 32, y * 32);

            Grid.SetRow(bi, 0);
            Grid.SetColumn(bi, blocksList.Count);
            bi.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            bi.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            //bi.Margin = new Thickness(0.5);

            bi.MouseUp += block_image_MouseUp;

            blocksList.Add(bi);
            BlocksGrid.Children.Insert(0, bi);
        }

        // добавление информации о блоке(набор тайлов)
        public void AddBlock(Tile[,] tiles)
        {
            int x = tiles.GetLength(0);
            int y = tiles.GetLength(1);

            widths.Add(x);
            heights.Add(y);

            Scena scn = Gl.curr_scena;
            ImageSource[,] bmp = new ImageSource[x, y];
            bitmaps.Add(bmp);

            //bi.Width = x * 32;
            //bi.Height = y * 32;

            for (int j = 0; j < y; j++)
                for (int i = 0; i < x; i++)
                    bmp[i, j] = scn.ground_sprites.getBitmapImage(tiles[i, j].SpriteId, 0);

            BlockSlider.Maximum = bitmaps.Count;
        }

        public void RemoveBlock(int n)
        {
            if (n < 0 || n >= widths.Count)
                return;
            widths.RemoveAt(n);
            heights.RemoveAt(n);

            bitmaps.RemoveAt(n);
            Gl.curr_scena.blocks.RemoveAt(n);

            BlockSlider.Maximum = bitmaps.Count;
        }

        public void InsertBlock(int n, int x, int y)
        {
            widths.Insert(n, x);
            heights.Insert(n, y);

            Scena scn = Gl.curr_scena;
            ImageSource[,] bmp = new ImageSource[x, y];
            bitmaps.Insert(n, bmp);

            Tile[,] block = new Tile[x, y];
            for (int j = 0; j < y; j++)
                for (int i = 0; i < x; i++)
                    block[i, j] = blank_Tile;

            Gl.curr_scena.blocks.Insert(n, block);

            for (int j = 0; j < y; j++)
                for (int i = 0; i < x; i++)
                {
                    bmp[i, j] = scn.ground_sprites.getBitmapImage(-1, 0);
                }

            BlockSlider.Maximum = bitmaps.Count;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            updateLabelInfo();
            UpdateBlocks();
        }

        void changeGridHeight(int h)
        {
            int h_old = 0; // до изменения высоты Grid

            if (h < 128)
                h = 128;
            h_old = (int)BlocksGrid.Height;
            Row1.Height = new GridLength(h); // изменяем размер строки блоков
            BlocksGrid.Height = h;           // изменяем размер строки блоков
            // подвигаем окно выше
            if (this.Top + h_old - h > 0)
                this.Top += h_old - h;
            else
                this.Top = 0;

            //this.Focus();

            // надо ещё изменять размер окна
            this.Height = h + 117;
        }

        // обновляем визуальную часть.
        // то на чем будем рисовать блоки
        public void UpdateBlocks()
        {
            int w = 0;//(int)Width;

            int bad_from = 0; // количество столбцов которые влезли в окно
            bool bigger = false; // true когда изображения вылазиют за пределы окна
            for (int i = 0; i < gridCols.Count; i++)
            {
                w += (int)gridCols[i].Width.Value;
                if (w > (int)ActualWidth)
                {
                    bigger = true;
                    bad_from = i;
                    i = gridCols.Count;
                }
            }

            if (bigger == true)
            {
                for (int i = gridCols.Count - 1; i > bad_from; i--)
                {
                    blocksList[i].Close();
                    blocksList.RemoveAt(i);
                    //widths.RemoveAt(i);
                    //heights.RemoveAt(i);
                    BlocksGrid.ColumnDefinitions.Remove(gridCols[i]);
                    gridCols.RemoveAt(i);
                }
            }
            else if (gridCols.Count < bitmaps.Count)
            {
                if ((int)BlockSlider.Value + gridCols.Count < widths.Count)
                {
                    while ((int)ActualWidth - w > 0)
                    {
                        AddBlockImage(4, 4);
                        w += (int)gridCols.Last().Width.Value;
                        if (gridCols.Count >= bitmaps.Count || (int)BlockSlider.Value + gridCols.Count >= widths.Count)
                            break;
                    }
                }
            }
            UpdateBlockImages();
        }

        // обновляем содержимое блоков-контролов
        // то что внутри
        public void UpdateBlockImages()
        {
            if (BlockSlider == null)
                return;

            int index;
            int h = 0;

            // меняем всем картинки
            for (int i = 0; i < blocksList.Count; i++)
            {
                index = (int)BlockSlider.Value + i;
                if (index < bitmaps.Count)
                {
                    blocksList[i].ChangeBlock(bitmaps[index]);

                    widths[index] = bitmaps[index].GetLength(0);
                    heights[index] = bitmaps[index].GetLength(1);

                    if (h < heights[index])
                        h = heights[index];


                    blocksList[i].Width = widths[index] * 32;  // размер контрола-блока
                    blocksList[i].Height = heights[index] * 32; // размер контрола-блока
                    gridCols[i].Width = new GridLength(widths[index] * 32 + space);// устанавливаем размер столбца
                }
                else
                    blocksList[i].Clear();
            }
            changeGridHeight(h * 32);
            index = (int)BlockSlider.Value;

            if (lastBlock_image != -1 && lastBlock_image < blocksList.Count) // убираем событие движения мыши с текущего контрола, т.к. сейчас его поменяем
                blocksList[lastBlock_image].MouseMove -= block_MouseMove;

            // передвинуть курсор? или скрыть?
            if (lastBlock - index < blocksList.Count && lastBlock - index >= 0 && lastBlock < widths.Count)
            {
                rectangle_select.Visibility = System.Windows.Visibility.Visible;
                Grid.SetColumn(rectangle_select, lastBlock - index);
                rectangle_select.Width = widths[lastBlock] * 32;
                rectangle_select.Height = heights[lastBlock] * 32;
                Grid.SetColumn(rectangle_cursor, lastBlock - index);

                lastBlock_image = lastBlock - index;
                blocksList[lastBlock_image].MouseMove += block_MouseMove;
            }
            else
            {
                lastBlock_image = -1;
                rectangle_select.Visibility = System.Windows.Visibility.Hidden;
                rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
            }
            //Window_SizeChanged(this, null);
            //updateLabelInfo();
        }



        public bool closing = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing == false) // КОСТЫЛЬ - нужен для того что бы крестом миникарта не закрывалась, а при выключении гл. формы закрывалась
            {
                e.Cancel = true;
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            int d = e.Delta;

            e.Handled = true;

            if (d > 0)
            {
                BlockSlider.Value -= 1;
            }
            else if (d < 0)
            {
                BlockSlider.Value += 1;
            }


        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBlocks();
            updateLabelInfo();
        }
        void updateLabelInfo()
        {
            if (size_label == null || count_label == null || pos_label == null)
                return;
            if (lastBlock >= 0 && lastBlock < widths.Count)
                size_label.Content = "Selected block " + lastBlock + " [" + widths[lastBlock] + "x" + heights[lastBlock] + "]";
            else
                size_label.Content = "Block not selected";
            count_label.Content = "Blocks in window: " + gridCols.Count;
            pos_label.Content = "Position = " + ((int)BlockSlider.Value).ToString();
        }

        public int lastBlock = -1;       // последний выбраный блок в списке ВСЕХ блоков
        public int lastBlock_image = -1; // последний выбраный блок-контрол в окне
        private void block_image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //int w = 0;
            Blocks_Image bi = null;
            if (sender.GetType() == typeof(Blocks_Image))
                bi = (Blocks_Image)sender;
            else
                bi = blocksList[Grid.GetColumn(rectangle_cursor)];

            int i = -1;
            for (i = 0; i < gridCols.Count; i++) // поиск блока-картинки
            {
                if (bi == blocksList[i])
                {
                    break;
                }
            }

            Gl.palette.Is_block_selected = true;

            int index = (int)BlockSlider.Value + i;

            if (lastBlock != index || Gl.palette.Is_block_selected == false)
            {
                //blocksList[i].MouseEnter -= block_MouseEnter;
                //blocksList[i].MouseLeave -= block_MouseLeave;
                reset_block_mouse_event();

                lastBlock_image = i;
                lastBlock = index;
                Grid.SetColumn(rectangle_select, i);
                Grid.SetColumn(rectangle_cursor, i);
                //blocksList[i].MouseEnter += block_MouseEnter;
                //blocksList[i].MouseLeave += block_MouseLeave;
                blocksList[i].MouseMove += block_MouseMove;
                rectangle_select.Width = widths[index] * 32;
                rectangle_select.Height = heights[index] * 32;
                rectangle_select.Visibility = System.Windows.Visibility.Visible;
                rectangle_cursor.Visibility = System.Windows.Visibility.Visible;

                UpdateBlockInMain();
                updateLabelInfo();

                //Gl.main.cursor_Block.ChangeBlock(bitmaps[index]);
                //Gl.palette.Is_block_selected = true;
                //Gl.palette.selected_block = index;
                //Gl.palette.block_width = widths[index];
                //Gl.palette.block_height = heights[index];
                //
                //Gl.main.CursorsUpdate();
                //Gl.main.selectedInfoUpdate();
            }
            else
            {
                Point p = Mouse.GetPosition(bi);//MouseUtilities.CorrectGetPosition(canvas_Map);
                int mx = (int)p.X;
                int my = (int)p.Y;
                int x = mx / 32;
                int y = my / 32;

                if (x < widths[index] && y < heights[index])
                {

                    if (e.ChangedButton == MouseButton.Left)
                    {
                        // установить тайл
                        if (Gl.palette.selected_sprite != -1)
                        {
                            setTileToBlock(x, y, lastBlock, Gl.palette.selected_sprite, true);
                            blocksList[i].ChangeBlock(bitmaps[index]);
                            Gl.main.cursor_Block.ChangeBlock(bitmaps[index]);
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        // удалить тайл
                        setTileToBlock(x, y, lastBlock, -1, true);
                        blocksList[i].ChangeBlock(bitmaps[index]);
                        Gl.main.cursor_Block.ChangeBlock(bitmaps[index]);
                    }
                    else if (e.ChangedButton == MouseButton.Middle)
                    {
                        // пипетка
                        Scena scn = Gl.curr_scena;
                        int tmp_ind = scn.blocks[lastBlock][x, y].SpriteId;

                        if (tmp_ind != -1)
                        {
                            Gl.palette.selected_tile = Gl.curr_scena.spriteSet[tmp_ind].TileId;
                            Gl.palette.selected_sprite = tmp_ind;
                            Gl.palette.UpdateGroundSelector(Gl.palette.selected_tile % 8, Gl.palette.selected_tile / 8);
                            Gl.palette.Is_block_selected = false;
                            Gl.main.CursorsUpdate();
                            Gl.main.selectedInfoUpdate();
                        }

                        // Окно тайлов.
                        Gl.tiles_window.SelectedTile = Gl.palette.selected_tile;
                        Gl.tiles_window.SelectedSprite = Gl.palette.selected_sprite;
                        Gl.tiles_window.FullUpdate();

                    }
                }

            }
        }

        private void block_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = Mouse.GetPosition((Blocks_Image)sender);//MouseUtilities.CorrectGetPosition(canvas_Map);
            int mx = (int)p.X;
            int my = (int)p.Y;
            int x = mx / 32;
            int y = my / 32;

            if (x >= 0 && x < widths[lastBlock] && y >= 0 && y < heights[lastBlock])
            {
                rectangle_cursor.Visibility = System.Windows.Visibility.Visible;
                rectangle_cursor.Margin = new Thickness(x * 32, y * 32, 0, 0);
            }
            else
            {
                rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public void reset_block_mouse_event()
        {
            if (lastBlock_image != -1)
                blocksList[lastBlock_image].MouseMove -= block_MouseMove;
        }

        void setTileToBlock(int x, int y, int block, int spr_id, bool write_to_history)
        {
            Scena scn = Gl.curr_scena;
            if (scn.blocks[block][x, y].SpriteId != spr_id)
            {
                if (write_to_history)
                    History.History.AddBlockEvent(x, y, block, scn.blocks[block][x, y].SpriteId, spr_id);
                if (spr_id != -1)
                {
                    scn.blocks[block][x, y] = scn.spriteSet[spr_id];
                    bitmaps[block][x, y] = scn.blocks[block][x, y].Sprite;
                }
                else
                {
                    scn.blocks[block][x, y] = blank_Tile;
                    bitmaps[block][x, y] = scn.ground_sprites.getBitmapImage(-1, 0);
                }

                blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock_image]);
                //UpdateBlockImages();
            }
        }

        public void CallKeyUp(KeyEventArgs e)
        {
            OnKeyUp(e);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            bool ctrl = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));

            if (ctrl && lastBlock != -1)
            {
                e.Handled = true;

                #region arrows keys
                Tile tmp = null;
                ImageSource tmp_bmp = null;
                bool isDirt = false;
                Tile[,] block = Gl.curr_scena.blocks[lastBlock];

                int w = bitmaps[lastBlock].GetLength(0); // старые размеры контрола
                int h = bitmaps[lastBlock].GetLength(1); // старые размеры контрола

                if (e.Key == Key.Right)
                {
                    for (int j = 0; j < h; j++)     //y
                    {
                        tmp = block[w - 1, j];
                        tmp_bmp = bitmaps[lastBlock][w - 1, j];
                        for (int i = w - 1; i >= 0; i--)  //x
                        {
                            if (i > 0)
                            {
                                block[i, j] = block[i - 1, j];
                                bitmaps[lastBlock][i, j] = bitmaps[lastBlock][i - 1, j];
                            }
                            else
                            {
                                block[0, j] = tmp;
                                bitmaps[lastBlock][0, j] = tmp_bmp;
                            }
                        }
                    }
                    isDirt = true;
                }
                if (e.Key == Key.Left)
                {
                    for (int j = 0; j < h; j++)     //y
                    {
                        tmp = block[0, j];
                        tmp_bmp = bitmaps[lastBlock][0, j];
                        for (int i = 0; i < w; i++)  //x
                        {
                            if (i < w - 1)
                            {
                                block[i, j] = block[i + 1, j];
                                bitmaps[lastBlock][i, j] = bitmaps[lastBlock][i + 1, j];
                            }
                            else
                            {
                                block[w - 1, j] = tmp;
                                bitmaps[lastBlock][w - 1, j] = tmp_bmp;
                            }
                        }
                    }
                    isDirt = true;
                }
                if (e.Key == Key.Down)
                {
                    for (int j = 0; j < w; j++)     //x
                    {
                        tmp = block[j, h - 1];
                        tmp_bmp = bitmaps[lastBlock][j, h - 1];
                        for (int i = h - 1; i >= 0; i--)  //y
                        {
                            if (i > 0)
                            {
                                block[j, i] = block[j, i - 1];
                                bitmaps[lastBlock][j, i] = bitmaps[lastBlock][j, i - 1];
                            }
                            else
                            {
                                block[j, 0] = tmp;
                                bitmaps[lastBlock][j, 0] = tmp_bmp;
                            }
                        }
                    }
                    isDirt = true;
                }
                if (e.Key == Key.Up)
                {
                    for (int j = 0; j < w; j++)     //x
                    {
                        tmp = block[j, 0];
                        tmp_bmp = bitmaps[lastBlock][j, 0];
                        for (int i = 0; i < h; i++)  //y
                        {
                            if (i < h - 1)
                            {
                                block[j, i] = block[j, i + 1];
                                bitmaps[lastBlock][j, i] = bitmaps[lastBlock][j, i + 1];
                            }
                            else
                            {
                                block[j, h - 1] = tmp; // !!%^!@&^*
                                bitmaps[lastBlock][j, h - 1] = tmp_bmp;
                            }
                        }
                    }
                    isDirt = true;
                }

                if (isDirt)
                {
                    if (lastBlock_image != -1)
                        blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
                    Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
                }
                #endregion arrows keys

                #region history
                if (e.Key == Key.Z && this.IsActive)
                {
                    SetLandEvent sle = History.History.ExtractUndoBlockEvent();
                    if (sle == null)
                        return;

                    setTileToBlock(sle.X, sle.Y, sle.Block, sle.SprFrom, false);

                    UpdateBlocks();
                    //if (lastBlock_image != -1)
                    //{
                    //    blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
                    //}
                    //int index = (int)BlockSlider.Value + lastBlock_image;
                    if (sle.Block == lastBlock)
                        Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
                }
                else if (e.Key == Key.Y && this.IsActive)
                {
                    SetLandEvent sle = History.History.ExtractRedoBlockEvent();
                    if (sle == null)
                        return;

                    setTileToBlock(sle.X, sle.Y, sle.Block, sle.SprTo, false);

                    UpdateBlocks();
                    //if (lastBlock_image != -1)
                    //{
                    //    blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
                    //}
                    //int index = (int)BlockSlider.Value + lastBlock_image;
                    if (sle.Block == lastBlock)
                        Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
                }
                #endregion history
            }

            if (e.Key == Key.Escape)
            {
                Gl.main.CallKeyUp(e);
            }

            if (e.Key == Key.Delete)
            {
                delete_block_button_Click(this, null);
            }

        }

        #region add/delete rows/columns
        public int max_columns = 32;
        public int max_rows = 32;
        private void add_right_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock == -1)
                return;

            int x = bitmaps[lastBlock].GetLength(0); // старые размеры контрола
            int y = bitmaps[lastBlock].GetLength(1); // старые размеры контрола

            if (x == max_columns)
                return;

            // Увеличиваем ширину и соответствующие массивы.
            widths[lastBlock]++;
            Gl.curr_scena.blocks[lastBlock] = Utils.ResizeArray(Gl.curr_scena.blocks[lastBlock], x + 1, y, blank_Tile);
            bitmaps[lastBlock] = Utils.ResizeArray(bitmaps[lastBlock], x + 1, y, Gl.curr_scena.ground_sprites.getBitmapImage(-1, 0));

            if (lastBlock_image != -1)
            {
                gridCols[lastBlock_image].Width = new GridLength((x + 1) * 32 + space);// устанавливаем размер столбца
                blocksList[lastBlock_image].Width = (x + 1) * 32; // размер контрола
                blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
            }
            UpdateBlocks();
            Gl.palette.block_width = x + 1;    // палитра
            Gl.palette.block_height = y;       // палитра
            Gl.main.cursor_Block.Width = (x + 1) * 32; // размер контрола
            Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
            Gl.main.CursorsUpdate();
        }

        private void add_down_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock == -1)
                return;

            int x = bitmaps[lastBlock].GetLength(0); // старые размеры контрола
            int y = bitmaps[lastBlock].GetLength(1); // старые размеры контрола

            if (y == max_rows)
                return;

            // Увеличиваем высоту и соответствующие массивы.
            heights[lastBlock]++;
            Gl.curr_scena.blocks[lastBlock] = Utils.ResizeArray(Gl.curr_scena.blocks[lastBlock], x, y + 1, blank_Tile);
            bitmaps[lastBlock] = Utils.ResizeArray(bitmaps[lastBlock], x, y + 1, Gl.curr_scena.ground_sprites.getBitmapImage(-1, 0));

            if (lastBlock_image != -1)
            {
                blocksList[lastBlock_image].Height = (y + 1) * 32; // размер контрола
                blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
            }
            //BlocksGrid.Height = (y + 1) * 32;
            UpdateBlocks();
            Gl.palette.block_width = x;          // палитра
            Gl.palette.block_height = y + 1;     // палитра
            Gl.main.cursor_Block.Height = (y + 1) * 32; // размер контрола
            Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
            Gl.main.CursorsUpdate();
        }

        private void delete_right_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock == -1)
                return;

            int x = bitmaps[lastBlock].GetLength(0); // старые размеры контрола
            int y = bitmaps[lastBlock].GetLength(1); // старые размеры контрола

            if (x == 1)
                return;

            // Уменьшаем ширину и соответствующие массивы.
            widths[lastBlock]++;
            Gl.curr_scena.blocks[lastBlock] = Utils.ResizeArray(Gl.curr_scena.blocks[lastBlock], x - 1, y, blank_Tile);
            bitmaps[lastBlock] = Utils.ResizeArray(bitmaps[lastBlock], x - 1, y, Gl.curr_scena.ground_sprites.getBitmapImage(-1, 0));

            if (lastBlock_image != -1)
            {
                gridCols[lastBlock_image].Width = new GridLength((x - 1) * 32 + space);// устанавливаем размер столбца
                blocksList[lastBlock_image].Width = (x - 1) * 32; // размер контрола
                blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
            }
            UpdateBlocks();
            Gl.palette.block_width = x - 1;    // палитра
            Gl.palette.block_height = y;       // палитра
            Gl.main.cursor_Block.Width = (x - 1) * 32; // размер контрола
            Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
            Gl.main.CursorsUpdate();
        }

        private void delete_down_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock == -1)
                return;

            int x = bitmaps[lastBlock].GetLength(0); // старые размеры контрола
            int y = bitmaps[lastBlock].GetLength(1); // старые размеры контрола

            if (y == 1)
                return;

            // Уменьшаем высоту и соответствующие массивы.
            heights[lastBlock]++;
            Gl.curr_scena.blocks[lastBlock] = Utils.ResizeArray(Gl.curr_scena.blocks[lastBlock], x, y - 1, blank_Tile);
            bitmaps[lastBlock] = Utils.ResizeArray(bitmaps[lastBlock], x, y - 1, Gl.curr_scena.ground_sprites.getBitmapImage(-1, 0));

            if (lastBlock_image != -1)
            {
                blocksList[lastBlock_image].Height = (y - 1) * 32; // размер контрола
                blocksList[lastBlock_image].ChangeBlock(bitmaps[lastBlock]);
            }
            //BlocksGrid.Height = (y - 1) * 32;
            UpdateBlocks();
            Gl.palette.block_width = x;          // палитра
            Gl.palette.block_height = y - 1;     // палитра
            Gl.main.cursor_Block.Height = (y - 1) * 32; // размер контрола
            Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
            Gl.main.CursorsUpdate();
        }
        #endregion add/delete rows/columns

        public void add_block_button_Click(object sender, RoutedEventArgs e)
        {
            InsertBlock(lastBlock + 1, 4, 4);

            UpdateBlockImages();
        }

        private void delete_block_button_Click(object sender, RoutedEventArgs e)
        {
            RemoveBlock(lastBlock);

            if (lastBlock >= widths.Count)
                lastBlock--;
            if (Gl.palette.selected_block >= widths.Count)
                Gl.palette.selected_block--;

            UpdateBlockImages();
            UpdateBlockInMain();
        }

        private void copy_block_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock < 0 || lastBlock >= widths.Count)
                return;

            int w = widths[lastBlock];
            int h = heights[lastBlock];

            InsertBlock(lastBlock + 1, w, h);

            Scena scn = Gl.curr_scena;

            Tile[,] block_new = scn.blocks[lastBlock + 1];
            Tile[,] block_old = scn.blocks[lastBlock];

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    bitmaps[lastBlock + 1][i, j] = bitmaps[lastBlock][i, j];
                    block_new[i, j] = block_old[i, j];
                }
            //AddBlock(Tile[,] tiles)

            UpdateBlockImages();
        }

        public void DeSelectBlock() // Убрать выделение блока
        {
            reset_block_mouse_event();
            lastBlock_image = -1;
            lastBlock = -1;
            rectangle_select.Visibility = System.Windows.Visibility.Hidden;
            rectangle_cursor.Visibility = System.Windows.Visibility.Hidden;
        }




        public void UpdateBlockInMain()
        {
            if (lastBlock >= 0 && lastBlock < bitmaps.Count)
            {
                Gl.main.cursor_Block.ChangeBlock(bitmaps[lastBlock]);
                Gl.palette.Is_block_selected = true;
                Gl.palette.selected_block = lastBlock;
                Gl.palette.block_width = widths[lastBlock];
                Gl.palette.block_height = heights[lastBlock];
            }
            else
            {
                Gl.palette.Is_block_selected = false;
                Gl.palette.selected_block = -1;
            }

            Gl.main.CursorsUpdate();
            Gl.main.selectedInfoUpdate();
        }

        private void move_left_block_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock <= 0 || lastBlock >= bitmaps.Count)
                return;

            int tmp = widths[lastBlock - 1];
            widths[lastBlock - 1] = widths[lastBlock];
            widths[lastBlock] = tmp;

            tmp = heights[lastBlock - 1];
            heights[lastBlock - 1] = heights[lastBlock];
            heights[lastBlock] = tmp;

            Scena scn = Gl.curr_scena;

            Tile[,] tmp_block = scn.blocks[lastBlock - 1];
            scn.blocks[lastBlock - 1] = scn.blocks[lastBlock];
            scn.blocks[lastBlock] = tmp_block;

            ImageSource[,] tmp_i = bitmaps[lastBlock - 1];
            bitmaps[lastBlock - 1] = bitmaps[lastBlock];
            bitmaps[lastBlock] = tmp_i;

            lastBlock--;

            updateLabelInfo();
            UpdateBlocks();
            UpdateBlockInMain();

        }

        private void move_right_block_button_Click(object sender, RoutedEventArgs e)
        {
            if (lastBlock < 0 || lastBlock >= bitmaps.Count - 1)
                return;

            int tmp = widths[lastBlock + 1];
            widths[lastBlock + 1] = widths[lastBlock];
            widths[lastBlock] = tmp;

            tmp = heights[lastBlock + 1];
            heights[lastBlock + 1] = heights[lastBlock];
            heights[lastBlock] = tmp;

            Scena scn = Gl.curr_scena;

            Tile[,] tmp_block = scn.blocks[lastBlock + 1];
            scn.blocks[lastBlock + 1] = scn.blocks[lastBlock];
            scn.blocks[lastBlock] = tmp_block;

            ImageSource[,] tmp_i = bitmaps[lastBlock + 1];
            bitmaps[lastBlock + 1] = bitmaps[lastBlock];
            bitmaps[lastBlock] = tmp_i;

            lastBlock++;

            updateLabelInfo();
            UpdateBlocks();
            UpdateBlockInMain();
        }

    }
}
