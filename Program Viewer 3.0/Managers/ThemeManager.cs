using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Text.RegularExpressions;

namespace ProgramViewer3.Managers
{
	class ThemeManager
	{
		public ObservableCollection<ThemeItem> themeItems = new ObservableCollection<ThemeItem>();

		private readonly Dictionary<string, ResourceDictionary> themeResources = new Dictionary<string, ResourceDictionary>();
		private readonly List<ResourceDictionary> DefaultResourceCollection = Application.Current.Resources.MergedDictionaries.ToList();
		private ResourceDictionary DefaultThemeDictionary;

		private static readonly string ThemeFolder = Path.Combine(ItemManager.ApplicationPath, "Themes");
		private static readonly string DefaultThemeName = "Default Theme";
		private static readonly string IconResourcesDictionarySourceString = "Resources/IconResources.xaml";

		public void Initialize()
		{
			CacheManager.InitiallizeDirectory(ThemeFolder);
			DefaultThemeDictionary = DefaultResourceCollection.Where(i => i.Source.OriginalString.Contains(Regex.Replace(DefaultThemeName, @"\s+", ""))).First();

			LoadThemes();
		}

		public void LoadThemes()
		{
			FileInfo[] fileInfos = new DirectoryInfo(ThemeFolder).GetFiles("*.xaml", SearchOption.TopDirectoryOnly);
			themeResources.Clear();
			themeResources.Add(DefaultThemeName, DefaultThemeDictionary);

			for (int i = 0; i < fileInfos.Length; i++)
			{
				FileInfo current = fileInfos[i];
				string title = ToTitle(Path.GetFileNameWithoutExtension(current.Name));

				themeItems.Add(new ThemeItem(title, current.FullName));
				themeResources.Add(title, new ResourceDictionary() { Source = new Uri(current.FullName, UriKind.Absolute) });
			}
			themeItems = new ObservableCollection<ThemeItem>(themeItems.OrderBy(i => i.Name).ToList());
		}

		public void ApplyTheme(string name)
		{
			var collection = DefaultResourceCollection;

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
		public string Name;
		public string Path;

		public ThemeItem(string name, string path)
		{
			Name = name;
			Path = path;
		}
	}
}
