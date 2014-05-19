using System.Collections.Generic;

namespace PresIt.Windows {
    public interface ISlidesImporter {
        IEnumerable<byte[]> Convert(string filename);
    }
}