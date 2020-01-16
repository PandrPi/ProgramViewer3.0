using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Program_Viewer_3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private AnimationManager animationManager;
        private ItemManager itemManager;
        private bool isWindowExpanded = true;
        /// <summary>
        /// Used to store cursor position at the moment when PiContextMenu was shown
        /// </summary>
        private Point lastCursorPoint;

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IconExtractor.BaseExeIcon = (FindResource("BaseExeImage") as Image).Source;
            itemManager = new ItemManager(Dispatcher);
            animationManager = new AnimationManager(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));
            animationManager.SetAddItemWindowShowCallback(() => AddItemGrid.Visibility = Visibility.Visible);
            animationManager.SetAddItemWindowHideCallback(() => AddItemGrid.Visibility = Visibility.Hidden);
            animationManager.SetContextMenuShowCallback(() => PiContextMenu.Visibility = Visibility.Visible);
            animationManager.SetContextMenuHideCallback(() => PiContextMenu.Visibility = Visibility.Hidden);

            DesktopLV.ItemsSource = itemManager.desktopItems;
            HotLV.ItemsSource = itemManager.hotItems;

            DesktopLV.LostFocus += (ev, ee) => DesktopLV.SelectedIndex = -1;
            HotLV.LostFocus     += (ev, ee) => HotLV.SelectedIndex = -1;

            double screenWidth = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight - 60;
            Left = screenWidth - Width - 10;
            Top = 10;
            //ToggleDesktop();
            AddItemGrid.Visibility = Visibility.Hidden;
            PiContextMenu.Visibility = Visibility.Hidden;
        }

        private void ToggleDesktop()
        {
            if (isWindowExpanded)
            {
                animationManager.ShirkDesktop();
                isWindowExpanded = false;
            }
            else
            {
                animationManager.ExpandDesktop();
                isWindowExpanded = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleDesktop();
        }

        private void MyGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string title = System.IO.Path.GetFileNameWithoutExtension(files[0]);

                AddWindowFilePath.Text = files[0];
                AddWindowFileTitle.Text = title;
                if (GetWindowTypeFromPoint(e.GetPosition(MyGrid)) == ItemType.Hot)
                    AddWindowFileWindow.SelectedIndex = 0;
                else
                    AddWindowFileWindow.SelectedIndex = 1;

                if (!isWindowExpanded)
                {
                    ToggleDesktop();
                }
                SetAddItemGridVisibility(Visibility.Visible);
            }
        }
        /// <summary>
        /// Determines over which window is point (Desktop or Hot)
        /// </summary>
        /// <param name="point">Cursor point relative to main grid</param>
        /// <returns></returns>
        private ItemType GetWindowTypeFromPoint(Point point)
        {
            if (point.X >= Width - (desktopShirkExpandButton.Margin.Right + desktopShirkExpandButton.Width))
                return ItemType.Hot;
            else
                return ItemType.Desktop;
        }
        /// <summary>
        /// Determines over which window is point (Desktop or Hot)
        /// </summary>
        /// <param name="X">X position of cursor click point</param>
        /// <returns></returns>
        private ItemType GetWindowTypeFromPoint(double X)
        {
            if (X >= Width - (desktopShirkExpandButton.Margin.Right + desktopShirkExpandButton.Width))
                return ItemType.Hot;
            else
                return ItemType.Desktop;
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            string title = AddWindowFileTitle.Text;
            string path = AddWindowFilePath.Text;
            string selectedWindow = (AddWindowFileWindow.Items[AddWindowFileWindow.SelectedIndex] as ComboBoxItem).Content.ToString();
            ItemType itemType = selectedWindow == "Hot Window" ? ItemType.Hot : ItemType.Desktop;
            itemManager.AddItem(title, path, itemType);
            SetAddItemGridVisibility(Visibility.Hidden);
        }

        /// <summary>
        /// Sets visibility of Add Item Window, starts occur/dissolve animation
        /// </summary>
        /// <param name="visibility"></param>
        private void SetAddItemGridVisibility(Visibility visibility)
        {

            if (visibility == Visibility.Visible)
            {
                AddItemGrid.Visibility = visibility;
                animationManager.AddItemWindowShow();
            }
            else
                animationManager.AddItemWindowHide();
        }
        /// <summary>
        /// Sets visibility of PiContextMenu, starts occur/dissolve animation
        /// </summary>
        /// <param name="visibility"></param>
        private void SetContextMenuVisibility(Visibility visibility)
        {

            if (visibility == Visibility.Visible)
            {
                PiContextMenu.Visibility = visibility;
                animationManager.ContextMenuShow();
            }
            else
                animationManager.ContextMenuHide();
        }

        private void AddWindowCloseButton_Click(object sender, RoutedEventArgs e)
        {
            SetAddItemGridVisibility(Visibility.Hidden);
            (sender as PiButton).Opacity = 1;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            itemManager.Dispose();
        }

        private void PiContextOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            ItemType itemType = GetWindowTypeFromPoint(lastCursorPoint);
            if(itemType == ItemType.Hot)
            {
                int selectedIndex = HotLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.OpenItem(selectedIndex, itemType);
                }
            }
            else
            {
                int selectedIndex = DesktopLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.OpenItem(selectedIndex, itemType);
                }
            }
            SetContextMenuVisibility(Visibility.Hidden);
        }

        private void PiContextRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ItemType itemType = GetWindowTypeFromPoint(lastCursorPoint);
            if (itemType == ItemType.Hot)
            {
                int selectedIndex = HotLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.RemoveItem(selectedIndex, itemType);
                }
            }
            else
            {
                int selectedIndex = DesktopLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.RemoveItem(selectedIndex, itemType);
                }
            }
            SetContextMenuVisibility(Visibility.Hidden);
        }

        private void PiContextShowInExlorerButton_Click(object sender, RoutedEventArgs e)
        {
            ItemType itemType = GetWindowTypeFromPoint(lastCursorPoint);
            if (itemType == ItemType.Hot)
            {
                int selectedIndex = HotLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.ShowItemInExplorer(selectedIndex, itemType);
                }
            }
            else
            {
                int selectedIndex = DesktopLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.ShowItemInExplorer(selectedIndex, itemType);
                }
            }
            SetContextMenuVisibility(Visibility.Hidden);
        }

        private void PiContextRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SetContextMenuVisibility(Visibility.Hidden);
        }

        private void Hot_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetContextMenuVisibility(Visibility.Hidden);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Point cursorPosition = e.GetPosition(MyGrid);
                lastCursorPoint = cursorPosition;
                if (GetWindowTypeFromPoint(cursorPosition) == ItemType.Hot)
                {
                    SetContextMenuVisibility(Visibility.Visible);
                    PiContextMenu.Margin = GetContextMenuMarginFromCursorPosition(cursorPosition, ItemType.Hot);
                }
            }
        }

        private void Desktop_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetContextMenuVisibility(Visibility.Hidden);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Point cursorPosition = e.GetPosition(MyGrid);
                lastCursorPoint = cursorPosition;
                if (GetWindowTypeFromPoint(cursorPosition) == ItemType.Desktop)
                {
                    SetContextMenuVisibility(Visibility.Visible);
                    PiContextMenu.Margin = GetContextMenuMarginFromCursorPosition(cursorPosition, ItemType.Desktop);
                }
            }
        }

        /// <summary>
        /// Determines the point(Margin) where the context menu will be shown
        /// </summary>
        /// <param name="point"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private Thickness GetContextMenuMarginFromCursorPosition(Point point, ItemType itemType)
        {
            double xDelta = PiContextMenu.Width * ((point.X + PiContextMenu.Width > MyGrid.Width)
                ? -1 : (itemType == ItemType.Desktop) ? 0 : 1);

            return new Thickness(point.X + xDelta, point.Y, 0, 0);
        }
        
        private void WindowBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetContextMenuVisibility(Visibility.Hidden);
            HotLV.SelectedIndex = -1;
            DesktopLV.SelectedIndex = -1;
        }
    }
}
