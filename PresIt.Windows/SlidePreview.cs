using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using PresIt.Data;
using PresIt.Windows.Properties;

namespace PresIt.Windows {

    /// <summary>
    /// Represents a Preview of a Slide, where the raw image is correctly converted already
    /// </summary>
    public class SlidePreview {

        private string slideText;
        private BitmapImage slideImage;
        private string presentationId;

        public static SlidePreview CreateFromSlide(Slide slide, string presentationId, string text = null) {
            var preview = new SlidePreview();
            preview.slideText = text ?? (slide != null ? ("" + slide.SlideNumber) : "1");
            preview.presentationId = presentationId;

            preview.slideImage = new BitmapImage();

            byte[] buffer;

            if (slide == null || slide.ImageData == null) {
                var bitmap = new Bitmap(1024, 768);
                var g = Graphics.FromImage(bitmap);
                g.Clear(Color.Transparent);

                using (var memory = new MemoryStream()) {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    using (var br = new BinaryReader(memory)) {
                        buffer = br.ReadBytes((Int32) memory.Length);
                    }
                }

                if (slide != null) slide.ImageData = buffer;
            } else {
                buffer = slide.ImageData;
            }

            preview.slideImage.BeginInit();
            preview.slideImage.StreamSource = new MemoryStream(buffer);
            preview.slideImage.EndInit();

            return preview;
        }

        private SlidePreview() {
        }

        public static SlidePreview CreateAddNewSlide(string presentationId, string text = "New Slide") {
            var preview = new SlidePreview();
            preview.slideText = text;
            preview.presentationId = presentationId;

            preview.slideImage = new BitmapImage();

            var bitmap = new Bitmap(Resources.newSlide);

            byte[] buffer;

            using (var memory = new MemoryStream()) {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                using (var br = new BinaryReader(memory)) {
                    buffer = br.ReadBytes((Int32)memory.Length);
                }
            }

            preview.slideImage.BeginInit();
            preview.slideImage.StreamSource = new MemoryStream(buffer);
            preview.slideImage.EndInit();

            return preview;
        }

        public string SlideText {
            get { return slideText; }
        }

        public BitmapImage SlideImage {
            get { return slideImage; }
        }

        public string PresentationId {
            get { return presentationId; }
        }
    }
}