using PresIt.Data;

namespace PresIt.Service {
    public class PresItService : IPresItService {
        public Presentation CreatePresentation(string clientId, string name) {
            return new Presentation {Name = name, Owner = clientId};
        }
    }
}