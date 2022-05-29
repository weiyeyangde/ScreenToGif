using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScreenToGif.Interop
{
    public enum DeviceCap
    {
        VERTRES = 10,
        DESKTOPVERTRES = 117,
        DESKTOPHORZRES = 118,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINTAPI
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CURSORINFO
    {
        public Int32 cbSize;
        public Int32 flags;
        public IntPtr hCursor;
        public POINTAPI ptScreenPos;
    }

    public static class DisplayUtils
    {
        private const int CURSOR_SHOWING = 0x00000001;

        private static readonly int PhysicalScreenWidth;
        private static readonly int PhysicalScreenHeight;
        private static readonly int LogicalScreenHeight;
        private static readonly double ScalingFactor;

        static DisplayUtils()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();

            PhysicalScreenWidth = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPHORZRES);
            PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            ScalingFactor = (double)PhysicalScreenHeight / (double)LogicalScreenHeight;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public static Bitmap CaptureScreen()
        {
            Bitmap bitmap = new Bitmap(PhysicalScreenWidth, PhysicalScreenHeight);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        DrawIcon(g.GetHdc(), (int)(pci.ptScreenPos.x * ScalingFactor), 
                                            (int)(pci.ptScreenPos.y * ScalingFactor), pci.hCursor);
                        g.ReleaseHdc();
                    }
                }
            }          
            return bitmap;
        }
    }
}
