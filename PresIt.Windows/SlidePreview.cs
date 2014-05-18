using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using PresIt.Data;
using PresIt.Windows.Properties;

namespace PresIt.Windows {
    public class SlidePreview {

        private readonly string slideText;
        private readonly BitmapImage slideImage;
        private readonly string presentationId;

        public SlidePreview(Slide slide, string presentationId, string text = null) {
            if (text != null) {
                slideText = text;
            } else {
                if (slide != null) {
                    slideText = "" + slide.SlideNumber;
                } else {
                    slideText = "New Slide";
                }
            }

            this.presentationId = presentationId;

            slideImage = new BitmapImage();

            if (slide != null && slide.ImageData != null) {
                slideImage.BeginInit();
                slideImage.StreamSource = new MemoryStream(slide.ImageData);
                slideImage.EndInit();
            } else {

                Bitmap bitmap;

                if (slide == null) {
                    bitmap = new Bitmap(Resources.newSlide);
                } else {
                    bitmap = new Bitmap(1024, 768);
                    var g = Graphics.FromImage(bitmap);
                    switch (slide.SlideNumber) {
                        case 1:
                            g.Clear(Color.Red);
                            break;
                        case 2:
                            g.Clear(Color.Blue);
                            break;
                        default:
                            g.Clear(Color.LightGray);
                            break;
                    }
                }

                byte[] buffer;

                using (var memory = new MemoryStream()) {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    using (var br = new BinaryReader(memory)) {
                        buffer = br.ReadBytes((Int32)memory.Length);
                    }
                }

                if (slide != null) slide.ImageData = buffer;

                slideImage.BeginInit();
                slideImage.StreamSource = new MemoryStream(buffer);
                slideImage.EndInit();
            }

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