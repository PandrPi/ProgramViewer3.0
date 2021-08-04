using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ProgramViewer3.Managers;

namespace ProgramViewer3
{
	/// <summary>
	/// Interaction logic for PiButton.xaml
	/// </summary>
	public partial class PiButton : UserControl
	{
		private static readonly Type ownerType = typeof(PiButton);
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(ImageSource), ownerType, new UIPropertyMetadata(default(ImageSource)));
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), ownerType, new UIPropertyMetadata(string.Empty));
		public static readonly DependencyProperty HoverColorProperty =
			DependencyProperty.Register("HoverBrush", typeof(Brush), ownerType, new UIPropertyMetadata(new SolidColorBrush(Colors.Transparent)));
		public static readonly DependencyProperty HoverDurationProperty =
			DependencyProperty.Register("HoverDuration", typeof(Duration), ownerType, new UIPropertyMetadata(default(Duration)));
		public static readonly DependencyProperty TextHorizontalAlignmentProperty =
			DependencyProperty.Register("TextHorizontalAlignment", typeof(HorizontalAlignment), ownerType, new UIPropertyMetadata(default(HorizontalAlignment)));
		public static readonly DependencyProperty TextVerticalAlignmentProperty =
			DependencyProperty.Register("TextVerticalAlignment", typeof(VerticalAlignment), ownerType, new UIPropertyMetadata(default(VerticalAlignment)));
		public static readonly DependencyProperty TextMarginProperty =
			DependencyProperty.Register("TextMargin", typeof(Thickness), ownerType, new UIPropertyMetadata(default(Thickness)));

		public ImageSource Source
		{
			get => (ImageSource)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}
		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}
		public Brush HoverBrush
		{
			get => (Brush)GetValue(HoverColorProperty);
			set => SetValue(HoverColorProperty, value);
		}
		public Duration HoverDuration
		{
			get => (Duration)GetValue(HoverDurationProperty);
			set => SetValue(HoverDurationProperty, value);

		}
		public HorizontalAlignment TextHorizontalAlignment
		{
			get => (HorizontalAlignment)GetValue(TextHorizontalAlignmentProperty);
			set => SetValue(TextHorizontalAlignmentProperty, value);
		}
		public VerticalAlignment TextVerticalAlignment
		{
			get => (VerticalAlignment)GetValue(TextVerticalAlignmentProperty);
			set => SetValue(TextVerticalAlignmentProperty, value);
		}
		public Thickness TextMargin
		{
			get => (Thickness)GetValue(TextMarginProperty);
			set => SetValue(TextMarginProperty, value);
		}

		public event RoutedEventHandler Click;

		public PiButton()
		{
			InitializeComponent();

			LostMouseCapture += PiButton_LostMouseCapture;
		}

		private void PiButton_LostMouseCapture(object sender, MouseEventArgs e) => Opacity = 1;

		private void Grid_MouseDown(object sender, MouseButtonEventArgs e) => Click?.Invoke(this, e);

		private void MouseEnterBackgroundStoryboard_Completed(object sender, EventArgs e)
		{
			if (HoverBrush is null == false)
			{
				(ButtonGrid.Background as SolidColorBrush).Color = (HoverBrush as SolidColorBrush).Color;
			}
			ButtonGrid.BeginAnimation(BackgroundProperty, null);
		}

		private void MouseLeaveBackgroundStoryboard_Completed(object sender, EventArgs e)
		{
			if (Background is null == false)
			{
				(ButtonGrid.Background as SolidColorBrush).Color = (Background as SolidColorBrush).Color;
			}
			ButtonGrid.BeginAnimation(BackgroundProperty, null);
		}
	}
}
