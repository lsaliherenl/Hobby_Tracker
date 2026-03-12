using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using HobbyTracker.Models;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class MoviesView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly SFirebase _firebaseService;
        private string _currentFilter = "Tümü";
        private string _searchText = "";
        
        // Session boyunca aynı featured filmi korumak için static ID
        private static string? _sessionFeaturedMovieId = null;
        
        public ObservableCollection<Movie> MoviesList { get; set; }
        
        private ObservableCollection<Movie> _filteredMovies;
        public ObservableCollection<Movie> FilteredMovies 
        { 
            get => _filteredMovies;
            set
            {
                _filteredMovies = value;
                OnPropertyChanged(nameof(FilteredMovies));
            }
        }
        
        private Movie _featuredMovie;
        public Movie FeaturedMovie 
        { 
            get => _featuredMovie;
            set
            {
                _featuredMovie = value;
                OnPropertyChanged(nameof(FeaturedMovie));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MoviesView()
        {
            InitializeComponent();
            
            _firebaseService = new SFirebase();
            MoviesList = new ObservableCollection<Movie>();
            FilteredMovies = new ObservableCollection<Movie>();
            
            this.DataContext = this;
            
            // Async veri yükle
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                var movies = await _firebaseService.GetMoviesAsync();

                MoviesList.Clear();
                
                if (movies != null && movies.Any())
                {
                    var sortedMovies = movies.OrderByDescending(m => m.WatchedDate).ToList();
                    
                    foreach (var movie in sortedMovies)
                    {
                        MoviesList.Add(movie);
                    }

                    // Hero: İzleniyor veya Listem filmlerinden seçim (session boyunca aynı kalır)
                    var heroCandidates = sortedMovies
                        .Where(m => m.UserStatus == WatchStatus.InProgress || m.UserStatus == WatchStatus.PlanToWatch)
                        .ToList();
                    
                    // Session'da kayıtlı bir featured film var mı kontrol et
                    Movie featuredFromSession = null;
                    if (!string.IsNullOrEmpty(_sessionFeaturedMovieId))
                    {
                        featuredFromSession = sortedMovies.FirstOrDefault(m => m.Id == _sessionFeaturedMovieId);
                    }
                    
                    if (featuredFromSession != null)
                    {
                        // Session'daki filmi kullan
                        FeaturedMovie = featuredFromSession;
                    }
                    else if (heroCandidates.Any())
                    {
                        // İlk kez - rastgele seç ve session'a kaydet
                        var random = new Random();
                        FeaturedMovie = heroCandidates[random.Next(heroCandidates.Count)];
                        _sessionFeaturedMovieId = FeaturedMovie.Id;
                    }
                    else
                    {
                        // Hiç İzleniyor/Listem yoksa en son eklenen filmi göster
                        FeaturedMovie = sortedMovies.First();
                        _sessionFeaturedMovieId = FeaturedMovie.Id;
                    }
                }
                else
                {
                    FeaturedMovie = null;
                }
                
                ApplyFilter();
                UpdateMovieStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filmler yüklenirken hata: {ex.Message}");
            }
        }
        
        private readonly StatsTrendService _trendService = new StatsTrendService();
        
        /// <summary>
        /// Film istatistiklerini hesaplar ve günceller
        /// </summary>
        private void UpdateMovieStats()
        {
            // Toplam film sayısı
            int totalMovies = MoviesList.Count;
            
            // İzlenen film sayısı (Completed)
            int watchedMovies = MoviesList.Count(m => m.UserStatus == WatchStatus.Completed);

            // İzleniyor film sayısı (InProgress)
            int watchingMovies = MoviesList.Count(m => m.UserStatus == WatchStatus.InProgress);
            
            // Toplam izleme süresi (saat cinsinden) - sadece izlenen filmler
            int totalMinutes = MoviesList
                .Where(m => m.UserStatus == WatchStatus.Completed)
                .Sum(m => m.DurationMinutes);
            int totalHours = totalMinutes / 60;
            
            // Ortalama rating (rating > 0 olanlar)
            var ratedMovies = MoviesList.Where(m => m.UserRating > 0).ToList();
            double avgRating = ratedMovies.Any() ? ratedMovies.Average(m => m.UserRating ?? 0) : 0;
            
            // UI Güncelle
            if (TxtTotalMovies != null) TxtTotalMovies.Text = totalMovies.ToString();
            if (TxtWatchedMovies != null) TxtWatchedMovies.Text = watchedMovies.ToString();
            if (TxtWatchingMovies != null) TxtWatchingMovies.Text = watchingMovies.ToString();
            if (TxtTotalWatchTime != null) TxtTotalWatchTime.Text = totalHours > 0 ? $"{totalHours} saat" : "0 saat";
            if (TxtAvgRating != null) TxtAvgRating.Text = avgRating.ToString("F1");
            
            // Özet satırı güncelle
            if (StatsText != null) StatsText.Text = $"Toplam {totalMovies} film • {watchedMovies} izlendi";
            
            // Trend ikonlarını güncelle
            UpdateTrendIcon(TrendTotalMovies, StatsTrendService.Keys.MoviesTotal, totalMovies);
            UpdateTrendIcon(TrendWatchedMovies, StatsTrendService.Keys.MoviesWatched, watchedMovies);
            UpdateTrendIcon(TrendWatchingMovies, StatsTrendService.Keys.MoviesWatching, watchingMovies);
            UpdateTrendIcon(TrendWatchTime, StatsTrendService.Keys.MoviesWatchTime, totalHours);
            UpdateTrendIcon(TrendRating, StatsTrendService.Keys.MoviesRating, avgRating);
            
            // Yeni değerleri kaydet
            _trendService.SaveValue(StatsTrendService.Keys.MoviesTotal, totalMovies);
            _trendService.SaveValue(StatsTrendService.Keys.MoviesWatched, watchedMovies);
            _trendService.SaveValue(StatsTrendService.Keys.MoviesWatching, watchingMovies);
            _trendService.SaveValue(StatsTrendService.Keys.MoviesWatchTime, totalHours);
            _trendService.SaveValue(StatsTrendService.Keys.MoviesRating, avgRating);
            _trendService.SaveToFile();
        }
        
        /// <summary>
        /// Trend ikonunu ve rengini günceller
        /// </summary>
        private void UpdateTrendIcon(System.Windows.Controls.TextBlock trendIcon, string key, double currentValue)
        {
            if (trendIcon == null) return;
            
            trendIcon.Text = _trendService.GetTrendIcon(key, currentValue);
            trendIcon.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    _trendService.GetTrendColor(key, currentValue, "#6b7280")
                )
            );
        }
        
        /// <summary>
        /// StatsText veya Panel tıklandığında Dashboard Sinema sekmesine git
        /// </summary>
        private void StatsText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NavigateToDashboardStats();
        }

        private void DetailedStatsPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NavigateToDashboardStats();
        }

        private void NavigateToDashboardStats()
        {
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToDashboardAndSelectTab("TabSinema");
        }

        // Filtre Sekmesi Değişince
        private void FilterRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag)
            {
                _currentFilter = tag;
                ApplyFilter();
            }
        }

        // Arama Kutusu Değişince
        private void TxtSearch_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            _searchText = TxtSearch.Text?.Trim() ?? "";
            ApplyFilter();
        }

        // Filtre ve arama uygula
        private void ApplyFilter()
        {
            if (MoviesList == null) return;

            var filtered = MoviesList.AsEnumerable();

            // Status filtresi (tab buttons)
            if (_currentFilter != "Tümü")
            {
                filtered = _currentFilter switch
                {
                    "İzlendi" => filtered.Where(m => m.UserStatus == WatchStatus.Completed),
                    "İzleniyor" => filtered.Where(m => m.UserStatus == WatchStatus.InProgress),
                    "Listem" => filtered.Where(m => m.UserStatus == WatchStatus.PlanToWatch),
                    "Yarım Bırakıldı" => filtered.Where(m => m.UserStatus == WatchStatus.Dropped),
                    _ => filtered
                };
            }

            // Arama filtresi
            if (!string.IsNullOrEmpty(_searchText))
            {
                filtered = filtered.Where(m => 
                    (m.Title?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Synopsis?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Genres != null && m.Genres.Any(g => g.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
                );
            }

            // Popup filtrelerini uygula
            var activeFilters = GetActiveFilters();
            
            // Puan filtreleri
            var ratingFilters = activeFilters.Where(f => f.StartsWith("Rating_")).ToList();
            if (ratingFilters.Any())
            {
                filtered = filtered.Where(m => {
                    foreach (var rf in ratingFilters)
                    {
                        if (rf == "Rating_9_10" && m.UserRating >= 9) return true;
                        if (rf == "Rating_7_8" && m.UserRating >= 7 && m.UserRating <= 8) return true;
                        if (rf == "Rating_5_6" && m.UserRating >= 5 && m.UserRating <= 6) return true;
                        if (rf == "Rating_Unrated" && m.UserRating == 0) return true;
                    }
                    return false;
                });
            }

            // Süre filtreleri
            var durationFilters = activeFilters.Where(f => f.StartsWith("Duration_")).ToList();
            if (durationFilters.Any())
            {
                filtered = filtered.Where(m => {
                    foreach (var df in durationFilters)
                    {
                        if (df == "Duration_Short" && m.DurationMinutes < 90) return true;
                        if (df == "Duration_Medium" && m.DurationMinutes >= 90 && m.DurationMinutes <= 150) return true;
                        if (df == "Duration_Long" && m.DurationMinutes > 150) return true;
                    }
                    return false;
                });
            }

            // Yıl filtreleri
            var yearFilters = activeFilters.Where(f => f.StartsWith("Year_")).ToList();
            if (yearFilters.Any())
            {
                filtered = filtered.Where(m => {
                    foreach (var yf in yearFilters)
                    {
                        if (yf == "Year_2020" && m.Year >= 2020) return true;
                        if (yf == "Year_2010" && m.Year >= 2010 && m.Year < 2020) return true;
                        if (yf == "Year_2000" && m.Year >= 2000 && m.Year < 2010) return true;
                        if (yf == "Year_Classic" && m.Year < 2000) return true;
                    }
                    return false;
                });
            }

            // Sıralama uygula
            var sortTag = GetSelectedSortTag();
            filtered = sortTag switch
            {
                "Date_Desc" => filtered.OrderByDescending(m => m.AddedDate),
                "Date_Asc" => filtered.OrderBy(m => m.AddedDate),
                "Title_Asc" => filtered.OrderBy(m => m.Title),
                "Title_Desc" => filtered.OrderByDescending(m => m.Title),
                "Rating_Desc" => filtered.OrderByDescending(m => m.UserRating),
                "Rating_Asc" => filtered.OrderBy(m => m.UserRating),
                "Duration_Desc" => filtered.OrderByDescending(m => m.DurationMinutes),
                "Duration_Asc" => filtered.OrderBy(m => m.DurationMinutes),
                "Year_Desc" => filtered.OrderByDescending(m => m.Year),
                _ => filtered.OrderByDescending(m => m.AddedDate)
            };

            FilteredMovies = new ObservableCollection<Movie>(filtered);
            
            // Grid görünümü için "Add" kartını ekle
            if (MoviesGridControl != null)
            {
                var itemsWithAdd = new System.Collections.Generic.List<object>(filtered);
                itemsWithAdd.Add("Add"); // "Yeni Film Ekle" kartı için
                MoviesGridControl.ItemsSource = itemsWithAdd;
            }
        }

        // Aktif filtreleri al
        private System.Collections.Generic.List<string> GetActiveFilters()
        {
            var filters = new System.Collections.Generic.List<string>();
            
            foreach (var child in GetLogicalChildren(FilterPopup))
            {
                if (child is DevExpress.Xpf.Editors.CheckEdit checkEdit && 
                    checkEdit.IsChecked == true && 
                    checkEdit.Tag is string tag)
                {
                    filters.Add(tag);
                }
            }
            
            return filters;
        }

        // Seçili sıralama seçeneğini al
        private string GetSelectedSortTag()
        {
            if (SortListBox?.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                return tag;
            }
            return "Date_Desc"; // Varsayılan
        }

        // "+" Butonuna Tıklayınca
        private void BtnAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var addWindow = new AddMovieWindow();
            addWindow.MovieAdded += OnMovieAdded;
            addWindow.ShowDialog();
        }

        private void BtnAddMovie_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnAdd_Click(sender, e);
        }

        // Yeni Film Gelince Listeye Ekle ve Firebase'e Kaydet
        private async void OnMovieAdded(Movie newMovie)
        {
            if (newMovie != null)
            {
                var result = await _firebaseService.AddMovieAsync(newMovie);
                
                if (result == "OK")
                {
                    MoviesList.Insert(0, newMovie);
                    
                    // "İzleniyor" ise Hero'yu güncelle
                    if (newMovie.UserStatus == WatchStatus.InProgress)
                    {
                        FeaturedMovie = newMovie;
                    }
                    
                    ApplyFilter();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Film kaydedilemedi: {result}");
                }
            }
        }

        // Filtre popup aç/kapat
        private void BtnFilter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FilterPopup.IsOpen = !FilterPopup.IsOpen;
        }

        // Sıralama popup aç/kapat
        private void BtnSort_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SortPopup.IsOpen = !SortPopup.IsOpen;
        }

        // Filtre değiştiğinde
        private void Filter_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplyFilter();
        }

        // Sıralama değiştiğinde
        private void SortOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        // Filtreleri temizle
        private void BtnClearFilters_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Tüm CheckEdit'leri temizle
            foreach (var child in GetLogicalChildren(FilterPopup))
            {
                if (child is DevExpress.Xpf.Editors.CheckEdit checkEdit)
                {
                    checkEdit.IsChecked = false;
                }
            }
            ApplyFilter();
        }

        // Yardımcı: Popup içindeki tüm çocukları bul
        private System.Collections.Generic.IEnumerable<System.Windows.DependencyObject> GetLogicalChildren(System.Windows.DependencyObject parent)
        {
            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren(parent))
            {
                if (child is System.Windows.DependencyObject depObj)
                {
                    yield return depObj;
                    foreach (var grandChild in GetLogicalChildren(depObj))
                    {
                        yield return grandChild;
                    }
                }
            }
        }

        // Grid görünümü
        private void BtnViewGrid_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnViewGrid.Opacity = 1;
            BtnViewList.Opacity = 0.5;
            
            // Grid görünümünü göster, liste görünümünü gizle
            if (MoviesGridControl != null) MoviesGridControl.Visibility = System.Windows.Visibility.Visible;
            if (MoviesListControl != null) MoviesListControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Liste görünümü
        private void BtnViewList_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnViewGrid.Opacity = 0.5;
            BtnViewList.Opacity = 1;
            
            // Liste görünümünü göster, grid görünümünü gizle
            if (MoviesGridControl != null) MoviesGridControl.Visibility = System.Windows.Visibility.Collapsed;
            if (MoviesListControl != null) MoviesListControl.Visibility = System.Windows.Visibility.Visible;
        }

        // Film kartına tıklayınca detay sayfasına git
        private void MovieCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.Tag is Movie movie)
            {
                var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToMovieDetail(movie);
            }
        }

        // Hero: Devam Et - opens movie detail to continue watching
        private void BtnContinueWatching_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FeaturedMovie != null)
            {
                var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToMovieDetail(FeaturedMovie);
            }
        }

        // Hero: Daha Fazla - opens movie detail for more info
        private void BtnMoreInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FeaturedMovie != null)
            {
                var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToMovieDetail(FeaturedMovie);
            }
        }
    }
}