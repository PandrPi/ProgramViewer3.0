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


		private static readonly string IconsCacheFolder = Path.Combine(ItemManager.ApplicationPath, "IconsCache");
		private static readonly string SourceIconsFolder = Path.Combine(IconsCacheFolder, "SourceIcons");
		private static readonly string CacheJSON = Path.Combine(IconsCacheFolder, "cacheFilesNames.json");
		private static readonly string CacheZip = Path.Combine(IconsCacheFolder, "cachedImages.zip");

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
			LogManager.Write($"Icons packed successfully!");
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
				LogManager.Write($"Image [{path}] was returned from dictionary!");
				return cachedIcons[path];
			}
			else
			{
				LogManager.Write($"Image [{path}] was extracted from file!");
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
				LogManager.Write($"Directory created: {path}");
				return Directory.CreateDirectory(path);
			}
			else
			{
				LogManager.Write($"Directory exist: {path}");
				return new DirectoryInfo(path);
			}
		}

		/// <summary>
		/// Creates an empty json file and fill it with basic json structure
		/// </summary>
		/// <param name="path"></param>
		public static void InitializeJSONFile(string path)
		{
			bool temp;
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
				LogManager.Write($"{path} file has no basic json structure, fixing...");
				File.WriteAllText(path, string.Empty);
				using (StreamWriter sw = File.CreateText(path))
				{
					sw.WriteLine("{"); sw.WriteLine(""); sw.WriteLine("}");
				}
			}
			LogManager.Write($"Json [{path}] was successfully initiallized!");
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
			LogManager.Write($"All files deleted from directory: {path}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InitiallizeCacheDictionary()
		{
			ZipFile.ExtractToDirectory(CacheZip, SourceIconsFolder);
			LogManager.Write($"Zip unpacked: {CacheZip}");
			var cacheData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(CacheJSON));
			FileInfo[] fileInfos = new DirectoryInfo(SourceIconsFolder).GetFiles();
			for(int i = 0; i < fileInfos.Length; i++)
			{
				string nameWithoutExt = Path.GetFileNameWithoutExtension(fileInfos[i].Name);
				if (cacheData.ContainsKey(nameWithoutExt))
				{
					cachedIcons.Add(cacheData[nameWithoutExt], LoadImageFromFile(fileInfos[i].FullName));
					LogManager.Write($"Cache icon [{nameWithoutExt}] assigned with image: {fileInfos[i].FullName}");
				}
				else
				{
					LogManager.Write($"Cache icon [{nameWithoutExt}] is not presented in dictionary");
				}
				File.Delete(fileInfos[i].FullName);
			}
		}

		/// <summary>
		/// Load BitmapSource object from file and returns it.
		/// </summary>
		/// <param name="path">Path of image file to load from.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private BitmapSource LoadImageFromFile(string path)
		{
			try
			{
				Image loadedImage = Image.FromFile(path);
				float aspectRatio = (float)loadedImage.Width / (float)loadedImage.Height; 
				Bitmap bmp;
				if (loadedImage.Width > 256)
					bmp = new Bitmap(loadedImage, 256, (int)(loadedImage.Height / aspectRatio));
				else
					bmp = new Bitmap(loadedImage);

				BitmapSource image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(bmp.GetHicon(),
							new System.Windows.Int32Rect(0, 0, bmp.Width, bmp.Height), BitmapSizeOptions.FromEmptyOptions());
				image.Freeze();
				bmp.Dispose();
				loadedImage.Dispose();

				LogManager.Write($"Image '{path}' was successfully loaded!");

				return image;
			}
			catch(Exception e)
			{
				LogManager.Write($"Message: {e.Message}. Stack trace: {e.StackTrace}");
				return null;
			}
		}

		/// <summary>
		/// Saves a BitmapSource object to an png image file
		/// </summary>
		/// <param name="path">Path of image file to save</param>
		/// <param name="image">Image object to save</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SaveImageToFile(string path, BitmapSource image)
		{
			try
			{
				using (var fileStream = new FileStream(path, FileMode.Create))
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(image));
					encoder.Save(fileStream);
					LogManager.Write($"Image '{path}' was successfully saved!");
				}
			}
			catch(Exception e)
			{
				LogManager.Write($"Message: {e.Message}. Stack trace: {e.StackTrace}");
			}
		}

		/// <summary>
		/// Generates random string.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
