using System.ServiceModel;

namespace PresIt.Data {
    [ServiceContract]
    public interface IPresItService {
        [OperationContract]
        Presentation CreatePresentation(string clientId, string name);

        [OperationContract]
        bool UpdateSlides(string clientId, Presentation presentation);

        [OperationContract]
        void AuthenticateId(string deviceId, string clientId);

        [OperationContract]
        bool IsAuthenticated(string clientId);
    }
}