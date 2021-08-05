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
using System.Collections.Concurrent;

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
		/// Gets a type of path(folder or file)
		/// </summary>
		/// <param name="path">Item full path</param>
		/// <returns></returns>
		private PathType GetPathType(string path)
		{
			return File.GetAttributes(path).HasFlag(FileAttributes.Directory) ? PathType.Folder : PathType.File;
		}

		public int CompareTo(ItemData that)
		{
			int pathCompare = PathType.CompareTo(that.PathType) * 2;
			int titleCompare = Title.CompareTo(that.Title) + pathCompare;

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
		public ObservableCollection<ItemData> desktopItems { get; private set; }
		public ObservableCollection<ItemData> hotItems { get; private set; }

		private CacheManager cacheManager;
		private Dispatcher mainDispatcher;
		private Dictionary<string, dynamic> hotItemsJsonData; // used to store loaded json data for hot items
		private ConcurrentDictionary<string, ItemData> desktopItemsData = new ConcurrentDictionary<string, ItemData>(); // used to store to get ItemData fast by file name
		private DirectoryInfo desktopDirectoryInfo;
		private FileSystemWatcher desktopFileWatcher;
		private readonly ItemDataComparer itemDataComparer = new ItemDataComparer();

		public static readonly string ApplicationPath = Path.GetDirectoryName(
			System.Reflection.Assembly.GetExecutingAssembly().Location);
		private static readonly string HotItemsJSONFilename = Path.Combine(ApplicationPath, "HotItems.json");
		private static readonly string DesktopFolderPath = Path.Combine(ApplicationPath, "PV Desktop");

		public ItemManager(Dispatcher dispatcher)
		{
			this.mainDispatcher = dispatcher;
			desktopItems = new ObservableCollection<ItemData>();
			hotItems = new ObservableCollection<ItemData>();
			cacheManager = new CacheManager(dispatcher);

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

		public async Task LoadFilesAsync()
		{
			CacheManager.InitializeJSONFile(HotItemsJSONFilename);
			desktopDirectoryInfo = CacheManager.InitializeDirectory(DesktopFolderPath);

			hotItemsJsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(HotItemsJSONFilename));

			var cacheWatch = Stopwatch.StartNew();
			await cacheManager.InitiallizeCacheDictionaryAsync();
			cacheWatch.Stop();
			LogManager.Write($"Cache init time: {cacheWatch.Elapsed.TotalMilliseconds} ms");

			var hotLoadedList = await LoadHotItemsAsync();
			var desktopLoadedList = await LoadDesktopItemsAsync();

			hotItems = new ObservableCollection<ItemData>(hotLoadedList.OrderBy(i => i, itemDataComparer));
			desktopItems = new ObservableCollection<ItemData>(desktopLoadedList.OrderBy(i => i, itemDataComparer));

			// Save icons to files
			cacheManager.SaveIcons(hotItems, desktopItems);

			// Free resources with some kind of Dispose
			hotLoadedList = null;
			desktopLoadedList = null;
		}

		private async Task<ConcurrentList<ItemData>> LoadHotItemsAsync()
		{
			List<Task> tasks = new List<Task>();
			List<string> hotItemsToRemove = new List<string>();
			ConcurrentList<ItemData> list = new ConcurrentList<ItemData>();

			foreach (var item in hotItemsJsonData)
			{
				if (Directory.Exists(item.Key) || File.Exists(item.Key))
				{
					tasks.Add(Task.Run(() =>
					{
						ItemData itemData = new ItemData(item.Value, item.Key, cacheManager.GetFileIcon(item.Key));
						list.Add(itemData);
					}));
				}
				else
				{
					hotItemsToRemove.Add(item.Key);
				}

			}

			if (hotItemsToRemove.Count != 0)
			{
				for (int i = 0; i < hotItemsToRemove.Count; i++)
				{
					hotItemsJsonData.Remove(hotItemsToRemove[i]);
				}
				HotItemsSave();
			}

			await Task.WhenAll(tasks);

			return list;
		}

		private async Task<ConcurrentList<ItemData>> LoadDesktopItemsAsync()
		{
			List<Task> tasks = new List<Task>();
			FileInfo[] fileInfos = desktopDirectoryInfo.GetFiles();
			ConcurrentList<ItemData> list = new ConcurrentList<ItemData>();

			void LoadItem(string title, string path)
			{
				ItemData itemData = new ItemData(title, path, cacheManager.GetFileIcon(path));
				list.Add(itemData);
				desktopItemsData.AddOrUpdate(path, itemData, (k, v) => v);
			}

			foreach (FileInfo item in fileInfos)
			{
				tasks.Add(Task.Run(() =>
				{
					FileInfo info = item;
					LoadItem(GetFileTitle(info.Name), info.FullName);
				}));
			}

			DirectoryInfo[] directoryInfos = desktopDirectoryInfo.GetDirectories();
			foreach (DirectoryInfo item in directoryInfos)
			{
				tasks.Add(Task.Run(() =>
				{
					DirectoryInfo info = item;
					LoadItem(info.Name, info.FullName);
				}));
			}

			await Task.WhenAll(tasks);

			return list;
		}

		public static string GetFileTitle(string path)
		{
			return Path.GetFileNameWithoutExtension(path);
		}

		/// <summary>
		/// This event rises when a new file or folder inside the Desktop folder was renamed 
		/// </summary>
		private void DesktopFileWatcher_Renamed(object sender, RenamedEventArgs e)
		{
			ItemData oldItem = desktopItemsData[e.OldFullPath];
			int index = desktopItems.IndexOf(oldItem);
			string newTitle = Path.GetFileNameWithoutExtension(e.Name);
			ItemData newItem = new ItemData(newTitle, e.FullPath, oldItem.ImageData);
			desktopItemsData.TryRemove(e.OldFullPath, out ItemData removedItem);

			mainDispatcher.BeginInvoke(new Action(() =>
			{
				desktopItems[index] = newItem;
				desktopItemsData.AddOrUpdate(e.FullPath, newItem, (k, v) => v);
			}));
			cacheManager.SaveIcons(hotItems, desktopItems);
		}

		/// <summary>
		/// This event rises when a new file or folder was removed from the Desktop folder
		/// </summary>
		private void DesktopFileWatcher_Deleted(object sender, FileSystemEventArgs e)
		{
			mainDispatcher.BeginInvoke(new Action(() =>
			{
				desktopItems.Remove(desktopItemsData[e.FullPath]);
				desktopItemsData.TryRemove(e.FullPath, out ItemData itemData);
			}));
			cacheManager.SaveIcons(hotItems, desktopItems);
		}

		/// <summary>
		/// This event is called when new file or folder was added to the Desktop folder
		/// </summary>
		private void DesktopFileWatcher_Created(object sender, FileSystemEventArgs e)
		{
			FileAttributes attributes = File.GetAttributes(e.FullPath);
			string title = attributes.HasFlag(FileAttributes.Directory) ? e.Name : Path.GetFileNameWithoutExtension(e.Name);
			mainDispatcher.BeginInvoke(new Action(() =>
			{
				ItemData itemData = new ItemData(title, e.FullPath, cacheManager.GetFileIcon(e.FullPath));
				AddItemToSortedCollection(desktopItems, itemData);
				desktopItemsData.AddOrUpdate(e.FullPath, itemData, (k, v) => v);
			}));
			cacheManager.SaveIcons(hotItems, desktopItems);
		}

		/// <summary>
		/// This method determines the index for the new item and inserts this item there
		/// </summary>
		/// <param name="list">Collection to which a new item will be added</param>
		/// <param name="item">The item we want to add</param>
		private void AddItemToSortedCollection(ObservableCollection<ItemData> list, ItemData item)
		{
			int i = 0;
			while (i < list.Count && itemDataComparer.Compare(list[i], item) < 0)
				i++;

			mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => list.Insert(i, item)));
		}

		public void AddItem(string title, string path, ItemType itemType, bool shouldCopy)
		{
			if (itemType == ItemType.Hot)
			{
				if (!hotItemsJsonData.ContainsKey(path))
				{
					AddItemToSortedCollection(hotItems, new ItemData(title, path, cacheManager.GetFileIcon(path)));
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
			var itemPath = itemType == ItemType.Hot ? hotItems[index].Path : desktopItems[index].Path;
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
				hotItemsJsonData.Remove(hotItems[index].Path);
				hotItems.RemoveAt(index);
				HotItemsSave();
			}
			else
			{
				Task.Run(() =>
				{
					string path = desktopItems[index].Path;
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
				var itemPath = itemType == ItemType.Hot ? hotItems[index].Path : desktopItems[index].Path;
				string argument = $"/select, \"{itemPath}\"";

				Task.Run(() => Process.Start("explorer.exe", argument).Dispose());
			}
			catch { }
		}
	}
}
