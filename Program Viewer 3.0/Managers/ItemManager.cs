using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace ProgramViewer3.Managers
{
	public struct ItemData
	{
		public string Title { get; set; }
		public string Path { get; set; }
		public ImageSource ImageData { get; set; }
		public PathType PathType { get; set; }

		public ItemData(string title, string path, ImageSource imageSource)
		{
			this.Title = title;
			this.Path = path;
			this.ImageData = imageSource;
			this.PathType = PathType.File;
			this.PathType = GetPathType(path);
		}

		/// <summary>
		/// Determines the actual type of the specified path parameter. Returns PathType.Folder if the
		/// file on the specified path has a directory attribute, otherwise returns PathType.File
		/// </summary>
		/// <param name="path">The full path to the item's source file</param>
		/// <returns></returns>
		private PathType GetPathType(string path)
		{
			return File.GetAttributes(path).HasFlag(FileAttributes.Directory) ? PathType.Folder : PathType.File;
		}

		public int CompareTo(ItemData other)
		{
			int pathCompare = PathType.CompareTo(other.PathType) * 2;
			int titleCompare = Title.CompareTo(other.Title) + pathCompare;

			return titleCompare;
		}

		public override string ToString()
		{
			return Title;
		}
	}

	public class ItemDataComparer : IComparer<ItemData>
	{
		public int Compare(ItemData x, ItemData y) => x.CompareTo(y);
	}

	public enum ItemType { Desktop, Hot };
	public enum PathType { Folder = 0, File = 1 };

	public class ItemManager
	{
		public ObservableCollection<ItemData> DesktopItems { get; private set; }
		public ObservableCollection<ItemData> HotItems { get; private set; }

		private CacheManager cacheManager;
		private Dispatcher mainDispatcher;
		private Dictionary<string, dynamic> hotItemsJsonData; // used to store loaded json data for hot items
		private Dictionary<string, ItemData> desktopItemsData = new Dictionary<string, ItemData>(); // used to store to get ItemData fast by file name
		private DirectoryInfo desktopDirectoryInfo;
		private FileSystemWatcher desktopFileWatcher;
		private readonly ItemDataComparer itemDataComparer = new ItemDataComparer();

		public static readonly string ApplicationPath = Path.GetDirectoryName(
			System.Reflection.Assembly.GetExecutingAssembly().Location);
		private static readonly string HotItemsJSONFilename = Path.Combine(ApplicationPath, "HotItems.json");
		private static readonly string DesktopFolderPath = Path.Combine(ApplicationPath, "PV Desktop");

		public ItemManager(Dispatcher dispatcher)
		{
			mainDispatcher = dispatcher;
			DesktopItems = new ObservableCollection<ItemData>();
			HotItems = new ObservableCollection<ItemData>();
			cacheManager = new CacheManager();

			InitializeDesktopFilesWatcher();
		}

		private void InitializeDesktopFilesWatcher()
		{
			desktopFileWatcher = new FileSystemWatcher(DesktopFolderPath)
			{
				NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName
			};
			desktopFileWatcher.Created += DesktopFileWatcher_Created;
			desktopFileWatcher.Deleted += DesktopFileWatcher_Deleted;
			desktopFileWatcher.Renamed += DesktopFileWatcher_Renamed;

			desktopFileWatcher.EnableRaisingEvents = true;
		}

		public void LoadItems()
		{
			CacheManager.InitializeJSONFile(HotItemsJSONFilename);
			desktopDirectoryInfo = CacheManager.InitializeDirectory(DesktopFolderPath);

			hotItemsJsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(HotItemsJSONFilename));

			var cacheWatch = Stopwatch.StartNew();
			cacheManager.InitiallizeCache();
			cacheWatch.Stop();
			LogManager.Write($"Cache init time: {cacheWatch.Elapsed.TotalMilliseconds} ms");

			var hotLoadedList = LoadHotItems();
			var desktopLoadedList = LoadDesktopItems();

			HotItems = new ObservableCollection<ItemData>(hotLoadedList.OrderBy(i => i, itemDataComparer));
			DesktopItems = new ObservableCollection<ItemData>(desktopLoadedList.OrderBy(i => i, itemDataComparer));

			// Save icons to files
			cacheManager.SaveCacheData();

			// Release resources
			hotLoadedList = null;
			desktopLoadedList = null;
		}

		private List<ItemData> LoadHotItems()
		{
			var hotItemsToRemove = new List<string>();
			var list = new List<ItemData>();

			foreach (var item in hotItemsJsonData)
			{
				if (Directory.Exists(item.Key) || File.Exists(item.Key))
				{
					var itemIcon = cacheManager.GetFileIcon(item.Key);
					ItemData itemData = new ItemData(item.Value, item.Key, itemIcon);
					list.Add(itemData);
				}
				else
				{
					hotItemsToRemove.Add(item.Key);
				}

			}

			for (int i = 0; i < hotItemsToRemove.Count; i++)
			{
				hotItemsJsonData.Remove(hotItemsToRemove[i]);
			}
			if (hotItemsToRemove.Count != 0) HotItemsSave();

			return list;
		}

		private List<ItemData> LoadDesktopItems()
		{
			var list = new List<ItemData>();

			void LoadItem(string title, string path)
			{
				ItemData itemData = new ItemData(title, path, cacheManager.GetFileIcon(path));
				list.Add(itemData);
				desktopItemsData[path] = itemData;
			}

			FileInfo[] fileInfos = desktopDirectoryInfo.GetFiles();
			for (int i = 0; i < fileInfos.Length; i++)
			{
				FileInfo info = fileInfos[i];
				string title = Path.GetFileNameWithoutExtension(info.Name);
				LoadItem(title, info.FullName);
			}

			DirectoryInfo[] directoryInfos = desktopDirectoryInfo.GetDirectories();
			for (int i = 0; i < directoryInfos.Length; i++)
			{
				DirectoryInfo info = directoryInfos[i];
				LoadItem(info.Name, info.FullName);
			}

			return list;
		}

		/// <summary>
		/// This event rises when a new file or folder inside the Desktop folder was renamed 
		/// </summary>
		private void DesktopFileWatcher_Renamed(object sender, RenamedEventArgs e)
		{
			ItemData oldItem = desktopItemsData[e.OldFullPath];
			int index = DesktopItems.IndexOf(oldItem);
			string newTitle = Path.GetFileNameWithoutExtension(e.Name);
			ItemData newItem = new ItemData(newTitle, e.FullPath, oldItem.ImageData);
			desktopItemsData.Remove(e.OldFullPath);

			mainDispatcher.Invoke(new Action(() =>
			{
				DesktopItems[index] = newItem;
				desktopItemsData[e.FullPath] = newItem;
			}));
			cacheManager.SaveCacheData();
		}

		/// <summary>
		/// This event rises when a new file or folder was removed from the Desktop folder
		/// </summary>
		private void DesktopFileWatcher_Deleted(object sender, FileSystemEventArgs e)
		{
			mainDispatcher.Invoke(new Action(() =>
			{
				DesktopItems.Remove(desktopItemsData[e.FullPath]);
				desktopItemsData.Remove(e.FullPath);
			}));
			cacheManager.SaveCacheData();
		}

		/// <summary>
		/// This event is called when new file or folder was added to the Desktop folder
		/// </summary>
		private void DesktopFileWatcher_Created(object sender, FileSystemEventArgs e)
		{
			FileAttributes attributes = File.GetAttributes(e.FullPath);
			string title = attributes.HasFlag(FileAttributes.Directory) ? e.Name : Path.GetFileNameWithoutExtension(e.Name);
			mainDispatcher.Invoke(new Action(() =>
			{
				ItemData itemData = new ItemData(title, e.FullPath, cacheManager.GetFileIcon(e.FullPath));
				AddItemToSortedCollection(DesktopItems, itemData);
				desktopItemsData[e.FullPath] = itemData;
			}));
			cacheManager.SaveCacheData();
		}

		/// <summary>
		/// Determines the index for the new item and inserts this item there
		/// </summary>
		/// <param name="list">Collection to which a new item will be added</param>
		/// <param name="item">The desired ItemData object for inserting</param>
		private void AddItemToSortedCollection(ObservableCollection<ItemData> list, ItemData item)
		{
			int i = 0;
			while (i < list.Count && itemDataComparer.Compare(list[i], item) < 0)
				i++;

			mainDispatcher.Invoke(new Action(() => list.Insert(i, item)));
		}

		public void AddItem(string title, string path, ItemType itemType, bool shouldCopy)
		{
			if (itemType == ItemType.Hot)
			{
				if (!hotItemsJsonData.ContainsKey(path))
				{
					AddItemToSortedCollection(HotItems, new ItemData(title, path, cacheManager.GetFileIcon(path)));
					hotItemsJsonData.Add(path, title);
					HotItemsSave();
				}
			}
			else if (itemType == ItemType.Desktop)
			{
				try
				{
					if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
					{
						FileManager.ProcessFolder(path, DesktopFolderPath, shouldCopy);
					}
					else
					{
						FileManager.ProcessFile(path, DesktopFolderPath, shouldCopy);
					}
				}
				catch (Exception e)
				{
					LogManager.Error(e);
					MessageBox.Show(e.StackTrace, e.Message);
				}
			}
		}

		private void HotItemsSave()
		{
			var json = JsonConvert.SerializeObject(hotItemsJsonData, Formatting.Indented);
			File.WriteAllText(HotItemsJSONFilename, json);
		}

		public void DisposeManager()
		{
			desktopFileWatcher.Dispose();
			cacheManager.ReleaseResources();
		}

		private void StartProcess(string fileName)
		{
			Process process = new Process();
			try
			{
				process.StartInfo = new ProcessStartInfo()
				{
					FileName = fileName
				};
				process.Start();
			}
			catch (Exception e)
			{
				if (e.Message != "The operation was canceled by the user")
				{
					process.StartInfo = new ProcessStartInfo()
					{
						FileName = "rundll32.exe",
						Arguments = "shell32.dll,OpenAs_RunDLL " + fileName
					};
					process.Start();
				}
			}
			process.Dispose();
		}

		/// <summary>
		/// Opens the process associated to the item source file. 
		/// </summary>
		/// <param name="index">Item index</param>
		/// <param name="itemType">Item type</param>
		public void OpenItem(int index, ItemType itemType)
		{
			var itemPath = itemType == ItemType.Hot ? HotItems[index].Path : DesktopItems[index].Path;
			Task.Run(() => StartProcess(itemPath));
		}

		/// <summary>
		/// Removes item specified by the index and itemType parameters from the app
		/// </summary>
		/// <param name="index">Item index</param>
		/// <param name="itemType">Item type</param>
		public void RemoveItem(int index, ItemType itemType)
		{
			if (itemType == ItemType.Hot)
			{
				hotItemsJsonData.Remove(HotItems[index].Path);
				HotItems.RemoveAt(index);
				HotItemsSave();
			}
			else
			{
				Task.Run(() =>
				{
					string path = DesktopItems[index].Path;
					FileManager.SendToRecycle(path);
				});
			}
		}

		/// <summary>
		/// Shows the the source file of the item specified by the index and itemType parameters
		/// </summary>
		/// <param name="index">Item index</param>
		/// <param name="itemType">Item type</param>
		public void ShowItemInExplorer(int index, ItemType itemType)
		{
			try
			{
				var itemPath = itemType == ItemType.Hot ? HotItems[index].Path : DesktopItems[index].Path;
				string argument = $"/select, \"{itemPath}\"";

				Task.Run(() => Process.Start("explorer.exe", argument).Dispose());
			}
			catch { }
		}
	}
}
