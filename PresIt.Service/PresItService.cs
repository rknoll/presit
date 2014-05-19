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

        private readonly Dictionary<string, Presentation> presentations = new Dictionary<string, Presentation>();
        private readonly Dictionary<string, string> authentications = new Dictionary<string, string>();
        private readonly Dictionary<string, AutoResetEvent> authenticationRequests = new Dictionary<string, AutoResetEvent>();
        private readonly Dictionary<string, CommandRequest> commandRequests = new Dictionary<string, CommandRequest>();

        public Presentation CreatePresentation(string clientId, string name) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return null;
            if (string.IsNullOrEmpty(name)) return null;
            if (presentations.Values.Any(p => p.Owner == authentications[clientId] && p.Name == name)) return null;
            var presentation = new Presentation { Name = name, Owner = authentications[clientId], Id = Guid.NewGuid().ToString() };
            presentations.Add(presentation.Id, presentation);
            return presentation;
        }

        public bool DeletePresentation(string clientId, string presentationId) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return false;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return false;
            if (presentations[presentationId].Owner != authentications[clientId]) return false;
            presentations.Remove(presentationId);
            return true;
        }

        public Presentation GetPresentation(string clientId, string presentationId) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return null;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return null;
            if (presentations[presentationId].Owner != authentications[clientId]) return null;
            return presentations[presentationId];
        }

        public void AuthenticateId(string deviceId, string clientId) {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(clientId)) return;
            if (authentications.ContainsValue(deviceId)) {
                authentications.Remove(authentications.First(pair => pair.Value == deviceId).Key);
            }
            authentications[clientId] = deviceId;
            if (authenticationRequests.ContainsKey(clientId)) authenticationRequests[clientId].Set();
        }

        public bool IsAuthenticated(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return false;

            //AuthenticateId("dev", clientId);

            if (authenticationRequests.ContainsKey(clientId)) authenticationRequests.Remove(clientId);
            if (authentications.ContainsKey(clientId)) return true;
            authenticationRequests.Add(clientId, new AutoResetEvent(false));
            var success = authenticationRequests[clientId].WaitOne(new TimeSpan(0, 0, 10));
            authenticationRequests.Remove(clientId);
            return success;
        }

        public CommandType GetNextCommand(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return CommandType.Error;
            if (!authentications.ContainsKey(clientId)) return CommandType.Error;
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

        public void NextSlide(string deviceId) {
            if (string.IsNullOrEmpty(deviceId)) return;
            var clientId = authentications.FirstOrDefault(pair => pair.Value == deviceId).Key;
            if (clientId == null) return;

            if (commandRequests.ContainsKey(clientId)) {
                commandRequests[clientId].CommandType = CommandType.NextSlide;
                commandRequests[clientId].CommandEvent.Set();
            }
        }

        public void PreviousSlide(string deviceId) {
            if (string.IsNullOrEmpty(deviceId)) return;
            var clientId = authentications.FirstOrDefault(pair => pair.Value == deviceId).Key;
            if (clientId == null) return;

            if (commandRequests.ContainsKey(clientId)) {
                commandRequests[clientId].CommandType = CommandType.PreviousSlide;
                commandRequests[clientId].CommandEvent.Set();
            }
        }

        public IEnumerable<PresentationPreview> GetPresentationPreviews(string clientId) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return null;
            return presentations.Values.Where(p => p.Owner == authentications[clientId]).Select(presentation => new PresentationPreview {
                Id = presentation.Id,
                Name = presentation.Name,
                FirstSlide = presentation.Slides != null ? presentation.Slides.FirstOrDefault() : null
            });
        }

        public bool UpdateSlides(string clientId, Presentation presentation) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return false;
            if (presentation == null || string.IsNullOrEmpty(presentation.Id) || !presentations.ContainsKey(presentation.Id)) return false;
            if (presentations[presentation.Id].Owner != authentications[clientId]) return false;
            presentations[presentation.Id].Slides = presentation.Slides;
            return true;
        }
    }
}