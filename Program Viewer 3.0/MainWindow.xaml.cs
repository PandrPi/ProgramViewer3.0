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
            itemManager = new ItemManager();
            animationManager = new AnimationManager(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));

            DesktopLV.ItemsSource = itemManager.desktopItems;
            HotLV.ItemsSource = itemManager.hotItems;

            double screenWidth = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight - 60;
            Left = screenWidth - Width - 10;
            Top = 10;
            //ToggleDesktop();
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

                foreach (var f in files)
                {
                    //desktopItems.Add(new ItemData { Title = "test", ImageData = IconExtractor.GetIcon(f) as BitmapImage });
                }
            }
        }
    }
}
