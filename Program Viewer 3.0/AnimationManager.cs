using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ProgramViewer3
{
    public class AnimationManager
    {
        private FrameworkElement frameworkElement;
        private Storyboard desktopShrinkSB = new Storyboard();
		private Storyboard desktopExpandSB = new Storyboard();
		private Storyboard settingsShrinkSB= new Storyboard();
        private Storyboard settingsExpandSB = new Storyboard();
        private Storyboard addItemWindowShowSB = new Storyboard();
        private Storyboard addItemWindowHideSB = new Storyboard();
        private Storyboard contextMenuShowSB = new Storyboard();
		private Storyboard contextMenuHideSB = new Storyboard();

        private TimeSpan addItemWindowSHAnimDuration = TimeSpan.FromSeconds(0.3);

		private Point settingsGridResizeArea = new Point(31, 491);

		/// <summary>
		/// Initializes animation manager
		/// </summary>
		/// <param name="frameworkElement">Current application window</param>
		/// <param name="resizeDuration">Duration of expand/shrink animations</param>
		/// <param name="resizeArea">Point to store resize area. X stores shrinked window width, Y - expanded window width</param>
        public void Initiallize(FrameworkElement frameworkElement, TimeSpan resizeDuration, Point resizeArea)
        {
            this.frameworkElement = frameworkElement;

            ExponentialEase exponentialEase = new ExponentialEase();
            exponentialEase.EasingMode = EasingMode.EaseIn;
            DoubleAnimation desktopWidthDA = CreateDoubleAnimation(resizeArea.X, resizeDuration, "WindowBorder", "Width", exponentialEase);
            DoubleAnimation settingsWidthDA = CreateDoubleAnimation(settingsGridResizeArea.X, resizeDuration, "SettingsGrid", "Width", exponentialEase);
            DoubleAnimation opacityDA = CreateDoubleAnimation(0, resizeDuration, "DesktopLV", "Opacity", exponentialEase);
			DoubleAnimation settingsShadowOpacityDA = CreateDoubleAnimation(0, resizeDuration, "SettingsGrid", "(Effect).Opacity", exponentialEase);
			DoubleAnimation addItemWindowDA = CreateDoubleAnimation(1, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEase);
            DoubleAnimation contextMenuDA = CreateDoubleAnimation(1, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEase);

			ColorAnimation settingsRectCA = CreateColorAnimation(Color.FromArgb(255, 111, 140, 156), resizeDuration, "SettingsVerticalRect", "(Rectangle.Fill).(SolidColorBrush.Color)", exponentialEase);

            desktopShrinkSB.Children.Add(desktopWidthDA);
            desktopShrinkSB.Children.Add(opacityDA);
			settingsShrinkSB.Children.Add(settingsWidthDA);
            settingsExpandSB.Children.Add(opacityDA);
            settingsExpandSB.Children.Add(settingsShadowOpacityDA);
            settingsExpandSB.Children.Add(settingsRectCA);
            addItemWindowShowSB.Children.Add(addItemWindowDA);
            contextMenuShowSB.Children.Add(contextMenuDA);

            exponentialEase.EasingMode = EasingMode.EaseOut;
            desktopWidthDA = CreateDoubleAnimation(resizeArea.Y, resizeDuration, "WindowBorder", "Width", exponentialEase);
			settingsWidthDA = CreateDoubleAnimation(settingsGridResizeArea.Y, resizeDuration, "SettingsGrid", "Width", exponentialEase);
			opacityDA = CreateDoubleAnimation(1, resizeDuration, "DesktopLV", "Opacity", exponentialEase);
			settingsShadowOpacityDA = CreateDoubleAnimation(0.5, resizeDuration, "SettingsGrid", "(Effect).Opacity", exponentialEase);
            addItemWindowDA = CreateDoubleAnimation(0, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEase);
            contextMenuDA = CreateDoubleAnimation(0, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEase);

			settingsRectCA = CreateColorAnimation(Color.FromArgb(255, 28, 28, 28), resizeDuration, "SettingsVerticalRect", "(Rectangle.Fill).(SolidColorBrush.Color)", exponentialEase);

			desktopExpandSB.Children.Add(desktopWidthDA);
            desktopExpandSB.Children.Add(opacityDA);
			settingsExpandSB.Children.Add(settingsWidthDA);
			settingsShrinkSB.Children.Add(opacityDA);
			settingsShrinkSB.Children.Add(settingsShadowOpacityDA);
			settingsShrinkSB.Children.Add(settingsRectCA);
			addItemWindowHideSB.Children.Add(addItemWindowDA);
            contextMenuHideSB.Children.Add(contextMenuDA);
        }

		/// <summary>
		/// Medhod to create new Double Animation object
		/// </summary>
		/// <param name="to"></param>
		/// <param name="duration"></param>
		/// <param name="targetProperty"></param>
		/// <param name="propertyPath"></param>
		/// <param name="easingFunction"></param>
		/// <returns></returns>
        private DoubleAnimation CreateDoubleAnimation(double to, TimeSpan duration,
            string targetProperty, string propertyPath, IEasingFunction easingFunction)
        {
            DoubleAnimation da = new DoubleAnimation(to, duration);
            da.SetValue(Storyboard.TargetNameProperty, targetProperty);
            Storyboard.SetTargetProperty(da, new PropertyPath(propertyPath));
            da.EasingFunction = easingFunction;

            return da;
        }

		private ColorAnimation CreateColorAnimation(Color to, TimeSpan duration,
			string targetProperty, string propertyPath, IEasingFunction easingFunction)
		{
			ColorAnimation ca = new ColorAnimation(to, duration);
			ca.SetValue(Storyboard.TargetNameProperty, targetProperty);
			Storyboard.SetTargetProperty(ca, new PropertyPath(propertyPath));
			ca.EasingFunction = easingFunction;

			return ca;
		}

		public void ToggleDesktop(bool value)
		{
			if (value)
				desktopShrinkSB.Begin(frameworkElement);
			else
				desktopExpandSB.Begin(frameworkElement);
		}

		public void ToggleSettings(bool value)
        {
			if (value)
				settingsShrinkSB.Begin(frameworkElement);
			else
				settingsExpandSB.Begin(frameworkElement);
        }

        public void AddItemWindowShow()
        {
            addItemWindowShowSB.Begin(frameworkElement);
        }
        public void AddItemWindowHide()
        {
            addItemWindowHideSB.Begin(frameworkElement);
        }

        public void ContextMenuShow()
        {
            contextMenuShowSB.Begin(frameworkElement);
        }
        public void ContextMenuHide()
        {
            contextMenuHideSB.Begin(frameworkElement);
        }

        public void SetAddItemWindowShowCallback(Action callback)
        {
            addItemWindowShowSB.Completed += (sender, e) => callback();
        }
        public void SetAddItemWindowHideCallback(Action callback)
        {
            addItemWindowHideSB.Completed += (sender, e) => callback();
        }
        public void SetContextMenuShowCallback(Action callback)
        {
            contextMenuShowSB.Completed += (sender, e) => callback();
        }
        public void SetContextMenuHideCallback(Action callback)
        {
            contextMenuHideSB.Completed += (sender, e) => callback();
        }

		public void ListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			e.Handled = true;

			ScrollViewer scrollViewer = ScrollAnimationBehavior.GetScrollViewer(sender as ListView) as ScrollViewer;
			scrollViewer.IsDeferredScrollingEnabled = true;

			DoubleAnimation verticalAnimation = new DoubleAnimation
			{
				From = scrollViewer.VerticalOffset,
				To = scrollViewer.VerticalOffset - (150 * Math.Sign(e.Delta)),
				Duration = TimeSpan.FromSeconds(0.5),
				EasingFunction = new ExponentialEase()
				{
					EasingMode = EasingMode.EaseOut
				}
			};

			Storyboard storyboard = new Storyboard();

			storyboard.Children.Add(verticalAnimation);
			Storyboard.SetTarget(verticalAnimation, scrollViewer);
			Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty));
			storyboard.Begin();
		}
	}

	public static class ScrollAnimationBehavior
	{

		public static DependencyProperty VerticalOffsetProperty =
		DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollAnimationBehavior),
											new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

		private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			ScrollViewer scrollViewer = target as ScrollViewer;

			if (scrollViewer != null)
			{
				scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
			}
		}

		public static DependencyObject GetScrollViewer(DependencyObject o)
		{
			if (o is ScrollViewer)
				return o;

			for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = System.Windows.Media.VisualTreeHelper.GetChild(o, i);

				var result = GetScrollViewer(child);
				if (result == null)
				{
					continue;
				}
				else
				{
					return result;
				}
			}
			return null;
		}
	}
}
