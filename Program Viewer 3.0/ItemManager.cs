using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Program_Viewer_3
{
    public struct ItemData
    {
        public string Title { get; set; }
        public ImageSource ImageData { get; set; }


        public ItemData(string title, ImageSource imageSource)
        {
            this.Title = title;
            this.ImageData = imageSource;
        }
    }

    public enum ItemType { Desktop, Hot};

    public class ItemManager
    {
        public ObservableCollection<ItemData> desktopItems { get; private set; }
        public ObservableCollection<ItemData> hotItems { get; private set; }

        private Dictionary<string, dynamic> hotItemsJsonData;
        private DirectoryInfo desktopDirectoryInfo;

        private static readonly string HotItemsJSONFilename = "HotItems.json";
        private static readonly string DesktopFolderPath = "PV Desktop";

        public ItemManager()
        {
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
                hotItems.Add(new ItemData(item.Key, IconExtractor.GetIcon(item.Value)));
            }

            FileInfo[] fileInfos = desktopDirectoryInfo.GetFiles();
            for(int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo info = fileInfos[i];
                desktopItems.Add(new ItemData(Path.GetFileNameWithoutExtension(info.Name), IconExtractor.GetIcon(info.FullName)));
            }
        }

        public void AddItem(string title, string path, ItemType itemType)
        {
            if(itemType == ItemType.Hot)
            {
                if (!hotItemsJsonData.ContainsKey(title))
                {
                    hotItems.Add(new ItemData(title, IconExtractor.GetIcon(path)));
                    hotItemsJsonData.Add(title, path);
                    HotItemsSave();
                }
            }
            else if(itemType == ItemType.Desktop)
            {
                desktopItems.Add(new ItemData(title, IconExtractor.GetIcon(path)));
            }
        }

        private void HotItemsSave()
        {
            var json = JsonConvert.SerializeObject(hotItemsJsonData, Formatting.Indented);
            File.WriteAllText(HotItemsJSONFilename, json);
        }
    }
}
