﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.IO;
using PresIt.Data;

namespace PresIt.Windows {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private readonly SynchronizationContext context;
        private IMainWindowPresenter dataContext;
        private Presentation currentPresentation;

        public MainWindow() {
            InitializeComponent();
            context = SynchronizationContext.Current;

            // initialize view
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Visible;
            NewPresentationGrid.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// User dropped a file on the new Presentation button, create a new Presentation with the filename
        /// </summary>
        private void OnNewPresentationDrop(object sender, DragEventArgs e) {
            dataContext.DroppedFileName = null;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1 || string.IsNullOrEmpty(files[0])) return;
            dataContext.DroppedFileName = files[0];
            var file = Path.GetFileName(files[0]);
            if (string.IsNullOrEmpty(file)) {
                dataContext.DroppedFileName = null;
                return;
            }
            if (file.Contains(".")) {
                file = file.Substring(0, file.LastIndexOf('.'));
            }
            var be = NewPresentationNameTextBox.GetBindingExpression(TextBox.TextProperty);
            NewPresentationNameTextBox.Text = file;
            if(be != null) be.UpdateSource();
            ShowNewPresentation();
        }

        private void OnNewPresentationClick(object sender, RoutedEventArgs e) {
            ShowNewPresentation();
        }

        private void ShowNewPresentation() {
            OverlayRectangle.Opacity = 0;
            OverlayRectangle.Visibility = Visibility.Visible;
            NewPresentationGrid.SetValue(Canvas.TopProperty, -NewPresentationContent.ActualHeight);
            NewPresentationGrid.Visibility = Visibility.Visible;

            var opacityAnimation = new DoubleAnimation {
                From = 0,
                To = 0.6,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            var topAnimation = new DoubleAnimation {
                From = -NewPresentationContent.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(opacityAnimation, OverlayRectangle);
            Storyboard.SetTarget(topAnimation, NewPresentationGrid);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(topAnimation, new PropertyPath(Canvas.TopProperty));

            storyboard.Children.Add(opacityAnimation);
            storyboard.Children.Add(topAnimation);

            storyboard.Begin();
        }

        private void HideNewPresentationScreen(bool fast = false) {
            var be = NewPresentationNameTextBox.GetBindingExpression(TextBox.TextProperty);
            NewPresentationNameTextBox.Text = "";
            if (be != null) be.UpdateSource();
            if (!fast) {
                OverlayRectangle.Opacity = 0.6;
                OverlayRectangle.Visibility = Visibility.Visible;
            }
            NewPresentationGrid.SetValue(Canvas.TopProperty, 0.0);
            NewPresentationGrid.Visibility = Visibility.Visible;

            var opacityAnimation = new DoubleAnimation {
                From = 0.6,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            var topAnimation = new DoubleAnimation {
                From = 0,
                To = -NewPresentationContent.ActualHeight,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(opacityAnimation, OverlayRectangle);
            Storyboard.SetTarget(topAnimation, NewPresentationGrid);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(topAnimation, new PropertyPath(Canvas.TopProperty));

            if (!fast) {
                storyboard.Children.Add(opacityAnimation);
            }

            storyboard.Children.Add(topAnimation);
            
            storyboard.Completed += (sender1, eventArgs) => {
                if (!fast) {
                    OverlayRectangle.BeginAnimation(OpacityProperty, null);
                    OverlayRectangle.Visibility = Visibility.Hidden;
                }

                NewPresentationGrid.Visibility = Visibility.Hidden;
            };

            storyboard.Begin();
        }

        private void HideLoginScreen() {
            CommandManager.InvalidateRequerySuggested();

            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;

            var opacityAnimation1 = new DoubleAnimation {
                From = 0.6,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
                
            var opacityAnimation2 = new DoubleAnimation {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(opacityAnimation1, OverlayRectangle);
            Storyboard.SetTarget(opacityAnimation2, LoginGrid);
            Storyboard.SetTargetProperty(opacityAnimation1, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(opacityAnimation2, new PropertyPath(OpacityProperty));

            storyboard.Children.Add(opacityAnimation1);
            storyboard.Children.Add(opacityAnimation2);

            storyboard.Completed += (sender1, eventArgs) => {
                OverlayRectangle.BeginAnimation(OpacityProperty, null);
                OverlayRectangle.Visibility = Visibility.Hidden;
                LoginGrid.Visibility = Visibility.Collapsed;
            };

            storyboard.Begin();
        }

        private void OnMainWindowSourceInitialized(object sender, EventArgs e) {
            dataContext = DataContext as IMainWindowPresenter;
            if (dataContext == null) return;

            // register callbacks after window is ready
            dataContext.IsAuthenticated += (o, args) => context.Post(state => HideLoginScreen(), null);
            dataContext.EditPresentation += (o, pres) => context.Post(state => EditPresentation(pres), null);
            dataContext.PresentationList += (o, pres) => context.Post(state => ShowSelectPresentation(pres), null);
            dataContext.PresentationSaved += (o, pres) => context.Post(state => HideEditPresentation(), null);
            dataContext.PresentationDeleted += (o, pres) => context.Post(state => HideEditPresentation(), null);
            dataContext.ShowPresentation += (o, pres) => context.Post(state => ShowPresentation(pres), null);
            dataContext.CancelStartPresentation += (o, args) => context.Post(state => CancelShowPresentation(), null);
            dataContext.GotPresentationSlidesCount += (o, count) => context.Post(state => GotPresentationSlidesCount(count), null);
            dataContext.GotPresentationSlide += (o, args) => context.Post(state => GotPresentationSlide(), null);
        }

        private void HideEditPresentation() {
            CancelShowPresentation();
            EditPresentationView.Visibility = Visibility.Hidden;
        }

        private void EditPresentation(Presentation pres) {
            CancelShowPresentation();
            currentPresentation = pres;
            EditPresentationSlides.ItemsSource = null;

            if (NewPresentationGrid.Visibility == Visibility.Visible) {
                HideNewPresentationScreen(pres != null);
            }
            if (SelectPresentationView.Visibility == Visibility.Visible) {
                HideSelectPresentation();
            }

            if (pres != null) {
                EditPresentationView.Visibility = Visibility.Visible;
                EditPresentationName.Content = pres.Name;
                if (dataContext.DroppedFileName != null) {
                    ImportSlides(-1);
                } else {
                    ShowSlides();
                }
            }

        }

        /// <summary>
        /// Convert all raw-data slides to previews and show them
        /// </summary>
        private void ShowSlides() {
            var slides = new ObservableCollection<SlidePreview>();

            if (currentPresentation == null) return;

            if (currentPresentation.Slides != null) {
                foreach (var slide in currentPresentation.Slides) {
                    slides.Add(SlidePreview.CreateFromSlide(slide, currentPresentation.Id));
                }
            }
            slides.Add(SlidePreview.CreateAddNewSlide(currentPresentation.Id));
            EditPresentationSlides.ItemsSource = slides;
            OverlayRectangle.Visibility = Visibility.Hidden;
            ImportSlidesGrid.Visibility = Visibility.Hidden;
        }

        private void GotPresentationSlidesCount(int count) {
            ImportSlidesProgressBar.Maximum = count;
        }
        
        private void GotPresentationSlide() {
            ImportSlidesProgressBar.Value ++;
        }

        private void CancelShowPresentation() {
            OverlayRectangle.Visibility = Visibility.Hidden;
            ImportSlidesGrid.Visibility = Visibility.Hidden;
        }

        private void OnCancelNewPresentationClick(object sender, RoutedEventArgs e) {
            HideNewPresentationScreen();
        }

        /// <summary>
        /// Convert Presentation Previews to Slide Images and show them
        /// </summary>
        private void ShowSelectPresentation(IEnumerable<PresentationPreview> presentationList) {
            CancelShowPresentation();
            SelectPresentationView.Visibility = Visibility.Visible;

            var presentations = new ObservableCollection<SlidePreview>();

            foreach (var presentation in presentationList) {
                presentations.Add(SlidePreview.CreateFromSlide(presentation.FirstSlide, presentation.Id, presentation.Name));
            }

            SelectPresentationList.ItemsSource = presentations;
        }
        
        private void HideSelectPresentation() {
            SelectPresentationView.Visibility = Visibility.Hidden;
        }

        private void OnCancelShowPresentationClick(object sender, RoutedEventArgs e) {
            HideSelectPresentation();
        }

        private void OnSelectPresentationListDoubleClick(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || e.ClickCount != 2) return;
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            if (slidePreview == null || slidePreview.PresentationId == null) return;

            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            ImportSlidesTitle.Text = "Loading Slides";
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.StartPresentation(slidePreview);
        }

        /// <summary>
        /// Start a Presentation
        /// </summary>
        private void ShowPresentation(Presentation pres) {
            CancelShowPresentation();
            new PresentationWindow(dataContext) {
                DataContext = pres
            }.ShowDialog();
            dataContext.StopPresentation();
        }

        /// <summary>
        /// User dropped a file on the edit presentation screen, insert the new slide(s) at that position
        /// </summary>
        private void OnEditPresentationSlideDrop(object sender, DragEventArgs e) {
            if (currentPresentation == null) return;
            var img = sender as Image;
            if(img == null) return;
            var preview = img.DataContext as SlidePreview;
            if (preview == null) return;

            dataContext.DroppedFileName = null;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1 || string.IsNullOrEmpty(files[0])) return;
            dataContext.DroppedFileName = files[0];
            var file = Path.GetFileName(files[0]);
            if (string.IsNullOrEmpty(file)) {
                dataContext.DroppedFileName = null;
                return;
            }

            int slideIndex;
            if (!int.TryParse(preview.SlideText, out slideIndex)) slideIndex = -1;

            ImportSlides(slideIndex);
        }

        private void ImportSlides(int slideIndex) {
            ImportSlidesTitle.Text = "Importing Slides";
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.ImportSlides(slideIndex);
        }

        private void OnSelectPresentationListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            EditPresentationButton.IsEnabled = slidePreview != null && slidePreview.PresentationId != null;
        }

        private void OnEditPresentationClick(object sender, RoutedEventArgs e) {
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            if (slidePreview == null || slidePreview.PresentationId == null) return;

            ImportSlidesTitle.Text = "Loading Slides";
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.ChangePresentation(slidePreview);
        }

        private void OnSavePresentationClick(object sender, RoutedEventArgs e) {
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            ImportSlidesTitle.Text = "Saving Slides";
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.SavePresentation();
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            Environment.Exit(0);
        }

        private void OnGetPresentationsButtonClick(object sender, RoutedEventArgs e) {
            ImportSlidesTitle.Text = "Loading Presentations";
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.GetPresentations();
        }
    }
}