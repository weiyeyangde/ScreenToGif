using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Timers;
using System.Runtime.InteropServices;
using ImageMagick;
using System.Drawing.Imaging;

namespace ScreenToGif
{
    public partial class Form1 : Form
    {   
        private static int physical_screen_width;
        private static int physical_screen_height;
        private static int logical_screen_height;
        private static float scaling_factor;
        private const Int32 CURSOR_SHOWING = 0x00000001;
        private static System.Timers.Timer aTimer;
        private static int i = 0;
        private static bool on = true;
        private static List<MagickImage> images = new List<MagickImage>();
        private static string default_path;
        private static string path;


        public Form1()
        {
            InitializeComponent();
            start_button.Text = "Start";
            GetSreenSize();
            GetScalingFactor();
            SetDefaultDirectory();
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            if (on)
            {
                if (directory_text_box.Text != default_path & !IsValidPath(directory_text_box.Text))
                {
                    MessageBox.Show("Please enter a valid directory.");
                    directory_text_box.Text = default_path;
                }
                else
                {
                    path = directory_text_box.Text;
                    Directory.CreateDirectory(path);
                    directory_text_box.Enabled = false;
                    on = false;
                    start_button.Text = "Stop";
                    SetTimer();
                }   
            }
            else
            {
                on = true;
                start_button.Text = "Start";
                directory_text_box.Enabled = true;
                aTimer.Stop();
                aTimer.Dispose();
                aTimer = null;
                await Task.Run(() => CreateAnimatedGif());
            }
        }

        private static void SetTimer()
        {   
            aTimer = new System.Timers.Timer(100);
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            aTimer.Elapsed += CaptureScreen;               
        }

        private static void CaptureScreen(Object source, ElapsedEventArgs e)
        {   
            if (!on)
            {
                Bitmap bitmap = CaptureScreen();               
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    var img = new MagickImage(ms);
                    img.Resize(new Percentage(40));
                    images.Add(img);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
            DESKTOPHORZRES = 118,
        }

        private static void GetSreenSize()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            physical_screen_width = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPHORZRES);
            physical_screen_height = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            logical_screen_height = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
        }

        private static void GetScalingFactor()
        {
            scaling_factor = (float)physical_screen_height / (float)logical_screen_height;
        }

        public static Bitmap CaptureScreen()
        {
            Bitmap bitmap = new Bitmap(physical_screen_width, physical_screen_height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        DrawIcon(g.GetHdc(), (int)(pci.ptScreenPos.x * scaling_factor), 
                                            (int)(pci.ptScreenPos.y * scaling_factor), pci.hCursor);
                        g.ReleaseHdc();
                    }
                }
            }          
            return bitmap;
        }

        private static void CreateAnimatedGif()
        {
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                foreach (var img in images)
                {                    
                     collection.Add(img);
                }
                //collection[0].AnimationDelay = 100;
                images = new List<MagickImage>();

                // Optionally reduce colors
                QuantizeSettings settings = new QuantizeSettings();
                settings.Colors = 256;
                collection.Quantize(settings);

                // Optionally optimize the images (images should have the same size).
                collection.Optimize();

                // Save gif
                string fileName = string.Format("ScreenShot{0}.gif", i); i++;
                collection.Write(Path.Combine(path, fileName));
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.directory_text_box.Text = default_path;
        }

        private static bool IsValidPath(string path)
        {
            return Directory.Exists(path);
        }

        private static void SetDefaultDirectory()
        {
            default_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                                        "ScreenToGif");
        }      
    }
}
