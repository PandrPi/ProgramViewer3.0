using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace ProgramViewer3.Managers
{
	class ThemeManager
	{
		public static List<ResourceDictionary> DefaultResourceCollection { get; private set; }
		public static ResourceDictionary DefaultThemeDictionary { get; private set; }
		public static int DefaultResourcesNumber { get; private set; }

		public ObservableCollection<ThemeItem> themeItems = new ObservableCollection<ThemeItem>();

		private readonly Dictionary<string, ResourceDictionary> themeResources = new Dictionary<string, ResourceDictionary>();

		private static readonly string ThemeFolder = Path.Combine(ItemManager.ApplicationPath, "Themes");
		private static readonly string DefaultThemeName = "Default Theme";
		private static readonly string IconResourcesDictionarySourceString = "Resources/IconResources.xaml";

		public void Initialize()
		{
			DefaultResourceCollection = Application.Current.Resources.MergedDictionaries.ToList();
			DefaultThemeDictionary = DefaultResourceCollection.Where(i => i.Source.OriginalString.Contains(Regex.Replace(DefaultThemeName, @"\s+", ""))).First();
			DefaultResourcesNumber = DefaultResourceCollection.Count;

			CacheManager.InitiallizeDirectory(ThemeFolder);

			LoadThemes();
		}

		public void LoadThemes()
		{
			FileInfo[] fileInfos = new DirectoryInfo(ThemeFolder).GetFiles("*.xaml", SearchOption.TopDirectoryOnly);
			themeItems.Clear();
			themeResources.Clear();
			themeResources.Add(DefaultThemeName, DefaultThemeDictionary);
			themeItems.Add(new ThemeItem(DefaultThemeName, DefaultThemeDictionary));

			for (int i = 0; i < fileInfos.Length; i++)
			{
				FileInfo current = fileInfos[i];
				try
				{
					string title = ToTitle(Path.GetFileNameWithoutExtension(current.Name));

					var dictionary = new ResourceDictionary() { Source = new Uri(current.FullName, UriKind.Absolute) };
					themeResources.Add(title, dictionary);
					themeItems.Add(new ThemeItem(title, dictionary));
				}
				catch(Exception e)
				{
					MessageBox.Show(e.Message, $"{e.GetType().Name} occured in file 'Themes/{current.Name}'");
				}
			}
			themeItems = new ObservableCollection<ThemeItem>(themeItems.OrderBy(i => i.Name).ToList());
		}

		public void ApplyTheme(string name)
		{
			var collection = DefaultResourceCollection.Take(DefaultResourcesNumber).ToList();
			DefaultResourceCollection = null;
			DefaultResourceCollection = new List<ResourceDictionary>(collection);

			if (name != DefaultThemeName)
				collection.Add(GetThemeRecource(name));

			Application.Current.Resources.MergedDictionaries.Clear();
			foreach (var item in collection)
			{
				Application.Current.Resources.MergedDictionaries.Add(item);
			}

			RefreshIconResources();
		}

		private ResourceDictionary GetThemeRecource(string themeName)
		{
			return themeResources.ContainsKey(themeName) ? themeResources[themeName] : null;
		}

		private void RefreshIconResources()
		{
			Application.Current.Resources.MergedDictionaries.Where(i => i.Source.OriginalString.Equals(
				IconResourcesDictionarySourceString)).First().Source = new Uri(IconResourcesDictionarySourceString, UriKind.RelativeOrAbsolute);
		}

		private string ToTitle(string title)
		{
			return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Regex.Replace(title, "([a-z])([A-Z])", "$1 $2").ToLower());
		}
	}

	public struct ThemeItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public Brush FirstBrush { get; set; }
		public Brush SecondBrush { get; set; }
		public Brush ThirdBrush { get; set; }
		public Brush FourthBrush { get; set; }
		public Brush FifthBrush { get; set; }

		private static readonly string DefaultDescription = "The description is not available...";
		private static readonly string DescriptionKey = "Description";
		private static readonly string FirstBrushKey = "HotRect.Background";
		private static readonly string SecondBrushKey = "DesktopRect.Background";
		private static readonly string ThirdBrushKey = "CustomWindow.TitleBar.Background";
		private static readonly string FourthBrushKey = "CustomWindow.Background";
		private static readonly string FifthBrushKey = "DesktopRect.ResizeButton.Background";

		public ThemeItem(string name, ResourceDictionary resource)
		{
			Name = name;
			Description = resource.Contains(DescriptionKey) ? (string)resource[DescriptionKey] : DefaultDescription;
			FirstBrush = GetResource<Brush>(resource, FirstBrushKey);
			SecondBrush = GetResource<Brush>(resource, SecondBrushKey);
			ThirdBrush = GetResource<Brush>(resource, ThirdBrushKey);
			FourthBrush = GetResource<Brush>(resource, FourthBrushKey);
			FifthBrush = GetResource<Brush>(resource, FifthBrushKey);
		}

		private static T GetResource<T>(ResourceDictionary resource, string key)
		{
			return resource.Contains(key) ? (T)resource[key] : (T)ThemeManager.DefaultThemeDictionary[key];
		}
	}
}
