namespace PresIt.Windows {

    /// <summary>
    /// Represents the Result of one Imported Slide
    /// </summary>
    public class SlidesImporterStatus {
        public int TotalSlides { get; set; }
        public int CurrentSlideIndex { get; set; }
        public byte[] CurrentSlideData { get; set; }
    }
}