using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

namespace PresIt.Windows {
    class PowerPointImporter : ISlidesImporter {

        /// <summary>
        /// Use an ImageImporter internally to Convert a saved png powerpoint Slide
        /// </summary>
        private readonly ISlidesImporter imageImporter;

        public PowerPointImporter() {
            imageImporter = new ImageImporter();
        }

        public bool CanHandle(string file) {
            return file.ToLower().EndsWith(".ppt") ||
                   file.ToLower().EndsWith(".pptx");
        }

        public IEnumerable<SlidesImporterStatus> Convert(string filename) {
            var pptApplication = new Application();
            var pptPresentation = pptApplication.Presentations.Open(filename, MsoTriState.msoFalse, MsoTriState.msoFalse, MsoTriState.msoFalse);
            
            var directoryName = Path.GetRandomFileName();
            directoryName = Path.Combine(Path.GetTempPath(), directoryName);
            Directory.CreateDirectory(directoryName);
            var fileName = Path.Combine(directoryName, "slide.png");

            var slideCount = pptPresentation.Slides.Count;
            var currentSlide = 1;

            foreach (_Slide slide in pptPresentation.Slides) {
                var h = pptPresentation.PageSetup.SlideHeight / pptPresentation.PageSetup.SlideWidth * 1024.0;
                slide.Export(fileName, "png", 1024, (int)h);

                var s = imageImporter.Convert(fileName).First();
                s.TotalSlides = slideCount;
                s.CurrentSlideIndex = currentSlide++;

                yield return s;

                File.Delete(fileName);
            }

            Directory.Delete(directoryName, true);
        }
    }
}