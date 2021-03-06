﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PresIt.Data;
using PresIt.Windows.Annotations;

namespace PresIt.Windows {
    /// <summary>
    ///     Interaction logic for PresentationWindow.xaml
    /// </summary>
    public partial class PresentationWindow : INotifyPropertyChanged {
        private Presentation dataContext;
        private Storyboard currentStoryboard;

        private List<SlidePreview> slides;
        private int currentSlideIndex;
        private int nextSlideIndex;
        private bool isPaused;

        private readonly SynchronizationContext context;

        public PresentationWindow(IMainWindowPresenter dataContext) {
            InitializeComponent();
            context = SynchronizationContext.Current;

            // register callbacks for slide control
            dataContext.NextSlide += (o, args) => context.Post(state => NextSlide(), null);
            dataContext.PreviousSlide += (o, pres) => context.Post(state => PreviousSlide(), null);
            dataContext.SwitchPause += (o, pres) => context.Post(state => SwitchPause(), null);
        }

        private void OnPresentationWindowSourceInitialized(object sender, EventArgs e) {
            dataContext = DataContext as Presentation;
            if (dataContext == null) return;
            if (dataContext.Slides == null) return;

            // get all slides as images from our model
            slides = new List<SlidePreview>();
            foreach (var slide in dataContext.Slides) {
                slides.Add(SlidePreview.CreateFromSlide(slide, dataContext.Id));
            }
            currentSlideIndex = 0;
            nextSlideIndex = -1;
            if (slides.Count == 0) return;

            SlideImageView.Source = slides[currentSlideIndex].SlideImage;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Show Next Slide with an Animation
        /// </summary>
        private void NextSlide() {
            if (currentStoryboard != null) {
                currentStoryboard.Stop(this);
                currentStoryboard.Remove(this);
            }
            if (nextSlideIndex >= 0) {
                currentSlideIndex = nextSlideIndex;
                SlideImageView.Source = slides[currentSlideIndex].SlideImage;
                nextSlideIndex = -1;
            }
            if (isPaused) {
                SwitchPause();
            } else {
                if (currentSlideIndex >= slides.Count - 1) return;
                nextSlideIndex = currentSlideIndex + 1;
                Animate();
            }
        }

        /// <summary>
        /// Show Previous Slide with an Animation
        /// </summary>
        private void PreviousSlide() {
            if (currentStoryboard != null) {
                currentStoryboard.Stop(this);
                currentStoryboard.Remove(this);
            }
            if (nextSlideIndex >= 0) {
                currentSlideIndex = nextSlideIndex;
                SlideImageView.Source = slides[currentSlideIndex].SlideImage;
                nextSlideIndex = -1;
            }
            if (isPaused) {
                SwitchPause();
            } else {
                if (currentSlideIndex <= 0) return;
                nextSlideIndex = currentSlideIndex - 1;
                Animate();
            }
        }
        
        /// <summary>
        /// Pause / Unpause Presentation
        /// </summary>
        private void SwitchPause() {
            if (currentStoryboard != null) {
                currentStoryboard.Stop(this);
                currentStoryboard.Remove(this);
            }
            if (nextSlideIndex >= 0) {
                currentSlideIndex = nextSlideIndex;
                SlideImageView.Source = slides[currentSlideIndex].SlideImage;
                nextSlideIndex = -1;
            }

            isPaused = !isPaused;

            AnimatePause();
        }

        private void Animate() {
            currentStoryboard = new Storyboard();
            NextSlideImageView.Opacity = 0;
            NextSlideImageView.Visibility = Visibility.Visible;
            NextSlideImageView.Source = slides[nextSlideIndex].SlideImage;

            var opacityAnimation1 = new DoubleAnimation {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
                
            var opacityAnimation2 = new DoubleAnimation {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            Storyboard.SetTarget(opacityAnimation1, NextSlideImageView);
            Storyboard.SetTarget(opacityAnimation2, SlideImageView);
            Storyboard.SetTargetProperty(opacityAnimation1, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(opacityAnimation2, new PropertyPath(OpacityProperty));

            currentStoryboard.Children.Add(opacityAnimation1);
            currentStoryboard.Children.Add(opacityAnimation2);

            currentStoryboard.Completed += (sender1, eventArgs) => {
                if (nextSlideIndex < 0) return;
                currentSlideIndex = nextSlideIndex;
                SlideImageView.Source = slides[currentSlideIndex].SlideImage;
                nextSlideIndex = -1;
                SlideImageView.Opacity = 1;
                NextSlideImageView.Visibility = Visibility.Hidden;
                currentStoryboard.Remove(this);
                currentStoryboard = null;
            };

            // start animation
            currentStoryboard.Begin(this, true);
        }

        private void AnimatePause() {
            currentStoryboard = new Storyboard();
            NextSlideImageView.Visibility = Visibility.Hidden;

            var opacityAnimation = new DoubleAnimation {
                From = isPaused ? 1 : 0,
                To = isPaused ? 0 : 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            Storyboard.SetTarget(opacityAnimation, SlideImageView);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

            currentStoryboard.Children.Add(opacityAnimation);

            currentStoryboard.Completed += (sender1, eventArgs) => {
                if (currentStoryboard == null) return;
                currentStoryboard.Remove(this);
                currentStoryboard = null;
                SlideImageView.Opacity = isPaused ? 0 : 1;
            };

            // start animation
            currentStoryboard.Begin(this, true);
        }

        /// <summary>
        /// Keyboard Control
        /// </summary>
        private void OnPresentationWindowKeyUp(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    Close();
                    break;
                case Key.Right:
                    NextSlide();
                    break;
                case Key.Left:
                    PreviousSlide();
                    break;
                case Key.B:
                case Key.OemPeriod:
                    SwitchPause();
                    break;
            }
        }
    }
}