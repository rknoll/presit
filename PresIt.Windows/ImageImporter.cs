using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PresIt.Windows {

    /// <summary>
    /// Imports an Image into a presentation
    /// </summary>
    public class ImageImporter : ISlidesImporter {
        public bool CanHandle(string file) {
            return file.ToLower().EndsWith(".jpg") ||
                   file.ToLower().EndsWith(".jpeg") ||
                   file.ToLower().EndsWith(".png") ||
                   file.ToLower().EndsWith(".bmp");
        }

        public IEnumerable<SlidesImporterStatus> Convert(string filename) {
            var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(filename)));
            
            var bitmap = new Bitmap(1024, 768);
            var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);

            var ratio = ((float) image.Width)/image.Height;
            
            float scaledWidth;
            float scaledHeight;

            if (ratio < ((float) 1024)/768) {
                scaledWidth = ((float) image.Width)/image.Height*768;
                scaledHeight = 768;
            } else {
                scaledWidth = 1024;
                scaledHeight = ((float)image.Height) / image.Width * 1024;
            }

            g.DrawImage(image, (1024 - scaledWidth)/2, (768 - scaledHeight)/2, scaledWidth, scaledHeight);
            SlidesImporterStatus slide;

            using (var memory = new MemoryStream()) {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                using (var br = new BinaryReader(memory)) {
                    slide = new SlidesImporterStatus {
                        CurrentSlideData = br.ReadBytes((Int32) memory.Length),
                        CurrentSlideIndex = 1,
                        TotalSlides = 1
                    };
                }
            }

            bitmap.Dispose();
            image.Dispose();

            yield return slide;
        }
    }
}