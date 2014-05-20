using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using PresIt.Data;

namespace PresIt.Service {
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class PresItService : IPresItService {

        private class CommandRequest {
            public AutoResetEvent CommandEvent { get; set; }
            public CommandType CommandType { get; set; }
        }
        
        private class AuthenticationRequest {
            public AutoResetEvent AuthenticationEvent { get; set; }
            public string ClientId { get; set; }
        }

        private readonly Dictionary<string, Presentation> presentations = new Dictionary<string, Presentation>();
        private readonly Dictionary<string, AuthenticationRequest> authenticationRequests = new Dictionary<string, AuthenticationRequest>();
        private readonly Dictionary<string, CommandRequest> commandRequests = new Dictionary<string, CommandRequest>();

        public Presentation CreatePresentation(string clientId, string name) {
            if (string.IsNullOrEmpty(clientId)) return null;
            if (string.IsNullOrEmpty(name)) return null;
            if (presentations.Values.Any(p => p.Owner == clientId && p.Name == name)) return null;
            var presentation = new Presentation { Name = name, Owner = clientId, Id = Guid.NewGuid().ToString() };
            presentations.Add(presentation.Id, presentation);
            return presentation;
        }

        public bool DeletePresentation(string clientId, string presentationId) {
            if (string.IsNullOrEmpty(clientId)) return false;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return false;
            if (presentations[presentationId].Owner != clientId) return false;
            presentations.Remove(presentationId);
            return true;
        }

        public Presentation GetPresentation(string clientId, string presentationId) {
            if (string.IsNullOrEmpty(clientId)) return null;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return null;
            if (presentations[presentationId].Owner != clientId) return null;
            return presentations[presentationId];
        }

        public void AuthenticateId(string clientId, string sessionId) {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(sessionId)) return;
            if (authenticationRequests.ContainsKey(sessionId)) {
                authenticationRequests[sessionId].ClientId = clientId;
                authenticationRequests[sessionId].AuthenticationEvent.Set();
            }
        }

        public string IsAuthenticated(string sessionId) {
            if (string.IsNullOrEmpty(sessionId)) return null;
            if (authenticationRequests.ContainsKey(sessionId)) authenticationRequests.Remove(sessionId);
            authenticationRequests.Add(sessionId, new AuthenticationRequest {AuthenticationEvent = new AutoResetEvent(false)});
            var success = authenticationRequests[sessionId].AuthenticationEvent.WaitOne(new TimeSpan(0, 0, 10));
            var clientId = success ? authenticationRequests[sessionId].ClientId : null;
            authenticationRequests.Remove(sessionId);
            return clientId;
        }

        public CommandType GetNextCommand(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return CommandType.Error;
            if (commandRequests.ContainsKey(clientId)) commandRequests.Remove(clientId);
            commandRequests.Add(clientId, new CommandRequest {
                CommandEvent = new AutoResetEvent(false),
                CommandType = CommandType.None
            });
            commandRequests[clientId].CommandEvent.WaitOne(new TimeSpan(0, 0, 10));
            var command = commandRequests[clientId].CommandType;
            commandRequests.Remove(clientId);
            return command;
        }

        public void NextSlide(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return;
            if (commandRequests.ContainsKey(clientId)) {
                commandRequests[clientId].CommandType = CommandType.NextSlide;
                commandRequests[clientId].CommandEvent.Set();
            }
        }

        public void PreviousSlide(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return;
            if (commandRequests.ContainsKey(clientId)) {
                commandRequests[clientId].CommandType = CommandType.PreviousSlide;
                commandRequests[clientId].CommandEvent.Set();
            }
        }

        public IEnumerable<PresentationPreview> GetPresentationPreviews(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return null;
            return presentations.Values.Where(p => p.Owner == clientId).Select(presentation => new PresentationPreview {
                Id = presentation.Id,
                Name = presentation.Name,
                FirstSlide = presentation.Slides != null ? presentation.Slides.FirstOrDefault() : null
            });
        }

        public bool UpdateSlides(string clientId, Presentation presentation) {
            if (string.IsNullOrEmpty(clientId)) return false;
            if (presentation == null || string.IsNullOrEmpty(presentation.Id) || !presentations.ContainsKey(presentation.Id)) return false;
            if (presentations[presentation.Id].Owner != clientId) return false;
            presentations[presentation.Id].Slides = presentation.Slides;
            return true;
        }
    }
}