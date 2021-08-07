using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ProgramViewer3.Managers
{
	/// <summary>
	/// Struct that is used for more convenient work with the cache data
	/// </summary>
	internal struct IconData
	{
		public DateTime LastWriteTime { get; set; }
		public List<string> FilesSourcePaths { get; set; }
	}

	/// <summary>
	/// Manages icons for ItemData objects in the app.
	/// </summary>
	public sealed class CacheManager
	{
		/// <summary>
		/// Displays key-value pairs in which Key is the path to a specific source file and Value is
		/// the hash string of the icon associated with that file
		/// </summary>
		private readonly Dictionary<string, string> itemPathToIconHash = new Dictionary<string, string>();
		/// <summary>
		/// Displays key-value pairs in which the Key is the hash string calculated from the icon that is
		/// associated with some source file and Value is the icon of that source file
		/// </summary>
		private readonly Dictionary<string, ImageSource> iconHashToIcon = new Dictionary<string, ImageSource>();
		/// <summary>
		/// Displays key-value pairs in which Key is the hash string calculated from the icon that is
		/// associated with some source file and Value is the StreamSource property of that icon
		/// </summary>
		private readonly Dictionary<string, Stream> iconHashToIconStream = new Dictionary<string, Stream>();
		/// <summary>
		/// Contains all the information about the SourceIcons directory
		/// </summary>
		private readonly DirectoryInfo sourceIconsFolderInfo;
		/// <summary>
		/// Displays key-value pairs in which the key is a hash string calculated from a file path and
		/// the value is a IconData object
		/// </summary>
		private Dictionary<string, IconData> cacheJson;

		private static readonly string IconsCacheFolderPath = Path.Combine(ItemManager.ApplicationPath, "IconsCache");
		private static readonly string SourceIconsFolderPath = Path.Combine(IconsCacheFolderPath, "SourceIcons");
		private static readonly string CacheJsonPath = Path.Combine(IconsCacheFolderPath, "CacheData.json");


		public CacheManager()
		{
			InitializeDirectory(IconsCacheFolderPath);
			InitializeDirectory(SourceIconsFolderPath);
			InitializeJSONFile(CacheJsonPath);

			sourceIconsFolderInfo = new DirectoryInfo(SourceIconsFolderPath);
		}

		/// <summary>
		/// Saves the cache data of the manager
		/// </summary>
		public void SaveCacheData()
		{
			// Start our timer/watch
			var watch = Stopwatch.StartNew();

			// Create a Dictionary object which will be serialized for our CacheData.json file.
			// Key is the hash string of the icon, Value is a list of the paths of the source files.
			var cacheData = new Dictionary<string, IconData>();

			// A lookup table that stores hash strings of icons that are already saved
			var savedIcons = new HashSet<string>();

			// Refresh our DirectoryInfo instance and loop throught all the files inside the directory with
			// icons sources to fill the iconFilesInfoDict dictionary with needed values
			sourceIconsFolderInfo.Refresh();
			var iconFilesInfoDict = new Dictionary<string, FileInfo>();
			FileInfo[] array = sourceIconsFolderInfo.GetFiles();
			for (int i = 0; i < array.Length; i++)
			{
				FileInfo item = array[i];
				iconFilesInfoDict.Add(Path.GetFileNameWithoutExtension(item.FullName), item);
			}

			foreach (var item in itemPathToIconHash)
			{
				string iconHash = item.Value;
				string sourcePath = item.Key;

				var actualLastWriteTime = iconFilesInfoDict.ContainsKey(iconHash) ? TrimMilliseconds(iconFilesInfoDict[iconHash].LastWriteTimeUtc) : DateTime.MinValue;
				var storedLastWriteTime = cacheJson.ContainsKey(iconHash) ? TrimMilliseconds(cacheJson[iconHash].LastWriteTime) : DateTime.MaxValue;

				// We have to save the icon to the file only if this icon was not saved before. Multiple
				// ItemData objects can share the same icon and such a check prevents us from saving the same
				// icon multiple times and thus improves the speed of our code.
				//
				// Also we have to compare the actual LastWriteTime of the icon file and the LastWriteTime
				// that we store inside our CacheData.json. There is no need to write to the icon file if this
				// file was not modified since the moment we write to it last time
				if (savedIcons.Contains(iconHash) == false && actualLastWriteTime != storedLastWriteTime)
				{
					string filePath = Path.Combine(SourceIconsFolderPath, $"{iconHash}.png");
					SaveImageToFile(filePath, iconHashToIcon[iconHash] as BitmapSource);
					storedLastWriteTime = TrimMilliseconds(DateTime.UtcNow);
				}

				if (cacheData.ContainsKey(iconHash))
				{
					cacheData[iconHash].FilesSourcePaths.Add(sourcePath);
				}
				else
				{
					cacheData[iconHash] = new IconData()
					{
						LastWriteTime = storedLastWriteTime,
						FilesSourcePaths = new List<string>() { sourcePath }
					};
				}
			}

			bool saveResult = WriteObjectToJsonFile(CacheJsonPath, cacheData);
			const string successfullSave = "Icons saved successfully!";
			const string failedSave = "Icons saved successfully!";
			string saveResultMessage = saveResult ? successfullSave : failedSave;
			LogManager.Write(saveResultMessage);

			cacheData.Clear();

			watch.Stop();
			LogManager.Write($"Icons saving time: {watch.Elapsed.TotalMilliseconds} ms");
		}

		/// <summary>
		/// Returns an associated icon for the file. If the specified path is presented in the cachedIcons
		/// returns the icon from the cache, otherwise extracts the icon from the file.
		/// </summary>
		/// <param name="path">The path to the file whose icon we want to get</param>
		/// <returns>The associated icon as ImageSource instance</returns>
		public ImageSource GetFileIcon(string path)
		{
			const string returnedFromCacheFormat = "Icon '{0}' was returned from the cache!";
			const string extractedFromFileFormat = "Icon '{0}' was extracted from file!";

			if (itemPathToIconHash.ContainsKey(path))
			{
				LogManager.Write(returnedFromCacheFormat, path);
				var iconHash = itemPathToIconHash[path];
				return iconHashToIcon[iconHash];
			}
			else
			{
				LogManager.Write(extractedFromFileFormat, path);

				var itemIcon = IconExtractor.GetIcon(path, out string iconHash);

				itemPathToIconHash[path] = iconHash;
				iconHashToIcon[iconHash] = itemIcon;

				return itemIcon;
			}
		}

		/// <summary>
		/// Returns a new instance of DirectoryInfo class for the directory specified by the path parameter. If 
		/// the directory specified by the path parameter does not exist this method will create the directory.
		/// </summary>
		/// <param name="path">The path to the directory for initialization</param>
		public static DirectoryInfo InitializeDirectory(string path)
		{
			const string directoryCreatedFormat = "Directory created: {0}";
			const string directoryExistsFormat = "Directory exists: {0}";

			if (Directory.Exists(path) == false)
			{
				LogManager.Write(directoryCreatedFormat, path);
				return Directory.CreateDirectory(path);
			}
			else
			{
				LogManager.Write(directoryExistsFormat, path);
				return new DirectoryInfo(path);
			}
		}

		/// <summary>
		/// Creates an empty json file and fills it with a basic json structure
		/// </summary>
		/// <param name="path">The desired path for the json file creation</param>
		public static void InitializeJSONFile(string path)
		{
			const string successfullInitializationFormat = "Json [{0}] was successfully initiallized!";
			const string failedInitializationFormat = "Json [{0}] does not exist or it is corrupted! This json file will be created and filled with an empty json structure automatically.";

			try
			{
				string fileContent = File.ReadAllText(path);
				var currentJsonContent = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(fileContent);
				if (currentJsonContent is null) throw new ArgumentException();
				LogManager.Write(successfullInitializationFormat, path);
			}
			catch
			{
				LogManager.Write(failedInitializationFormat, path);
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
		/// Loads all the cached icons from the cache files
		/// </summary>
		public void InitiallizeCache()
		{
			var fileContent = File.ReadAllText(CacheJsonPath);
			cacheJson = JsonConvert.DeserializeObject<Dictionary<string, IconData>>(fileContent);
			const string successfulConnectionMessageFormat = "Source path '{0}' is successfully linked to the '{1}' icon hash string!";

			foreach (var item in cacheJson)
			{
				string iconHash = item.Key;
				var iconData = item.Value;
				var filesSourcePaths = iconData.FilesSourcePaths;

				for (int i = 0; i < filesSourcePaths.Count; i++)
				{
					string sourcePath = filesSourcePaths[i];
					itemPathToIconHash[sourcePath] = iconHash;
					LogManager.Write(successfulConnectionMessageFormat, sourcePath, iconHash);
				}
				string iconFilePath = Path.Combine(SourceIconsFolderPath, $"{iconHash}.png");
				var loadedIcon = LoadImageFromFile(iconFilePath);
				if (loadedIcon is null == false) iconHashToIcon[iconHash] = loadedIcon;
			}
		}

		/// <summary>
		/// Loads and returns a BitmapSource object from the specified path
		/// </summary>
		/// <param name="imagePath">The path to the desired image file for loading</param>
		/// <param name="sourceFilePath">The path to the source file</param>
		private BitmapSource LoadImageFromFile(string imagePath)
		{
			const string successfulLoadingFormat = "Image '{0}' was successfully loaded!";

			try
			{
				var stream = new MemoryStream(File.ReadAllBytes(imagePath));

				var image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = stream;
				image.EndInit();
				if (image.CanFreeze) image.Freeze();

				LogManager.Write(successfulLoadingFormat, imagePath);
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
			foreach (var item in iconHashToIconStream)
			{
				item.Value.Dispose();
			}
		}

		/// <summary>
		/// Saves the input BitmapSource object to a file located at the specified path
		/// </summary>
		/// <param name="path">The desired image file path for saving</param>
		/// <param name="image">The desired image object for saving</param>
		public static void SaveImageToFile(string path, BitmapSource image)
		{
			const string successfulSaveFormat = "Image '{0}' was successfully saved!";
#if DEBUG
			using (var fileStream = new FileStream(path, FileMode.Create))
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(image));
				encoder.Save(fileStream);
				LogManager.Write(successfulSaveFormat, path);
			}
#else
			try
			{
				using (var fileStream = new FileStream(path, FileMode.Create))
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(image));
					encoder.Save(fileStream);
					LogManager.Write(successfulSaveFormat, path);
				}
			}
			catch(Exception e)
			{
				LogManager.Error(e);
			}
#endif
		}

		/// <summary>
		/// Returns a copy of the input DateTime object with Miliseconds property set to zero
		/// </summary>
		private DateTime TrimMilliseconds(DateTime dt)
		{
			return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
		}
	}
}
