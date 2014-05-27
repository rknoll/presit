using System.ServiceModel;

namespace PresIt.Data {
    [ServiceContract]
    public interface IPresItService {
        [OperationContract]
        Presentation CreatePresentation(string clientId, string name);

        [OperationContract]
        bool UpdateSlide(string clientId, string presentationId, Slide slide);

        [OperationContract]
        bool UpdateSlidesCount(string clientId, string presentationId, int slidesCount);

        [OperationContract]
        bool DeletePresentation(string clientId, string presentationId);

        [OperationContract]
        Slide GetPresentationSlide(string clientId, string presentationId, int slideIndex);

        [OperationContract]
        int GetPresentationSlidesCount(string clientId, string presentationId);

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
        int GetPresentationCount(string clientId);

        [OperationContract]
        PresentationPreview GetPresentationPreview(string clientId, int presentationIndex);
    }
}