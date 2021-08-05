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

namespace ProgramViewer3.Managers
{
	/// <summary>
	/// This class is used for cache files icons. It is neccessary because extracting them directly from files can be only executed on main thread, which causes freezes.
	/// </summary>
	public sealed class CacheManager
	{
		private readonly ConcurrentDictionary<string, ImageSource> cachedIcons = new ConcurrentDictionary<string, ImageSource>();
		private readonly Dictionary<string, Stream> iconStreams = new Dictionary<string, Stream>();
		/// <summary>
		/// This dictionary contains information about each cached icon file, namely the path to the icon file
		/// and the date of the last modification of the file
		/// </summary>
		private Dictionary<string, dynamic> cacheJson;
		private readonly Dispatcher dispatcher;
		private readonly DirectoryInfo sourceIconsFolderInfo; // It stores all the info about the exact directory

		private static readonly string IconsCacheFolderPath = Path.Combine(ItemManager.ApplicationPath, "IconsCache");
		private static readonly string SourceIconsFolderPath = Path.Combine(IconsCacheFolderPath, "SourceIcons");
		private static readonly string CacheJSONPath = Path.Combine(IconsCacheFolderPath, "cacheFilesNames.json");

		public CacheManager(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
			InitializeDirectory(IconsCacheFolderPath);
			InitializeDirectory(SourceIconsFolderPath);
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
			// Start our timer/watch
			var watch = Stopwatch.StartNew();

			// Merge hot and desktop items into a single list
			List<ItemData> hotAndDesktopItemsDataList = hotItems.ToList();
			hotAndDesktopItemsDataList.AddRange(desktopItems);

			// Refresh our DirectoryInfo instance and loop throught all the files inside the directory with
			// icons sources to fill
			sourceIconsFolderInfo.Refresh();
			var iconFilesInfoDict = new Dictionary<string, FileInfo>();
			foreach (var item in sourceIconsFolderInfo.GetFiles())
			{
				iconFilesInfoDict.Add(Path.GetFileNameWithoutExtension(item.FullName), item);
			}

			// Loop throught every desktop and hot item and save its icon to a file if it is necessary
			foreach (var item in hotAndDesktopItemsDataList)
			{
				string hashedName = GetHash(item.Path); // get the hash string of the path
				bool needToSaveIcon = true;
				if (iconFilesInfoDict.ContainsKey(hashedName) && cacheJson.ContainsKey(hashedName))
				{
					var iconFileInfo = iconFilesInfoDict[hashedName];
					if (iconFileInfo.LastWriteTimeUtc == (DateTime)cacheJson[hashedName]["LastWriteTime"])
					{
						needToSaveIcon = false;
					}
				}
				// We have to save the icon only if LastWriteTimeUtc of the file and LastWriteTime from
				// our cache is not the same or we failed to compare them
				if (needToSaveIcon == true)
				{
					string filePath = Path.Combine(SourceIconsFolderPath, $"{hashedName}.png");
					SaveImageToFile(filePath, item.ImageData as BitmapSource);
					var iconFileInfo = new FileInfo(filePath);
					var newIconData = new Dictionary<string, dynamic>
					{
						{ "Path", item.Path },
						{ "LastWriteTime", iconFileInfo.LastAccessTimeUtc }
					};

					cacheJson[hashedName] = newIconData;
				}
			}

			bool saveResult = WriteObjectToJsonFile(CacheJSONPath, cacheJson);
			//string json = JsonConvert.SerializeObject(cacheJson, Formatting.Indented);
			//File.WriteAllText(CacheJSONPath, json);
			const string successfullSave = "Icons saved successfully!";
			const string failedSave = "Icons saved successfully!";
			string saveResultMessage = saveResult ? successfullSave : failedSave;
			LogManager.Write(saveResultMessage);

			watch.Stop();
			LogManager.Write($"Icons saving time: {watch.Elapsed.TotalMilliseconds} ms");
		}

		/// <summary>
		/// Returns an associated icon for the file. If the specified path is presented in the cachedIcons we
		/// return the icon from the cache, otherwise we have to extract the icon from the file.
		/// </summary>
		/// <param name="path">The path to the file whose icon we want to get</param>
		/// <returns>The associated icon as ImageSource instance</returns>
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
		/// Returns a new instance of DirectoryInfo class for the directory specified by the path parameter. If 
		/// the directory specified by the path parameter does not exist this method will create the directory.
		/// </summary>
		/// <param name="path">Path to the directory for initialization</param>
		public static DirectoryInfo InitializeDirectory(string path)
		{
			if (Directory.Exists(path) == false)
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
		/// Creates an empty json file and fills it with a basic json structure
		/// </summary>
		/// <param name="path">A path where we want to create our json file</param>
		public static void InitializeJSONFile(string path)
		{
			try
			{
				string fileContent = File.ReadAllText(path);
				var currentJsonContent = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(fileContent);
				if (currentJsonContent is null) throw new NullReferenceException();
				LogManager.Write($"Json [{path}] was successfully initiallized!");
			}
			catch
			{
				LogManager.Write($"Json [{path}] does not exist or it is corrupted! This json file will be created and filled with an empty json structure automatically");
				WriteObjectToJsonFile(path, new Dictionary<string, dynamic>());
			}
		}

		/// <summary>
		/// This method tries to save the desired object to an json file specified by the path parameter
		/// </summary>
		/// <param name="path">The path where the new json file will be created</param>
		/// <param name="objectForSerialization">The object for serialization</param>
		/// <returns>True if object was sasved successfully, otherwise False</returns>
		private static bool WriteObjectToJsonFile(string path, object objectForSerialization)
		{
			try
			{
				string jsonContent = JsonConvert.SerializeObject(objectForSerialization, Formatting.Indented);
				File.WriteAllText(path, jsonContent);
				return true;
			}
			catch (Exception e)
			{
				LogManager.Error(e);
				return false;
			}
		}

		/// <summary>
		/// Loads all the cached icons from files asynchronously
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task InitiallizeCacheDictionaryAsync()
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
						if (properName is null || properName == string.Empty)
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
		/// Loads and returns a BitmapSource object from the specified path
		/// </summary>
		/// <param name="path">Path to an image file for loading</param>
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
				LogManager.Error(e);
				return null;
			}
		}

		/// <summary>
		/// This method releases all the resources used by the CacheManager
		/// </summary>
		public void ReleaseResources()
		{
			// Loop throught all iconStreams items and dispose the streams
			foreach (var item in iconStreams)
			{
				item.Value.Dispose();
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
		/// Generates a new hash string from the specified input string.
		/// </summary>
		/// <returns>Generated hash string</returns>
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
