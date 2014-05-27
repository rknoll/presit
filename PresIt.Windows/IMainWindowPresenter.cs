using System;
using System.Collections.Generic;
using PresIt.Data;

namespace PresIt.Windows {

    /// <summary>
    /// Interface of the Presenter
    /// </summary>
    public interface IMainWindowPresenter {

        /// <summary>
        /// Event after we got authenticated
        /// </summary>
        event EventHandler IsAuthenticated;

        /// <summary>
        /// Event after we got a Presentation to Edit
        /// </summary>
        event EventHandler<Presentation> EditPresentation;

        /// <summary>
        /// Event after we got a Presentation to Show on Screen
        /// </summary>
        event EventHandler<Presentation> ShowPresentation;

        /// <summary>
        /// Event after we got all Presentation Previews
        /// </summary>
        event EventHandler<IEnumerable<PresentationPreview>> PresentationList;

        /// <summary>
        /// Event after a Presentation got Saved
        /// </summary>
        event EventHandler PresentationSaved;

        /// <summary>
        /// Event after a Presentation got Deleted
        /// </summary>
        event EventHandler PresentationDeleted;

        /// <summary>
        /// Event if we should switch to the Next Slide
        /// </summary>
        event EventHandler NextSlide;

        /// <summary>
        /// Event if we should switch to the Previous Slide
        /// </summary>
        event EventHandler PreviousSlide;

        /// <summary>
        /// Event if we should switch to the Next Slide
        /// </summary>
        event EventHandler SwitchPause;

        /// <summary>
        /// Event after we got the Number of Slides, to set the max value of the progress bar
        /// </summary>
        event EventHandler<int> GotPresentationSlidesCount;

        /// <summary>
        /// Event after we received one Slide, to update the progress bar
        /// </summary>
        event EventHandler GotPresentationSlide;

        /// <summary>
        /// Event if we got an error while fetching slides
        /// </summary>
        event EventHandler CancelStartPresentation;

        /// <summary>
        /// Set / Get the Filename which was dropped into the GUI
        /// </summary>
        string DroppedFileName { get; set; }

        /// <summary>
        /// Save the current Presentation to the Server
        /// </summary>
        void SavePresentation();

        /// <summary>
        /// Get all Presentations from the Server
        /// </summary>
        void GetPresentations();

        /// <summary>
        /// Start a Presentation, identified by a Preview
        /// </summary>
        void StartPresentation(SlidePreview presentationPrview);

        /// <summary>
        /// Stop the Presentation
        /// </summary>
        void StopPresentation();

        /// <summary>
        /// Modify a Presentation, identifies by its Preview
        /// </summary>
        void ChangePresentation(SlidePreview presentationPrview);

        /// <summary>
        /// Import Slides to a specific Position in the presentation
        /// </summary>
        void ImportSlides(int slideIndex);
    }
}