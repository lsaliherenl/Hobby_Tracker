using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HobbyTracker.Models;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class AddSeriesWindow : Window
    {
        private readonly TmdbService _tmdbService;
        private Series? _selectedSeries;
        private int _userRating = 0;

        // Dizi eklendiğinde ana sayfaya haber vermek için Event
        public event Action<Series>? SeriesAdded;

        public AddSeriesWindow()
        {
            InitializeComponent();
            _tmdbService = new TmdbService();
            
            // Tarih alanını bugün olarak ayarla
            DtDate.DateTime = DateTime.Today;
            
            // Yıldızları başlat ve rating durumunu ayarla
            Loaded += (s, e) =>
            {
                InitializeStars();
                UpdateRatingButtonState();
            };
        }

        // Durum değişince rating butonunu güncelle
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRatingButtonState();
        }

        // "Listem" seçiliyse rating butonunu devre dışı bırak
        private void UpdateRatingButtonState()
        {
            if (BtnStarSelect == null) return;
            
            bool isListem = CmbStatus.SelectedIndex == 2; // "Listem" index 2
            BtnStarSelect.IsEnabled = !isListem;
            BtnStarSelect.Opacity = isListem ? 0.4 : 1.0;
            
            if (isListem)
            {
                _userRating = 0;
                TxtStarRating.Text = "-";
                StarPopup.IsOpen = false;
            }
        }

        // Initialize 10 stars
        private void InitializeStars()
        {
            StarContainer.Children.Clear();
            for (int i = 1; i <= 10; i++)
            {
                var star = new TextBlock
                {
                    Text = "★",
                    FontSize = 22,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(3, 0, 3, 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = i
                };
                star.MouseEnter += Star_MouseEnter;
                star.MouseLeave += Star_MouseLeave;
                star.MouseLeftButtonDown += Star_Click;
                StarContainer.Children.Add(star);
            }
            UpdateStarDisplay();
        }
        
        private void Star_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock star && star.Tag is int index)
            {
                HighlightStars(index);
            }
        }
        
        private void Star_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UpdateStarDisplay();
        }
        
        private void Star_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock star && star.Tag is int index)
            {
                _userRating = index;
                UpdateStarDisplay();
                StarPopup.IsOpen = false;
                TxtStarRating.Text = $"{_userRating}/10";
            }
        }
        
        private void HighlightStars(int upToIndex)
        {
            foreach (var child in StarContainer.Children)
            {
                if (child is TextBlock star && star.Tag is int idx)
                {
                    star.Foreground = idx <= upToIndex 
                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36))
                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                }
            }
        }
        
        private void UpdateStarDisplay()
        {
            HighlightStars(_userRating);
        }
        
        private void BtnStarSelect_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StarPopup.IsOpen = !StarPopup.IsOpen;
        }
        
        // Tarih inputına tıklanınca popup aç
        private void DateInput_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DtDate.ShowPopup();
            e.Handled = true;
        }

        // --- PENCERE YÖNETİMİ ---
        
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void Background_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        // --- ARAMA İŞLEMLERİ ---

        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private async void PerformSearch()
        {
            string query = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            EmptyStatePanel.Visibility = Visibility.Collapsed;
            SearchResultsPanel.Visibility = Visibility.Visible;
            
            LoadingBar.Visibility = Visibility.Visible;
            LstSeries.ItemsSource = null;
            DetailPanel.Visibility = Visibility.Hidden;

            // Servisten dizi ara
            var results = await _tmdbService.SearchSeriesAsync(query);
            
            LstSeries.ItemsSource = results;
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        // --- SEÇİM VE DETAY GÖSTERİMİ ---

        private async void LstSeries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstSeries.SelectedItem is Series seriesStub)
            {
                DetailPanel.Visibility = Visibility.Visible;

                // Seçilen dizinin detaylarını çek
                var fullSeries = await _tmdbService.GetSeriesDetailsAsync(seriesStub.TmdbId);

                if (fullSeries != null)
                {
                    _selectedSeries = fullSeries;
                    PopulateDetails(fullSeries);
                }
            }
        }

        private void PopulateDetails(Series s)
        {
            TxtTitle.Text = s.Title;
            TxtSynopsis.Text = s.Overview;
            TxtSeasonCount.Text = $"{s.TotalSeasons} Sezon";
            TxtNetwork.Text = s.Network ?? "Bilinmiyor";
            
            // Türler
            TxtGenre.Text = !string.IsNullOrEmpty(s.Genres) 
                ? s.Genres.Split(',').Take(2).FirstOrDefault()?.Trim() ?? "Tür Yok"
                : "Tür Yok";

            // Poster Resmi
            if (!string.IsNullOrEmpty(s.CoverImageUrl))
            {
                ImgPoster.ImageSource = new BitmapImage(new Uri(s.CoverImageUrl));
            }

            // Puanı varsayılan olarak TMDB puanından ayarla
            _userRating = (int)Math.Round(s.TmdbRating);
            TxtStarRating.Text = $"{_userRating}/10";
            UpdateStarDisplay();
        }

        // --- EKLEME İŞLEMİ ---

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSeries == null) return;

            // Durum (ComboBox)
            int selectedIndex = CmbStatus.SelectedIndex;
            switch (selectedIndex)
            {
                case 0: _selectedSeries.UserStatus = WatchStatus.Completed; break;
                case 1: _selectedSeries.UserStatus = WatchStatus.InProgress; break;
                case 2: _selectedSeries.UserStatus = WatchStatus.PlanToWatch; break;
                case 3: _selectedSeries.UserStatus = WatchStatus.Dropped; break;
            }

            // Auto-fill progress when completed
            if (_selectedSeries.UserStatus == WatchStatus.Completed && _selectedSeries.TotalSeasons > 0)
            {
                _selectedSeries.MyCurrentSeason = _selectedSeries.TotalSeasons;
                _selectedSeries.MyCurrentEpisode = _selectedSeries.TotalEpisodes;
            }

            // Puan
            _selectedSeries.UserRating = _userRating;

            // Tarih
            if (DtDate.DateTime != DateTime.MinValue)
            {
                _selectedSeries.WatchedDate = DtDate.DateTime;
            }

            // Ana sayfaya gönder
            SeriesAdded?.Invoke(_selectedSeries);

            this.Close();
        }
    }
}
