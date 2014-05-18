using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using PresIt.Data;

namespace PresIt.Windows {
    public class SlidePreview {

        private readonly Slide slide;
        private readonly BitmapImage slideImage;

        public SlidePreview(Slide slide) {
            this.slide = slide;

            slideImage = new BitmapImage();

            if (slide != null && slide.ImageData != null) {
                slideImage.BeginInit();
                slideImage.StreamSource = new MemoryStream(slide.ImageData);
                slideImage.EndInit();
            } else {

                var bitmap = new Bitmap(1024, 768);
                var g = Graphics.FromImage(bitmap);
                g.Clear(Color.LightGray);

                byte[] buffer;

                using (var memory = new MemoryStream()) {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    using (var br = new BinaryReader(memory)) {
                        buffer = br.ReadBytes((Int32)memory.Length);
                    }
                }

                slideImage.BeginInit();
                slideImage.StreamSource = new MemoryStream(buffer);
                slideImage.EndInit();
            }

        }

        public string SlideNumber {
            get { return slide == null ? "New Slide" : ("" + slide.SlideNumber); }
        }

        public BitmapImage SlideImage {
            get { return slideImage; }
        }
    }
}