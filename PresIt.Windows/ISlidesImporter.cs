using System.Collections.Generic;

namespace PresIt.Windows {

    /// <summary>
    /// Interface for Slide Importers
    /// </summary>
    public interface ISlidesImporter {

        /// <summary>
        /// Check if this converter can handle a file
        /// </summary>
        bool CanHandle(string file);

        /// <summary>
        /// Convert a file to a list of slides
        /// </summary>
        IEnumerable<SlidesImporterStatus> Convert(string filename);
    }
}