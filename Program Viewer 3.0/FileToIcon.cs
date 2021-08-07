using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace ProgramViewer3
{

	public class FileToIconConverter
	{
		#region Win32api
		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		[StructLayout(LayoutKind.Sequential)]
		internal struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		};

		internal const uint SHGFI_ICON = 0x100;
		internal const uint SHGFI_TYPENAME = 0x400;
		internal const uint SHGFI_LARGEICON = 0x0; // 'Large icon
		internal const uint SHGFI_SMALLICON = 0x1; // 'Small icon
		internal const uint SHGFI_SYSICONINDEX = 16384;
		internal const uint SHGFI_USEFILEATTRIBUTES = 16;
		internal const int SHIL_EXTRALARGE = 0x2; // 48x48 pixels Icon
		internal const int SHIL_JUMBO = 0x4; // 256x256 pixels Icon

		// <summary>
		/// Get Icons that are associated with files.
		/// To use it, use (System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon));
		/// hImgSmall = SHGetFileInfo(fName, 0, ref shinfo,(uint)Marshal.SizeOf(shinfo),Win32.SHGFI_ICON |Win32.SHGFI_SMALLICON);
		/// </summary>
		[DllImport("shell32.dll")]
		internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
												  ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
		[DllImport("shell32.dll", EntryPoint = "#727")]
		private extern static int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

		[DllImport("user32")]
		public static extern int DestroyIcon(IntPtr hIcon);

		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int left, top, right, bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			int x;
			int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IMAGELISTDRAWPARAMS
		{
			public int cbSize;
			public IntPtr himl;
			public int i;
			public IntPtr hdcDst;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int xBitmap;    // x offest from the upperleft of bitmap
			public int yBitmap;    // y offset from the upperleft of bitmap
			public int rgbBk;
			public int rgbFg;
			public int fStyle;
			public int dwRop;
			public int fState;
			public int Frame;
			public int crEffect;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IMAGEINFO
		{
			public IntPtr hbmImage;
			public IntPtr hbmMask;
			public int Unused1;
			public int Unused2;
			public RECT rcImage;
		}
		[ComImportAttribute()]
		[GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
		interface IImageList
		{
			[PreserveSig]
			int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

			[PreserveSig]
			int ReplaceIcon(int i, IntPtr hicon, ref int pi);

			[PreserveSig]
			int SetOverlayImage(int iImage, int iOverlay);
			[PreserveSig]
			int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
			[PreserveSig]
			int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

			[PreserveSig]
			int Draw(ref IMAGELISTDRAWPARAMS pimldp);

			[PreserveSig]
			int Remove(int i);

			[PreserveSig]
			int GetIcon(int i, int flags, ref IntPtr picon);

			[PreserveSig]
			int GetImageInfo(int i, ref IMAGEINFO pImageInfo);

			[PreserveSig]
			int Copy(int iDst, IImageList punkSrc, int iSrc, int uFlags);

			[PreserveSig]
			int Merge(int i1, IImageList punk2, int i2, int dx, int dy, ref Guid riid, ref IntPtr ppv);

			[PreserveSig]
			int Clone(ref Guid riid, ref IntPtr ppv);

			[PreserveSig]
			int GetImageRect(int i, ref RECT prc);

			[PreserveSig]
			int GetIconSize(ref int cx, ref int cy);

			[PreserveSig]
			int SetIconSize(int cx, int cy);

			[PreserveSig]
			int GetImageCount(ref int pi);

			[PreserveSig]
			int SetImageCount(int uNewCount);

			[PreserveSig]
			int SetBkColor(int clrBk, ref int pclr);

			[PreserveSig]
			int GetBkColor(ref int pclr);

			[PreserveSig]
			int BeginDrag(int iTrack, int dxHotspot, int dyHotspot);

			[PreserveSig]
			int EndDrag();

			[PreserveSig]
			int DragEnter(IntPtr hwndLock, int x, int y);

			[PreserveSig]
			int DragLeave(IntPtr hwndLock);

			[PreserveSig]
			int DragMove(int x, int y);

			[PreserveSig]
			int SetDragCursorImage(ref IImageList punk, int iDrag, int dxHotspot, int dyHotspot);

			[PreserveSig]
			int DragShowNolock(int fShow);

			[PreserveSig]
			int GetDragImage(ref POINT ppt, ref POINT pptHotspot, ref Guid riid, ref IntPtr ppv);

			[PreserveSig]
			int GetItemFlags(int i, ref int dwFlags);

			[PreserveSig]
			int GetOverlayImage(int iOverlay, ref int piIndex);
		};

		#endregion

		// <summary>
		/// Return large file icon of the specified file.
		/// </summary>
		internal static unsafe Icon GetFileIcon(string fileName)
		{
			SHFILEINFO shinfo = new SHFILEINFO();

			uint flags = SHGFI_SYSICONINDEX;
			if (fileName.IndexOf(":") == -1) flags |= SHGFI_USEFILEATTRIBUTES;
			else flags |= SHGFI_ICON;

			SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

			var iconIndex = shinfo.iIcon;
			Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
			// Try to load 256x256 pixels Icon
			SHGetImageList(SHIL_JUMBO, ref iidImageList, out IImageList iml);

			IntPtr hIcon = IntPtr.Zero;
			const int ILD_TRANSPARENT = 1;
			iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
			Bitmap bmp = Bitmap.FromHicon(hIcon);


			if (fileName.Contains("Cheat"))
				Console.WriteLine();

			// If our icon looks something like this we have to load a smaller version of the icon
			//
			//		Where '*' is non a transparent pixel and '-' is a transparent pixel
			//
			//		***-------------
			//		***-------------
			//		***-------------
			//		----------------
			//		----------------
			//		----------------
			//		----------------
			//
			// It means that the actual icon have a size of 48x48 pixels, not a 256x256 pixels
			// In this case using the SHIL_JUMBO is a bad idea and we have to use the SHIL_EXTRALARGE
			// to load a smaller icon
			if (IsIconAlmostEmpty(bmp) == true)
			{
				// Try to load 48x48 pixels Icon
				SHGetImageList(SHIL_EXTRALARGE, ref iidImageList, out iml);
				hIcon = IntPtr.Zero;
				iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
			}
			bmp.Dispose();

			return Icon.FromHandle(hIcon);
		}

		/// <summary>
		/// Determines how many pixels of the specified image are not transparent and returns a boolean
		/// that shows whether the input image is almost empty
		/// </summary>
		/// <param name="image">The image for check</param>
		/// <returns>True if the image have less than or equal to 48*48 non transparent pixels, otherwise False</returns>
		private static unsafe bool IsIconAlmostEmpty(Bitmap image)
		{
			var bmpRect = new Rectangle(0, 0, image.Width, image.Height);
			BitmapData imageData = image.LockBits(bmpRect, ImageLockMode.ReadOnly, image.PixelFormat);

			byte bitsPerPixel = (byte)Image.GetPixelFormatSize(image.PixelFormat);

			/*This time we convert the IntPtr to a ptr*/
			byte* scan0 = (byte*)imageData.Scan0.ToPointer();

			int actualNumberOfNonTransparentPixels = 0;
			for (int i = 0; i < imageData.Height; i += 1)
			{
				for (int j = 0; j < imageData.Width; j += 1)
				{
					byte* pixelPointer = scan0 + i * imageData.Stride + j * bitsPerPixel / 8;

					var alpha = pixelPointer[3];

					if (alpha != 0) actualNumberOfNonTransparentPixels += 1;
				}
			}
			image.UnlockBits(imageData);

			const int minNumberOfNonTransparentPixels = 48 * 48;

			return actualNumberOfNonTransparentPixels <= minNumberOfNonTransparentPixels;
		}

		/// <summary>
		/// Converts the source Bitmap object to a BitmapSource instance and releases the handle pointer
		/// of the source object
		/// </summary>
		/// <param name="source">The source Bitmap object</param>
		/// <returns></returns>
		public static BitmapSource LoadBitmap(Bitmap source)
		{
			IntPtr hBitmap = source.GetHbitmap();
			//Memory Leak fixes, for more info : http://social.msdn.microsoft.com/forums/en-US/wpf/thread/edcf2482-b931-4939-9415-15b3515ddac6/
			try
			{
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
				   BitmapSizeOptions.FromEmptyOptions());
			}
			finally
			{
				DeleteObject(hBitmap);
			}

		}

		/// <summary>
		/// Converts the source Icon object to a BitmapSource instance and releases the handle pointer
		/// of the source object
		/// </summary>
		/// <param name="source">The source Icon object</param>
		/// <returns></returns>
		public static BitmapSource LoadBitmap(Icon source)
		{
			IntPtr hIcon = source.Handle;
			//Memory Leak fixes, for more info : http://social.msdn.microsoft.com/forums/en-US/wpf/thread/edcf2482-b931-4939-9415-15b3515ddac6/
			try
			{
				var sourceRect = new Int32Rect(0, 0, source.Width, source.Height);
				return Imaging.CreateBitmapSourceFromHIcon(hIcon, sourceRect,
					BitmapSizeOptions.FromEmptyOptions());
			}
			finally
			{
				DeleteObject(hIcon);
			}

		}
	}
}