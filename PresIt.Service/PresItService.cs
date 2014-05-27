using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using PresIt.Data;

namespace PresIt.Service {

    /// <summary>
    /// Main PresIt Service
    /// </summary>
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

        // storage, in memory for this POC
        private readonly Dictionary<string, Presentation> presentations = new Dictionary<string, Presentation>();
        private readonly Dictionary<string, AuthenticationRequest> authenticationRequests = new Dictionary<string, AuthenticationRequest>();
        private readonly Dictionary<string, CommandRequest> commandRequests = new Dictionary<string, CommandRequest>();

        public Presentation CreatePresentation(string clientId, string name) {
            if (string.IsNullOrEmpty(clientId)) return null;
            if (string.IsNullOrEmpty(name)) return null;
            if (presentations.Values.Any(p => p.Owner == clientId && p.Name == name)) return null;
            
            // create new presentation
            var presentation = new Presentation { Name = name, Owner = clientId, Id = Guid.NewGuid().ToString() };
            presentations.Add(presentation.Id, presentation);
            return presentation;
        }

        public bool UpdateSlide(string clientId, string presentationId, Slide slide) {
            if (string.IsNullOrEmpty(clientId)) return false;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return false;
            if (presentations[presentationId].Owner != clientId) return false;
            if (presentations[presentationId].Slides == null) return false;
            if (slide == null) return false;
            var slides = presentations[presentationId].Slides.ToList();
            if (slides.Count <= (slide.SlideNumber - 1)) return false;

            // update slide
            slides[slide.SlideNumber - 1] = slide;
            presentations[presentationId].Slides = slides;
            return true;
        }

        public bool UpdateSlidesCount(string clientId, string presentationId, int slidesCount) {
            if (string.IsNullOrEmpty(clientId)) return false;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return false;
            if (presentations[presentationId].Owner != clientId) return false;

            // update slide count
            presentations[presentationId].Slides = new List<Slide>(new Slide[slidesCount]);
            return true;
        }

        public bool DeletePresentation(string clientId, string presentationId) {
            if (string.IsNullOrEmpty(clientId)) return false;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return false;
            if (presentations[presentationId].Owner != clientId) return false;

            // remove presentation
            presentations.Remove(presentationId);
            return true;
        }

        public Slide GetPresentationSlide(string clientId, string presentationId, int slideIndex) {
            if (string.IsNullOrEmpty(clientId)) return null;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return null;
            if (presentations[presentationId].Owner != clientId) return null;
            if (presentations[presentationId].Slides == null) return null;
            var slides = presentations[presentationId].Slides.ToList();
            if (slides.Count <= slideIndex || slideIndex < 0) return null;

            // return the slide
            return slides[slideIndex];
        }

        public int GetPresentationSlidesCount(string clientId, string presentationId) {
            if (string.IsNullOrEmpty(clientId)) return -1;
            if (string.IsNullOrEmpty(presentationId) || !presentations.ContainsKey(presentationId)) return -1;
            if (presentations[presentationId].Owner != clientId) return -1;
            if (presentations[presentationId].Slides == null) return 0;

            // get the slide count
            return presentations[presentationId].Slides.Count();
        }

        public void AuthenticateId(string clientId, string sessionId) {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(sessionId)) return;
            if (authenticationRequests.ContainsKey(sessionId)) {
                // authenticate the session ID
                authenticationRequests[sessionId].ClientId = clientId;
                authenticationRequests[sessionId].AuthenticationEvent.Set();
            }
        }

        public string IsAuthenticated(string sessionId) {
            if (string.IsNullOrEmpty(sessionId)) return null;
            if (authenticationRequests.ContainsKey(sessionId)) authenticationRequests.Remove(sessionId);
            authenticationRequests.Add(sessionId, new AuthenticationRequest {AuthenticationEvent = new AutoResetEvent(false)});
            
            // wait until we get authenticated
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

            // wait for the next command
            commandRequests[clientId].CommandEvent.WaitOne(new TimeSpan(0, 0, 10));
            var command = commandRequests[clientId].CommandType;
            commandRequests.Remove(clientId);
            return command;
        }

        public void NextSlide(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return;
            if (commandRequests.ContainsKey(clientId)) {
                // send new message (next slide)
                commandRequests[clientId].CommandType = CommandType.NextSlide;
                commandRequests[clientId].CommandEvent.Set();
            }
        }

        public void PreviousSlide(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return;
            if (commandRequests.ContainsKey(clientId)) {
                // send new message (previous slide)
                commandRequests[clientId].CommandType = CommandType.PreviousSlide;
                commandRequests[clientId].CommandEvent.Set();
            }
        }

        public int GetPresentationCount(string clientId) {
            if (string.IsNullOrEmpty(clientId)) return -1;

            // return presentation count
            return presentations.Values.Count(p => p.Owner == clientId);
        }

        public PresentationPreview GetPresentationPreview(string clientId, int presentationIndex) {
            if (string.IsNullOrEmpty(clientId)) return null;

            // get the preview of a presentation
            return presentations.Values.Where(p => p.Owner == clientId).Skip(presentationIndex).Select(presentation => new PresentationPreview {
                Id = presentation.Id,
                Name = presentation.Name,
                FirstSlide = presentation.Slides != null ? presentation.Slides.FirstOrDefault() : null
            }).FirstOrDefault();
        }

    }
}