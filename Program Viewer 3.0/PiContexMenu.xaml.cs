using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Program_Viewer_3
{
    public partial class PiContexMenu : UserControl
    {
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public Color HoverColor
        {
            get { return (Color)GetValue(HoverColorProperty); }
            set { SetValue(HoverColorProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
          DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(PiContexMenu));

        public static readonly DependencyProperty HoverColorProperty =
            DependencyProperty.Register("HoverColor", typeof(Color), typeof(PiContexMenu), new UIPropertyMetadata(default(Color)));

        public PiContexMenu()
        {
            InitializeComponent();
        }

        
    }
}
