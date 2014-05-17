using System.ServiceModel;

namespace PresIt.Data {
    [ServiceContract]
    public interface IPresItService {
        [OperationContract]
        Presentation CreatePresentation(string clientId, string name);
    }
}