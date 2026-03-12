using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HobbyTracker.Models;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class EditSeriesWindow : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly SFirebase _firebaseService;
        private readonly TmdbService _tmdbService;
        private Series? _currentSeries;
        private int _userRating = 0;
        private bool _isFavorite = false;
        
        // Episodes tab fields
        private int _selectedSeason = 1;
        private System.Collections.Generic.List<Episode> _currentEpisodes = new System.Collections.Generic.List<Episode>();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<Series>? SeriesUpdated;

        public EditSeriesWindow()
        {
            InitializeComponent();
            _firebaseService = new SFirebase();
            _tmdbService = new TmdbService();
            
            Loaded += (s, e) => {
                InitializeHeaderStars();
            };
        }

        public void LoadSeries(Series series)
        {
            _currentSeries = series;
            _userRating = (int)series.UserRating;
            _isFavorite = series.IsFavorite;
            DataContext = series;

            // Parse genres for display
            if (!string.IsNullOrEmpty(series.Genres))
            {
                var genreList = series.Genres.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                GenresItemsControl.ItemsSource = genreList;
            }

            // Update header elements
            TxtStarRating.Text = $"{_userRating}/10";
            TxtStatusDisplayHeader.Text = series.StatusDisplay;
            UpdateFavoriteIcon();
            UpdateHeaderStarHighlight();
            
            // Update progress bar
            UpdateProgressUI();
            
            // Initialize season selector popup
            InitializeSeasonSelector();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Back button
        private void BtnBack_Click(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is MainWindow mainWindow)
            {
                mainWindow.NavigateBackToSeries();
            }
            else
            {
                // If hosted in a popup window, close it
                window?.Close();
            }
        }

        #region Header Star Rating
        private void InitializeHeaderStars()
        {
            StarContainerHeader.Children.Clear();
            for (int i = 1; i <= 10; i++)
            {
                var star = new TextBlock
                {
                    Text = "★",
                    FontSize = 20,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = i
                };
                star.MouseEnter += HeaderStar_MouseEnter;
                star.MouseLeave += HeaderStar_MouseLeave;
                star.MouseLeftButtonDown += HeaderStar_Click;
                StarContainerHeader.Children.Add(star);
            }
            UpdateHeaderStarHighlight();
        }

        private void HeaderStar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock star && star.Tag is int index)
            {
                HighlightHeaderStars(index);
            }
        }

        private void HeaderStar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UpdateHeaderStarHighlight();
        }

        private void HeaderStar_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock star && star.Tag is int index)
            {
                _userRating = index;
                _currentSeries.UserRating = index;
                UpdateHeaderStarHighlight();
                TxtStarRating.Text = $"{_userRating}/10";
                StarPopupHeader.IsOpen = false;
                SaveSeriesAsync();
            }
        }

        private void HighlightHeaderStars(int upToIndex)
        {
            foreach (var child in StarContainerHeader.Children)
            {
                if (child is TextBlock star && star.Tag is int idx)
                {
                    star.Foreground = idx <= upToIndex
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                }
            }
        }

        private void UpdateHeaderStarHighlight()
        {
            HighlightHeaderStars(_userRating);
        }

        private void BtnStarSelectHeader_Click(object sender, MouseButtonEventArgs e)
        {
            StarPopupHeader.IsOpen = !StarPopupHeader.IsOpen;
        }
        #endregion

        #region Header Status
        private void BtnStatusSelectHeader_Click(object sender, MouseButtonEventArgs e)
        {
            StatusPopupHeader.IsOpen = !StatusPopupHeader.IsOpen;
        }

        private async void StatusOptionHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string statusTag)
            {
                WatchStatus newStatus = statusTag switch
                {
                    "Completed" => WatchStatus.Completed,
                    "InProgress" => WatchStatus.InProgress,
                    "PlanToWatch" => WatchStatus.PlanToWatch,
                    "Dropped" => WatchStatus.Dropped,
                    _ => WatchStatus.PlanToWatch
                };

                _currentSeries.UserStatus = newStatus;
                TxtStatusDisplayHeader.Text = _currentSeries.StatusDisplay;
                TxtStatusSidebar.Text = _currentSeries.StatusDisplay;
                
                // Auto-mark all episodes as watched when status is Completed
                if (newStatus == WatchStatus.Completed)
                {
                    await MarkAllEpisodesAsWatchedAsync();
                    // UI'ı yenile - mevcut sezon bölümlerini tekrar yükle
                    if (_currentSeries.TotalSeasons > 0)
                    {
                        LoadSeasonEpisodes(_selectedSeason > 0 ? _selectedSeason : 1);
                        UpdateSeasonProgressUI();
                    }
                }

                // Update progress bar based on status
                UpdateProgressUI();

                StatusPopupHeader.IsOpen = false;
                SaveSeriesAsync();
            }
        }
        
        // Helper method to mark all episodes as watched
        private async Task MarkAllEpisodesAsWatchedAsync()
        {
            if (_currentSeries.WatchedEpisodes == null)
                _currentSeries.WatchedEpisodes = new HashSet<string>();
            
            try
            {
                // Fetch and mark episodes for each season
                for (int season = 1; season <= _currentSeries.TotalSeasons; season++)
                {
                    var episodes = await _tmdbService.GetSeasonDetailsAsync(_currentSeries.TmdbId, season);
                    foreach (var ep in episodes)
                    {
                        _currentSeries.WatchedEpisodes.Add($"{season}_{ep.EpisodeNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tüm bölümleri işaretlerken hata: {ex.Message}");
            }
        }

        private void UpdateProgressUI()
        {
            var progressPercent = _currentSeries.ProgressPercentage;
            TxtProgressPercent.Text = $"{progressPercent}%";
            TxtProgressText.Text = _currentSeries.ProgressText;
            
            // Update status text in sidebar
            TxtProgressStatus.Text = _currentSeries.UserStatus == WatchStatus.Completed ? "Completed" : "In Progress";
            
            // Update sidebar progress bar fill width
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var totalWidth = SidebarProgressBackground.ActualWidth;
                if (totalWidth > 0)
                {
                    SidebarProgressFill.Width = (progressPercent / 100.0) * totalWidth;
                }
                else
                {
                    // Fallback: use a fixed width and calculate
                    SidebarProgressFill.Width = (progressPercent / 100.0) * 200; // Approximate width
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        #endregion

        #region Favorite Button
        private void BtnFavorite_Click(object sender, MouseButtonEventArgs e)
        {
            _isFavorite = !_isFavorite;
            _currentSeries.IsFavorite = _isFavorite;
            UpdateFavoriteIcon();
            SaveSeriesAsync();
        }

        private void UpdateFavoriteIcon()
        {
            TxtFavoriteIcon.Text = _isFavorite ? "❤️" : "🤍";
        }
        #endregion

        // Tab switching
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag)
            {
                OverviewContent.Visibility = tag == "Overview" ? Visibility.Visible : Visibility.Collapsed;
                EpisodesContent.Visibility = tag == "Episodes" ? Visibility.Visible : Visibility.Collapsed;
                CastContent.Visibility = tag == "Cast" ? Visibility.Visible : Visibility.Collapsed;
                NotesContent.Visibility = tag == "Notes" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Navigate to notes tab
        private void BtnEditNotes_Click(object sender, MouseButtonEventArgs e)
        {
            // Switch to Notes tab
            TabNotes.IsChecked = true;
            NotesContent.Visibility = Visibility.Visible;
            OverviewContent.Visibility = Visibility.Collapsed;
            EpisodesContent.Visibility = Visibility.Collapsed;
            CastContent.Visibility = Visibility.Collapsed;
        }

        // Navigate to cast tab
        private void BtnSeeAllCast_Click(object sender, MouseButtonEventArgs e)
        {
            // Switch to Cast tab
            TabCast.IsChecked = true;
            CastContent.Visibility = Visibility.Visible;
            OverviewContent.Visibility = Visibility.Collapsed;
            EpisodesContent.Visibility = Visibility.Collapsed;
            NotesContent.Visibility = Visibility.Collapsed;
        }

        // Add new tag
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            AddNewTags();
        }

        private void TxtNewTag_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddNewTags();
            }
        }

        private void AddNewTags()
        {
            if (string.IsNullOrWhiteSpace(TxtNewTag.Text)) return;
            
            // Ensure UserTags is not null
            if (_currentSeries.UserTags == null)
                _currentSeries.UserTags = new System.Collections.Generic.List<string>();

            var newTags = TxtNewTag.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tag in newTags)
            {
                var trimmedTag = tag.Trim();
                if (!string.IsNullOrEmpty(trimmedTag) && !_currentSeries.UserTags.Contains(trimmedTag))
                {
                    _currentSeries.UserTags.Add(trimmedTag);
                }
            }

            TagsItemControl.ItemsSource = null;
            TagsItemControl.ItemsSource = _currentSeries.UserTags;
            UserTagsList.ItemsSource = null;
            UserTagsList.ItemsSource = _currentSeries.UserTags;
            TxtNewTag.Text = "";
            SaveSeriesAsync();
        }

        // Remove tag
        private void BtnRemoveTag_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string tagToRemove)
            {
                _currentSeries.UserTags.Remove(tagToRemove);
                TagsItemControl.ItemsSource = null;
                TagsItemControl.ItemsSource = _currentSeries.UserTags;
                UserTagsList.ItemsSource = null;
                UserTagsList.ItemsSource = _currentSeries.UserTags;
                SaveSeriesAsync();
            }
        }

        // Update progress - navigate to Episodes tab
        private void BtnUpdateProgress_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Episodes tab where user can manage progress
            TabEpisodes.IsChecked = true;
            EpisodesContent.Visibility = Visibility.Visible;
            OverviewContent.Visibility = Visibility.Collapsed;
            CastContent.Visibility = Visibility.Collapsed;
            NotesContent.Visibility = Visibility.Collapsed;
        }

        // Save notes
        private void BtnSaveNotes_Click(object sender, RoutedEventArgs e)
        {
            _currentSeries.PersonalNote = TxtNotesEditor.Text;
            
            // Update preview in Overview tab
            TxtPersonalNotePreview.Text = _currentSeries.PersonalNote;
            
            SaveSeriesAsync();
            
            // Navigate back to Overview tab to show saved notes
            TabOverview.IsChecked = true;
            OverviewContent.Visibility = Visibility.Visible;
            NotesContent.Visibility = Visibility.Collapsed;
            EpisodesContent.Visibility = Visibility.Collapsed;
            CastContent.Visibility = Visibility.Collapsed;
        }

        // Delete series
        private void BtnDeleteSeries_Click(object sender, RoutedEventArgs e)
        {
            DeleteOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSeries != null && !string.IsNullOrEmpty(_currentSeries.Id))
            {
                var result = await _firebaseService.DeleteSeriesAsync(_currentSeries.Id);
                if (result == "OK")
                {
                    var window = Window.GetWindow(this);
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.NavigateBackToSeries();
                    }
                    else
                    {
                        window?.Close();
                    }
                }
            }
            DeleteOverlay.Visibility = Visibility.Collapsed;
        }

        // Save to Firebase
        private async void SaveSeriesAsync()
        {
            if (_currentSeries != null && !string.IsNullOrEmpty(_currentSeries.Id))
            {
                var result = await _firebaseService.UpdateSeriesAsync(_currentSeries);
                if (result == "OK")
                {
                    SeriesUpdated?.Invoke(_currentSeries);
                }
            }
        }

        #region Episodes Tab Methods
        
        // Initialize season selector popup with all seasons
        private void InitializeSeasonSelector(bool preserveCurrentSeason = false)
        {
            SeasonListPanel.Children.Clear();
            
            for (int i = 1; i <= _currentSeries.TotalSeasons; i++)
            {
                int seasonNum = i;
                var watchedCount = _currentSeries.GetWatchedCountForSeason(seasonNum);
                
                var border = new Border
                {
                    Background = System.Windows.Media.Brushes.Transparent,
                    Padding = new Thickness(15, 10, 15, 10),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = seasonNum
                };
                
                var stack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                stack.Children.Add(new TextBlock 
                { 
                    Text = $"Sezon {seasonNum}", 
                    Foreground = System.Windows.Media.Brushes.White, 
                    FontSize = 14,
                    Width = 80
                });
                stack.Children.Add(new TextBlock 
                { 
                    Text = watchedCount > 0 ? $"({watchedCount} izlendi)" : "", 
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                
                border.Child = stack;
                border.MouseEnter += (s, e) => ((Border)s).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
                border.MouseLeave += (s, e) => ((Border)s).Background = System.Windows.Media.Brushes.Transparent;
                border.MouseLeftButtonUp += SeasonItem_Click;
                
                SeasonListPanel.Children.Add(border);
            }
            
            // Mevcut sezonu koru veya 1. sezonu yükle
            if (!preserveCurrentSeason || _selectedSeason < 1 || _selectedSeason > _currentSeries.TotalSeasons)
            {
                _selectedSeason = 1;
                LoadSeasonEpisodes(_selectedSeason);
            }
            TxtSelectedSeason.Text = $"Sezon {_selectedSeason}";
        }
        
        // Load episodes for a specific season
        private async void LoadSeasonEpisodes(int seasonNumber)
        {
            _selectedSeason = seasonNumber;
            TxtSelectedSeason.Text = $"Sezon {seasonNumber}";
            
            try
            {
                // Fetch episodes from TMDB
                var episodes = await _tmdbService.GetSeasonDetailsAsync(_currentSeries.TmdbId, seasonNumber);
                
                // Set IsWatched based on our saved data
                foreach (var ep in episodes)
                {
                    ep.IsWatched = _currentSeries.IsEpisodeWatched(seasonNumber, ep.EpisodeNumber);
                }
                
                _currentEpisodes = episodes;
                EpisodesList.ItemsSource = null;
                EpisodesList.ItemsSource = _currentEpisodes;
                
                UpdateSeasonProgressUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSeasonEpisodes Error: {ex.Message}");
            }
        }
        
        // Update the season progress indicator
        private void UpdateSeasonProgressUI()
        {
            var totalEpisodes = _currentEpisodes.Count;
            var watchedCount = _currentSeries.GetWatchedCountForSeason(_selectedSeason);
            
            TxtSeasonProgress.Text = $"{watchedCount}/{totalEpisodes} İzlendi";
            
            // Update progress bar
            double percentage = totalEpisodes > 0 ? (double)watchedCount / totalEpisodes * 100 : 0;
            SeasonProgressFill.Width = percentage;
        }
        
        // Season selector click
        private void SeasonSelector_Click(object sender, MouseButtonEventArgs e)
        {
            SeasonPopup.IsOpen = !SeasonPopup.IsOpen;
        }
        
        // Season item selected
        private void SeasonItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int seasonNum)
            {
                SeasonPopup.IsOpen = false;
                LoadSeasonEpisodes(seasonNum);
            }
        }
        
        // Toggle episode watched status
        private void BtnToggleEpisodeWatched_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Episode episode)
            {
                // Toggle in model
                _currentSeries.ToggleEpisodeWatched(episode.SeasonNumber, episode.EpisodeNumber);
                
                // Toggle in UI
                episode.IsWatched = !episode.IsWatched;
                
                // Refresh the list
                EpisodesList.ItemsSource = null;
                EpisodesList.ItemsSource = _currentEpisodes;
                
                // Update progress displays
                UpdateSeasonProgressUI();
                UpdateProgressUI();
                
                // Save to Firebase
                SaveSeriesAsync();
            }
        }
        
        // Mark entire season as watched
        private void BtnMarkSeasonWatched_Click(object sender, MouseButtonEventArgs e)
        {
            var episodeCount = _currentEpisodes.Count;
            if (episodeCount == 0) return;
            
            // Mark all episodes in the season as watched
            _currentSeries.MarkSeasonWatched(_selectedSeason, episodeCount);
            
            // Update UI for all episodes
            foreach (var ep in _currentEpisodes)
            {
                ep.IsWatched = true;
            }
            
            // Refresh the list
            EpisodesList.ItemsSource = null;
            EpisodesList.ItemsSource = _currentEpisodes;
            
            // Update progress displays
            UpdateSeasonProgressUI();
            UpdateProgressUI();
            InitializeSeasonSelector(preserveCurrentSeason: true); // Refresh season selector to show updated counts
            
            // Save to Firebase
            SaveSeriesAsync();
        }
        
        #endregion
    }

    // Simple dialog for series progress update
    public class ProgressUpdateDialogSeries : Window
    {
        public int NewSeason { get; private set; }
        public int NewEpisode { get; private set; }

        public ProgressUpdateDialogSeries(int currentSeason, int currentEpisode, int totalSeasons, int totalEpisodes)
        {
            NewSeason = currentSeason;
            NewEpisode = currentEpisode;

            Title = "İlerleme Güncelle";
            Width = 350;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Season label
            var seasonLabel = new TextBlock { Text = $"Sezon (1-{totalSeasons})", Foreground = System.Windows.Media.Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(seasonLabel, 0);
            grid.Children.Add(seasonLabel);

            // Season slider
            var seasonSlider = new Slider { Minimum = 1, Maximum = Math.Max(1, totalSeasons), Value = currentSeason, TickFrequency = 1, IsSnapToTickEnabled = true };
            Grid.SetRow(seasonSlider, 1);
            grid.Children.Add(seasonSlider);

            // Episode label
            var episodeLabel = new TextBlock { Text = $"Bölüm (1-{totalEpisodes})", Foreground = System.Windows.Media.Brushes.White, FontSize = 13, Margin = new Thickness(0, 15, 0, 5) };
            Grid.SetRow(episodeLabel, 2);
            grid.Children.Add(episodeLabel);

            // Episode slider
            var episodeSlider = new Slider { Minimum = 1, Maximum = Math.Max(1, totalEpisodes), Value = currentEpisode, TickFrequency = 1, IsSnapToTickEnabled = true };
            Grid.SetRow(episodeSlider, 3);
            grid.Children.Add(episodeSlider);

            // Current display
            var displayText = new TextBlock { Foreground = System.Windows.Media.Brushes.LightGray, FontSize = 14, Margin = new Thickness(0, 15, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            displayText.Text = $"S{currentSeason} B{currentEpisode}";
            Grid.SetRow(displayText, 4);
            grid.Children.Add(displayText);

            seasonSlider.ValueChanged += (s, e) =>
            {
                NewSeason = (int)seasonSlider.Value;
                displayText.Text = $"S{NewSeason} B{NewEpisode}";
            };

            episodeSlider.ValueChanged += (s, e) =>
            {
                NewEpisode = (int)episodeSlider.Value;
                displayText.Text = $"S{NewSeason} B{NewEpisode}";
            };

            // Buttons
            var buttonsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            Grid.SetRow(buttonsPanel, 5);

            var cancelBtn = new System.Windows.Controls.Button { Content = "İptal", Width = 100, Height = 35, Margin = new Thickness(0, 0, 10, 0), Background = System.Windows.Media.Brushes.Gray, Foreground = System.Windows.Media.Brushes.White };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            buttonsPanel.Children.Add(cancelBtn);

            var saveBtn = new System.Windows.Controls.Button { Content = "Kaydet", Width = 100, Height = 35, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White };
            saveBtn.Click += (s, e) => { DialogResult = true; Close(); };
            buttonsPanel.Children.Add(saveBtn);

            grid.Children.Add(buttonsPanel);
            Content = grid;
        }
    }
}
