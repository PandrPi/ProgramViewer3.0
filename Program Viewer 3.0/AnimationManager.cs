using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Program_Viewer_3
{
    public class AnimationManager
    {
        private FrameworkElement frameworkElement;
        private Storyboard shrinkSB;
        private Storyboard expandSB;
        private Storyboard addItemWindowShowSB;
        private Storyboard addItemWindowHideSB;
        private Storyboard contextMenuShowSB;
        private Storyboard contextMenuHideSB;

        private TimeSpan addItemWindowSHAnimDuration;

        public AnimationManager()
        {
            shrinkSB = new Storyboard();
            expandSB = new Storyboard();
            addItemWindowShowSB = new Storyboard();
            addItemWindowHideSB = new Storyboard();
            contextMenuShowSB = new Storyboard();
            contextMenuHideSB = new Storyboard();
            addItemWindowSHAnimDuration = TimeSpan.FromSeconds(0.3);
        }

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
            DoubleAnimation widthDA = CreateDoubleAnimation(resizeArea.Y, resizeArea.X, resizeDuration, "WindowBorder", "Width", exponentialEase);
            DoubleAnimation opacityDA = CreateDoubleAnimation(1, 0, resizeDuration, "DesktopLV", "Opacity", exponentialEase);
            DoubleAnimation addItemWindowDA = CreateDoubleAnimation(0, 1, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEase);
            DoubleAnimation contextMenuDA = CreateDoubleAnimation(0, 1, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEase);

            shrinkSB.Children.Add(widthDA);
            shrinkSB.Children.Add(opacityDA);
            addItemWindowShowSB.Children.Add(addItemWindowDA);
            contextMenuShowSB.Children.Add(contextMenuDA);

            exponentialEase.EasingMode = EasingMode.EaseOut;
            widthDA = CreateDoubleAnimation(resizeArea.X, resizeArea.Y, resizeDuration, "WindowBorder", "Width", exponentialEase);
            opacityDA = CreateDoubleAnimation(0, 1, resizeDuration, "DesktopLV", "Opacity", exponentialEase);
            addItemWindowDA = CreateDoubleAnimation(1, 0, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEase);
            contextMenuDA = CreateDoubleAnimation(1, 0, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEase);

            expandSB.Children.Add(widthDA);
            expandSB.Children.Add(opacityDA);
            addItemWindowHideSB.Children.Add(addItemWindowDA);
            contextMenuHideSB.Children.Add(contextMenuDA);
        }

		/// <summary>
		/// Medhod to create new Double Animation object
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="duration"></param>
		/// <param name="targetProperty"></param>
		/// <param name="propertyPath"></param>
		/// <param name="easingFunction"></param>
		/// <returns></returns>
        private DoubleAnimation CreateDoubleAnimation(double from, double to, TimeSpan duration,
            string targetProperty, string propertyPath, IEasingFunction easingFunction)
        {
            DoubleAnimation da = new DoubleAnimation(from, to, duration);
            da.SetValue(Storyboard.TargetNameProperty, targetProperty);
            Storyboard.SetTargetProperty(da, new PropertyPath(propertyPath));
            da.EasingFunction = easingFunction;

            return da;
        }

        public void ShrinkDesktop()
        {
            shrinkSB.Begin(frameworkElement);
        }

        public void ExpandDesktop()
        {
            expandSB.Begin(frameworkElement);
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
