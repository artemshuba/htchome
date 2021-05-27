using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace Home.Base
{
    public static class WinAPI
    {
        public const int GwlExstyle = -20;

        public const int GwlStyle = -16;

        public const uint WM_SETICON = 0x0080;
        public const uint WM_CLOSE = 0x0010;

        public const int WS_EX_DLGMODALFRAME = 0x0001;
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_FRAMECHANGED = 0x0020;
        public const int SWP_NOACTIVATE = 0x0010;

        public const int WsExToolwindow = 0x00000080;

        public const int Flip3D = 8;

        [StructLayout(LayoutKind.Sequential)]
        public struct Margins
        {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        };

        public struct BbStruct //Blur Behind Structure
        {
            public BbFlags Flags;
            public bool Enable;
            public IntPtr Region;
            public bool TransitionOnMaximized;
        }

        [Flags]
        public enum BbFlags : byte //Blur Behind Flags
        {
            DwmBbEnable = 1,
            DwmBbBlurregion = 2,
            DwmBbTransitiononmaximized = 4,
        };

        public enum Flip3DPolicy
        {
            Default = 0,
            ExcludeBelow,
            ExcludeAbove
        }


        [DllImport("DwmApi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref BbStruct blurBehind);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr window, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr window, int index, uint value);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int xradius, int yradius);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateEllipticRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("kernel32")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool FreeConsole();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("shell32.dll")]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        public static extern bool SetWindowRgn(IntPtr hwnd, IntPtr rgn, bool redraw);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        public static void RemoveWindowIcon(IntPtr hwnd)
        {
            // Change the extended window style to not show a window icon
            int extendedStyle = GetWindowLong(hwnd, GwlExstyle);
            SetWindowLong(hwnd, GwlExstyle, (uint)extendedStyle | WS_EX_DLGMODALFRAME);

            // Update the window's non-client area to reflect the changes
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE |
                  SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }
}
