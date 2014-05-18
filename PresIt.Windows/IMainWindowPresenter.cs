using System;
using System.Collections.Generic;
using PresIt.Data;

namespace PresIt.Windows {
    public interface IMainWindowPresenter {
        event EventHandler IsAuthenticated;
        event EventHandler<Presentation> EditPresentation;
        event EventHandler<Presentation> ShowPresentation;
        event EventHandler<IEnumerable<PresentationPreview>> PresentationList;
        event EventHandler PresentationSaved;
        event EventHandler PresentationDeleted;

        void GetPresentation(string presentationId);
    }
}