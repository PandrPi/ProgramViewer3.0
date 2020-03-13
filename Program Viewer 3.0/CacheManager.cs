using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Program_Viewer_3
{
	/// <summary>
	/// This class is used for cache files icons. It is neccessary because extracting them directly from files can be only executed on main thread, which causes freezes.
	/// </summary>
	public sealed class CacheManager
	{
		private Dictionary<string, ImageSource> cachedIcons = new Dictionary<string, ImageSource>();


		private static readonly string IconsCacheFolder = "IconsCache";
		private static readonly string SourceIconsFolder = $"{IconsCacheFolder}/SourceIcons";
		private static readonly string CacheJSON = $"{IconsCacheFolder}/cacheFilesNames.json";
		private static readonly string CacheZip = $"{IconsCacheFolder}/cachedImages.zip";

		private static readonly Point[] RandomCharsEntry = { new Point(26, 65), new Point(26, 97), new Point(10, 48) };


		public CacheManager()
		{
			InitiallizeDirectory(IconsCacheFolder);
			InitiallizeDirectory(SourceIconsFolder);

			InitializeJSONFile(CacheJSON);
			if (!File.Exists(CacheZip))
				ZipFile.CreateFromDirectory(SourceIconsFolder, CacheZip);

			InitiallizeCacheDictionary();
		}

		/// <summary>
		/// This method is used to pack all items icons to zip archive(cache them)
		/// </summary>
		/// <param name="hotItems"></param>
		/// <param name="desktopItems"></param>
		public void PackIcons(ObservableCollection<ItemData> hotItems, ObservableCollection<ItemData> desktopItems)
		{
			Dictionary<string, dynamic> cache = new Dictionary<string, dynamic>();
			Parallel.For(0, hotItems.Count, (i) =>
			{
				string randomName = GetRandomString();
				while (cache.ContainsKey(randomName))
				{
					randomName = GetRandomString();
				}
				var item = hotItems[i];
				cache.Add(randomName, item.Path);
				SaveImageToFile($"{SourceIconsFolder}/{randomName}.png", item.ImageData as BitmapSource);
			});

			Parallel.For(0, desktopItems.Count, (i) =>
			{
				string randomName = GetRandomString();
				while (cache.ContainsKey(randomName))
				{
					randomName = GetRandomString();
				}
				var item = desktopItems[i];
				cache.Add(randomName, item.Path);
				SaveImageToFile($"{SourceIconsFolder}/{randomName}.png", item.ImageData as BitmapSource);
			});

			if (File.Exists(CacheZip))
				File.Delete(CacheZip);

			ZipFile.CreateFromDirectory(SourceIconsFolder, CacheZip);
			string json = JsonConvert.SerializeObject(cache, Formatting.Indented);
			File.WriteAllText(CacheJSON, json);

			DeleteAllFilesFromDirectory(SourceIconsFolder);
		}

		/// <summary>
		/// Returns a file's associated icon. If icon is cached method will return it, else method will extract icon from file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImageSource GetFileIcon(string path)
		{
			if (cachedIcons.ContainsKey(path))
			{
				return cachedIcons[path];
			}
			else
			{
				return IconExtractor.GetIcon(path);
			}
		}

		/// <summary>
		/// Creates directory if it doesn't exist
		/// </summary>
		/// <param name="path"></param>
		public static DirectoryInfo InitiallizeDirectory(string path)
		{
			if (!Directory.Exists(path))
			{
				return Directory.CreateDirectory(path);
			}
			else
			{
				return new DirectoryInfo(path);
			}
		}

		/// <summary>
		/// Creates an empty json file and fill it with basic json structure
		/// </summary>
		/// <param name="path"></param>
		public static void InitializeJSONFile(string path)
		{
			bool temp = false;
			if (!File.Exists(path))
			{
				temp = true;
			}
			else
			{
				string fileContent = File.ReadAllText(path);
				temp = !(fileContent.Contains("{") && fileContent.Contains("}"));
			}
			if (temp)
			{
				File.WriteAllText(path, string.Empty);
				using (StreamWriter sw = File.CreateText(path))
				{
					sw.WriteLine("{"); sw.WriteLine(""); sw.WriteLine("}");
				}
			}
		}

		/// <summary>
		/// Deletes all files from directory
		/// </summary>
		/// <param name="path">Directory path</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DeleteAllFilesFromDirectory(string path)
		{
			FileInfo[] fileInfos = new DirectoryInfo(path).GetFiles();
			for(int i = 0; i < fileInfos.Length; i++)
			{
				File.Delete(fileInfos[i].FullName);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InitiallizeCacheDictionary()
		{
			ZipFile.ExtractToDirectory(CacheZip, SourceIconsFolder);
			var cacheData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(CacheJSON));
			FileInfo[] fileInfos = new DirectoryInfo(SourceIconsFolder).GetFiles();
			for(int i = 0; i < fileInfos.Length; i++)
			{
				string nameWithoutExt = Path.GetFileNameWithoutExtension(fileInfos[i].Name);
				if (cacheData.ContainsKey(nameWithoutExt))
					cachedIcons.Add(cacheData[nameWithoutExt], LoadImageFromFile(fileInfos[i].FullName));
				File.Delete(fileInfos[i].FullName);
			}
		}

		/// <summary>
		/// Load BitmapSource object from file and returns it.
		/// </summary>
		/// <param name="path">Path of image file to load from.</param>
		/// <returns></returns>
		private BitmapSource LoadImageFromFile(string path)
		{
			Bitmap bmp = Bitmap.FromFile(path) as Bitmap;
			BitmapSource image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(bmp.GetHicon(),
						new System.Windows.Int32Rect(0, 0, bmp.Width, bmp.Height), BitmapSizeOptions.FromEmptyOptions());
			image.Freeze();
			bmp.Dispose();

			return image;
		}

		/// <summary>
		/// Saves a BitmapSource object to an png image file
		/// </summary>
		/// <param name="path">Path of image file to save</param>
		/// <param name="image">Image object to save</param>
		private void SaveImageToFile(string path, BitmapSource image)
		{
			using (var fileStream = new FileStream(path, FileMode.Create))
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(image));
				encoder.Save(fileStream);
			}
		}

		/// <summary>
		/// Generates random string.
		/// </summary>
		/// <returns></returns>
		public static string GetRandomString()
		{
			int stringLength = 64;
			System.Text.StringBuilder sb = new System.Text.StringBuilder(stringLength);
			Random random = new Random(Guid.NewGuid().GetHashCode());

			for (int i = 0; i < stringLength; i++)
			{
				var p = RandomCharsEntry[random.Next(0, 3)];
				sb.Append(Convert.ToChar(Convert.ToInt32(Math.Floor(p.X * random.NextDouble() + p.Y))));
			}

			return sb.ToString();
		}
	}
}
