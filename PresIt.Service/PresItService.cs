using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using PresIt.Data;

namespace PresIt.Service {
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class PresItService : IPresItService {

        private readonly Dictionary<string, Presentation> presentations = new Dictionary<string, Presentation>();
        private readonly Dictionary<string, string> authentications = new Dictionary<string, string>();
        private readonly Dictionary<string, AutoResetEvent> authenticationRequests = new Dictionary<string, AutoResetEvent>();

        public Presentation CreatePresentation(string clientId, string name) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return null;
            if (string.IsNullOrEmpty(name)) return null;
            if (presentations.Values.Any(p => p.Owner == authentications[clientId] && p.Name == name)) return null;
            var presentation = new Presentation { Name = name, Owner = authentications[clientId], Id = Guid.NewGuid().ToString() };

            var slides = new List<Slide>();
            slides.Add(new Slide {
                SlideNumber = 1
            });
            slides.Add(new Slide {
                SlideNumber = 2
            });
            presentation.Slides = slides;

            presentations.Add(presentation.Id, presentation);
            return presentation;
        }

        public void AuthenticateId(string deviceId, string clientId) {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(clientId)) return;
            authentications[clientId] = deviceId;
            if (authenticationRequests.ContainsKey(clientId)) authenticationRequests[clientId].Set();
        }

        public bool IsAuthenticated(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return false;
            if (authenticationRequests.ContainsKey(clientId)) authenticationRequests.Remove(clientId);
            if (authentications.ContainsKey(clientId)) return true;
            authenticationRequests.Add(clientId, new AutoResetEvent(false));
            var success = authenticationRequests[clientId].WaitOne(new TimeSpan(0, 0, 10));
            authenticationRequests.Remove(clientId);
            return success;
        }

        public IEnumerable<PresentationPreview> GetPresentationPreviews(string clientId) {
            if (string.IsNullOrEmpty(clientId) || !authentications.ContainsKey(clientId)) return null;
            return presentations.Values.Where(p => p.Owner == authentications[clientId]).Select(presentation => new PresentationPreview {
                Id = presentation.Id,
                Name = presentation.Name,
                FirstSlide = presentation.Slides.FirstOrDefault()
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