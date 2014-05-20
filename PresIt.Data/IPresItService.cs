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
        void AuthenticateId(string clientId, string sessionId);

        [OperationContract]
        string IsAuthenticated(string sessionId);

        [OperationContract]
        CommandType GetNextCommand(string clientId);

        [OperationContract]
        void NextSlide(string clientId);

        [OperationContract]
        void PreviousSlide(string clientId);

        [OperationContract]
        IEnumerable<PresentationPreview> GetPresentationPreviews(string clientId);
    }
}