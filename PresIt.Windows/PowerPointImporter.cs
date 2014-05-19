/*using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

namespace PresIt.Windows {
    class PowerPointImporter : ISlidesImporter {
        public IEnumerable<byte[]> Convert(string filename) {
            var pptApplication = new Application();
            var pptPresentation = pptApplication.Presentations.Open(filename, MsoTriState.msoFalse, MsoTriState.msoFalse, MsoTriState.msoFalse);
            
            var directoryName = Path.GetRandomFileName();
            directoryName = Path.Combine(Path.GetTempPath(), directoryName);
            Directory.CreateDirectory(directoryName);
            var fileName = Path.Combine(directoryName, "slide.png");

            foreach (_Slide slide in pptPresentation.Slides) {
                slide.Export(fileName, "png", 1024, 768);
                var fs = new FileStream(fileName, FileMode.Open) {Position = 0};
                using (var br = new BinaryReader(fs)) {
                    yield return br.ReadBytes((Int32)fs.Length);
                }
                fs.Close();
                File.Delete(fileName);
            }

            Directory.Delete(directoryName, true);
        }
    }
}*/