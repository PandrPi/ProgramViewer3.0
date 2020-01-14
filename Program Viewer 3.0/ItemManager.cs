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

        private static readonly string HotItemsJSONFilename = "HotItems.json";

        public ItemManager()
        {
            desktopItems = new ObservableCollection<ItemData>();
            hotItems = new ObservableCollection<ItemData>();

            ImageSource image1 = IconExtractor.GetIcon("temp1.lnk");
            var temp = IconExtractor.GetIcon("image.jpg");

            desktopItems.Add(new ItemData { Title = "Movie 1", ImageData = IconExtractor.GetIcon("HotItems.json") });
            desktopItems.Add(new ItemData { Title = "Movie 2", ImageData = IconExtractor.GetIcon("IconLib.dll") });
            desktopItems.Add(new ItemData { Title = "Movie 3", ImageData = IconExtractor.GetIcon("temp.exe") });
            desktopItems.Add(new ItemData { Title = "Movie 4", ImageData = IconExtractor.GetIcon("Program Viewer 3.pdb") });
            desktopItems.Add(new ItemData { Title = "Movie 5", ImageData = IconExtractor.GetIcon("image.jpg") });
            desktopItems.Add(new ItemData { Title = "Movie 6", ImageData = IconExtractor.GetIcon("Program Viewer 3") });

            hotItems.Add(new ItemData { Title = "Movie 1", ImageData = image1 });
            hotItems.Add(new ItemData { Title = "Movie 2", ImageData = image1 });

            hotItemsJsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(HotItemsJSONFilename));

            foreach(var item in hotItemsJsonData)
            {
                dynamic value = item.Value;
                //hotItems.Add(new ItemData { Title = value.Title, ImageData = IconExtractor.GetIcon(value.Path) });
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
                }
            }
            else if(itemType == ItemType.Desktop)
            {
                desktopItems.Add(new ItemData(title, IconExtractor.GetIcon(path)));
            }
        }
    }
}
