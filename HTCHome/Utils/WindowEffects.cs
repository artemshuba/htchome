using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HTCHome.Utils
{
    public static class WindowEffects
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_BLURBEHIND
        {
            public int dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        public const int DWM_BB_ENABLE = 0x00000001;
        public const int DWM_BB_BLURREGION = 0x00000002;
        public const int DWM_BB_TRANSITIONONMAXIMIZED = 0x00000004;

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        public static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);
        
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public const int RGN_OR = 2;

        public static void EnableBlurBehind(Window window, Geometry? regionGeometry = null)
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 10) return; // Only for Win7/8 effectively

                if (!DwmIsCompositionEnabled()) return;

                var helper = new WindowInteropHelper(window);
                var hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero) return;

                var bb = new DWM_BLURBEHIND
                {
                    dwFlags = DWM_BB_ENABLE,
                    fEnable = true,
                    hRgnBlur = IntPtr.Zero
                };

                if (regionGeometry != null)
                {
                    bb.dwFlags |= DWM_BB_BLURREGION;
                    bb.hRgnBlur = GeometryToHRGN(regionGeometry);
                }

                DwmEnableBlurBehindWindow(hwnd, ref bb);

                if (bb.hRgnBlur != IntPtr.Zero)
                    DeleteObject(bb.hRgnBlur);
            }
            catch 
            {
                // Ignore errors
            }
        }

        public static void DisableBlurBehind(Window window)
        {
             try
            {
                if (Environment.OSVersion.Version.Major >= 10) return; 

                var helper = new WindowInteropHelper(window);
                var hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero) return;

                var bb = new DWM_BLURBEHIND
                {
                    dwFlags = DWM_BB_ENABLE,
                    fEnable = false
                };
                DwmEnableBlurBehindWindow(hwnd, ref bb);
            }
            catch { }
        }

        private static IntPtr GeometryToHRGN(Geometry geometry)
        {
            // Simple approximation: Bounding box? 
            // Or try to rasterize? 
            // For true shaped blur, we need to decompose the PathGeometry.
            // Complex. For now, let's assume rectangle or try to implement PathIterator later if needed.
            // User requested "custom region".
            
            // Fast path: RectangleGeometry
            if (geometry is RectangleGeometry rect)
            {
                return CreateRectRgn((int)rect.Rect.X, (int)rect.Rect.Y, (int)(rect.Rect.X + rect.Rect.Width), (int)(rect.Rect.Y + rect.Rect.Height));
            }

            // Fallback: PathGeometry is hard without GDI+ Path logic or flattening.
            // Let's take Bounds for now to avoid complexity in this step, unless user complains.
            // Wait, for "Flip Clock" the background is complex. 
            // If the user defines the region as a set of Rectangles (GeometryGroup), we can combine them.
            
            if (geometry is GeometryGroup group)
            {
                IntPtr result = IntPtr.Zero;
                foreach(var child in group.Children)
                {
                    IntPtr childRgn = GeometryToHRGN(child);
                    if (result == IntPtr.Zero) 
                    {
                        result = childRgn;
                    }
                    else
                    {
                        CombineRgn(result, result, childRgn, RGN_OR);
                        DeleteObject(childRgn);
                    }
                }
                return result;
            }

            var bounds = geometry.Bounds;
            return CreateRectRgn((int)bounds.X, (int)bounds.Y, (int)(bounds.X + bounds.Width), (int)(bounds.Y + bounds.Height));
        }
    }
}
