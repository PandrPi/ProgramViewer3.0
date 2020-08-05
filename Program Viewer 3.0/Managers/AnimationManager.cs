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
		private readonly Storyboard menuShrinkSB = new Storyboard();
		private readonly Storyboard menuExpandSB = new Storyboard();
		//private readonly Storyboard addItemWindowShowSB = new Storyboard();
  //      private readonly Storyboard addItemWindowHideSB = new Storyboard();
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

		public TimeSpan addItemWindowSHAnimDuration = TimeSpan.FromSeconds(0.3);
		public TimeSpan WindowResizeDuration;
		public Point MenuGridResizeArea = new Point(31, 491);
		public Point WindowResizeArea;

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
			WindowResizeDuration = resizeDuration;
			WindowResizeArea = resizeArea;

			mainWindow.RefreshControlsAfterThemeChanging();

            var settingsWidthDA = CreateDoubleAnimation(MenuGridResizeArea.X, resizeDuration, "MenuGrid", "Width", exponentialEaseIn);
            var opacityDA = CreateDoubleAnimation(0, resizeDuration, "DesktopGridPanel", "Opacity", exponentialEaseIn);
            var contextMenuDA = CreateDoubleAnimation(1, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEaseIn);
			var menuShadowOpacityDA = CreateDoubleAnimation(0, resizeDuration, "MenuGrid", "(Effect).Opacity", exponentialEaseIn);

			var menuRectCA = CreateMenuVerticalRectExpandAnimation();

			menuShrinkSB.Children.Add(settingsWidthDA);
            menuExpandSB.Children.Add(opacityDA);
            menuExpandSB.Children.Add(menuShadowOpacityDA);
            menuExpandSB.Children.Insert(0, menuRectCA);

            contextMenuShowSB.Children.Add(contextMenuDA);

			settingsWidthDA = CreateDoubleAnimation(MenuGridResizeArea.Y, resizeDuration, "MenuGrid", "Width", exponentialEaseOut);
			opacityDA = CreateDoubleAnimation(1, resizeDuration, "DesktopGridPanel", "Opacity", exponentialEaseOut);
			menuShadowOpacityDA = CreateDoubleAnimation(0.5, resizeDuration, "MenuGrid", "(Effect).Opacity", exponentialEaseOut);
            contextMenuDA = CreateDoubleAnimation(0, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEaseOut);

			menuRectCA = CreateMenuVerticalRectShrinkAnimation();

			menuExpandSB.Children.Add(settingsWidthDA);
			menuShrinkSB.Children.Add(opacityDA);
			menuShrinkSB.Children.Add(menuShadowOpacityDA);
			menuShrinkSB.Children.Insert(0, menuRectCA);

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

		public static void StartAnimation_FadeIn(FrameworkElement element, Duration duration)
		{
			DoubleAnimation animation = new DoubleAnimation(element.Opacity, 1.0, duration)
			{
				EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
			};
			element.BeginAnimation(UIElement.OpacityProperty, animation);
		}
		public static void StartAnimation_FadeIn(FrameworkElement element, Duration duration, Action callback)
		{
			DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, duration)
			{
				EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
			};
			animation.Completed += (sender, e) => callback();
			element.BeginAnimation(UIElement.OpacityProperty, animation);
		}

		public static void StartAnimation_FadeOut(FrameworkElement element, Duration duration)
		{
			DoubleAnimation animation = new DoubleAnimation(0.0, duration)
			{
				EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
			};
			element.BeginAnimation(UIElement.OpacityProperty, animation);
		}
		public static void StartAnimation_FadeOut(FrameworkElement element, Duration duration, Action callback)
		{
			DoubleAnimation animation = new DoubleAnimation(1.0, 0.0, duration)
			{
				EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
			};
			animation.Completed += (sender, e) => callback();
			element.BeginAnimation(UIElement.OpacityProperty, animation);
		}

		public static void StartAnimation_Double(FrameworkElement target, DependencyProperty property, double from, double to, Duration duration)
		{
			target.BeginAnimation(property, null);
			DoubleAnimation animation = new DoubleAnimation
			{
				From = from,
				To = to,
				Duration = duration
			};

			target.BeginAnimation(property, animation);
		}
		public static void StartAnimation_DoubleTransform(Transform target, DependencyProperty property, double to, Duration duration)
		{
			target.BeginAnimation(property, null);
			DoubleAnimation animation = new DoubleAnimation
			{
				To = to,
				Duration = duration
			};

			target.BeginAnimation(property, animation);
		}

		public void ToggleDesktop(bool value)
		{
			if (value)
			{
				StartAnimation_Double(mainWindow.WindowBorder, Border.WidthProperty,
					mainWindow.WindowBorder.Width, WindowResizeArea.X, WindowResizeDuration);
				StartAnimation_FadeOut(mainWindow.DesktopGridPanel, WindowResizeDuration);
			}
			else
			{
				StartAnimation_Double(mainWindow.WindowBorder, Border.WidthProperty,
					mainWindow.WindowBorder.Width, WindowResizeArea.Y, WindowResizeDuration);
				StartAnimation_FadeIn(mainWindow.DesktopGridPanel, WindowResizeDuration);
			}
		}

		private ColorAnimation CreateMenuVerticalRectShrinkAnimation()
		{
			return CreateColorAnimation((mainWindow.FindResource("CustomWindow.TitleBar.Background") as SolidColorBrush).Color, WindowResizeDuration, "SettingsVerticalRect", "(Rectangle.Fill).(SolidColorBrush.Color)", exponentialEaseOut);
		}

		private ColorAnimation CreateMenuVerticalRectExpandAnimation()
		{
			return CreateColorAnimation((mainWindow.FindResource("SettingVerticalRect.ExpandBackground") as SolidColorBrush).Color, WindowResizeDuration, "SettingsVerticalRect", "(Rectangle.Fill).(SolidColorBrush.Color)", exponentialEaseIn);
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
			mainWindow.SettingGrid.Visibility = Visibility.Visible;
			StartAnimation_FadeIn(mainWindow.SettingGrid, WindowResizeDuration);
		}

		public void ExpandThemeGrid()
		{
			mainWindow.ThemeGrid.Visibility = Visibility.Visible;
			StartAnimation_FadeIn(mainWindow.ThemeGrid, WindowResizeDuration);
		}

        public void AddItemWindowShow()
        {
			StartAnimation_FadeIn(mainWindow.AddItemGrid, addItemWindowSHAnimDuration, () => mainWindow.AddItemGrid.Visibility = Visibility.Visible);
        }
        public void AddItemWindowHide()
        {
			StartAnimation_FadeOut(mainWindow.AddItemGrid, addItemWindowSHAnimDuration, () => mainWindow.AddItemGrid.Visibility = Visibility.Hidden);
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

		public static void ListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			e.Handled = true;

			ScrollViewer scrollViewer = ScrollAnimationBehavior.GetScrollViewer((DependencyObject)sender) as ScrollViewer;
			int offset = 240;
			AnimateScroll(scrollViewer, scrollViewer.VerticalOffset - (offset * Math.Sign(e.Delta)));
		}
		private static void AnimateScroll(ScrollViewer scrollViewer, double ToValue)
		{
			scrollViewer.BeginAnimation(ScrollAnimationBehavior.VerticalOffsetProperty, null);
			DoubleAnimation verticalAnimation = new DoubleAnimation
			{
				From = scrollViewer.VerticalOffset,
				To = ToValue,
				Duration = TimeSpan.FromSeconds(0.7),
				EasingFunction = new ExponentialEase
				{
					EasingMode = EasingMode.EaseOut,
					Exponent = 1.2
				}
			};
			scrollViewer.BeginAnimation(ScrollAnimationBehavior.VerticalOffsetProperty, verticalAnimation);
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
