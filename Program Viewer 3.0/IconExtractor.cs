using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.Windows.Interop;

namespace ProgramViewer3
{
	public static class IconExtractor
	{
		private static ImageSource BaseExeIcon { get; set; }
		private static Dispatcher Dispatcher { get; set; }

		private static readonly int DefaultIconSize = 256;
		private static readonly HashSet<string> imageExtensions = new HashSet<string>(new[] { ".png", ".jpg",
			".gif", ".bmp", ".jpeg", ".tga", ".tiff", ".psd", ".pdf" });

		public static void Initialize(ImageSource baseExeIcon, Dispatcher dispatcher)
		{
			BaseExeIcon = baseExeIcon;
			if (BaseExeIcon.CanFreeze) BaseExeIcon.Freeze();
			Dispatcher = dispatcher;
		}

		public static ImageSource GetIcon(string filePath)
		{
			string fileExtension = Path.GetExtension(filePath);

			// If the specified file is a link we have to extract its target file
			if (fileExtension == ".lnk")
			{
				var tempIcon = LoadIcon(filePath);
				if (tempIcon != BaseExeIcon)
					return tempIcon;

				filePath = GetShortcutTargetFile(Path.GetFullPath(filePath));
				fileExtension = Path.GetExtension(filePath);
			}

			if (fileExtension == ".exe")
			{
				try
				{
					return LoadIcon(filePath);
				}
				catch
				{
					return BaseExeIcon;
				}
			}
			else
			{
				if (imageExtensions.Contains(fileExtension))
				{
					// Our file is an image file
					return LoadIconFromImageFile(filePath);
				}
				else
				{
					// Our file is not an image file
					BitmapSource image = LoadIcon(filePath);
					return image;
				}
			}
		}

		private static BitmapSource LoadIcon(string filePath)
		{
			Icon icon = null;
			Dispatcher.Invoke(() => icon = FileToIconConverter.GetFileIcon(filePath, FileToIconConverter.IconSize.jumbo));
			BitmapSource image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
				new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
			if (image.CanFreeze) image.Freeze();
			icon?.Dispose();

			return image;
		}

		private static ImageSource LoadIconFromImageFile(string filePath)
		{
			Bitmap originalImage = null;
			Bitmap resizedImage = null;
			try
			{
				originalImage = new Bitmap(filePath);
				float aspectRatio = originalImage.Width / (float)originalImage.Height;
				int newWidth = DefaultIconSize;
				int newHeight = (int)(DefaultIconSize / aspectRatio);
				resizedImage = new Bitmap(originalImage, newWidth, newHeight);

				IntPtr hBitmap = resizedImage.GetHbitmap();
				ImageSource resultImage = Imaging.CreateBitmapSourceFromHBitmap(hBitmap,
					IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				if (resultImage.CanFreeze) resultImage.Freeze();

				return resultImage;
			}
			catch
			{
				return null;
			}
			finally
			{
				originalImage?.Dispose();
				resizedImage?.Dispose();
			}
		}

		/// <summary>
		/// This method extracts the target file path from the spetified shortcut (*.lnk) file path
		/// </summary>
		/// <param name="shortcutPath">The path ro the shortcut file</param>
		/// <returns>Target file path of the shortcut</returns>
		private static string GetShortcutTargetFile(string shortcutPath)
		{
			IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
			IWshRuntimeLibrary.IWshShortcut link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);

			return link.TargetPath;
		}
	}
}
