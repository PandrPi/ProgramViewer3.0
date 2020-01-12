using System;
using System.Windows.Media;

namespace Program_Viewer_3
{
    public static class IconExtractor
    {
        public static ImageSource GetIcon(string filename)
        {
            if (System.IO.File.Exists(filename))
            {
                using (System.Drawing.Icon sysicon = System.Drawing.Icon.ExtractAssociatedIcon(filename))
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle,
                        System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            else
            {
                return null;
            }
        }
    }
}
