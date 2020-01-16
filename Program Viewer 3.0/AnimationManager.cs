using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Program_Viewer_3
{
    public class AnimationManager
    {
        private FrameworkElement frameworkElement;
        private Storyboard shirkSB               = new Storyboard();
        private Storyboard expandSB              = new Storyboard();
        private Storyboard addItemWindowShowSB   = new Storyboard();
        private Storyboard addItemWindowHideSB   = new Storyboard();
        private Storyboard contextMenuShowSB   = new Storyboard();
        private Storyboard contextMenuHideSB = new Storyboard();

        private TimeSpan addItemWindowSHAnimDuration = TimeSpan.FromSeconds(0.3);

        public AnimationManager(FrameworkElement frameworkElement, TimeSpan duration, Point expandArea)
        {
            this.frameworkElement = frameworkElement;

            ExponentialEase exponentialEase = new ExponentialEase();
            exponentialEase.EasingMode = EasingMode.EaseIn;
            DoubleAnimation widthDA = CreateDoubleAnimation(expandArea.Y, expandArea.X, duration, "WindowBorder", "Width", exponentialEase);
            DoubleAnimation opacityDA = CreateDoubleAnimation(1, 0, duration, "DesktopLV", "Opacity", exponentialEase);
            DoubleAnimation addItemWindowDA = CreateDoubleAnimation(0, 1, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEase);
            DoubleAnimation contextMenuDA = CreateDoubleAnimation(0, 1, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEase);

            shirkSB.Children.Add(widthDA);
            shirkSB.Children.Add(opacityDA);
            addItemWindowShowSB.Children.Add(addItemWindowDA);
            contextMenuShowSB.Children.Add(contextMenuDA);

            exponentialEase.EasingMode = EasingMode.EaseOut;
            widthDA = CreateDoubleAnimation(expandArea.X, expandArea.Y, duration, "WindowBorder", "Width", exponentialEase);
            opacityDA = CreateDoubleAnimation(0, 1, duration, "DesktopLV", "Opacity", exponentialEase);
            addItemWindowDA = CreateDoubleAnimation(1, 0, addItemWindowSHAnimDuration, "AddItemGrid", "Opacity", exponentialEase);
            contextMenuDA = CreateDoubleAnimation(1, 0, addItemWindowSHAnimDuration, "PiContextMenu", "Opacity", exponentialEase);

            expandSB.Children.Add(widthDA);
            expandSB.Children.Add(opacityDA);
            addItemWindowHideSB.Children.Add(addItemWindowDA);
            contextMenuHideSB.Children.Add(contextMenuDA);
        }

        private DoubleAnimation CreateDoubleAnimation(double from, double to, TimeSpan duration,
            string targetProperty, string propertyPath, IEasingFunction easingFunction)
        {
            DoubleAnimation da = CreateDoubleAnimation(from, to, duration, targetProperty, propertyPath);
            da.EasingFunction = easingFunction;

            return da;
        }

        private DoubleAnimation CreateDoubleAnimation(double from, double to, TimeSpan duration,
            string targetProperty, string propertyPath)
        {
            DoubleAnimation da = new DoubleAnimation(from, to, duration);
            da.SetValue(Storyboard.TargetNameProperty, targetProperty);
            Storyboard.SetTargetProperty(da, new PropertyPath(propertyPath));

            return da;
        }

        public void ShirkDesktop()
        {
            shirkSB.Begin(frameworkElement);
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
    }
}
