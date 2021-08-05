using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using ProgramViewer3.Managers;

namespace ProgramViewer3
{
	class VirtualizingGridPanel : ContentControl
	{
		#region DependencyRegisters
		private static Type OwnerType = typeof(VirtualizingGridPanel);
		public static DependencyProperty ItemSizeProperty = DependencyProperty.Register("ItemSize", typeof(Size), OwnerType);
		public static DependencyProperty ScrollOffsetProperty = DependencyProperty.Register("ScrollOffset", typeof(double), OwnerType);
		public static DependencyProperty ColumnsCountProperty = DependencyProperty.Register("ColumnsCount", typeof(int), OwnerType);
		public static DependencyProperty PanelOpacityMaskProperty = DependencyProperty.Register("PanelOpacityMask", typeof(Brush), OwnerType);
		public static DependencyProperty ChildItemsVerticalOffsetProperty = DependencyProperty.Register("ChildItemsVerticalOffset", typeof(double), OwnerType);
		public static DependencyProperty ChildItemsHorizontalOffsetProperty = DependencyProperty.Register("ChildItemsHorizontalOffset", typeof(double), OwnerType);
		public static DependencyProperty ItemHighlighterFadeDurationProperty = DependencyProperty.Register("ItemHighlighterFadeDuration", typeof(TimeSpan), OwnerType, new PropertyMetadata(TimeSpan.FromSeconds(0.5)));
		#endregion

		#region DependencyProperties
		public Size ItemSize
		{
			get
			{
				return (Size)GetValue(ItemSizeProperty);
			}
			set
			{
				SetValue(ItemSizeProperty, value);
			}
		}

		public double ScrollOffset
		{
			get
			{
				return (double)GetValue(ScrollOffsetProperty);
			}
			set
			{
				SetValue(ScrollOffsetProperty, value);
			}
		}

		public int ColumnsCount
		{
			get
			{
				int output = 0;
				Dispatcher.Invoke(() => output = (int)GetValue(ColumnsCountProperty));
				return output;
			}
			set
			{
				SetValue(ColumnsCountProperty, value);
			}
		}

		public Brush PanelOpacityMask
		{
			get
			{
				return (Brush)GetValue(PanelOpacityMaskProperty);
			}
			set
			{
				SetValue(PanelOpacityMaskProperty, (Brush)value);
			}
		}

		public double ChildItemsVerticalOffset
		{
			get
			{
				return (double)GetValue(ChildItemsVerticalOffsetProperty);
			}
			set
			{
				SetValue(ChildItemsVerticalOffsetProperty, value);
			}
		}

		public double ChildItemsHorizontalOffset
		{
			get
			{
				return (double)GetValue(ChildItemsHorizontalOffsetProperty);
			}
			set
			{
				SetValue(ChildItemsHorizontalOffsetProperty, value);
			}
		}

		public TimeSpan ItemHighlighterFadeDuration
		{
			get
			{
				return (TimeSpan)GetValue(ItemHighlighterFadeDurationProperty);
			}
			set
			{
				SetValue(ItemHighlighterFadeDurationProperty, value);
			}
		}
		#endregion

		public event MouseButtonEventHandler Grid_MouseDown;
		public event MouseWheelEventHandler Grid_MouseWheel;

		private ObservableCollection<Managers.ItemData> ItemSource { get; set; }
		private int SelectedIndex { get; set; } = -1;
		private Grid ChildrenGrid { get; set; }
		private ScrollViewer ScrollOwner { get; set; }
		private VirtualizingStackPanel ItemsOwner { get; set; }
		private StackPanel RowRenderer { get; set; }
		private DropShadowEffect ShadowEffect { get; set; }
		private Ellipse RippleEllipse { get; set; }
		private Rectangle ItemHighlighter { get; set; }
		private Point LastItemHighlighter_Offset { get; set; }

		public void Initialize(StackPanel rowRendererPanelTemplate, Brush highlighterBrush, Brush rippleBrush)
		{
			Initialize_ChildrenGrid();
			Initialize_ItemHighlighter(highlighterBrush);
			Initialize_Ripple(rippleBrush);
			Initialize_ItemsOwner();
			Initialize_ScrollOwner();
			Initialize_RowRenderer(rowRendererPanelTemplate);
		}

		private void Initialize_ChildrenGrid()
		{
			ChildrenGrid = new Grid
			{
				Width = this.Width,
				Height = this.Height,
				FocusVisualStyle = null,
			};
			UpdateFrameworkElement(ChildrenGrid, RenderSize, new Rect(RenderSize));
		}

		private void Initialize_Ripple(Brush fillBrush)
		{
			RippleEllipse = new Ellipse
			{
				Width = 10,
				Height = 10,
				Opacity = 0,
				Fill = fillBrush,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			ChildrenGrid.Children.Add(RippleEllipse);
			UpdateFrameworkElement(RippleEllipse, RippleEllipse.DesiredSize, new Rect(RippleEllipse.DesiredSize));
		}

		private void Initialize_ItemHighlighter(Brush fillBrush)
		{
			ItemHighlighter = new Rectangle
			{
				Width = ItemSize.Width,
				Height = ItemSize.Height,
				Fill = fillBrush,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				RadiusX = 5,
				RadiusY = 5,
				Margin = new Thickness(-ItemSize.Width, -ItemSize.Height, 0, 0)
			};
			UpdateFrameworkElement(ItemHighlighter, ItemSize, new Rect(ItemSize));
			ChildrenGrid.Children.Add(ItemHighlighter);
		}

		/// <summary>
		/// Updates ItemsOwner object
		/// </summary>
		private void Initialize_ItemsOwner()
		{
			ItemsOwner = new VirtualizingStackPanel
			{
				Width = this.Width,
				Height = this.Height,
				FocusVisualStyle = null,
			};
			VirtualizingPanel.SetIsVirtualizing(ItemsOwner, true);
			VirtualizingPanel.SetCacheLengthUnit(ItemsOwner, VirtualizationCacheLengthUnit.Item);
			VirtualizingPanel.SetCacheLength(ItemsOwner, new VirtualizationCacheLength(2));
			VirtualizingPanel.SetVirtualizationMode(ItemsOwner, VirtualizationMode.Recycling);
			UpdateFrameworkElement(ItemsOwner, RenderSize, new Rect(RenderSize));
			ChildrenGrid.Children.Add(ItemsOwner);
		}

		private void Initialize_ScrollOwner()
		{
			ScrollOwner = new ScrollViewer()
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				FocusVisualStyle = null,
				Opacity = this.Opacity,
				OpacityMask = this.PanelOpacityMask,
			};
			ScrollOwner.InvalidateScrollInfo();
			UpdateFrameworkElement(ScrollOwner, RenderSize, new Rect(RenderSize));
			ScrollOwner.Content = ChildrenGrid;
			Content = ScrollOwner;
		}

		/// <summary>
		/// Initializes a RowRenderer, which is used for baking whole row of elements to a single ImageSource object 
		/// </summary>
		public void Initialize_RowRenderer(StackPanel PanelTemplate)
		{
			string PanelTemplateXaml = XamlWriter.Save(PanelTemplate);
			int columnsCount = ColumnsCount;

			// We have to initialize the RowRenderer object not in the main thread to have an ability to
			// use the RowRenderer not in the main thread. It allows us to use asynchronous item loading
			Thread thread = new Thread(() =>
			{
				AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
				{
					var exception = e.ExceptionObject as Exception;
					Console.WriteLine($"Error: {exception.Message}\n{exception.StackTrace}");
				};

				RowRenderer = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Name = "RowRenderer"
				};
				Size panelSize = Size.Empty;
				for (int i = 0; i < columnsCount; i++)
				{
					StackPanel panel = DuplicateControl<StackPanel>(PanelTemplateXaml);
					panel.SnapsToDevicePixels = true;
					panel.UseLayoutRounding = true;
					panel.Margin = new Thickness(0);
					panel.Effect = ShadowEffect;

					TextBlock textBlock = panel.Children[1] as TextBlock;
					textBlock.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);

					panelSize = new Size(panel.Width, panel.Height);
					UpdateFrameworkElement(panel, panelSize, new Rect(panelSize));
					RowRenderer.Children.Add(panel);
				}
				Size rendererSize = new Size(panelSize.Width * columnsCount, panelSize.Height);
				UpdateFrameworkElement(RowRenderer, rendererSize, new Rect(rendererSize));
				System.Windows.Threading.Dispatcher.Run();
			});

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		/// <summary>
		/// Sets a new brush for the RippleEllipse object
		/// </summary>
		/// <param name="fillBrush">New fill brush</param>
		public void SetRippleBrush(Brush fillBrush) => RippleEllipse.Fill = fillBrush;

		/// <summary>
		/// Sets a new brush for the ItemHighlighter object
		/// </summary>
		/// <param name="fillBrush">New fill brush</param>
		public void SetItemHighlighterBrush(Brush fillBrush) => ItemHighlighter.Fill = fillBrush;

		/// <summary>
		/// Renders a UI object to BitmapSource object in the same way as it would be rendered on the display
		/// </summary>
		/// <param name="element">UI object to render</param>
		/// <returns>Returns rendered freezed image</returns>
		private BitmapSource RenderFrameworkElementToBitmap(FrameworkElement element)
		{
			const int bitmapDPI = 96;
			RenderTargetBitmap bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, bitmapDPI, bitmapDPI, PixelFormats.Default);
			bitmap.Render(element);
			if (bitmap.CanFreeze)
				bitmap.Freeze();

			return bitmap;
		}

		/// <summary>
		/// Duplicates UI object
		/// </summary>
		/// <typeparam name="T">Type of object to duplicate</typeparam>
		/// <param name="control">Object to duplicate</param>
		/// <returns></returns>
		private T DuplicateControl<T>(string savedXaml)
		{
			StringReader stringReader = new StringReader(savedXaml);
			XmlReader xmlReader = XmlReader.Create(stringReader);
			return (T)XamlReader.Load(xmlReader);
		}

		/// <summary>
		/// Updates layout of FrameworkElement object
		/// </summary>
		/// <param name="element">Framework element to update</param>
		/// <param name="availableSize">Meashre with availableSize</param>
		/// <param name="finalRect">Arrange with finalRect</param>
		private void UpdateFrameworkElement(FrameworkElement element, Size availableSize, Rect finalRect)
		{
			element.Measure(availableSize);
			element.Arrange(finalRect);
			element.UpdateLayout();
		}

		private int RoundToLowerMultiple(int value, int multiple)
		{
			value = Convert.ToInt16(value);
			if (value % multiple != 0) value = (value / multiple) * multiple;

			return value;
		}

		/// <summary>
		/// Fill RowRenderer with relative items data from ItemSource collection.
		/// It prepares RowRenderer for rendering a whole row to a single image.
		/// </summary>
		/// <param name="startIndex">Index in ItemSource collection to start with</param>
		private async Task<int> FillRowRendererWithItemDataAsync(int startIndex)
		{
			int itemsInRow = 0;
			await Task.Run(() =>
			{
				RowRenderer.Dispatcher.Invoke(new Action(() =>
				{
					for (int itemIndex = startIndex, columnIndex = 0; columnIndex < ColumnsCount; columnIndex++, itemIndex++)
					{
						bool needToFill = true;
						if (itemIndex < 0 || itemIndex > ItemSource.Count - 1) // need to check bounds
							needToFill = false;

						var panel = (StackPanel)RowRenderer.Children[columnIndex];
						if (needToFill)
						{
							ItemData itemData = ItemSource[itemIndex];
							(panel.Children[0] as Image).Source = itemData.ImageData;
							(panel.Children[1] as TextBlock).Text = itemData.Title;
							itemsInRow++;
						}
						else
						{
							(panel.Children[0] as Image).Source = null;
							(panel.Children[1] as TextBlock).Text = string.Empty;
						}
					}
					RowRenderer.UpdateLayout();
				}));
			});

			return itemsInRow;
		}

		/// <summary>
		/// Loads items inside Viewport
		/// </summary>
		private async Task UpdateInternalChildrenAsync(int startingIndex)
		{
			startingIndex = RoundToLowerMultiple(startingIndex, ColumnsCount);
			int rowIndex = startingIndex / ColumnsCount;
			int itemsNumber = ItemsOwner.Children.Count;

			if (rowIndex < itemsNumber)
			{
				ItemsOwner.Children.RemoveRange(rowIndex, itemsNumber - rowIndex);
			}

			for (int i = startingIndex; i < ItemSource.Count; i += ColumnsCount)
			{
				try
				{
					int itemsInRow = await FillRowRendererWithItemDataAsync(i);
					AddChild(await RenderRowToImageAsync(), itemsInRow);
				}
				catch (Exception e)
				{
					LogManager.Error(e);
					MessageBox.Show(e.StackTrace, e.Message);
				}
			}
		}

		private async Task<BitmapSource> RenderRowToImageAsync()
		{
			BitmapSource result = null;
			await Task.Run(() =>
			{
				RowRenderer.Dispatcher.Invoke(() =>
				{
					result = RenderFrameworkElementToBitmap(RowRenderer);
				});
			});

			return result;
		}

		private void AddChild(BitmapSource image, int itemsInRow)
		{
			Image imageControl = new Image
			{
				Cursor = Cursors.Hand,
				Height = image.Height,
				IsHitTestVisible = true,
				Opacity = 0,
				SnapsToDevicePixels = true,
				Source = image,
				Tag = itemsInRow,
				UseLayoutRounding = true,
				Width = image.Width,
				HorizontalAlignment = this.HorizontalContentAlignment,
				Margin = new Thickness(ChildItemsHorizontalOffset, ChildItemsVerticalOffset, 0, 0),
			};
			RenderOptions.SetBitmapScalingMode(imageControl, BitmapScalingMode.NearestNeighbor);
			imageControl.CacheMode = new BitmapCache();
			imageControl.MouseEnter += InternalItem_MouseEnter;
			imageControl.MouseMove += InternalItem_MouseMove;
			imageControl.PreviewMouseDown += InternalItem_PreviewMouseDown;

			Size imageSize = new Size(image.Width, image.Height);
			ItemsOwner.Children.Add(imageControl);

			UpdateFrameworkElement(imageControl, imageSize, new Rect(imageSize));
			ItemsOwner.UpdateLayout();
			AnimationManager.StartAnimation_FadeIn(imageControl, TimeSpan.FromSeconds(0.7));
		}

		public ObservableCollection<Managers.ItemData> GetItemSource()
		{
			return ItemSource;
		}

		public async void SetItemSource(ObservableCollection<Managers.ItemData> items)
		{
			ItemSource = null;
			ItemSource = items;
			if (ItemSource != null)
			{
				ItemSource.CollectionChanged += ItemSource_Changed;
				ScrollOwner.InvalidateScrollInfo();
				await UpdateInternalChildrenAsync(0);
			}
		}

		private async void ItemSource_Changed(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					// remove all rows starting from the row with (e.NewStatringIndex / columnsCount) index,
					// rebuild them and add again 
					await UpdateInternalChildrenAsync(e.NewStartingIndex);
					break;
				case NotifyCollectionChangedAction.Remove:
					// remove all rows starting from the row with (e.OldStatringIndex / columnsCount) index,
					// rebuild them and add again 
					await UpdateInternalChildrenAsync(e.OldStartingIndex);
					break;
				case NotifyCollectionChangedAction.Replace:
					// remove all rows starting from the row with (e.NewStatringIndex / columnsCount) index,
					// rebuild them and add again 
					await UpdateInternalChildrenAsync(e.NewStartingIndex);
					break;
				case NotifyCollectionChangedAction.Move:
					// remove all rows starting from the row with (e.OldStatringIndex / columnsCount) index,
					// rebuild them and add again 
					await UpdateInternalChildrenAsync(e.OldStartingIndex);
					break;
			}
		}

		public int GetSelectedIndex()
		{
			return SelectedIndex;
		}

		public void SetSelectedIndex(int newIndex)
		{
			if (newIndex == -1)
			{
				StartItemHighlighter_FadeAnimation(false);
			}
			SelectedIndex = newIndex;
		}

		private void InternalItem_MouseEnter(object sender, MouseEventArgs e)
		{
			Image imageControl = sender as Image;
			Point offset = new Point(e.GetPosition(this).X, VisualTreeHelper.GetOffset(imageControl).Y);
			ReplaceItemHighlighter(offset, Convert.ToDouble(imageControl.Tag));
		}

		private void InternalItem_MouseMove(object sender, MouseEventArgs e)
		{
			Image imageControl = sender as Image;
			Point offset = new Point(e.GetPosition(this).X, VisualTreeHelper.GetOffset(imageControl).Y);
			ReplaceItemHighlighter(offset, Convert.ToDouble(imageControl.Tag));
		}

		/// <summary>
		/// Replace the ItemHighlighter to be behind the current internal item pointed to by the mouse
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="elementsInRow">The number of elements in the current row</param>
		private void ReplaceItemHighlighter(Point offset, double elementsInRow)
		{
			offset.X = RoundToLowerMultiple((int)(offset.X - ChildItemsHorizontalOffset), (int)ItemSize.Width);
			if (offset.X > (elementsInRow - 1) * ItemSize.Width)
			{
				StartItemHighlighter_FadeAnimation(false);
			}
			else
			{
				if (offset != LastItemHighlighter_Offset) StartItemHighlighter_FadeAnimation(true);
			}
			LastItemHighlighter_Offset = new Point(offset.X, offset.Y);
			ItemHighlighter.Margin = new Thickness(offset.X + ChildItemsHorizontalOffset, offset.Y + ChildItemsVerticalOffset, 0, 0);
		}
		private void StartItemHighlighter_FadeAnimation(bool isFadeInAnimation)
		{
			// This line stops the current animation if it exists
			ItemHighlighter.BeginAnimation(OpacityProperty, null);
			ItemHighlighter.Opacity = 0;

			var fade_Duration = ItemHighlighterFadeDuration;
			if (isFadeInAnimation)
				AnimationManager.StartAnimation_FadeIn(ItemHighlighter, fade_Duration);
			else
				AnimationManager.StartAnimation_FadeOut(ItemHighlighter, fade_Duration);
		}

		/// <summary>
		/// Method provides important functionality for MouseDown event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void InternalItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			Point clickPoint = e.GetPosition(ItemsOwner);
			int itemIndex = (int)(clickPoint.Y / ItemSize.Height) * ColumnsCount + (int)(clickPoint.X / ItemSize.Width);
			StartRippleClickAnimation(clickPoint);

			if (itemIndex >= 0 && itemIndex < ItemSource.Count)
			{
				SetSelectedIndex(itemIndex);
				Grid_MouseDown?.Invoke(this, e);
			}
			else
			{
				SetSelectedIndex(-1);
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			StartItemHighlighter_FadeAnimation(false);
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			StartItemHighlighter_FadeAnimation(false);
			LastItemHighlighter_Offset = new Point(-1, -1);
		}

		protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
		{
			base.OnPreviewMouseWheel(e);
			Grid_MouseWheel?.Invoke(this, e);
		}

		private void StartRippleClickAnimation(Point clickPoint)
		{
			Size startSize = new Size(10, 10);
			Size finalSize = new Size(300, 300);
			Duration duration = TimeSpan.FromSeconds(0.4);

			RippleEllipse.BeginAnimation(OpacityProperty, null);
			RippleEllipse.Opacity = 1;
			RippleEllipse.Width = startSize.Width;
			RippleEllipse.Height = startSize.Height;
			RippleEllipse.Margin = new Thickness(clickPoint.X, clickPoint.Y, 0, 0);
			var scaleTransform = new ScaleTransform(1, 1);
			var translateTransform = new TranslateTransform(0, 0);
			var transformGroup = new TransformGroup { Children = { scaleTransform, translateTransform } };
			RippleEllipse.RenderTransform = null;
			RippleEllipse.RenderTransform = transformGroup;

			AnimationManager.StartAnimation_DoubleTransform(scaleTransform, ScaleTransform.ScaleXProperty, finalSize.Width / startSize.Width, duration);
			AnimationManager.StartAnimation_DoubleTransform(scaleTransform, ScaleTransform.ScaleYProperty, finalSize.Height / startSize.Height, duration);
			AnimationManager.StartAnimation_DoubleTransform(translateTransform, TranslateTransform.XProperty, -(finalSize.Width / 2), duration);
			AnimationManager.StartAnimation_DoubleTransform(translateTransform, TranslateTransform.YProperty, -(finalSize.Height / 2), duration);
			AnimationManager.StartAnimation_FadeOut(RippleEllipse, duration);
		}

		public void Dispose()
		{
			RowRenderer?.Dispatcher?.InvokeShutdown();
		}
	}
}
