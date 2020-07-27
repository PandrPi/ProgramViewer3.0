using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProgramViewer3.Managers
{
	/// <summary>
	/// This class is used for cache files icons. It is neccessary because extracting them directly from files can be only executed on main thread, which causes freezes.
	/// </summary>
	public sealed class CacheManager
	{
		private readonly ConcurrentDictionary<string, ImageSource> cachedIcons = new ConcurrentDictionary<string, ImageSource>();
		private readonly Dictionary<string, Stream> iconStreams = new Dictionary<string, Stream>();
		private Dictionary<string, dynamic> cacheJson;
		private Dispatcher dispatcher;
		private DirectoryInfo sourceIconsFolderInfo;

		private static readonly string IconsCacheFolderPath = Path.Combine(ItemManager.ApplicationPath, "IconsCache");
		private static readonly string SourceIconsFolderPath = Path.Combine(IconsCacheFolderPath, "SourceIcons");
		private static readonly string CacheJSONPath = Path.Combine(IconsCacheFolderPath, "cacheFilesNames.json");

		public CacheManager(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
			InitiallizeDirectory(IconsCacheFolderPath);
			InitiallizeDirectory(SourceIconsFolderPath);
			InitializeJSONFile(CacheJSONPath);

			sourceIconsFolderInfo = new DirectoryInfo(SourceIconsFolderPath);
		}

		/// <summary>
		/// This method is used to cache all icons into files in SourceIconsFolder
		/// </summary>
		/// <param name="hotItems"></param>
		/// <param name="desktopItems"></param>
		public void SaveIcons(ObservableCollection<ItemData> hotItems, ObservableCollection<ItemData> desktopItems)
		{
			var watch = Stopwatch.StartNew();
			List<ItemData> items = hotItems.ToList();
			items.AddRange(desktopItems);

			sourceIconsFolderInfo.Refresh();
			var infos = new Dictionary<string, FileInfo>();
			foreach (var item in sourceIconsFolderInfo.GetFiles())
			{
				infos.Add(Path.GetFileNameWithoutExtension(item.FullName), item);
			}

			foreach (var item in items)
			{
				string hashedName = GetHash(item.Path);
				bool needToSaveIcon = true;
				if (infos.ContainsKey(hashedName) && cacheJson.ContainsKey(hashedName))
				{
					var info = infos[hashedName];
					if (info.LastWriteTimeUtc == (DateTime)cacheJson[hashedName].LastWriteTime)
					{
						needToSaveIcon = false;
					}
				}
				if (needToSaveIcon)
				{
					string fileName = Path.Combine(SourceIconsFolderPath, $"{hashedName}.png");
					SaveImageToFile(fileName, item.ImageData as BitmapSource);
					var info = new FileInfo(fileName);
					var newIconData = new Dictionary<string, dynamic> { { "Path", item.Path },
						{ "LastWriteTime", info.LastAccessTimeUtc } };

					if (cacheJson.ContainsKey(hashedName)) // need to check contains as files can have different LastWrite times
					{
						cacheJson[hashedName] = null; // to get GC message that it can release previous dictionary resources
						cacheJson[hashedName] = newIconData;
					}
					else
					{
						cacheJson.Add(hashedName, newIconData);
					}
				}
			}

			LogManager.Write($"Icons saved successfully!");
			string json = JsonConvert.SerializeObject(cacheJson, Formatting.Indented);
			File.WriteAllText(CacheJSONPath, json);

			foreach (var item in iconStreams)
			{
				item.Value.Dispose();
			}

			watch.Stop();
			LogManager.Write($"Icons saving time: {watch.Elapsed.TotalMilliseconds} ms");
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
				LogManager.Write($"Image '{path}' was returned from dictionary!");
				return cachedIcons[path];
			}
			else
			{
				LogManager.Write($"Image '{path}' was extracted from file!");
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task InitiallizeCacheDictionary()
		{
			cacheJson = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(CacheJSONPath));
			FileInfo[] fileInfos = new DirectoryInfo(SourceIconsFolderPath).GetFiles();
			var tasks = new List<Task>();

			foreach (FileInfo info in fileInfos)
			{
				tasks.Add(Task.Run(() =>
				{
					string nameWithoutExt = Path.GetFileNameWithoutExtension(info.Name);
					if (cacheJson.ContainsKey(nameWithoutExt))
					{
						string properName = cacheJson[nameWithoutExt].Path;
						if (properName == null || properName == string.Empty)
						{
							LogManager.Write($"Error: Cache item '{nameWithoutExt}' has corrupted Path value!!!");
						}
						else
						{
							cachedIcons.AddOrUpdate(properName, LoadImageFromFile(info.FullName, properName), (k, v) => v);
							LogManager.Write($"Cache icon '{nameWithoutExt}' assigned with image: {info.FullName}");
						}
					}
					else
					{
						LogManager.Write($"Error: Cache icon '{nameWithoutExt}' is not presented in dictionary");
					}
				}));
			}

			await Task.WhenAll(tasks);
		}		

		/// <summary>
		/// Load BitmapSource object from file and returns it.
		/// </summary>
		/// <param name="path">Path of image file to load from.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private BitmapSource LoadImageFromFile(string path, string name)
		{
			try
			{
				var stream = new MemoryStream(File.ReadAllBytes(path));

				var image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = stream;
				image.EndInit();
				image.Freeze();

				dispatcher.Invoke(() => iconStreams.Add(name, stream));

				LogManager.Write($"Image '{path}' was successfully loaded!");
				return image;
			}
			catch (Exception e)
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
		public static void SaveImageToFile(string path, BitmapSource image)
		{
#if DEBUG
			using (var fileStream = new FileStream(path, FileMode.Create))
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(image));
				encoder.Save(fileStream);
				LogManager.Write($"Image '{path}' was successfully saved!");
			}
#else
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
#endif
		}

		/// <summary>
		/// Generates random string.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetHash(string input)
		{
			using (var hashAlgorithm = System.Security.Cryptography.SHA256.Create())
			{
				byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

				var sBuilder = new StringBuilder();

				for (int i = 0; i < data.Length; i++)
				{
					sBuilder.Append(data[i].ToString("x2"));
				}

				return sBuilder.ToString();
			}			
		}
	}
}
