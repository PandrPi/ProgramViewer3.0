using Microsoft.Win32;
using ProgramViewer3.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProgramViewer3
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private readonly AnimationManager animationManager = new AnimationManager();
		private readonly SettingsManager settingsManager = new SettingsManager();
		private readonly ThemeManager themeManager = new ThemeManager();
		private ItemManager itemManager;

		private bool isWindowExpanded = true;
		private bool shouldShirkWindowAfterContextMenuClosing = false;
		private bool isMenuExpanded = false;
		private Point lastCursorPoint;  // Used to store cursor position at the moment when PiContextMenu was shown
		private List<Grid> menuGridChildren;
		private Dictionary<string, Action> menuGridExpandActions;
		
		private readonly Queue<Tuple<string, ItemType>> filesToAdd = new Queue<Tuple<string, ItemType>>();
		private int filesToAddCounter = 0;

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

				settingsManager.Initialize();
				menuGridChildren = new List<Grid>() { SettingGrid, ThemeGrid };
				menuGridExpandActions = new Dictionary<string, Action>()
				{
					{ SettingGrid.Name, animationManager.ExpandSettingGrid },
					{ ThemeGrid.Name, animationManager.ExpandThemeGrid }
				};

				LogManager.Initiallize(settingsManager.GetSettingValue<bool>("RedirectMessageLogging"));
				IconExtractor.BaseExeIcon = (FindResource("BaseExeImage") as Image).Source;
				IconExtractor.Dispatcher = Dispatcher;

				AddItemGrid.Visibility = Visibility.Hidden;
				PiContextMenu.Visibility = Visibility.Hidden;
				RefreshWindow();

				themeManager.Initialize();
				ThemeView.ItemsSource = themeManager.themeItems;
				themeManager.ApplyTheme(settingsManager.GetSettingValue<string>("LastUsedTheme"));
				RefreshControlsAfterThemeChanging();

				InitializeGridPanels();

				System.Diagnostics.Stopwatch managerStopwatch = System.Diagnostics.Stopwatch.StartNew();

				itemManager = new ItemManager(Dispatcher);
				await itemManager.LoadFilesAsync();

				DesktopGridPanel.SetItemSource(itemManager.desktopItems);
				HotGridPanel.SetItemSource(itemManager.hotItems);

				managerStopwatch.Stop();
				LogManager.Write($"Item loading time : {managerStopwatch.Elapsed.TotalMilliseconds} ms");

				animationManager.Initiallize(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));
				animationManager.SetStoryboardCompletedCallback("addItemWindowShowSB", () => AddItemGrid.Visibility = Visibility.Visible);
				animationManager.SetStoryboardCompletedCallback("addItemWindowHideSB", () => AddItemGrid.Visibility = Visibility.Hidden);
				animationManager.SetStoryboardCompletedCallback("menuShrinkSB", () => MakeAllMenuChildrenGridsTransparent());

				InitializeSmoothScrolling();
				ToggleDesktop();
				RegisterAssembly();

				stopwatch.Stop();
				LogManager.Write($"Application starting time : {stopwatch.Elapsed.TotalMilliseconds} ms");
			}
			catch (Exception exc)
			{
				string excMessage = $"{exc.GetType().Name}. Message: {exc.Message}. Stack trace: {exc.StackTrace}";
				LogManager.Write(excMessage);
				MessageBox.Show(exc.StackTrace, exc.Message);
				DisposeAndCloseAll();
				Application.Current.Shutdown();
			}
		}

		/// <summary>
		/// Creates a key in Registry for application's autorun
		/// </summary>
		private void RegisterAssembly()
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			string assemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
			if (registryKey.GetValue(assemblyName) == null)
			{
				registryKey.SetValue(assemblyName, System.Reflection.Assembly.GetExecutingAssembly().Location);
			}
			registryKey.Dispose();
		}

		private void InitializeSmoothScrolling()
		{
			DesktopGridPanel.PreviewMouseWheel += AnimationManager.ListView_PreviewMouseWheel;
			HotGridPanel.PreviewMouseWheel += AnimationManager.ListView_PreviewMouseWheel;
			ThemeView.PreviewMouseWheel += AnimationManager.ListView_PreviewMouseWheel;
		}

		private T FindResource<T>(string key)
		{
			return (T)FindResource(key);
		}

		private StackPanel GetGridPanel_ItemTemplate()
		{
			var panelTemplate = FindResource<StackPanel>("GridPanel.ItemTemplate");
			var titleShadowEffect = FindResource<System.Windows.Media.Effects.DropShadowEffect>("GridPanel.Item.Text.Shadow");
			titleShadowEffect.Freeze();
			var imageShadowEffect = FindResource<System.Windows.Media.Effects.DropShadowEffect>("GridPanel.Item.Image.Shadow");
			imageShadowEffect.Freeze();
			var titleForeground = FindResource<Brush>("GridPanel.Item.Text.Foreground");
			titleForeground.Freeze();

			panelTemplate.Effect = titleShadowEffect;
			var image = panelTemplate.Children[0] as Image;
			var title = panelTemplate.Children[1] as TextBlock;
			image.Effect = imageShadowEffect;
			title.Effect = titleShadowEffect;
			title.Foreground = titleForeground;

			return panelTemplate;
		}

		private void InitializeGridPanels()
		{
			var rippleBrush = FindResource<Brush>("GridPanel.Item.RippleBrush");
			rippleBrush.Freeze();
			var highlightBrush = FindResource<Brush>("GridPanel.Item.HighlightBrush");
			highlightBrush.Freeze();

			DesktopGridPanel.Initialize(GetGridPanel_ItemTemplate(), highlightBrush, rippleBrush);
			HotGridPanel.Initialize(GetGridPanel_ItemTemplate(), highlightBrush, rippleBrush);

			DesktopGridPanel.KeyDown += GridPanel_KeyDown;
			DesktopGridPanel.Grid_MouseDown += Desktop_StackPanel_MouseDown;
			DesktopGridPanel.Grid_MouseDown += StackPanel_MouseLeftButtonDown;
			DesktopGridPanel.Grid_MouseWheel += GridPanel_MouseWheel;

			HotGridPanel.KeyDown += GridPanel_KeyDown;
			HotGridPanel.Grid_MouseDown += Hot_StackPanel_MouseDown;
			HotGridPanel.Grid_MouseDown += StackPanel_MouseLeftButtonDown;
			HotGridPanel.Grid_MouseWheel += GridPanel_MouseWheel;
		}

		private void GridPanel_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (PiContextMenu.Visibility == Visibility.Visible) SetContextMenuVisibility(Visibility.Hidden);
		}

		private void ToggleDesktop()
		{
			animationManager.ToggleDesktop(isWindowExpanded);
			isWindowExpanded = !isWindowExpanded;
		}

		private void ToggleMenu()
		{
			animationManager.ToggleMenu(isMenuExpanded);
			isMenuExpanded = !isMenuExpanded;
		}

		private void OnToggleDesktopClick(object sender, RoutedEventArgs e)
		{
			ToggleDesktop();
		}

		private void OnToggleSettingsClick(object sender, RoutedEventArgs e)
		{
			Grid activeGrid = menuGridChildren.Where(i => i.Opacity != 0.0).FirstOrDefault();
			string buttonTag = (string)(sender as PiButton).Tag;

			if (activeGrid == default(Grid))
			{
				if (!isMenuExpanded)
				{
					ToggleMenu();
					menuGridExpandActions[buttonTag]();
				}
			}
			else
			{
				if (menuGridExpandActions[activeGrid.Name] != menuGridExpandActions[buttonTag])
				{
					MakeAllMenuChildrenGridsTransparent();
					menuGridExpandActions[buttonTag]();
				}
				else
				{
					ToggleMenu();
				}
			}
		}

		private void MakeAllMenuChildrenGridsTransparent()
		{
			for (int i = 0; i < menuGridChildren.Count; i++)
			{
				Grid item = menuGridChildren[i];
				item.BeginAnimation(OpacityProperty, null);
				item.Opacity = 0.0;
				item.Visibility = Visibility.Hidden;
			}
		}

		private void LoadThemesButton_Click(object sender, RoutedEventArgs e)
		{
			ThemeView.ItemsSource = null;
			themeManager.LoadThemes();
			ThemeView.ItemsSource = themeManager.themeItems;
		}

		private void SelectThemeButton_Click(object sender, RoutedEventArgs e)
		{
			string themeName = (string)((sender as PiButton).Parent as Grid).Tag;

			themeManager.ApplyTheme(themeName);
			RefreshControlsAfterThemeChanging();
			settingsManager.SetSettingValue<string>("LastUsedTheme", themeName);
		}

		public void RefreshControlsAfterThemeChanging()
		{
			SettingsVerticalRect.Fill = null;
			const string expandBackground = "SettingVerticalRect.ExpandBackground";
			const string background = "CustomWindow.TitleBar.Background";
			SettingsVerticalRect.Fill = FindResource<Brush>(isMenuExpanded ? expandBackground : background).Clone();

			const string resizeButtonBackground = "DesktopRect.ResizeButton.Background";
			desktopResizeButton.BeginAnimation(BackgroundProperty, null);
			desktopResizeButton.Background = null;
			desktopResizeButton.Background = FindResource<Brush>(resizeButtonBackground).Clone();
		}

		private void MyGrid_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				Activate();
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

				ItemType itemType = GetItemTypeFromPoint(e.GetPosition(MyGrid));
				for (int i = 1; i < files.Length; i++)
				{
					filesToAdd.Enqueue(new Tuple<string, ItemType>(files[i], itemType));
				}

				string title = System.IO.Path.GetFileNameWithoutExtension(files[0]);
				AddWindowFilePath.Text = files[0];
				AddWindowFileTitle.Text = title;
				AddWindowFileWindow.SelectedIndex = (itemType == ItemType.Hot) ? 0 : 1;

				if (!isWindowExpanded)
					ToggleDesktop();

				if (isMenuExpanded)
				{
					ToggleMenu();
				}

				filesToAddCounter = filesToAdd.Count + 1;
				AddFileWindowFilesCount.Content = $"Files to proceed - {filesToAddCounter}";
				SetAddItemGridVisibility(Visibility.Visible);
			}
		}
		/// <summary>
		/// Determines over which window is point (Desktop or Hot)
		/// </summary>
		/// <param name="point">Cursor point relative to main grid</param>
		/// <returns></returns>
		private ItemType GetItemTypeFromPoint(Point point)
		{
			if (point.X >= Width - (desktopResizeButton.Margin.Right + desktopResizeButton.Width))
				return ItemType.Hot;
			else
				return ItemType.Desktop;
		}

		private void AddFileButton_Click(object sender, RoutedEventArgs e)
		{
			AddFile();
		}

		private void AddFile()
		{
			string title = AddWindowFileTitle.Text;
			string path = AddWindowFilePath.Text;
			ItemType itemType = AddWindowFileWindow.SelectedIndex == 0 ? ItemType.Hot : ItemType.Desktop;
			itemManager.AddItem(title, path, itemType);

			ProceedNextFile();
		}

		private void ProceedNextFile()
		{
			if (filesToAdd.Count > 0)
			{
				var item = filesToAdd.Dequeue();
				AddWindowFilePath.Text = item.Item1;
				AddWindowFileTitle.Text = System.IO.Path.GetFileNameWithoutExtension(item.Item1);
				AddWindowFileWindow.SelectedIndex = (item.Item2 == ItemType.Hot) ? 0 : 1;
			}

			filesToAddCounter--;
			AddFileWindowFilesCount.Content = $"Files to proceed - {filesToAddCounter}";

			if (filesToAddCounter == 0)
			{
				SetAddItemGridVisibility(Visibility.Hidden);
			}
		}

		/// <summary>
		/// Sets visibility of Add Item Window, starts occur/dissolve animation
		/// </summary>
		/// <param name="visibility"></param>
		private void SetAddItemGridVisibility(Visibility visibility)
		{

			if (visibility == Visibility.Visible)
			{
				AddItemGrid.Visibility = visibility;
				animationManager.AddItemWindowShow();
			}
			else
			{
				animationManager.AddItemWindowHide();
			}
		}
		/// <summary>
		/// Sets visibility of PiContextMenu, starts occur/dissolve animation
		/// </summary>
		/// <param name="visibility"></param>
		private void SetContextMenuVisibility(Visibility visibility)
		{
			Duration fadeDuration = TimeSpan.FromSeconds(0.3);
			if (visibility == Visibility.Visible)
			{
				PiContextMenu.Visibility = visibility;
				AnimationManager.StartAnimation_FadeIn(PiContextMenu, fadeDuration, () => PiContextMenu.Visibility = Visibility.Visible);
			}
			else
			{
				AnimationManager.StartAnimation_FadeOut(PiContextMenu, fadeDuration, () => PiContextMenu.Visibility = Visibility.Hidden);
				if (shouldShirkWindowAfterContextMenuClosing)
				{
					ToggleDesktop();
					shouldShirkWindowAfterContextMenuClosing = false;
				}
			}
		}

		private void AddWindowCloseButton_Click(object sender, RoutedEventArgs e)
		{
			ProceedNextFile();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			DisposeAndCloseAll();
		}

		private void DisposeAndCloseAll()
		{
			settingsManager.SetSettingValue<string>("LastUsedTheme", themeManager.LastAppliedThemeName);
			settingsManager?.CloseManager();
			TaskbarIcon?.Dispose();
			HotGridPanel.SetItemSource(null);
			HotGridPanel.Dispose();
			DesktopGridPanel.SetItemSource(null);
			DesktopGridPanel.Dispose();
			itemManager?.DisposeManager();
			LogManager.Close();
		}

		private delegate void ExecuteContextCommand(int index, ItemType itemType);
		private void ExexuteContextMenuCommand(ExecuteContextCommand command)
		{
			ItemType itemType = GetItemTypeFromPoint(lastCursorPoint);
			if (itemType == ItemType.Hot)
			{
				int selectedIndex = HotGridPanel.GetSelectedIndex();
				if (selectedIndex != -1)
				{
					command(selectedIndex, itemType);
				}
			}
			else
			{
				int selectedIndex = DesktopGridPanel.GetSelectedIndex();
				if (selectedIndex != -1)
				{
					command(selectedIndex, itemType);
				}
			}
			SetContextMenuVisibility(Visibility.Hidden);
		}

		private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				ExecuteContextCommand command = itemManager.OpenItem;
				ExexuteContextMenuCommand(command);
			}
		}

		private void PiContextOpenFileButton_Click(object sender, RoutedEventArgs e)
		{
			ExecuteContextCommand command = itemManager.OpenItem;
			ExexuteContextMenuCommand(command);
		}

		private void PiContextRemoveButton_Click(object sender, RoutedEventArgs e)
		{
			ExecuteContextCommand command = itemManager.RemoveItem;
			ExexuteContextMenuCommand(command);
		}

		private void PiContextShowInExlorerButton_Click(object sender, RoutedEventArgs e)
		{
			ExecuteContextCommand command = itemManager.ShowItemInExplorer;
			ExexuteContextMenuCommand(command);
		}

		private void PiContextRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshWindow();
		}

		private void RefreshWindow()
		{
			double screenWidth = SystemParameters.VirtualScreenWidth;
			Height = SystemParameters.VirtualScreenHeight - 44;
			Left = screenWidth - Width - 2;
			Top = 2;


		}

		private void Hot_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SetContextMenuVisibility(Visibility.Hidden);
			Point cursorPosition = e.GetPosition(MyGrid);
			lastCursorPoint = cursorPosition;
			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (GetItemTypeFromPoint(cursorPosition) == ItemType.Hot)
				{
					PiContextRemoveButton.Text = "Remove From List";
					if (!isWindowExpanded)
					{
						ToggleDesktop();
					}
					SetContextMenuVisibility(Visibility.Visible);
					if (isMenuExpanded)
					{
						ToggleMenu();
					}
					PiContextMenu.Margin = GetContextMenuMarginFromCursorPosition(cursorPosition, ItemType.Hot);
				}
			}
		}

		private void Desktop_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SetContextMenuVisibility(Visibility.Hidden);
			Point cursorPosition = e.GetPosition(MyGrid);
			lastCursorPoint = cursorPosition;
			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (GetItemTypeFromPoint(cursorPosition) == ItemType.Desktop)
				{
					PiContextRemoveButton.Text = "Remove From Drive";
					SetContextMenuVisibility(Visibility.Visible);
					PiContextMenu.Margin = GetContextMenuMarginFromCursorPosition(cursorPosition, ItemType.Desktop);
				}
			}
		}

		/// <summary>
		/// Determines the point(Margin) where the context menu will be shown
		/// </summary>
		/// <param name="point">Cursor click point</param>
		/// <param name="itemType">Determines the type of clicked item</param>
		/// <returns></returns>
		private Thickness GetContextMenuMarginFromCursorPosition(Point point, ItemType itemType)
		{
			double xDelta = PiContextMenu.Width * ((point.X + PiContextMenu.Width > MyGrid.Width)
				? -1 : (itemType == ItemType.Desktop) ? 0 : 1);
			double contextHeight = PiContextMenu.Height;
			double yDelta = (contextHeight + point.Y > Height) ? -contextHeight : 0;

			return new Thickness(point.X + xDelta, point.Y + yDelta, 0, 0);
		}

		private void WindowBorder_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Right)
			{
				SetContextMenuVisibility(Visibility.Hidden);
				HotGridPanel.SetSelectedIndex(-1);
				DesktopGridPanel.SetSelectedIndex(-1);
			}
		}

		private void ExitToolTipButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void GridPanel_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				VirtualizingGridPanel gridPanel = sender as VirtualizingGridPanel;
				int selectedIndex = gridPanel.GetSelectedIndex();

				if (selectedIndex != -1)
				{
					//TODO: Provide it
					itemManager.OpenItem(selectedIndex, gridPanel.Name == HotGridPanel.Name ? ItemType.Hot : ItemType.Desktop);
				}
			}
		}

		private void AddItemGrid_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				AddFile();
			}
		}
	}
}
