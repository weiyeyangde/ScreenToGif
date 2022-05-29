using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Timers;
using ImageMagick;
using System.Drawing.Imaging;
using ScreenToGif.Interop;
using Timer = System.Timers.Timer;

namespace ScreenToGif
{
    public partial class Main : Form
    {
        private const string ButtonTextStart = "Start Recording";
        private const string ButtonTextStop = "Stop Recording";

        private readonly string DefaultResultSavePath;

        private bool On = true;
        private int screenshotIdx = 0;
        private string resultDirPath;
        private Timer screenshotTimer;
        private List<MagickImage> images = new List<MagickImage>();

        public Main()
        {
            InitializeComponent();
            start_button.Text = ButtonTextStart;
            DefaultResultSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreenToGif");
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            if (On)
            {
                if (directory_text_box.Text != DefaultResultSavePath & !Directory.Exists(directory_text_box.Text))
                {
                    MessageBox.Show("Please enter a valid directory.");
                    directory_text_box.Text = DefaultResultSavePath;
                }
                else
                {
                    resultDirPath = directory_text_box.Text;
                    Directory.CreateDirectory(resultDirPath);
                    directory_text_box.Enabled = false;
                    On = false;
                    start_button.Text = ButtonTextStop;
                    SetTimer();
                }   
            }
            else
            {
                On = true;
                start_button.Text = ButtonTextStart;
                directory_text_box.Enabled = true;

                screenshotTimer.Stop();
                screenshotTimer.Dispose();
                screenshotTimer = null;

                await Task.Run(() => CreateAnimatedGif());
            }
        }

        private void SetTimer()
        {   
            screenshotTimer = new System.Timers.Timer(100);
            screenshotTimer.Elapsed += CaptureScreen;               
            screenshotTimer.AutoReset = true;
            screenshotTimer.Enabled = true;
        }

        private void CaptureScreen(Object source, ElapsedEventArgs e)
        {   
            if (!On)
            {
                using (Bitmap bitmap = DisplayUtils.CaptureScreen())
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

        private void CreateAnimatedGif()
        {
            if (images.Count == 0)
            {
                // If no screenshot taken, just return
                return;
            }

            using (MagickImageCollection collection = new MagickImageCollection())
            {
                collection.AddRange(images);
                images.Clear();

                // Optionally reduce colors
                QuantizeSettings settings = new QuantizeSettings();
                settings.Colors = 256;
                collection.Quantize(settings);

                // Optionally optimize the images (images should have the same size).
                collection.Optimize();

                // Save gif
                string fileName = string.Format("ScreenShot{0}.gif", screenshotIdx); screenshotIdx++;
                collection.Write(Path.Combine(resultDirPath, fileName));
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.directory_text_box.Text = DefaultResultSavePath;
        }
    }
}
