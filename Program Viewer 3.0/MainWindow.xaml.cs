using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using Microsoft.Win32;

namespace Program_Viewer_3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private AnimationManager animationManager;
        private ItemManager itemManager;

        private bool isWindowExpanded = true;
        private bool shouldShirkWindowAfterContextMenuClosing = false;
        /// <summary>
        /// Used to store cursor position at the moment when PiContextMenu was shown
        /// </summary>
        private Point lastCursorPoint;
        private Queue<Tuple<string, ItemType>> filesToAdd = new Queue<Tuple<string, ItemType>>();
        private int filesToAddCounter = 0;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();

				LogManager.Initiallize(false);
				IconExtractor.BaseExeIcon = (FindResource("BaseExeImage") as Image).Source;
                IconExtractor.Dispatcher = Dispatcher;

				TaskAsync(() =>
				{
					System.Diagnostics.Stopwatch itemStopwatch = new System.Diagnostics.Stopwatch();
					itemStopwatch.Start();

					itemManager = new ItemManager(Dispatcher);
					Dispatcher.Invoke(() =>
					{
						DesktopLV.ItemsSource = itemManager.desktopItems;
						HotLV.ItemsSource = itemManager.hotItems;
						DesktopLV.LostFocus += (ev, ee) => DesktopLV.SelectedIndex = -1;
						HotLV.LostFocus += (ev, ee) => HotLV.SelectedIndex = -1;
					});
					itemStopwatch.Stop();
					LogManager.Write($"Item loading time : {itemStopwatch.Elapsed.TotalMilliseconds} ms");
				});

				animationManager = new AnimationManager();
                animationManager.Initiallize(this, TimeSpan.FromSeconds(0.5), new Point(110, 600));
                animationManager.SetAddItemWindowShowCallback(() => AddItemGrid.Visibility = Visibility.Visible);
                animationManager.SetAddItemWindowHideCallback(() => AddItemGrid.Visibility = Visibility.Hidden);
                animationManager.SetContextMenuShowCallback(() => PiContextMenu.Visibility = Visibility.Visible);
                animationManager.SetContextMenuHideCallback(() => PiContextMenu.Visibility = Visibility.Hidden);
				DesktopLV.PreviewMouseWheel += animationManager.ListView_PreviewMouseWheel;
				HotLV.PreviewMouseWheel += animationManager.ListView_PreviewMouseWheel;

                double screenWidth = SystemParameters.PrimaryScreenWidth;
                Height = SystemParameters.PrimaryScreenHeight - 46;
                Left = screenWidth - Width - 2;
                Top = 2;
                ToggleDesktop();
                AddItemGrid.Visibility = Visibility.Hidden;
                PiContextMenu.Visibility = Visibility.Hidden;

                RegisterAssembly();

				stopwatch.Stop();
				LogManager.Write($"Application starting time : {stopwatch.Elapsed.TotalMilliseconds} ms");
            }
            catch(Exception exc)
            {
				string excMessage = $"{exc.GetType().Name}. Message: {exc.Message}. Stack trace: {exc.StackTrace}";
				Clipboard.SetText(excMessage);
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

		private async void TaskAsync(Action todo)
		{
			try
			{
				await System.Threading.Tasks.Task.Run(() => todo());
			}
			catch (Exception e)
			{
				LogManager.Write($"Message: {e.Message}. Stack trace: {e.StackTrace}");
			}
		}

		private void ToggleDesktop()
        {
            if (isWindowExpanded)
            {
                animationManager.ShrinkDesktop();
                isWindowExpanded = false;
            }
            else
            {
                animationManager.ExpandDesktop();
                isWindowExpanded = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleDesktop();
        }

        private void MyGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Focus();
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                ItemType itemType = GetWindowTypeFromPoint(e.GetPosition(MyGrid));
                for(int i = 1; i < files.Length; i++)
                {
                    filesToAdd.Enqueue(new Tuple<string, ItemType>(files[i], itemType));
                }

                string title = System.IO.Path.GetFileNameWithoutExtension(files[0]);
                AddWindowFilePath.Text = files[0];
                AddWindowFileTitle.Text = title;
                AddWindowFileWindow.SelectedIndex = (itemType == ItemType.Hot) ? 0 : 1;

                if (!isWindowExpanded)
                    ToggleDesktop();

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
        private ItemType GetWindowTypeFromPoint(Point point)
        {
            if (point.X >= Width - (desktopShirkExpandButton.Margin.Right + desktopShirkExpandButton.Width))
                return ItemType.Hot;
            else
                return ItemType.Desktop;
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
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
                animationManager.AddItemWindowHide();
        }
        /// <summary>
        /// Sets visibility of PiContextMenu, starts occur/dissolve animation
        /// </summary>
        /// <param name="visibility"></param>
        private void SetContextMenuVisibility(Visibility visibility)
        {

            if (visibility == Visibility.Visible)
            {
                PiContextMenu.Visibility = visibility;
                animationManager.ContextMenuShow();
            }
            else
            {
                animationManager.ContextMenuHide();
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
			TaskbarIcon.Dispose();
			itemManager.DisposeManager();
			LogManager.Close();
		}

        private delegate void ExecuteContextCommand(int index, ItemType itemType);
        private void ExexuteContextMenuCommand(ExecuteContextCommand command)
        {
            ItemType itemType = GetWindowTypeFromPoint(lastCursorPoint);
            if (itemType == ItemType.Hot)
            {
                int selectedIndex = HotLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    command(selectedIndex, itemType);
                }
            }
            else
            {
                int selectedIndex = DesktopLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    command(selectedIndex, itemType);
                }
            }
            SetContextMenuVisibility(Visibility.Hidden);
        }

        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2)
            {
                ItemType itemType = GetWindowTypeFromPoint(Mouse.GetPosition(MyGrid));
                if (itemType == ItemType.Hot)
                {
                    int selectedIndex = HotLV.SelectedIndex;
                    if (selectedIndex != -1)
                    {
                        itemManager.OpenItem(selectedIndex, itemType);
                    }
                }
                else
                {
                    int selectedIndex = DesktopLV.SelectedIndex;
                    if (selectedIndex != -1)
                    {
                        itemManager.OpenItem(selectedIndex, itemType);
                    }
                }
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
            double screenWidth = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight - 46;
            Left = screenWidth - Width - 2;
            Top = 2;
            SetContextMenuVisibility(Visibility.Hidden);
        }

        private void Hot_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetContextMenuVisibility(Visibility.Hidden);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Point cursorPosition = e.GetPosition(MyGrid);
                lastCursorPoint = cursorPosition;
                if (GetWindowTypeFromPoint(cursorPosition) == ItemType.Hot)
                {
                    PiContextRemoveButton.Text = "Remove From List";
                    if (!isWindowExpanded)
                    {
                        ToggleDesktop();
                    }
                    SetContextMenuVisibility(Visibility.Visible);
                    PiContextMenu.Margin = GetContextMenuMarginFromCursorPosition(cursorPosition, ItemType.Hot);
                }
            }
        }

        private void Desktop_StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetContextMenuVisibility(Visibility.Hidden);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Point cursorPosition = e.GetPosition(MyGrid);
                lastCursorPoint = cursorPosition;
                if (GetWindowTypeFromPoint(cursorPosition) == ItemType.Desktop)
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
            SetContextMenuVisibility(Visibility.Hidden);
            HotLV.SelectedIndex = -1;
            DesktopLV.SelectedIndex = -1;
        }

        private void ExitToolTipButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DesktopLV_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                int selectedIndex = DesktopLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.OpenItem(selectedIndex, ItemType.Desktop);
                }
            }
        }

        private void HotLV_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int selectedIndex = HotLV.SelectedIndex;
                if (selectedIndex != -1)
                {
                    itemManager.OpenItem(selectedIndex, ItemType.Hot);
                }
            }
        }
	}
}
