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

        public AnimationManager(FrameworkElement frameworkElement, TimeSpan duration, Point expandArea)
        {
            this.frameworkElement = frameworkElement;

            shirkSB = new Storyboard();

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

            shirkSB.Children.Add(widthDA);
            shirkSB.Children.Add(opacityDA);


            exponentialEase.EasingMode = EasingMode.EaseOut;
            expandSB = new Storyboard();

            widthDA = new DoubleAnimation(expandArea.X, expandArea.Y, duration);
            widthDA.SetValue(Storyboard.TargetNameProperty, "WindowBorder");
            Storyboard.SetTargetProperty(widthDA, new PropertyPath(FrameworkElement.WidthProperty));
            widthDA.EasingFunction = exponentialEase;

            opacityDA = new DoubleAnimation(0, 1, duration);
            opacityDA.SetValue(Storyboard.TargetNameProperty, "DesktopLV");
            Storyboard.SetTargetProperty(opacityDA, new PropertyPath(FrameworkElement.OpacityProperty));
            opacityDA.EasingFunction = exponentialEase;

            expandSB.Children.Add(widthDA);
            expandSB.Children.Add(opacityDA);
        }

        public void ShirkDesktop()
        {
            shirkSB.Begin(frameworkElement);
        }

        public void ExpandDesktop()
        {
            expandSB.Begin(frameworkElement);
        }
    }
}
