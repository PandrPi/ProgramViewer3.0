using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text;

namespace ProgramViewer3
{
	public static class IconExtractor
	{
		private static ImageSource BaseExeIcon { get; set; }
		private static Dispatcher Dispatcher { get; set; }

		private const int DefaultIconSize = 256;
		private const int ThumbnailWidth = 64;
		private const int ThumbnailHeight = 64;
		private const string BaseExeIconHash = "36cef4168c8c3ad5871bb12c1150b57a004493c68442a948e09d46638cbac06d";

		private static readonly HashSet<string> imageExtensions = new HashSet<string>(new[] { ".png", ".jpg",
			".gif", ".bmp", ".jpeg", ".tga", ".tiff", ".psd", ".pdf" });

		public static void Initialize(ImageSource baseExeIcon, Dispatcher dispatcher)
		{
			BaseExeIcon = baseExeIcon;
			if (BaseExeIcon.CanFreeze) BaseExeIcon.Freeze();
			Dispatcher = dispatcher;
		}


		/// <summary>
		/// Extracts the icon from a file at sthe specified path
		/// </summary>
		/// <param name="filePath">The path to the file from which to load the icon</param>
		/// <param name="iconHash">The output icon hash string</param>
		public static ImageSource GetIcon(string filePath, out string iconHash)
		{
			string fileExtension = Path.GetExtension(filePath);

			// If the specified file is a link we have to extract its target file
			if (fileExtension == ".lnk")
			{
				var tempIcon = LoadIcon(filePath, out iconHash);
				if (tempIcon != BaseExeIcon)
				{
					return tempIcon;
				}

				filePath = GetShortcutTargetFile(Path.GetFullPath(filePath));
				fileExtension = Path.GetExtension(filePath);
			}


			if (fileExtension == ".exe")
			{
				// If the our file is executable
				try
				{
					return LoadIcon(filePath, out iconHash);
				}
				catch
				{
					// If we cannot load the file icon we have to return the BaseExeIcon object
					iconHash = BaseExeIconHash;
					return BaseExeIcon;
				}
			}
			else
			{
				if (imageExtensions.Contains(fileExtension))
				{
					// Our file is an image file
					return LoadIconFromImageFile(filePath, out iconHash);
				}
				else
				{
					// Our file is not an image file
					BitmapSource image = LoadIcon(filePath, out iconHash);
					return image;
				}
			}
		}

		/// <summary>
		/// Loads an associated icon from a file located in the specified filePath
		/// </summary>
		/// <param name="filePath">The path to the file from which to load the associated icon</param>
		/// <param name="iconHash">The output icon hash string</param>
		/// <returns></returns>
		private static BitmapSource LoadIcon(string filePath, out string iconHash)
		{
			Icon icon = null;
			// The LoadIcon method can be called from not the main thread but GetFileIcon have to be called only
			// from the main thread so we need to always use our Dispatcher object to invoke GetFileIcon method
			Dispatcher.Invoke(() => icon = FileToIconConverter.GetFileIcon(filePath));
			BitmapSource image = FileToIconConverter.LoadBitmap(icon);

			if (image.CanFreeze) image.Freeze();

			var tempBmp = icon.ToBitmap();
			var thumbnail = GetThumbnail(tempBmp);
			iconHash = GetImageHash(thumbnail);

			thumbnail?.Dispose();
			tempBmp?.Dispose();
			icon?.Dispose();

			return image;
		}


		/// <summary>
		/// Loads an icon from an image file located in the specified filePath
		/// </summary>
		/// <param name="filePath">The path to the image file from which to load the icon</param>
		/// <param name="iconHash">The output icon hash string</param>
		/// <returns></returns>
		private static ImageSource LoadIconFromImageFile(string filePath, out string iconHash)
		{
			Bitmap originalImage = null;
			Bitmap resizedImage = null;
			Bitmap thumbnail = null;
			try
			{
				originalImage = new Bitmap(filePath);
				float aspectRatio = originalImage.Width / (float)originalImage.Height;
				int newWidth = DefaultIconSize;
				int newHeight = (int)(DefaultIconSize / aspectRatio);
				resizedImage = new Bitmap(originalImage, newWidth, newHeight);

				ImageSource resultImage = FileToIconConverter.LoadBitmap(resizedImage);

				if (resultImage.CanFreeze) resultImage.Freeze();

				thumbnail = GetThumbnail(resizedImage);
				iconHash = GetImageHash(thumbnail);

				return resultImage;
			}
			catch
			{
				iconHash = string.Empty;
				return null;
			}
			finally
			{
				originalImage?.Dispose();
				resizedImage?.Dispose();
				thumbnail?.Dispose();
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

		/// <summary>
		/// Creates a thumbnail of the specified image object
		/// </summary>
		/// <param name="image">The source image for thumbnail</param>
		/// <returns></returns>
		private static Bitmap GetThumbnail(Bitmap image)
		{
			return new Bitmap(image, ThumbnailWidth, ThumbnailHeight);
		}

		/// <summary>
		/// Generates a hash string from the specified image object
		/// </summary>
		/// <param name="image">The image for which the hash is calculated</param>
		/// <returns>The hash string calculated for the specified image</returns>
		private static string GetImageHash(Bitmap image)
		{
			using (var hashAlgorithm = System.Security.Cryptography.SHA256.Create())
			{
				byte[] data = hashAlgorithm.ComputeHash(ImageToBytesArray(image));

				var sBuilder = new StringBuilder();

				for (int i = 0; i < data.Length; i++)
				{
					sBuilder.Append(data[i].ToString("x2"));
				}

				return sBuilder.ToString();
			}
		}

		/// <summary>
		/// Converts an image object to byte array
		/// </summary>
		/// <param name="image">Image for conversion</param>
		/// <returns>The resulting byte array</returns>
		private static byte[] ImageToBytesArray(Image image)
		{
			ImageConverter converter = new ImageConverter();
			return converter.ConvertTo(image, typeof(byte[])) as byte[];
		}
	}
}
