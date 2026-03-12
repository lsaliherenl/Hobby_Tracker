using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using HobbyTracker.Models;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class SeriesView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly SFirebase _firebaseService;
        private string _currentFilter = "Tümü";
        private string _searchText = "";
        
        // Session boyunca aynı featured diziyi korumak için static ID
        private static string? _sessionFeaturedSeriesId = null;
        
        public ObservableCollection<Series> SeriesList { get; set; }
        
        private ObservableCollection<Series> _filteredSeries;
        public ObservableCollection<Series> FilteredSeries 
        { 
            get => _filteredSeries;
            set
            {
                _filteredSeries = value;
                OnPropertyChanged(nameof(FilteredSeries));
            }
        }
        
        private Series _featuredSeries;
        public Series FeaturedSeries 
        { 
            get => _featuredSeries;
            set
            {
                _featuredSeries = value;
                OnPropertyChanged(nameof(FeaturedSeries));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SeriesView()
        {
            InitializeComponent();
            
            _firebaseService = new SFirebase();
            SeriesList = new ObservableCollection<Series>();
            FilteredSeries = new ObservableCollection<Series>();
            
            this.DataContext = this;
            this.Loaded += async (s, e) => await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                var series = await _firebaseService.GetSeriesAsync();

                SeriesList.Clear();
                
                if (series != null && series.Any())
                {
                    var sortedSeries = series.OrderByDescending(s => s.WatchedDate).ToList();
                    
                    foreach (var s in sortedSeries)
                    {
                        SeriesList.Add(s);
                    }

                    // Hero: İzleniyor veya Listem dizilerinden seçim (session boyunca aynı kalır)
                    var heroCandidates = sortedSeries
                        .Where(s => s.UserStatus == WatchStatus.InProgress || s.UserStatus == WatchStatus.PlanToWatch)
                        .ToList();
                    
                    // Session'da kayıtlı bir featured dizi var mı kontrol et
                    Series featuredFromSession = null;
                    if (!string.IsNullOrEmpty(_sessionFeaturedSeriesId))
                    {
                        featuredFromSession = sortedSeries.FirstOrDefault(s => s.Id == _sessionFeaturedSeriesId);
                    }
                    
                    if (featuredFromSession != null)
                    {
                        // Session'daki diziyi kullan
                        FeaturedSeries = featuredFromSession;
                    }
                    else if (heroCandidates.Any())
                    {
                        // İlk kez - rastgele seç ve session'a kaydet
                        var random = new Random();
                        FeaturedSeries = heroCandidates[random.Next(heroCandidates.Count)];
                        _sessionFeaturedSeriesId = FeaturedSeries.Id;
                    }
                    else
                    {
                        FeaturedSeries = sortedSeries.First();
                        _sessionFeaturedSeriesId = FeaturedSeries.Id;
                    }
                }
                else
                {
                    FeaturedSeries = null;
                }
                
                ApplyFilter();
                UpdateSeriesStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Diziler yüklenirken hata: {ex.Message}");
            }
        }
        
        private readonly StatsTrendService _trendService = new StatsTrendService();
        
        /// <summary>
        /// Dizi istatistiklerini hesaplar ve günceller
        /// </summary>
        private void UpdateSeriesStats()
        {
            // Toplam dizi sayısı
            int totalSeries = SeriesList.Count;
            
            // İzlenen dizi sayısı (Completed)
            int watchedSeries = SeriesList.Count(s => s.UserStatus == WatchStatus.Completed);

            // İzleniyor dizi sayısı (InProgress)
            int watchingSeries = SeriesList.Count(s => s.UserStatus == WatchStatus.InProgress);
            
            // Toplam izlenen bölüm sayısı (tüm dizilerin WatchedEpisodes toplamı)
            int totalEpisodesWatched = SeriesList.Sum(s => s.WatchedEpisodesCount);
            
            // Toplam izleme süresi (saat) - TotalWatchedMinutes kullanarak
            int totalWatchedMinutes = SeriesList.Sum(s => s.TotalWatchedMinutes);
            int totalWatchedHours = totalWatchedMinutes / 60;
            
            // Ortalama rating (rating > 0 olanlar)
            var ratedSeries = SeriesList.Where(s => s.UserRating > 0).ToList();
            double avgRating = ratedSeries.Any() ? ratedSeries.Average(s => s.UserRating) : 0;
            
            // UI Güncelle
            if (TxtTotalSeries != null) TxtTotalSeries.Text = totalSeries.ToString();
            if (TxtWatchedSeries != null) TxtWatchedSeries.Text = watchedSeries.ToString();
            if (TxtWatchingSeries != null) TxtWatchingSeries.Text = watchingSeries.ToString();
            if (TxtTotalEpisodesWatched != null) TxtTotalEpisodesWatched.Text = totalEpisodesWatched.ToString();
            if (TxtTotalWatchHours != null) TxtTotalWatchHours.Text = $"({totalWatchedHours} saat)";
            if (TxtAvgRating != null) TxtAvgRating.Text = avgRating.ToString("F1");
            
            // Özet satırı güncelle
            if (StatsText != null) StatsText.Text = $"Toplam {totalSeries} dizi • {watchedSeries} izlendi";
            
            // Trend ikonlarını güncelle
            UpdateTrendIcon(TrendTotalSeries, StatsTrendService.Keys.SeriesTotal, totalSeries);
            UpdateTrendIcon(TrendWatchedSeries, StatsTrendService.Keys.SeriesWatched, watchedSeries);
            UpdateTrendIcon(TrendWatchingSeries, StatsTrendService.Keys.SeriesWatching, watchingSeries);
            UpdateTrendIcon(TrendEpisodes, StatsTrendService.Keys.SeriesEpisodes, totalEpisodesWatched);
            UpdateTrendIcon(TrendRating, StatsTrendService.Keys.SeriesRating, avgRating);
            
            // Yeni değerleri kaydet
            _trendService.SaveValue(StatsTrendService.Keys.SeriesTotal, totalSeries);
            _trendService.SaveValue(StatsTrendService.Keys.SeriesWatched, watchedSeries);
            _trendService.SaveValue(StatsTrendService.Keys.SeriesWatching, watchingSeries);
            _trendService.SaveValue(StatsTrendService.Keys.SeriesEpisodes, totalEpisodesWatched);
            _trendService.SaveValue(StatsTrendService.Keys.SeriesRating, avgRating);
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
        /// StatsText tıklandığında istatistik panelini açar/kapatır
        /// </summary>
        private void StatsText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DetailedStatsPanel != null)
            {
                DetailedStatsPanel.Visibility = DetailedStatsPanel.Visibility == System.Windows.Visibility.Visible 
                    ? System.Windows.Visibility.Collapsed 
                    : System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// DetailedStatsPanel tıklandığında Dashboard Sinema sekmesine yönlendirir
        /// </summary>
        private void DetailedStatsPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateToDashboardAndSelectTab("TabSinema");
            }
        }

        private void FilterRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag)
            {
                _currentFilter = tag;
                ApplyFilter();
            }
        }

        private void TxtSearch_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            _searchText = TxtSearch.Text?.Trim() ?? "";
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (SeriesList == null) return;

            var filtered = SeriesList.AsEnumerable();

            // Status filtresi
            if (_currentFilter != "Tümü")
            {
                filtered = _currentFilter switch
                {
                    "İzlendi" => filtered.Where(s => s.UserStatus == WatchStatus.Completed),
                    "İzleniyor" => filtered.Where(s => s.UserStatus == WatchStatus.InProgress),
                    "Listem" => filtered.Where(s => s.UserStatus == WatchStatus.PlanToWatch),
                    "Yarım Bırakıldı" => filtered.Where(s => s.UserStatus == WatchStatus.Dropped),
                    _ => filtered
                };
            }

            // Arama filtresi
            if (!string.IsNullOrEmpty(_searchText))
            {
                filtered = filtered.Where(s => 
                    (s.Title?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Overview?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Genres?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            // Popup filtrelerini uygula
            var activeFilters = GetActiveFilters();
            
            // Puan filtreleri
            var ratingFilters = activeFilters.Where(f => f.StartsWith("Rating_")).ToList();
            if (ratingFilters.Any())
            {
                filtered = filtered.Where(s => {
                    foreach (var rf in ratingFilters)
                    {
                        if (rf == "Rating_9_10" && s.UserRating >= 9) return true;
                        if (rf == "Rating_7_8" && s.UserRating >= 7 && s.UserRating <= 8) return true;
                        if (rf == "Rating_5_6" && s.UserRating >= 5 && s.UserRating <= 6) return true;
                        if (rf == "Rating_Unrated" && s.UserRating == 0) return true;
                    }
                    return false;
                });
            }

            // Sezon filtreleri
            var seasonFilters = activeFilters.Where(f => f.StartsWith("Season_")).ToList();
            if (seasonFilters.Any())
            {
                filtered = filtered.Where(s => {
                    foreach (var sf in seasonFilters)
                    {
                        if (sf == "Season_Short" && s.TotalSeasons <= 2) return true;
                        if (sf == "Season_Medium" && s.TotalSeasons >= 3 && s.TotalSeasons <= 5) return true;
                        if (sf == "Season_Long" && s.TotalSeasons >= 6) return true;
                    }
                    return false;
                });
            }

            // Yıl filtreleri
            var yearFilters = activeFilters.Where(f => f.StartsWith("Year_")).ToList();
            if (yearFilters.Any())
            {
                filtered = filtered.Where(s => {
                    int year = 0;
                    int.TryParse(s.Year, out year);
                    foreach (var yf in yearFilters)
                    {
                        if (yf == "Year_2020" && year >= 2020) return true;
                        if (yf == "Year_2010" && year >= 2010 && year < 2020) return true;
                        if (yf == "Year_2000" && year >= 2000 && year < 2010) return true;
                        if (yf == "Year_Classic" && year < 2000 && year > 0) return true;
                    }
                    return false;
                });
            }

            // Sıralama uygula
            var sortTag = GetSelectedSortTag();
            filtered = sortTag switch
            {
                "Date_Desc" => filtered.OrderByDescending(s => s.AddedDate),
                "Date_Asc" => filtered.OrderBy(s => s.AddedDate),
                "Title_Asc" => filtered.OrderBy(s => s.Title),
                "Title_Desc" => filtered.OrderByDescending(s => s.Title),
                "Rating_Desc" => filtered.OrderByDescending(s => s.UserRating),
                "Rating_Asc" => filtered.OrderBy(s => s.UserRating),
                "Season_Desc" => filtered.OrderByDescending(s => s.TotalSeasons),
                "Season_Asc" => filtered.OrderBy(s => s.TotalSeasons),
                "Year_Desc" => filtered.OrderByDescending(s => s.Year),
                _ => filtered.OrderByDescending(s => s.AddedDate)
            };

            FilteredSeries = new ObservableCollection<Series>(filtered);
            
            // Grid görünümü için "Add" kartını ekle
            if (SeriesGridControl != null)
            {
                var itemsWithAdd = new System.Collections.Generic.List<object>(filtered);
                itemsWithAdd.Add("Add"); // "Yeni Dizi Ekle" kartı için
                SeriesGridControl.ItemsSource = itemsWithAdd;
            }
        }

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

        private string GetSelectedSortTag()
        {
            if (SortListBox?.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                return tag;
            }
            return "Date_Desc";
        }

        private void BtnAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var addWindow = new AddSeriesWindow();
            addWindow.SeriesAdded += OnSeriesAdded;
            addWindow.ShowDialog();
        }

        private void BtnAddSeries_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnAdd_Click(sender, e);
        }

        private async void OnSeriesAdded(Series newSeries)
        {
            if (newSeries != null)
            {
                var result = await _firebaseService.AddSeriesAsync(newSeries);
                
                if (result == "OK")
                {
                    SeriesList.Insert(0, newSeries);
                    
                    if (newSeries.UserStatus == WatchStatus.InProgress)
                    {
                        FeaturedSeries = newSeries;
                    }
                    
                    ApplyFilter();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Dizi kaydedilemedi: {result}");
                }
            }
        }

        private void BtnFilter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FilterPopup.IsOpen = !FilterPopup.IsOpen;
        }

        private void BtnSort_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SortPopup.IsOpen = !SortPopup.IsOpen;
        }

        private void Filter_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void SortOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnClearFilters_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var child in GetLogicalChildren(FilterPopup))
            {
                if (child is DevExpress.Xpf.Editors.CheckEdit checkEdit)
                {
                    checkEdit.IsChecked = false;
                }
            }
            ApplyFilter();
        }

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

        private void BtnViewGrid_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnViewGrid.Opacity = 1;
            BtnViewList.Opacity = 0.5;
            
            // Grid görünümünü göster, liste görünümünü gizle
            if (SeriesGridControl != null) SeriesGridControl.Visibility = System.Windows.Visibility.Visible;
            if (SeriesListControl != null) SeriesListControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void BtnViewList_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnViewGrid.Opacity = 0.5;
            BtnViewList.Opacity = 1;
            
            // Liste görünümünü göster, grid görünümünü gizle
            if (SeriesGridControl != null) SeriesGridControl.Visibility = System.Windows.Visibility.Collapsed;
            if (SeriesListControl != null) SeriesListControl.Visibility = System.Windows.Visibility.Visible;
        }

        private void SeriesCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.Tag is Series series)
            {
                var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToSeriesDetail(series);
            }
        }

        private void BtnContinueWatching_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FeaturedSeries != null)
            {
                var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToSeriesDetail(FeaturedSeries);
            }
        }

        private void BtnMoreInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FeaturedSeries != null)
            {
                var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
                mainWindow?.NavigateToSeriesDetail(FeaturedSeries);
            }
        }

        // Dışarıdan data yenileme için public metod
        public async void RefreshData()
        {
            await LoadDataAsync();
        }
    }
}