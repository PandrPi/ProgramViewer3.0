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

    }
    public class ItemManager
    {
        public ObservableCollection<ItemData> desktopItems { get; private set; }
        public ObservableCollection<ItemData> hotItems { get; private set; }


        private static readonly string HotItemsJSONFilename = "HotItems.json";

        public ItemManager()
        {
            desktopItems = new ObservableCollection<ItemData>();
            hotItems = new ObservableCollection<ItemData>();

            ImageSource image1 = IconExtractor.GetIcon("temp1.lnk");

            desktopItems.Add(new ItemData { Title = "Movie 1", ImageData = IconExtractor.GetIcon("HotItems.json") });
            desktopItems.Add(new ItemData { Title = "Movie 2", ImageData = IconExtractor.GetIcon("IconLib.dll") });
            desktopItems.Add(new ItemData { Title = "Movie 3", ImageData = IconExtractor.GetIcon("temp.exe") });
            desktopItems.Add(new ItemData { Title = "Movie 4", ImageData = IconExtractor.GetIcon("Program Viewer 3.pdb") });
            desktopItems.Add(new ItemData { Title = "Movie 5", ImageData = IconExtractor.GetIcon("image.jpg") });
            desktopItems.Add(new ItemData { Title = "Movie 6", ImageData = IconExtractor.GetIcon("Program Viewer 3.exe") });

            hotItems.Add(new ItemData { Title = "Movie 1", ImageData = image1 });
            hotItems.Add(new ItemData { Title = "Movie 2", ImageData = image1 });

            var json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(HotItemsJSONFilename));

            foreach(var item in json)
            {
                Console.WriteLine(item.Value.Title + " | " + item.Value.Path);
            }
        }

        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + filename, UriKind.Absolute));
        }
    }
}
