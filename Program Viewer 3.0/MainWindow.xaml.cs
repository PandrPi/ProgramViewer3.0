﻿using Microsoft.Win32;
using ProgramViewer3.Managers;
using System;
using System.Collections.Generic;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProgramViewer3
{
	public partial class MainWindow : Window
	{
		public static MainWindow Instance { get; private set; }

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;
		}

		private readonly AnimationManager animationManager = new AnimationManager();
		private readonly SettingsManager settingsManager = new SettingsManager();
		private readonly ThemeManager themeManager = new ThemeManager();
		private ItemManager itemManager;
		private Grid ActiveMenuGrid;

		private bool isWindowExpanded = true;
		private bool shouldShirkWindowAfterContextMenuClosing = false;
		private bool isMenuExpanded = false;
		private Point lastCursorPoint;  // Used to store cursor position at the moment when PiContextMenu was shown

		private readonly Queue<(string, ItemType)> DragAndDrop_FilesToProcessQueue = new Queue<(string, ItemType)>();
		private int filesToAddCounter = 0;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

				settingsManager.Initialize();

				LogManager.Initiallize(settingsManager.GetSettingValue<bool>("RedirectMessageLogging"));
				IconExtractor.Initialize((FindResource("BaseExeImage") as Image).Source, Dispatcher);

				AddItemGrid.Visibility = Visibility.Hidden;
				PiContextMenu.Visibility = Visibility.Hidden;
				RefreshWindow();

				InitializeGridPanels();
				InitializeThemeManager();
				InitializeItemManager();

				InitializeAnimationManager();
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
				DisposeAndCloseEverything();
				Application.Current.Shutdown();
			}
		}

		#region Initialization methods

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

		private void InitializeThemeManager()
		{
			themeManager.Initialize();
			ThemeView.ItemsSource = themeManager.themeItems;
			themeManager.ApplyTheme(settingsManager.GetSettingValue<string>("LastUsedTheme"));
			RefreshControlsAfterThemeChanging();
		}

		private void InitializeItemManager()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();

			itemManager = new ItemManager(Dispatcher);
			itemManager.LoadItems();

			DesktopGridPanel.SetItemSource(itemManager.DesktopItems);
			HotGridPanel.SetItemSource(itemManager.HotItems);

			watch.Stop();
			LogManager.Write($"Items loading time : {watch.Elapsed.TotalMilliseconds} ms");
		}

		private void InitializeAnimationManager()
		{
			animationManager.Initiallize(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));
			animationManager.SetStoryboardCompletedCallback("addItemWindowShowSB", () => AddItemGrid.Visibility = Visibility.Visible);
			animationManager.SetStoryboardCompletedCallback("addItemWindowHideSB", () => AddItemGrid.Visibility = Visibility.Hidden);

			DesktopGridPanel.PreviewMouseWheel += AnimationManager.ListView_PreviewMouseWheel;
			HotGridPanel.PreviewMouseWheel += AnimationManager.ListView_PreviewMouseWheel;
			ThemeView.PreviewMouseWheel += AnimationManager.ListView_PreviewMouseWheel;
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
		#endregion

		private T FindResource<T>(string key)
		{
			return (T)FindResource(key);
		}

		private T FindResourceAsFrozen<T>(string key) where T : Freezable
		{
			return (T)(FindResource(key) as Freezable).GetAsFrozen();
		}

		private T FindChildByName<T>(FrameworkElement parent, string childName) where T : FrameworkElement
		{
			if (parent == null || childName == null || childName == string.Empty)
				return null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;

				if (child.Name == childName)
					return child as T;
			}

			return null;
		}

		private StackPanel GetGridPanel_ItemTemplate()
		{
			var titleShadowEffect = FindResourceAsFrozen<System.Windows.Media.Effects.DropShadowEffect>("GridPanel.Item.Text.Shadow");
			var imageShadowEffect = FindResourceAsFrozen<System.Windows.Media.Effects.DropShadowEffect>("GridPanel.Item.Image.Shadow");
			var titleForeground = FindResourceAsFrozen<Brush>("GridPanel.Item.Text.Foreground");

			var panelTemplate = FindResource<StackPanel>("GridPanel.ItemTemplate");
			panelTemplate.Effect = titleShadowEffect;
			var image = panelTemplate.Children[0] as Image;
			var title = panelTemplate.Children[1] as TextBlock;
			image.Effect = imageShadowEffect;
			title.Effect = titleShadowEffect;
			title.Foreground = titleForeground;

			return panelTemplate;
		}

		/// <summary>
		/// Updates the DesktopGridPanel and HotGridPanel with a new theme bindings (mostly brushes)
		/// </summary>
		public void UpdateThemeOfVirtualizingGridPanels()
		{
			var rippleBrush = FindResourceAsFrozen<Brush>("GridPanel.Item.RippleBrush");
			var highlightBrush = FindResourceAsFrozen<Brush>("GridPanel.Item.HighlightBrush");

			DesktopGridPanel.SetRippleBrush(rippleBrush);
			DesktopGridPanel.SetItemHighlighterBrush(highlightBrush);
			HotGridPanel.SetRippleBrush(rippleBrush);
			HotGridPanel.SetItemHighlighterBrush(highlightBrush);
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


		private void OnToggleDesktopClick(object sender, RoutedEventArgs e)
		{
			ToggleDesktop();
		}

		private void ToggleMenu(Grid gridForProcessing)
		{
			void HideMenuGrid(Grid grid)
			{
				grid.BeginAnimation(OpacityProperty, null);
				grid.Opacity = 0.0;
				grid.Visibility = Visibility.Hidden;
			}

			void ShowMenuGrid(Grid grid)
			{
				grid.Visibility = Visibility.Visible;
				AnimationManager.StartAnimation_FadeIn(grid, animationManager.WindowResizeDuration);
			}

			if (isMenuExpanded)
			{
				if (ActiveMenuGrid == gridForProcessing)
				{
					Action callback = () =>
					{
						if (gridForProcessing.HasAnimatedProperties == false)
							gridForProcessing.Visibility = Visibility.Hidden;
					};
					AnimationManager.StartAnimation_FadeOut(gridForProcessing, animationManager.WindowResizeDuration, callback);
					animationManager.ToggleMenu(isMenuExpanded);
					isMenuExpanded = !isMenuExpanded;
				}
				else
				{
					HideMenuGrid(ActiveMenuGrid);
					ShowMenuGrid(gridForProcessing);
				}
			}
			else
			{
				animationManager.ToggleMenu(isMenuExpanded);
				isMenuExpanded = !isMenuExpanded;
				ShowMenuGrid(gridForProcessing);
			}
			ActiveMenuGrid = gridForProcessing;

		}

		private void OnToggleSettingsClick(object sender, RoutedEventArgs e)
		{
			string gridToProcess_Name = Convert.ToString((sender as FrameworkElement).Tag);
			Grid gridToOpen = FindChildByName<Grid>(MenuGrid, gridToProcess_Name);

			ToggleMenu(gridToOpen);
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
			const string expandBackground = "SettingVerticalRect.ExpandBackground";
			const string background = "CustomWindow.TitleBar.Background";
			SettingsVerticalRect.Fill = null;
			SettingsVerticalRect.Fill = FindResourceAsFrozen<Brush>(isMenuExpanded ? expandBackground : background);

			const string resizeButtonBackground = "DesktopRect.ResizeButton.Background";
			desktopResizeButton.Background = null;
			desktopResizeButton.Background = FindResourceAsFrozen<Brush>(resizeButtonBackground);
			desktopResizeButton.ButtonGrid.Background = desktopResizeButton.Background;

			UpdateThemeOfVirtualizingGridPanels();
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
					DragAndDrop_FilesToProcessQueue.Enqueue((files[i], itemType));
				}

				string title = System.IO.Path.GetFileNameWithoutExtension(files[0]);
				AddWindowFilePath.Text = files[0];
				AddWindowFileTitle.Text = title;
				AddFileButton.Tag = e.KeyStates == DragDropKeyStates.ControlKey;
				AddWindowFileWindow.SelectedIndex = (itemType == ItemType.Hot) ? 0 : 1;

				if (!isWindowExpanded)
					ToggleDesktop();

				if (isMenuExpanded)
				{
					ToggleMenu(null);
				}

				filesToAddCounter = DragAndDrop_FilesToProcessQueue.Count + 1;
				AddFileWindowFilesCount.Content = $"Files to process - {filesToAddCounter}";
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
			bool shouldCopy = Convert.ToBoolean((sender as FrameworkElement).Tag);
			AddItem_ToItemManager(shouldCopy);
		}

		private void AddItemGrid_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				bool shouldCopy = Convert.ToBoolean((sender as FrameworkElement).Tag);
				AddItem_ToItemManager(shouldCopy);
			}
		}

		private void AddItem_ToItemManager(bool shouldCopy)
		{
			string title = AddWindowFileTitle.Text;
			string path = AddWindowFilePath.Text;
			ItemType itemType = AddWindowFileWindow.SelectedIndex == 0 ? ItemType.Hot : ItemType.Desktop;
			itemManager.AddItem(title, path, itemType, shouldCopy);

			ProcessNextFile();
		}

		private void ProcessNextFile()
		{
			if (DragAndDrop_FilesToProcessQueue.Count > 0)
			{
				var item = DragAndDrop_FilesToProcessQueue.Dequeue();
				AddWindowFilePath.Text = item.Item1;
				AddWindowFileTitle.Text = System.IO.Path.GetFileNameWithoutExtension(item.Item1);
				AddWindowFileWindow.SelectedIndex = (item.Item2 == ItemType.Hot) ? 0 : 1;
			}

			filesToAddCounter--;
			AddFileWindowFilesCount.Content = $"Files to proceed - {filesToAddCounter}";

			if (filesToAddCounter == 0)
			{
				AddFileButton.Tag = string.Empty;
				SetAddItemGridVisibility(Visibility.Hidden);
			}
		}

		/// <summary>
		/// Sets visibility of Add Item Window, starts occur/dissolve animation
		/// </summary>
		/// <param name="visibility">The desired visibility value</param>
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
		/// <param name="visibility">The desired visibility value</param>
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
			ProcessNextFile();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			DisposeAndCloseEverything();
		}

		private void DisposeAndCloseEverything()
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

		private void GetMonitorDetails()
		{
			using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM CIM_VideoControllerResolution"))
			{
				foreach (ManagementObject currentObj in searcher.Get())
				{
					var device_id = currentObj.Properties;
					//Console.WriteLine($"Monitor info: {currentObj.ToString()}");
				}
			}
		}

		private void RefreshWindow()
		{
			GetMonitorDetails();

			LogManager.Write($"Monitor Width: {SystemParameters.PrimaryScreenWidth}");
			LogManager.Write($"Monitor Height: {SystemParameters.PrimaryScreenHeight}");

			const double offsetFromCorner = 2;
			const double taskbarHeight = 40;
			double screenWidth = SystemParameters.PrimaryScreenWidth;
			Height = SystemParameters.PrimaryScreenHeight - taskbarHeight - offsetFromCorner * 2;
			Left = screenWidth - Width - offsetFromCorner;
			Top = offsetFromCorner;
		}

		private void Hot_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SetContextMenuVisibility(Visibility.Hidden);
			Point cursorPosition = e.GetPosition(MyGrid);
			lastCursorPoint = cursorPosition;

			const string removeFromListString = "Remove From List";

			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (GetItemTypeFromPoint(cursorPosition) == ItemType.Hot)
				{
					PiContextRemoveButton.Text = removeFromListString;
					if (!isWindowExpanded)
					{
						ToggleDesktop();
					}
					SetContextMenuVisibility(Visibility.Visible);
					if (isMenuExpanded)
					{
						ToggleMenu(null);
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

			const string removeFromDriveString = "Remove From Drive";

			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (GetItemTypeFromPoint(cursorPosition) == ItemType.Desktop)
				{
					PiContextRemoveButton.Text = removeFromDriveString;
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
				if (sender is VirtualizingGridPanel == false) return;

				VirtualizingGridPanel gridPanel = sender as VirtualizingGridPanel;
				int selectedIndex = gridPanel.GetSelectedIndex();

				if (selectedIndex != -1)
				{
					var itemType = gridPanel.Name == HotGridPanel.Name ? ItemType.Hot : ItemType.Desktop;
					itemManager.OpenItem(selectedIndex, itemType);
				}
			}
		}
	}
}
