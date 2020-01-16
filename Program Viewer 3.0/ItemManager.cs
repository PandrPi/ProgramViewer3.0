using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        private static readonly string HotItemsJSONFilename = "HotItems.json";
        private static readonly string DesktopFolderPath = "PV Desktop";
        private static readonly string DesktopFolderFullPath =
            System.AppDomain.CurrentDomain.BaseDirectory + DesktopFolderPath + "\\";

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
            foreach(var item in hotItemsJsonData)
            {
                hotItems.Add(new ItemData(item.Key, item.Value, IconExtractor.GetIcon(item.Value)));
            }

            FileInfo[] fileInfos = desktopDirectoryInfo.GetFiles();
            for(int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo info = fileInfos[i];
                ItemData itemData = new ItemData(Path.GetFileNameWithoutExtension(info.Name), info.FullName, IconExtractor.GetIcon(info.FullName));
                desktopItems.Add(itemData);
                desktopKeyValuePair.Add(info.Name, itemData);
            }

            desktopFileWatcher = new FileSystemWatcher(DesktopFolderPath)
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName,
            };
            desktopFileWatcher.Created += DesktopFileWatcher_Created;
            desktopFileWatcher.Deleted += DesktopFileWatcher_Deleted;
            desktopFileWatcher.Renamed += DesktopFileWatcher_Renamed;

            desktopFileWatcher.EnableRaisingEvents = true;
        }

        private void DesktopFileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            ItemData oldItem = desktopKeyValuePair[e.OldName];
            int index = desktopItems.IndexOf(oldItem);
            string newTitle = Path.GetFileNameWithoutExtension(e.Name);
            ItemData newItem = new ItemData(newTitle, e.FullPath, oldItem.ImageData);
            desktopKeyValuePair.Remove(e.OldName);

            Action action = () => 
            {
                desktopItems[index] = newItem;
                desktopKeyValuePair.Add(e.Name, newItem);
            };
            mainDispatcher.BeginInvoke(action);
        }

        private void DesktopFileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Action action = () =>
            {
                desktopItems.Remove(desktopKeyValuePair[e.Name]);
                desktopKeyValuePair.Remove(e.Name);
            };
            mainDispatcher.BeginInvoke(action);
        }

        private void DesktopFileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            string title = Path.GetFileNameWithoutExtension(e.Name);
            Action action = () => AddItem(title, e.FullPath, ItemType.Desktop);
            mainDispatcher.BeginInvoke(action);
        }

        public void AddItem(string title, string path, ItemType itemType)
        {
            if(itemType == ItemType.Hot)
            {
                if (!hotItemsJsonData.ContainsKey(title))
                {
                    hotItems.Add(new ItemData(title, path, IconExtractor.GetIcon(path)));
                    hotItemsJsonData.Add(title, path);
                    HotItemsSave();
                }
            }
            else if(itemType == ItemType.Desktop)
            {
                ItemData itemData = new ItemData(title, path, IconExtractor.GetIcon(path));
                desktopItems.Add(itemData);
                desktopKeyValuePair.Add(path, itemData);
            }
        }

        private void HotItemsSave()
        {
            var json = JsonConvert.SerializeObject(hotItemsJsonData, Formatting.Indented);
            File.WriteAllText(HotItemsJSONFilename, json);
        }

        public void Dispose()
        {
            desktopFileWatcher.Dispose();
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

            if (itemType == ItemType.Hot)
            {
                StartProcess(hotItems[index].Path);
            }
            else
            {
                StartProcess(desktopItems[index].Path);
            }
        }

        public void RemoveItem(int index, ItemType itemType)
        {
            if(itemType == ItemType.Hot)
            {
                hotItemsJsonData.Remove(hotItems[index].Title);
                hotItems.RemoveAt(index);
                HotItemsSave();
            }
        }

        public void ShowItemInExplorer(int index, ItemType itemType)
        {

        }
    }
}
