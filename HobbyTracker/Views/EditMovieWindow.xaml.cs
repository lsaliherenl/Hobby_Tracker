using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HobbyTracker.Models;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class EditMovieWindow : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private Movie? _movie;
        private readonly SFirebase _firebaseService;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<Movie>? MovieUpdated;

        public Movie Movie
        {
            get => _movie;
            set
            {
                _movie = value;
                OnPropertyChanged(nameof(Movie));
            }
        }

        public EditMovieWindow()
        {
            InitializeComponent();
            _firebaseService = new SFirebase();
        }

        public void LoadMovie(Movie movie)
        {
            Movie = movie;
            DataContext = movie;
            
            // Initialize UI
            InitializeStars();
            UpdateStarRatingDisplay();
            UpdateStatusDisplay();
            UpdateFavoriteIcon();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Geri butonu - MoviesView'a dön
        private void BtnBack_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is MainWindow mainWindow)
            {
                mainWindow.NavigateBackToMovies();
            }
            else
            {
                window?.Close();
            }
        }

        #region Star Rating

        private void InitializeStars()
        {
            StarContainer.Children.Clear();
            for (int i = 1; i <= 10; i++)
            {
                var star = new System.Windows.Controls.TextBlock
                {
                    Text = "★",
                    FontSize = 22,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(3, 0, 3, 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = i
                };
                star.MouseEnter += Star_MouseEnter;
                star.MouseLeave += Star_MouseLeave;
                star.MouseLeftButtonDown += Star_Click;
                StarContainer.Children.Add(star);
            }
        }

        private void Star_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock star && star.Tag is int index)
            {
                HighlightStars(index);
            }
        }

        private void Star_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UpdateStarHighlight();
        }

        private void Star_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock star && star.Tag is int index && Movie != null)
            {
                Movie.UserRating = index;
                UpdateStarHighlight();
                StarPopup.IsOpen = false;
                UpdateStarRatingDisplay();
                SaveMovieAsync();
            }
        }

        private void HighlightStars(int upToIndex)
        {
            foreach (var child in StarContainer.Children)
            {
                if (child is System.Windows.Controls.TextBlock star && star.Tag is int idx)
                {
                    star.Foreground = idx <= upToIndex
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                }
            }
        }

        private void UpdateStarHighlight()
        {
            int rating = (int)(Movie?.UserRating ?? 0);
            HighlightStars(rating);
        }

        private void UpdateStarRatingDisplay()
        {
            int rating = (int)(Movie?.UserRating ?? 0);
            TxtStarRating.Text = $"{rating}/10";
        }

        private void BtnStarSelect_Click(object sender, MouseButtonEventArgs e)
        {
            StarPopup.IsOpen = !StarPopup.IsOpen;
        }

        #endregion

        #region Status

        private void BtnStatusSelect_Click(object sender, MouseButtonEventArgs e)
        {
            StatusPopup.IsOpen = !StatusPopup.IsOpen;
        }

        private void StatusOption_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.Tag is string statusTag && Movie != null)
            {
                Movie.UserStatus = statusTag switch
                {
                    "Completed" => WatchStatus.Completed,
                    "InProgress" => WatchStatus.InProgress,
                    "PlanToWatch" => WatchStatus.PlanToWatch,
                    "Dropped" => WatchStatus.Dropped,
                    _ => WatchStatus.PlanToWatch
                };

                // Auto-fill progress to 100% when marked as Completed
                if (Movie.UserStatus == WatchStatus.Completed && Movie.DurationMinutes > 0)
                {
                    Movie.WatchedMinutes = Movie.DurationMinutes;
                    // Refresh DataContext to update progress bar
                    DataContext = null;
                    DataContext = Movie;
                }

                StatusPopup.IsOpen = false;
                UpdateStatusDisplay();
                SaveMovieAsync();
            }
        }

        private void UpdateStatusDisplay()
        {
            if (Movie == null) return;
            TxtStatusDisplay.Text = Movie.StatusDisplay;
        }

        #endregion

        // Tab switching
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (OverviewContent == null || CastContent == null || NotesContent == null) return;

            OverviewContent.Visibility = Visibility.Collapsed;
            CastContent.Visibility = Visibility.Collapsed;
            NotesContent.Visibility = Visibility.Collapsed;

            if (TabOverview.IsChecked == true)
                OverviewContent.Visibility = Visibility.Visible;
            else if (TabCast.IsChecked == true)
                CastContent.Visibility = Visibility.Visible;
            else if (TabNotes.IsChecked == true)
                NotesContent.Visibility = Visibility.Visible;
        }

        // Favorite toggle
        private void BtnFavorite_Click(object sender, MouseButtonEventArgs e)
        {
            if (Movie == null) return;

            Movie.IsFavorite = !Movie.IsFavorite;
            UpdateFavoriteIcon();
            SaveMovieAsync();
        }

        private void UpdateFavoriteIcon()
        {
            if (Movie == null) return;
            TxtFavoriteIcon.Text = Movie.IsFavorite ? "❤️" : "🤍";
        }

        // Edit personal notes
        private void BtnEditNotes_Click(object sender, MouseButtonEventArgs e)
        {
            // Show notes edit popup or navigate to notes tab
            TabNotes.IsChecked = true;
        }

        // Update progress - open inline popup
        private void BtnUpdateProgress_Click(object sender, RoutedEventArgs e)
        {
            if (Movie == null) return;
            
            // Set slider maximum to duration minutes
            ProgressSlider.Maximum = Movie.DurationMinutes > 0 ? Movie.DurationMinutes : 100;
            ProgressSlider.Value = Movie.WatchedMinutes;
            
            UpdatePopupTexts();
            ProgressPopup.IsOpen = true;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePopupTexts();
        }

        private void UpdatePopupTexts()
        {
            if (Movie == null || PopupProgressText == null) return;
            
            int watched = (int)ProgressSlider.Value;
            int total = Movie.DurationMinutes;
            double percent = total > 0 ? (watched * 100.0 / total) : 0;
            
            PopupProgressText.Text = $"%{percent:0}";
            PopupWatchedText.Text = $"{watched / 60}s {watched % 60}d izlendi";
            PopupTotalText.Text = $"Toplam: {total / 60}s {total % 60}d";
        }

        private void BtnSaveProgress_Click(object sender, RoutedEventArgs e)
        {
            if (Movie == null) return;
            
            Movie.WatchedMinutes = (int)ProgressSlider.Value;
            
            // Auto-set status based on progress
            if (Movie.WatchedMinutes >= Movie.DurationMinutes)
            {
                Movie.UserStatus = WatchStatus.Completed;
            }
            else if (Movie.WatchedMinutes > 0)
            {
                Movie.UserStatus = WatchStatus.InProgress;
            }

            ProgressPopup.IsOpen = false;
            
            // Refresh UI
            DataContext = null;
            DataContext = Movie;
            UpdateStatusDisplay();
            SaveMovieAsync();
        }

        private async void SaveMovieAsync()
        {
            try
            {
                var result = await _firebaseService.UpdateMovieAsync(Movie);
                if (result == "OK")
                {
                    MovieUpdated?.Invoke(Movie);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Film güncellenirken hata: {ex.Message}");
            }
        }

        // Navigate to Cast tab when "Tümünü gör" clicked
        private void BtnSeeAllCast_Click(object sender, MouseButtonEventArgs e)
        {
            TabCast.IsChecked = true;
        }

        // Save notes
        private void BtnSaveNotes_Click(object sender, RoutedEventArgs e)
        {
            if (Movie == null) return;
            
            Movie.PersonalNote = TxtNotesEditor.Text;
            SaveMovieAsync();
        }

        // Add tag
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTags();
        }

        private void TxtNewTag_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddTags();
            }
        }

        private void AddTags()
        {
            if (Movie == null || string.IsNullOrWhiteSpace(TxtNewTag.Text)) return;
            
            // Initialize UserTags if null
            if (Movie.UserTags == null)
                Movie.UserTags = new System.Collections.Generic.List<string>();
            
            // Parse comma-separated tags
            var newTags = TxtNewTag.Text.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t) && !Movie.UserTags.Contains(t));
            
            foreach (var tag in newTags)
            {
                Movie.UserTags.Add(tag);
            }
            
            TxtNewTag.Text = "";
            
            // Refresh UI
            DataContext = null;
            DataContext = Movie;
            SaveMovieAsync();
        }

        // Remove tag
        private void BtnRemoveTag_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock textBlock && 
                textBlock.Tag is string tagToRemove && 
                Movie?.UserTags != null)
            {
                Movie.UserTags.Remove(tagToRemove);
                
                // Refresh UI
                DataContext = null;
                DataContext = Movie;
                SaveMovieAsync();
            }
        }

        // Delete movie - show custom popup
        private void BtnDeleteMovie_Click(object sender, RoutedEventArgs e)
        {
            if (Movie == null) return;
            
            TxtDeleteMessage.Text = $"\"{Movie.Title}\" filmini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.";
            DeletePopupOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelDelete_Click(object sender, RoutedEventArgs e)
        {
            DeletePopupOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Movie == null) return;

            try
            {
                var deleteResult = await _firebaseService.DeleteMovieAsync(Movie.Id);
                if (deleteResult == "OK")
                {
                    // Movie silindi, event'i tetikleme (null geçilemez)
                    // Ana sayfa kendi refresh mekanizmasını kullanacak
                    DeletePopupOverlay.Visibility = Visibility.Collapsed;
                    
                    var window = Window.GetWindow(this);
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.NavigateBackToMovies();
                    }
                    else
                    {
                        window?.Close();
                    }
                }
                else
                {
                    DeletePopupOverlay.Visibility = Visibility.Collapsed;
                    // Show error in a simple way - could create another popup
                }
            }
            catch (Exception ex)
            {
                DeletePopupOverlay.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Film silinirken hata: {ex.Message}");
            }
        }
    }

    // Simple progress update dialog
    public class ProgressUpdateDialog : Window
    {
        public int NewWatchedMinutes { get; private set; }
        private Slider _slider;
        private TextBlock _txtProgress;
        private int _totalMinutes;

        public ProgressUpdateDialog(int currentMinutes, int totalMinutes)
        {
            _totalMinutes = totalMinutes;
            NewWatchedMinutes = currentMinutes;

            Title = "Update Progress";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "İzleme İlerlemesi",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            _txtProgress = new TextBlock
            {
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            UpdateProgressText();
            Grid.SetRow(_txtProgress, 1);
            grid.Children.Add(_txtProgress);

            _slider = new Slider
            {
                Minimum = 0,
                Maximum = totalMinutes,
                Value = currentMinutes,
                Margin = new Thickness(0, 0, 0, 20)
            };
            _slider.ValueChanged += (s, e) =>
            {
                NewWatchedMinutes = (int)_slider.Value;
                UpdateProgressText();
            };
            Grid.SetRow(_slider, 2);
            grid.Children.Add(_slider);

            var buttonsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            Grid.SetRow(buttonsPanel, 4);

            var cancelBtn = new System.Windows.Controls.Button
            {
                Content = "İptal",
                Width = 80,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            buttonsPanel.Children.Add(cancelBtn);

            var saveBtn = new System.Windows.Controls.Button
            {
                Content = "Kaydet",
                Width = 80,
                Height = 35,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White
            };
            saveBtn.Click += (s, e) => { DialogResult = true; Close(); };
            buttonsPanel.Children.Add(saveBtn);

            grid.Children.Add(buttonsPanel);
            Content = grid;
        }

        private void UpdateProgressText()
        {
            var watched = TimeSpan.FromMinutes(NewWatchedMinutes);
            var total = TimeSpan.FromMinutes(_totalMinutes);
            var percent = _totalMinutes > 0 ? (NewWatchedMinutes * 100.0 / _totalMinutes) : 0;
            _txtProgress.Text = $"{watched.Hours}h {watched.Minutes}m / {total.Hours}h {total.Minutes}m ({percent:0}%)";
        }
    }
}
