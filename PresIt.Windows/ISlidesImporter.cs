using System.Collections.Generic;

namespace PresIt.Windows {
    public interface ISlidesImporter {
        IEnumerable<SlidesImporterStatus> Convert(string filename);
    }
}