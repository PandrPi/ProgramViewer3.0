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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Program_Viewer_3
{
    /// <summary>
    /// Interaction logic for PiButton.xaml
    /// </summary>
    public partial class PiButton : UserControl
    {
        public static readonly DependencyProperty SourceProperty = 
            DependencyProperty.Register("Source", typeof(string), typeof(PiButton), new UIPropertyMetadata(""));
        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public PiButton()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler Click;

        void onButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.Click != null)
            {
                this.Click(this, e);
            }
        }
    }
}
