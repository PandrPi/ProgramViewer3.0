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
        private bool isWindowExpanded = true;

        private ObservableCollection<ItemData> desktopItems = new ObservableCollection<ItemData>();
        private ObservableCollection<ItemData> hotItems = new ObservableCollection<ItemData>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BitmapImage image1 = LoadImage("image.jpg");

            desktopItems.Add(new ItemData { Title = "Movie 1", ImageData = image1 });
            desktopItems.Add(new ItemData { Title = "Movie 2", ImageData = image1 });
            desktopItems.Add(new ItemData { Title = "Movie 3", ImageData = image1 });
            desktopItems.Add(new ItemData { Title = "Movie 4", ImageData = image1 });
            desktopItems.Add(new ItemData { Title = "Movie 5", ImageData = image1 });
            desktopItems.Add(new ItemData { Title = "Movie 6", ImageData = image1 });
            DesktopLV.ItemsSource = desktopItems;

            hotItems.Add(new ItemData { Title = "Movie 1", ImageData = image1 });
            hotItems.Add(new ItemData { Title = "Movie 2", ImageData = image1 });
            HotLV.ItemsSource = hotItems;

            animationManager = new AnimationManager(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));

            double screenWidth = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight - 60;
            Left = screenWidth - Width - 10;
            Top = 10;
            //ToggleDesktop();
        }

        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + filename, UriKind.Absolute));
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
                    desktopItems.Add(new ItemData { Title = "test", ImageData = IconExtractor.GetIcon(f) as BitmapImage });
                }
            }
        }
    }
}
