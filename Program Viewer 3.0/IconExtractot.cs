using System;
using System.Collections.Generic;
using Shell32;
using System.IO;
using System.Drawing;
using System.Drawing.IconLib;
using System.Drawing.IconLib.ColorProcessing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickZip.Tools;

namespace Program_Viewer_3
{
    public static class IconExtractor
    {
        public static ImageSource BaseExeIcon;

        private static FileToIconConverter fic = new FileToIconConverter();
        private static Dictionary<string, ImageSource> cachedImages = new Dictionary<string, ImageSource>();
        private static readonly int DefaultIconSize = 192;
        private static readonly HashSet<string> imageExtensions = 
            new HashSet<string>(new string[] { ".png", ".jpg", ".gif", ".bmp", ".jpeg", ".tga", ".tiff", ".psd", ".pdf" });

        public static ImageSource GetIcon(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if (extension == ".lnk")
            {
                fileName = GetShortcutTargetFile(Path.GetFullPath(fileName));
                extension = Path.GetExtension(fileName);
            }

            if (extension == ".exe")
            {
                try
                {
                    var icon = FileToIconConverter.GetFileIcon(fileName, FileToIconConverter.IconSize.jumbo);
                    BitmapSource image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                                new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
                    image.Freeze();
                    icon.Dispose();
                    return image;
                }
                catch
                {
                    return BaseExeIcon;
                }
            }
            else
            {
                if (cachedImages.ContainsKey(extension))
                {
                    return cachedImages[extension];
                }
                else
                {
                    ImageSource image = fic.GetImage(fileName, DefaultIconSize);
                    if (!imageExtensions.Contains(extension))
                        cachedImages.Add(extension, image);
                    return image;
                }
            }
        }

        private static string GetShortcutTargetFile(string shortcutFilename)
        {
            string pathOnly = Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = Path.GetFileName(shortcutFilename);

            Shell shell = new Shell();
            Folder folder = shell.NameSpace(pathOnly);
            FolderItem folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;
                return link.Path;
            }

            return string.Empty;
        }

        private static void MultiIconDispose(MultiIcon multiIcon)
        {
            for(int i = 0; i < multiIcon.Count; i++)
            {
                SingleIcon icons = multiIcon[i];
                for(int j = 0; j < icons.Count; j++)
                {
                    icons[j].Icon.Dispose();
                }
                multiIcon[i].Icon.Dispose();
            }
        }

    }
}
