using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HobbyTracker.Models;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class AddMovieWindow : Window
    {
        private readonly TmdbService _tmdbService; // Servis örneği
        private Movie? _selectedMovie; // Seçilen film verisi
        private int _userRating = 0; // Kullanıcı puanı (0-10)

        // Film eklendiğinde ana sayfaya haber vermek için Event
        public event Action<Movie>? MovieAdded;

        public AddMovieWindow()
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
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)), // #64748b
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
                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)) // #fbbf24 (gold)
                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)); // #64748b (gray)
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
        
        // Kapatma Tuşu
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        // Arka plan tıklanınca pencereyi kapat
        private void Background_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        // --- ARAMA İŞLEMLERİ ---

        // Enter'a basınca ara
        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        // Butona basınca ara
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        // Asıl Arama Fonksiyonu
        private async void PerformSearch()
        {
            string query = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            // Boş durumu gizle, arama sonuçlarını göster
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            SearchResultsPanel.Visibility = Visibility.Visible;
            
            LoadingBar.Visibility = Visibility.Visible;
            LstMovies.ItemsSource = null; // Listeyi temizle
            DetailPanel.Visibility = Visibility.Hidden; // Detay panelini gizle

            // Servisten ara
            var results = await _tmdbService.SearchMoviesAsync(query);
            
            LstMovies.ItemsSource = results;
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        // --- SEÇİM VE DETAY GÖSTERİMİ ---

        private async void LstMovies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstMovies.SelectedItem is Movie movieStub)
            {
                // UI: Yükleniyor hissi verilebilir (Opsiyonel)
                DetailPanel.Visibility = Visibility.Visible;

                // Seçilen filmin DETAYLARINI (Süre, Tam Türler vb.) çek
                // SearchMovies sadece ID ve Başlık getirmişti.
                var fullMovie = await _tmdbService.GetMovieDetailsAsync(movieStub.Id);

                if (fullMovie != null)
                {
                    _selectedMovie = fullMovie;
                    PopulateDetails(fullMovie);
                }
            }
        }

        // Sağ Paneli Doldur
        private void PopulateDetails(Movie m)
        {
            TxtTitle.Text = m.Title;
            TxtSynopsis.Text = m.Synopsis;
            TxtDuration.Text = m.DurationFormatted;
            
            // Türleri birleştir (Bilim Kurgu, Macera...)
            TxtGenre.Text = m.Genres != null && m.Genres.Any() 
                ? string.Join(", ", m.Genres.Take(2)) 
                : "Tür Yok";

            // Poster Resmi
            if (!string.IsNullOrEmpty(m.PosterUrl))
            {
                ImgPoster.ImageSource = new BitmapImage(new Uri(m.PosterUrl));
            }

            // Puanı varsayılan olarak TMDB puanından ayarla (yuvarlayarak)
            _userRating = (int)Math.Round(m.GlobalRating);
            TxtStarRating.Text = $"{_userRating}/10";
            UpdateStarDisplay();
        }

        // --- EKLEME İŞLEMİ ---

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMovie == null) return;

            // Formdaki verileri modele işle
            
            // 1. Durum (ComboBox) - Yeni sıralama: İzlendi(0), İzleniyor(1), Listem(2), Yarım Bırakıldı(3)
            int selectedIndex = CmbStatus.SelectedIndex;
            // Map to WatchStatus enum: 0=İzlendi(Completed), 1=İzleniyor(InProgress), 2=Listem(PlanToWatch), 3=YarımBırakıldı(Dropped)
            switch (selectedIndex)
            {
                case 0: _selectedMovie.UserStatus = WatchStatus.Completed; break;
                case 1: _selectedMovie.UserStatus = WatchStatus.InProgress; break;
                case 2: _selectedMovie.UserStatus = WatchStatus.PlanToWatch; break;
                case 3: _selectedMovie.UserStatus = WatchStatus.Dropped; break;
            }

            // Auto-fill progress to 100% when status is Completed
            if (_selectedMovie.UserStatus == WatchStatus.Completed && _selectedMovie.DurationMinutes > 0)
            {
                _selectedMovie.WatchedMinutes = _selectedMovie.DurationMinutes;
            }

            // 2. Puan (Star rating)
            _selectedMovie.UserRating = _userRating;

            // 3. Tarih (DevExpress DateEdit uses DateTime)
            if (DtDate.DateTime != DateTime.MinValue)
            {
                _selectedMovie.WatchedDate = DtDate.DateTime;
            }

            // Ana sayfaya (MoviesView) gönder
            MovieAdded?.Invoke(_selectedMovie);

            // Pencereyi kapat
            this.Close();
        }
    }
}
