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
            DependencyProperty.Register("Source", typeof(string), typeof(PiButton), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty TextProperty = 
            DependencyProperty.Register("Text", typeof(string), typeof(PiButton), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty HoverColorProperty = 
            DependencyProperty.Register("HoverColor", typeof(Color), typeof(PiButton), new UIPropertyMetadata(default(Color)));
        public static readonly DependencyProperty TextHorizontalAlignmentProperty = 
            DependencyProperty.Register("TextHorizontalAlignment", typeof(HorizontalAlignment), typeof(PiButton), new UIPropertyMetadata(default(HorizontalAlignment)));
        public static readonly DependencyProperty TextVerticalAlignmentProperty = 
            DependencyProperty.Register("TextVerticalAlignment", typeof(VerticalAlignment), typeof(PiButton), new UIPropertyMetadata(default(VerticalAlignment)));

        public string Source { get { return (string)GetValue(SourceProperty); } set { SetValue(SourceProperty, value); } }
        public string Text { get { return (string)GetValue(TextProperty); } set { SetValue(TextProperty, value); } }
        public Color HoverColor { get { return (Color)GetValue(HoverColorProperty); } set { SetValue(HoverColorProperty, value); } }
        public HorizontalAlignment TextHorizontalAlignment { get { return (HorizontalAlignment)GetValue(HoverColorProperty); }
            set { SetValue(HoverColorProperty, value); } }
        public VerticalAlignment TextVerticalAlignment { get { return (VerticalAlignment)GetValue(HoverColorProperty); }
            set { SetValue(HoverColorProperty, value); } }

        public PiButton()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler Click;
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.Click != null)
            {
                this.Click(this, e);
            }
        }
    }
}
