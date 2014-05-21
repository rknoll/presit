using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PresIt.Windows {
    public class ImageImporter : ISlidesImporter {
        public IEnumerable<SlidesImporterStatus> Convert(string filename) {
            var image = Image.FromFile(filename);
            
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

            using (var memory = new MemoryStream()) {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                using (var br = new BinaryReader(memory)) {
                    yield return new SlidesImporterStatus {
                        CurrentSlideData = br.ReadBytes((Int32) memory.Length),
                        CurrentSlideIndex = 1,
                        TotalSlides = 1
                    };
                }
            }
        }
    }
}