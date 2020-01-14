using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Program_Viewer_3
{
    public class AnimationManager
    {
        private FrameworkElement frameworkElement;
        private Storyboard shirkSB;
        private Storyboard expandSB;
        private Storyboard addItemWindowShowSB;
        private Storyboard addItemWindowHideSB;
        private TimeSpan addItemWindowSHAnimDuration = TimeSpan.FromSeconds(0.3);

        public AnimationManager(FrameworkElement frameworkElement, TimeSpan duration, Point expandArea)
        {
            this.frameworkElement = frameworkElement;

            shirkSB = new Storyboard();
            expandSB = new Storyboard();
            addItemWindowShowSB = new Storyboard();
            addItemWindowHideSB = new Storyboard();

            ExponentialEase exponentialEase = new ExponentialEase();
            exponentialEase.EasingMode = EasingMode.EaseIn;

            DoubleAnimation widthDA = new DoubleAnimation(expandArea.Y, expandArea.X, duration);
            widthDA.SetValue(Storyboard.TargetNameProperty, "WindowBorder");
            Storyboard.SetTargetProperty(widthDA, new PropertyPath(FrameworkElement.WidthProperty));
            widthDA.EasingFunction = exponentialEase;

            DoubleAnimation opacityDA = new DoubleAnimation(1, 0, duration);
            opacityDA.SetValue(Storyboard.TargetNameProperty, "DesktopLV");
            Storyboard.SetTargetProperty(opacityDA, new PropertyPath(FrameworkElement.OpacityProperty));
            opacityDA.EasingFunction = exponentialEase;

            DoubleAnimation addItemWindowDA = new DoubleAnimation(0, 1, addItemWindowSHAnimDuration);
            addItemWindowDA.SetValue(Storyboard.TargetNameProperty, "AddItemGrid");
            Storyboard.SetTargetProperty(addItemWindowDA, new PropertyPath(FrameworkElement.OpacityProperty));
            addItemWindowDA.EasingFunction = exponentialEase;

            shirkSB.Children.Add(widthDA);
            shirkSB.Children.Add(opacityDA);
            addItemWindowShowSB.Children.Add(addItemWindowDA);

            exponentialEase.EasingMode = EasingMode.EaseOut;

            widthDA = new DoubleAnimation(expandArea.X, expandArea.Y, duration);
            widthDA.SetValue(Storyboard.TargetNameProperty, "WindowBorder");
            Storyboard.SetTargetProperty(widthDA, new PropertyPath(FrameworkElement.WidthProperty));
            widthDA.EasingFunction = exponentialEase;

            opacityDA = new DoubleAnimation(0, 1, duration);
            opacityDA.SetValue(Storyboard.TargetNameProperty, "DesktopLV");
            Storyboard.SetTargetProperty(opacityDA, new PropertyPath(FrameworkElement.OpacityProperty));
            opacityDA.EasingFunction = exponentialEase;

            addItemWindowDA = new DoubleAnimation(1, 0, addItemWindowSHAnimDuration);
            addItemWindowDA.SetValue(Storyboard.TargetNameProperty, "AddItemGrid");
            Storyboard.SetTargetProperty(addItemWindowDA, new PropertyPath(FrameworkElement.OpacityProperty));
            addItemWindowDA.EasingFunction = exponentialEase;

            expandSB.Children.Add(widthDA);
            expandSB.Children.Add(opacityDA);
            addItemWindowHideSB.Children.Add(addItemWindowDA);
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

        public void SetAddItemWindowShowCallback(Action callback)
        {
            addItemWindowShowSB.Completed += (sender, e) => callback();
        }
        public void SetAddItemWindowHideCallback(Action callback)
        {
            addItemWindowHideSB.Completed += (sender, e) => callback();
        }
    }
}
