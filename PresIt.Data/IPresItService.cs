using System.ServiceModel;

namespace PresIt.Data {

    /// <summary>
    /// The main service of PresIt
    /// </summary>
    [ServiceContract]
    public interface IPresItService {

        /// <summary>
        /// Creates a new Presentation with a Name on the Server
        /// </summary>
        [OperationContract]
        Presentation CreatePresentation(string clientId, string name);

        /// <summary>
        /// Update a specific Slide from a Presentation on the Server
        /// </summary>
        [OperationContract]
        bool UpdateSlide(string clientId, string presentationId, Slide slide);

        /// <summary>
        /// Update the Number of Slides of a Presentation on the Server
        /// </summary>
        [OperationContract]
        bool UpdateSlidesCount(string clientId, string presentationId, int slidesCount);

        /// <summary>
        /// Delete a Presentation on the Server
        /// </summary>
        [OperationContract]
        bool DeletePresentation(string clientId, string presentationId);

        /// <summary>
        /// Get a specific Slide from a Presentation on the Server
        /// </summary>
        [OperationContract]
        Slide GetPresentationSlide(string clientId, string presentationId, int slideIndex);

        /// <summary>
        /// Get the Number of Slides of a Presentation
        /// </summary>
        [OperationContract]
        int GetPresentationSlidesCount(string clientId, string presentationId);

        /// <summary>
        /// Authenticate a Random Session ID with a Client ID
        /// </summary>
        [OperationContract]
        void AuthenticateId(string clientId, string sessionId);

        /// <summary>
        /// Check if a session ID is Authenticated
        /// </summary>
        [OperationContract]
        string IsAuthenticated(string sessionId);

        /// <summary>
        /// Get the next Command from the Server to the Client Application
        /// </summary>
        [OperationContract]
        CommandType GetNextCommand(string clientId);

        /// <summary>
        /// Send Command to Client
        /// </summary>
        [OperationContract]
        void SendCommand(string clientId, CommandType command);

        /// <summary>
        /// Get the Number of stored Presentations for a User
        /// </summary>
        [OperationContract]
        int GetPresentationCount(string clientId);

        /// <summary>
        /// Get the preview of a specific Presentation from the Server
        /// </summary>
        [OperationContract]
        PresentationPreview GetPresentationPreview(string clientId, int presentationIndex);
    }
}