using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using PresIt.Data;

namespace PresIt.Service {
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class PresItService : IPresItService {

        private readonly Dictionary<string, Presentation> presentations = new Dictionary<string, Presentation>();
        private readonly Dictionary<string, string> authentications = new Dictionary<string, string>();

        public Presentation CreatePresentation(string clientId, string name) {
            if (string.IsNullOrEmpty(name)) return null;
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return null;
            if (presentations.Values.Any(p => p.Owner == authentications[clientId] && p.Name == name)) return null;
            var presentation = new Presentation { Name = name, Owner = authentications[clientId], Id = Guid.NewGuid().ToString() };
            presentations.Add(presentation.Id, presentation);
            return presentation;
        }

        public void AuthenticateId(string deviceId, string clientId) {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(clientId)) return;
            authentications[clientId] = deviceId;
        }

        public bool IsAuthenticated(string clientId) {
            return !(string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId));
        }

        public bool UpdateSlides(string clientId, Presentation presentation) {
            if (presentation == null || string.IsNullOrEmpty(presentation.Id) || !presentations.ContainsKey(presentation.Id)) return false;
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return false;
            if (presentations[presentation.Id].Owner != authentications[clientId]) return false;
            presentations[presentation.Id].Slides = presentation.Slides;
            return true;
        }
    }
}