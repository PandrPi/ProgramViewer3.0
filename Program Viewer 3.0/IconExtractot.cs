﻿using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickZip.Tools;

namespace Program_Viewer_3
{
    public static class IconExtractor
    {
        public static ImageSource BaseExeIcon;
        public static Dispatcher Dispatcher;

        private static Dictionary<string, ImageSource> cachedImages = new Dictionary<string, ImageSource>();
        private static readonly HashSet<string> imageExtensions = 
            new HashSet<string>(new [] { ".png", ".jpg", ".gif", ".bmp", ".jpeg", ".tga", ".tiff", ".psd", ".pdf" });

        public static ImageSource GetIcon(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if (extension == ".lnk")
            {
				var tempIcon = LoadIcon(fileName);
				if (tempIcon != BaseExeIcon)
					return tempIcon;

                fileName = GetShortcutTargetFile(Path.GetFullPath(fileName));
                extension = Path.GetExtension(fileName);
            }

            if (extension == ".exe")
            {
                try
                {
                    return LoadIcon(fileName);
				}
                catch
                {
                    return BaseExeIcon;
                }
            }
            else
            {
                if (cachedImages.ContainsKey(extension))
                {
                    return cachedImages[extension];
                }
                else
                {
					BitmapSource image = LoadIcon(fileName);

                    if (!imageExtensions.Contains(extension))
                        cachedImages.Add(extension, image);

                    return image;
                }
            }
        }

		private static BitmapSource LoadIcon(string fileName)
		{
			Icon icon = null;
			Dispatcher.Invoke(() =>
			{
				icon = FileToIconConverter.GetFileIcon(fileName, FileToIconConverter.IconSize.jumbo);
			});
			BitmapSource image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
						new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
			image.Freeze();
			icon.Dispose();

			return image;
		}

        private static string GetShortcutTargetFile(string shortcutFilename)
        {
			IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
			IWshRuntimeLibrary.IWshShortcut link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutFilename);

            return link.TargetPath;
        }
	}
}
