using System.Collections.Generic;
using System.ServiceModel;

namespace PresIt.Data {
    [ServiceContract]
    public interface IPresItService {
        [OperationContract]
        Presentation CreatePresentation(string clientId, string name);

        [OperationContract]
        bool UpdateSlides(string clientId, Presentation presentation);

        [OperationContract]
        bool DeletePresentation(string clientId, string presentationId);

        [OperationContract]
        Presentation GetPresentation(string clientId, string presentationId);

        [OperationContract]
        void AuthenticateId(string deviceId, string clientId);

        [OperationContract]
        bool IsAuthenticated(string clientId);

        [OperationContract]
        CommandType GetNextCommand(string clientId);

        [OperationContract]
        void NextSlide(string deviceId);

        [OperationContract]
        void PreviousSlide(string deviceId);

        [OperationContract]
        IEnumerable<PresentationPreview> GetPresentationPreviews(string clientId);
    }
}