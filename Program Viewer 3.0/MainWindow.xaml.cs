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



        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IconExtractor.BaseExeIcon = (FindResource("BaseExeImage") as Image).Source;
            itemManager = new ItemManager();
            animationManager = new AnimationManager(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));
            animationManager.SetAddItemWindowShowCallback(() => AddItemGrid.Visibility = Visibility.Visible);
            animationManager.SetAddItemWindowHideCallback(() => AddItemGrid.Visibility = Visibility.Hidden);

            DesktopLV.ItemsSource = itemManager.desktopItems;
            HotLV.ItemsSource = itemManager.hotItems;

            double screenWidth = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight - 60;
            Left = screenWidth - Width - 10;
            Top = 10;
            //ToggleDesktop();
            AddItemGrid.Visibility = Visibility.Hidden;
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
                if (e.GetPosition(MyGrid).X >= Width - (desktopShirkExpandButton.Margin.Right + desktopShirkExpandButton.Width))
                    AddWindowFileWindow.SelectedIndex = 0;
                else
                    AddWindowFileWindow.SelectedIndex = 1;

                if (!isWindowExpanded)
                {
                    ToggleDesktop();
                }
                AddItemGridVisibility(Visibility.Visible);
            }
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            string title = AddWindowFileTitle.Text;
            string path = AddWindowFilePath.Text;
            string selectedWindow = (AddWindowFileWindow.Items[AddWindowFileWindow.SelectedIndex] as ComboBoxItem).Content.ToString();
            ItemType itemType = selectedWindow == "Hot Window" ? ItemType.Hot : ItemType.Desktop;
            itemManager.AddItem(title, path, itemType);
            AddItemGridVisibility(Visibility.Hidden);
        }

        private void AddItemGridVisibility(Visibility visibility)
        {

            if (visibility == Visibility.Visible)
            {
                AddItemGrid.Visibility = visibility;
                animationManager.AddItemWindowShow();
            }
            else
                animationManager.AddItemWindowHide();
        }

        private void AddWindowCloseButton_Click(object sender, RoutedEventArgs e)
        {
            AddItemGridVisibility(Visibility.Hidden);
            (sender as PiButton).Opacity = 1;
        }
    }
}
