using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Program_Viewer_3
{
    public struct ItemData
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public ImageSource ImageData { get; set; }



        public ItemData(string title, string path, ImageSource imageSource)
        {
            this.Title = title;
            this.Path = path;
            this.ImageData = imageSource;
        }
    }

    public class ItemDataComparer : IComparer<ItemData>
    {
        public int Compare(ItemData x, ItemData y)
        {
            return x.Title.CompareTo(y.Title);
        }
    }

    public enum ItemType { Desktop, Hot};

    public class ItemManager
    {
        public ObservableCollection<ItemData> desktopItems { get; private set; }
        public ObservableCollection<ItemData> hotItems { get; private set; }

        private Dispatcher mainDispatcher;
        private Dictionary<string, dynamic> hotItemsJsonData;       // used to store loaded json data for hot items
        private Dictionary<string, ItemData> desktopKeyValuePair = new Dictionary<string, ItemData>();   // used to store to get ItemData fast by file name
        private DirectoryInfo desktopDirectoryInfo;
        private FileSystemWatcher desktopFileWatcher;
        private ItemDataComparer itemDataComparer = new ItemDataComparer();

        private static readonly string ApplicationPath = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string HotItemsJSONFilename = Path.Combine(ApplicationPath, "HotItems.json");
        private static readonly string DesktopFolderPath = Path.Combine(ApplicationPath, "PV Desktop");

        public ItemManager(Dispatcher dispatcher)
        {
            this.mainDispatcher = dispatcher;
            desktopItems = new ObservableCollection<ItemData>();
            hotItems = new ObservableCollection<ItemData>();

            // if hotItems json file does not exist create it and write an empty json content
            if (!File.Exists(HotItemsJSONFilename))
            {
                using (StreamWriter sw = File.CreateText(HotItemsJSONFilename))
                {
                    sw.WriteLine("{"); sw.WriteLine(""); sw.WriteLine("}");
                }
            }

            if (!Directory.Exists(DesktopFolderPath))
            {
                desktopDirectoryInfo = Directory.CreateDirectory(DesktopFolderPath);
            }
            else
            {
                desktopDirectoryInfo = new DirectoryInfo(DesktopFolderPath);
            }

            hotItemsJsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(HotItemsJSONFilename));
            List<string> hotItemsToRemove = new List<string>();
            foreach (var item in hotItemsJsonData)
            {
                if (Directory.Exists(item.Key) || File.Exists(item.Key))
                    AddSorted(hotItems, new ItemData(item.Value, item.Key, IconExtractor.GetIcon(item.Key)), itemDataComparer);
                else
                    hotItemsToRemove.Add(item.Key);
            }
            for (int i = 0; i < hotItemsToRemove.Count; i++)
            {
                hotItemsJsonData.Remove(hotItemsToRemove[i]);
            }
            HotItemsSave();

            FileInfo[] fileInfos = desktopDirectoryInfo.GetFiles();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo info = fileInfos[i];
                ItemData itemData = new ItemData(Path.GetFileNameWithoutExtension(info.Name), info.FullName, IconExtractor.GetIcon(info.FullName));
                AddSorted(desktopItems, itemData, itemDataComparer);
                desktopKeyValuePair.Add(DesktopFolderPath + "\\" + info.Name, itemData);
            }
            DirectoryInfo[] directoryInfos = desktopDirectoryInfo.GetDirectories();
            for (int i = 0; i < directoryInfos.Length; i++)
            {
                DirectoryInfo info = directoryInfos[i];
                ItemData itemData = new ItemData(info.Name, info.FullName, IconExtractor.GetIcon(info.FullName));
                AddSorted(desktopItems, itemData, itemDataComparer);
                desktopKeyValuePair.Add(DesktopFolderPath + "\\" + info.Name, itemData);
            }

            desktopFileWatcher = new FileSystemWatcher(DesktopFolderPath)
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            desktopFileWatcher.Created += DesktopFileWatcher_Created;
            desktopFileWatcher.Deleted += DesktopFileWatcher_Deleted;
            desktopFileWatcher.Renamed += DesktopFileWatcher_Renamed;

            desktopFileWatcher.EnableRaisingEvents = true;
        }

        private void DesktopFileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            ItemData oldItem = desktopKeyValuePair[e.OldFullPath];
            int index = desktopItems.IndexOf(oldItem);
            string newTitle = Path.GetFileNameWithoutExtension(e.Name);
            ItemData newItem = new ItemData(newTitle, e.FullPath, oldItem.ImageData);
            desktopKeyValuePair.Remove(e.OldFullPath);

            Action action = () => 
            {
                desktopItems[index] = newItem;
                desktopKeyValuePair.Add(e.FullPath, newItem);
            };
            mainDispatcher.BeginInvoke(action);
        }

        private void DesktopFileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Action action = () =>
            {
                desktopItems.Remove(desktopKeyValuePair[e.FullPath]);
                desktopKeyValuePair.Remove(e.FullPath);
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
                AddSorted(desktopItems, itemData, itemDataComparer);
                desktopKeyValuePair.Add(e.FullPath, itemData);
            };
            mainDispatcher.BeginInvoke(action);
        }


        private void AddSorted<T>(Collection<T> list, T item, IComparer<T> comparer)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;
            while (i < list.Count && comparer.Compare(list[i], item) < 0)
                i++;

            list.Insert(i, item);
        }

        public void AddItem(string title, string path, ItemType itemType)
        {
            if (itemType == ItemType.Hot)
            {
                if (!hotItemsJsonData.ContainsKey(path))
                {
                    AddSorted(hotItems, new ItemData(title, path, IconExtractor.GetIcon(path)), itemDataComparer);
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
            catch(Exception e)
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
            if(itemType == ItemType.Hot)
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
            if(itemType == ItemType.Hot)
                argument += hotItems[index].Path + "\"";
            else
                argument += desktopItems[index].Path + "\"";

            try
            {
                Task.Run(() => Process.Start("explorer.exe", argument));
            }
            catch { }
        }
    }
}
