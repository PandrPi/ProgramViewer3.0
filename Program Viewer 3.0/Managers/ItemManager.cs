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
			this.mainDispatcher = dispatcher;
			desktopItems = new ObservableCollection<ItemData>();
			hotItems = new ObservableCollection<ItemData>();
			cacheManager = new CacheManager(dispatcher);

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
			desktopDirectoryInfo = CacheManager.InitiallizeDirectory(DesktopFolderPath);

			hotItemsJsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(HotItemsJSONFilename));
			List<string> hotItemsToRemove = new List<string>();

			var cacheWatch = Stopwatch.StartNew();
			await cacheManager.InitiallizeCacheDictionary();
			cacheWatch.Stop();
			LogManager.Write($"Cache init time: {cacheWatch.Elapsed.TotalMilliseconds} ms");

			List<Task> tasks = new List<Task>();

			List<ItemData> tempHot = new List<ItemData>();
			List<ItemData> tempDesktop = new List<ItemData>();

			foreach (var item in hotItemsJsonData)
			{
				if (Directory.Exists(item.Key) || File.Exists(item.Key))
				{
					tasks.Add(Task.Run(() =>
					{
						ItemData itemData = new ItemData(item.Value, item.Key, cacheManager.GetFileIcon(item.Key));
						tempHot.Add(itemData);
					}));
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
			HotItemsSave();

			FileInfo[] fileInfos = desktopDirectoryInfo.GetFiles();

			foreach (FileInfo item in fileInfos)
			{
				tasks.Add(Task.Run(async () =>
				{
					FileInfo info = item;
					ItemData itemData = new ItemData(Path.GetFileNameWithoutExtension(info.Name),
						info.FullName, cacheManager.GetFileIcon(info.FullName));
					await mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
					{
						tempDesktop.Add(itemData);
						desktopItemsData.Add($"{DesktopFolderPath}\\{info.Name}", itemData);
					}));
				}));
			}

			DirectoryInfo[] directoryInfos = desktopDirectoryInfo.GetDirectories();
			foreach (DirectoryInfo item in directoryInfos)
			{
				tasks.Add(Task.Run(async () =>
				{
					DirectoryInfo info = item;
					ItemData itemData = new ItemData(info.Name, info.FullName, cacheManager.GetFileIcon(info.FullName));
					await mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
					{
						tempDesktop.Add(itemData);
						desktopItemsData.Add($"{DesktopFolderPath}\\{info.Name}", itemData);
					}));
				}));
			}

			await Task.WhenAll(tasks);

			hotItems = new ObservableCollection<ItemData>(tempHot.OrderBy(i => i, itemDataComparer));
			desktopItems = new ObservableCollection<ItemData>(tempDesktop.OrderBy(i => i, itemDataComparer));
		}

		private void DesktopFileWatcher_Renamed(object sender, RenamedEventArgs e)
		{
			ItemData oldItem = desktopItemsData[e.OldFullPath];
			int index = desktopItems.IndexOf(oldItem);
			string newTitle = Path.GetFileNameWithoutExtension(e.Name);
			ItemData newItem = new ItemData(newTitle, e.FullPath, oldItem.ImageData);
			desktopItemsData.Remove(e.OldFullPath);

			Action action = () =>
			{
				desktopItems[index] = newItem;
				desktopItemsData.Add(e.FullPath, newItem);
			};
			mainDispatcher.BeginInvoke(action);
		}

		private void DesktopFileWatcher_Deleted(object sender, FileSystemEventArgs e)
		{
			Action action = () =>
			{
				desktopItems.Remove(desktopItemsData[e.FullPath]);
				desktopItemsData.Remove(e.FullPath);
			};
			mainDispatcher.BeginInvoke(action);
		}

		private void DesktopFileWatcher_Created(object sender, FileSystemEventArgs e)
		{
			FileAttributes attributes = File.GetAttributes(e.FullPath);
			string title;
			if (attributes.HasFlag(FileAttributes.Directory))
				title = e.Name;
			else
				title = Path.GetFileNameWithoutExtension(e.Name);
			Action action = () =>
			{
				ItemData itemData = new ItemData(title, e.FullPath, IconExtractor.GetIcon(e.FullPath));
				AddSorted(desktopItems, itemData);
				desktopItemsData.Add(e.FullPath, itemData);
			};
			mainDispatcher.Invoke(action);
		}


		private void AddSorted(ObservableCollection<ItemData> list, ItemData item)
		{
			int i = 0;
			while (i < list.Count && itemDataComparer.Compare(list[i], item) < 0)
				i++;

			mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => list.Insert(i, item)));
		}

		public void AddItem(string title, string path, ItemType itemType)
		{
			if (itemType == ItemType.Hot)
			{
				if (!hotItemsJsonData.ContainsKey(path))
				{
					AddSorted(hotItems, new ItemData(title, path, IconExtractor.GetIcon(path)));
					hotItemsJsonData.Add(path, title);
					HotItemsSave();
				}
			}
			else if (itemType == ItemType.Desktop)
			{
				try
				{
					FileAttributes attributes = File.GetAttributes(path);
					if (attributes.HasFlag(FileAttributes.Directory))
					{
						FileManager.MoveFolder(path, DesktopFolderPath);
					}
					else
					{
						string filename = Path.GetFileName(path);
						string sourcePath = Path.GetDirectoryName(path);
						FileManager.MoveFile(filename, sourcePath, DesktopFolderPath);
					}
				}
				catch (Exception e)
				{
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
			HotItemsSave();
			
			cacheManager.SaveIcons(hotItems, desktopItems);
		}

		private void StartProcess(string filename)
		{
			Process process = new Process();
			try
			{
				process.StartInfo = new ProcessStartInfo()
				{
					FileName = filename
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
						Arguments = "shell32.dll,OpenAs_RunDLL " + filename
					};
					process.Start();
				}
			}
			process.Dispose();
		}

		public void OpenItem(int index, ItemType itemType)
		{
			Task.Run(() =>
			{
				if (itemType == ItemType.Hot)
				{
					StartProcess(hotItems[index].Path);
				}
				else
				{
					StartProcess(desktopItems[index].Path);
				}
			});
		}

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

		public void ShowItemInExplorer(int index, ItemType itemType)
		{
			string argument = "/select, \"";
			if (itemType == ItemType.Hot)
				argument += hotItems[index].Path + "\"";
			else
				argument += desktopItems[index].Path + "\"";

			try
			{
				Task.Run(() => Process.Start("explorer.exe", argument).Dispose());
			}
			catch { }
		}
	}
}
