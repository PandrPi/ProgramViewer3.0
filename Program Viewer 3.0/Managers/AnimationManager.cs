using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;

namespace ProgramViewer3.Managers
{
    public class AnimationManager
    {
		private MainWindow mainWindow;
        private FrameworkElement frameworkElement;
        private readonly Storyboard desktopShrinkSB = new Storyboard();
		private readonly Storyboard desktopExpandSB = new Storyboard();
		private readonly Storyboard menuShrinkSB= new Storyboard();
        private readonly Storyboard menuExpandSB = new Storyboard();
        private readonly Storyboard addItemWindowShowSB = new Storyboard();
        private readonly Storyboard addItemWindowHideSB = new Storyboard();
        private readonly Storyboard contextMenuShowSB = new Storyboard();
		private readonly Storyboard contextMenuHideSB = new Storyboard();

		private readonly Dictionary<string, Storyboard> storyboards = new Dictionary<string, Storyboard>();

		private readonly ExponentialEase exponentialEaseIn = new ExponentialEase
		{
			EasingMode = EasingMode.EaseIn
		};
		private readonly ExponentialEase exponentialEaseOut = new ExponentialEase
		{
			EasingMode = EasingMode.EaseOut
		};

		private TimeSpan addItemWindowSHAnimDuration = TimeSpan.FromSeconds(0.3);
		private TimeSpan windowResizeDuration;

		private Point MenuGridResizeArea = new Point(31, 491);

		/// <summary>
		/// Initializes animation manager
		/// </summary>
		/// <param name="frameworkElement">Current application window</param>
		/// <param name="resizeDuration">Duration of expand/shrink animations</param>
		/// <param name="resizeArea">Point to store resize area. X stores shrinked window width, Y - expanded window width</param>
        public void Initiallize(MainWindow mainWindow, TimeSpan resizeDuration, Point resizeArea)
        {
			this.mainWindow = mainWindow;
            frameworkElement = mainWindow;
			windowResizeDuration = resizeDuration;

			mainWindow.RefreshControlsAfterThemeChanging();

			var desktopWidthDA = CreateDoubleAnimation(resizeArea.X, resizeDuration, "WindowBorder", "Width", exponentialEaseIn);
            var settingsWidthDA = CreateDoubleAnimation(MenuGridResizeArea.X, resizeDuration, "MenuGrid", "Width", exponentialEaseIn);
            var opacityDA = CreateDoubleAnimation(0, resizeDuration, "DesktopLV", "Opacity", exponentialEaseIn);
			var addItemWindowDA = CreateDoubleAnimation(1, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEaseIn);
            var contextMenuDA = CreateDoubleAnimation(1, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEaseIn);
			var menuShadowOpacityDA = CreateDoubleAnimation(0, resizeDuration, "MenuGrid", "(Effect).Opacity", exponentialEaseIn);

			var menuRectCA = CreateMenuVerticalRectExpandAnimation();

            desktopShrinkSB.Children.Add(desktopWidthDA);
            desktopShrinkSB.Children.Add(opacityDA);

			menuShrinkSB.Children.Add(settingsWidthDA);
            menuExpandSB.Children.Add(opacityDA);
            menuExpandSB.Children.Add(menuShadowOpacityDA);
            menuExpandSB.Children.Insert(0, menuRectCA);

            addItemWindowShowSB.Children.Add(addItemWindowDA);
            contextMenuShowSB.Children.Add(contextMenuDA);

            desktopWidthDA = CreateDoubleAnimation(resizeArea.Y, resizeDuration, "WindowBorder", "Width", exponentialEaseOut);
			settingsWidthDA = CreateDoubleAnimation(MenuGridResizeArea.Y, resizeDuration, "MenuGrid", "Width", exponentialEaseOut);
			opacityDA = CreateDoubleAnimation(1, resizeDuration, "DesktopLV", "Opacity", exponentialEaseOut);
			menuShadowOpacityDA = CreateDoubleAnimation(0.5, resizeDuration, "MenuGrid", "(Effect).Opacity", exponentialEaseOut);
            addItemWindowDA = CreateDoubleAnimation(0, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEaseOut);
            contextMenuDA = CreateDoubleAnimation(0, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEaseOut);

			menuRectCA = CreateMenuVerticalRectShrinkAnimation();


			desktopExpandSB.Children.Add(desktopWidthDA);
            desktopExpandSB.Children.Add(opacityDA);

			menuExpandSB.Children.Add(settingsWidthDA);
			menuShrinkSB.Children.Add(opacityDA);
			menuShrinkSB.Children.Add(menuShadowOpacityDA);
			menuShrinkSB.Children.Insert(0, menuRectCA);

			addItemWindowHideSB.Children.Add(addItemWindowDA);
            contextMenuHideSB.Children.Add(contextMenuDA);

			FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
			foreach ((FieldInfo item, object value) in from item in fields let value = item.GetValue(this) where value is Storyboard select (item, value))
			{
				storyboards.Add(item.Name, (Storyboard)value);
			}
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
			{
				desktopShrinkSB.Begin(frameworkElement);
			}
			else
			{
				desktopExpandSB.Begin(frameworkElement);
			}
		}

		private ColorAnimation CreateMenuVerticalRectShrinkAnimation()
		{
			return CreateColorAnimation((mainWindow.FindResource("CustomWindow.TitleBar.Background") as SolidColorBrush).Color, windowResizeDuration, "SettingsVerticalRect", "(Rectangle.Fill).(SolidColorBrush.Color)", exponentialEaseOut);
		}

		private ColorAnimation CreateMenuVerticalRectExpandAnimation()
		{
			return CreateColorAnimation((mainWindow.FindResource("SettingVerticalRect.ExpandBackground") as SolidColorBrush).Color, windowResizeDuration, "SettingsVerticalRect", "(Rectangle.Fill).(SolidColorBrush.Color)", exponentialEaseIn);
		}

		public void ToggleMenu(bool value)
        {
			if (value)
			{
				menuShrinkSB.Children.RemoveAt(0);
				menuShrinkSB.Children.Insert(0, CreateMenuVerticalRectShrinkAnimation());
				menuShrinkSB.Begin(frameworkElement);
			}
			else
			{
				menuExpandSB.Children.RemoveAt(0);
				menuExpandSB.Children.Insert(0, CreateMenuVerticalRectExpandAnimation());
				menuExpandSB.Begin(frameworkElement);
			}
        }

		public void ExpandSettingGrid()
		{
			var animation = new DoubleAnimation(1.0, windowResizeDuration)
			{
				EasingFunction = exponentialEaseOut
			};
			mainWindow.SettingGrid.Visibility = Visibility.Visible;
			mainWindow.SettingGrid.BeginAnimation(UIElement.OpacityProperty, animation);
		}

		public void ExpandThemeGrid()
		{
			var animation = new DoubleAnimation(1.0, windowResizeDuration)
			{
				EasingFunction = exponentialEaseOut
			};
			mainWindow.ThemeGrid.Visibility = Visibility.Visible;
			mainWindow.ThemeGrid.BeginAnimation(UIElement.OpacityProperty, animation);
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

		public void SetStoryboardCompletedCallback(string key, Action callback)
		{
			if (storyboards.ContainsKey(key))
				storyboards[key].Completed += (sender, e) => callback();
		}

		public void SetStoryboardCompletedCallback(Action callback, params string[] keys)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				var key = keys[i];
				if (storyboards.ContainsKey(key))
					storyboards[key].Completed += (sender, e) => callback();
			}
		}

		public void ListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			e.Handled = true;

			ScrollViewer scrollViewer = ScrollAnimationBehavior.GetScrollViewer(sender as ListView) as ScrollViewer;
			scrollViewer.IsDeferredScrollingEnabled = true;
			int offset = 200;

			DoubleAnimation verticalAnimation = new DoubleAnimation
			{
				To = scrollViewer.VerticalOffset - (offset * Math.Sign(e.Delta)),
				Duration = TimeSpan.FromSeconds(0.4),
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
			if (target is ScrollViewer scrollViewer)
			{
				scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
			}
		}

		public static DependencyObject GetScrollViewer(DependencyObject o)
		{
			if (o is ScrollViewer)
				return o;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);

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
