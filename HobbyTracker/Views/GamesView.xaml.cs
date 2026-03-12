using DevExpress.Xpf.Core;
using HobbyTracker.Models; // Game modelini kullanmak için
using HobbyTracker.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input; // MouseButtonEventArgs için
using System.Windows.Data;
using System.Globalization;

namespace HobbyTracker.Views
{
    public partial class GamesView : System.Windows.Controls.UserControl
    {
        private readonly SFirebase _firebaseService;
        private List<Game> _allGames = new List<Game>();
        private List<Game> _filteredGames = new List<Game>();
        private HashSet<string> _activeFilters = new HashSet<string>();
        private string _currentSortOption = "Date_Desc";
        private string _currentFilter = "Tümü";
        
        // Görünüm modu
        private string _viewMode = "Grid"; // Grid veya List
        
        // Toplu işlemler
        private bool _isSelectionMode = false;
        private HashSet<string> _selectedGameIds = new HashSet<string>();

        public GamesView()
        {
            InitializeComponent();
            _firebaseService = new SFirebase();
            this.Loaded += GamesView_Loaded;
            
            // Eğer grid/list butonları varsa başlangıç durumunu ayarla
            var btnGrid = this.FindName("BtnViewGrid") as System.Windows.Controls.Control;
            var btnList = this.FindName("BtnViewList") as System.Windows.Controls.Control;
            if (btnGrid != null && btnList != null)
            {
                btnGrid.Opacity = 1.0;
                btnList.Opacity = 0.5;
            }
        }
        
        private async void GamesView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGamesAsync();
        }

        private async System.Threading.Tasks.Task LoadGamesAsync()
        {
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                _allGames = await _firebaseService.GetGamesAsync();
                ApplyFilters();
                Mouse.OverrideCursor = null;
            }
            catch (System.Exception ex)
            {
                Mouse.OverrideCursor = null;
                DXMessageBox.Show($"Oyunlar yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is DevExpress.Xpf.Editors.CheckEdit checkEdit && checkEdit.Tag != null)
            {
                string tag = checkEdit.Tag.ToString();
                if (checkEdit.IsChecked == true)
                {
                    _activeFilters.Add(tag);
                }
                else
                {
                    _activeFilters.Remove(tag);
                }
                ApplyFilters();
            }
        }

        private void SortOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox && listBox.SelectedItem != null)
            {
                if (listBox.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag != null)
                {
                    _currentSortOption = item.Tag.ToString();
                    ApplyFilters();
                    
                    // Popup'ı kapat
                    if (this.FindName("SortPopup") is System.Windows.Controls.Primitives.Popup popup)
                        popup.IsOpen = false;
                }
            }
        }
        private void DetailedStatsPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Dashboard'a git
                mainWindow.NavigateToDashboardAndSelectTab("TabOyunlar");
            }
        }
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("FilterPopup") is Popup popup)
            {
                popup.IsOpen = true;
            }
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("SortPopup") is Popup popup)
            {
                popup.IsOpen = true;
            }
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _activeFilters.Clear();
            
            // UI'daki CheckBox'ları temizle
            ResetCheckBoxes(this);
            
            ApplyFilters();
        }

        private void ResetCheckBoxes(DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is DevExpress.Xpf.Editors.CheckEdit checkEdit && checkEdit.Tag != null)
                {
                    checkEdit.IsChecked = false;
                }
                else
                {
                    ResetCheckBoxes(child);
                }
            }
        }

        private void ApplyFilters()
        {
            // Tüm filtreleri birleştirip tek ToList() çağrısı yap (performans optimizasyonu)
            var query = _allGames.AsEnumerable();
            
            // 1. Arama Metni
            string searchText = TxtSearch?.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(g => (g.Title?.ToLower().Contains(searchText) ?? false) ||
                                         (g.Developer?.ToLower().Contains(searchText) ?? false));
            }

            // 2. Status Filtresi (RadioButton)
            if (_currentFilter != "Tümü")
            {
                if (_currentFilter == "Devam Edenler") query = query.Where(g => g.Status == "Oynuyor");
                else if (_currentFilter == "Favoriler") query = query.Where(g => g.IsFavorite);
                else query = query.Where(g => g.Status == _currentFilter);
            }

            // 3. Platform Filtresi
            var platformFilters = _activeFilters.Where(f => f.StartsWith("Platform_")).ToList();
            if (platformFilters.Any())
            {
                query = query.Where(g => 
                    (platformFilters.Contains("Platform_Steam") && (g.Platform.Contains("PC") || g.Platform.Contains("Steam"))) ||
                    (platformFilters.Contains("Platform_PS5") && (g.Platform.Contains("PS5") || g.Platform.Contains("PlayStation"))) ||
                    (platformFilters.Contains("Platform_Xbox") && (g.Platform.Contains("Xbox") || g.Platform.Contains("Series"))) ||
                    (platformFilters.Contains("Platform_Switch") && (g.Platform.Contains("Switch") || g.Platform.Contains("Nintendo"))) ||
                    (platformFilters.Contains("Platform_Mobile") && g.Platform.Contains("Mobil"))
                );
            }

            // 4. Rating Filtresi
            var ratingFilters = _activeFilters.Where(f => f.StartsWith("Rating_")).ToList();
            if (ratingFilters.Any())
            {
                query = query.Where(g => 
                    (ratingFilters.Contains("Rating_9_10") && g.UserRating >= 9 && g.UserRating <= 10) ||
                    (ratingFilters.Contains("Rating_7_8") && g.UserRating >= 7 && g.UserRating < 9) ||
                    (ratingFilters.Contains("Rating_5_6") && g.UserRating >= 5 && g.UserRating < 7) ||
                    (ratingFilters.Contains("Rating_Unrated") && g.Score == 0)
                );
            }

            // 5. Yıl Filtresi
            var yearFilters = _activeFilters.Where(f => f.StartsWith("Year_")).ToList();
            if (yearFilters.Any())
            {
                int currentYear = DateTime.Now.Year;
                query = query.Where(g => {
                     if (!int.TryParse(g.ReleaseYear, out int y)) return false;
                     return (yearFilters.Contains("Year_Last5") && y >= currentYear - 5) ||
                            (yearFilters.Contains("Year_2010_2020") && y >= 2010 && y <= 2020) ||
                            (yearFilters.Contains("Year_Oldies") && y < 2010);
                });
            }
            
            // 6. Sıralama
            switch (_currentSortOption)
            {
                case "Date_Desc": query = query.OrderByDescending(g => g.AddedDate); break;
                case "Date_Asc": query = query.OrderBy(g => g.AddedDate); break;
                case "Title_Asc": query = query.OrderBy(g => g.Title ?? ""); break;
                case "Title_Desc": query = query.OrderByDescending(g => g.Title ?? ""); break;
                case "Rating_Desc": query = query.OrderByDescending(g => g.UserRating); break;
                case "Rating_Asc": query = query.OrderBy(g => g.UserRating); break;
                case "PlayTime_Desc": query = query.OrderByDescending(g => g.PlayTime); break;
                case "PlayTime_Asc": query = query.OrderBy(g => g.PlayTime); break;
                case "Year_Desc": query = query.OrderByDescending(g => g.ReleaseYear ?? ""); break;
                case "Year_Asc": query = query.OrderBy(g => g.ReleaseYear ?? ""); break;
                default: query = query.OrderByDescending(g => g.AddedDate); break;
            }
            
            // Tek seferde liste oluştur
            _filteredGames = query.ToList();
            
            // UI Güncelleme (Items Source ve List 'Add' buttonu)
            var gamesList = new List<object>(_filteredGames);
            gamesList.Add("Add"); // "Yeni Oyun Ekle" kartı için (sadece grid için)

            // Grid görünümü için (Add butonu dahil)
            if (GamesGridControl != null)
            {
                GamesGridControl.ItemsSource = gamesList;
            }
            
            // Liste görünümü için (Add butonu olmadan, sadece oyunlar)
            if (GamesListControl != null)
            {
                GamesListControl.ItemsSource = _filteredGames;
            }

            UpdateActiveSegment();
            UpdateStatistics();
        }

        private readonly StatsTrendService _trendService = new StatsTrendService();

        private void UpdateStatistics()
        {
            int totalGames = _allGames.Count;
            int finishedGames = _allGames.Count(g => g.Status == "Tamamlandı");
            int playingGames = _allGames.Count(g => g.Status == "Oynuyor");
            int totalPlayTime = _allGames.Where(g => g.Status != "İstek Listesi").Sum(g => g.PlayTime);
            var ratedGames = _allGames.Where(g => g.Status != "İstek Listesi" && g.UserRating > 0).ToList();
            double avgRating = ratedGames.Count > 0 ? ratedGames.Average(g => g.UserRating) : 0;

            if (StatsText != null) StatsText.Text = $"Toplam {totalGames} oyun • {finishedGames} tamamlandı";
            if (TxtTotalGames != null) TxtTotalGames.Text = totalGames.ToString();
            if (TxtCompletedGames != null) TxtCompletedGames.Text = finishedGames.ToString();
            if (TxtPlayingGames != null) TxtPlayingGames.Text = playingGames.ToString();
            if (TxtTotalPlayTime != null) TxtTotalPlayTime.Text = $"{totalPlayTime} Saat";
            if (TxtAvgRating != null) TxtAvgRating.Text = avgRating.ToString("F1");
            
            // Trend ikonlarını güncelle
            UpdateTrendIcon(TrendTotalGames, StatsTrendService.Keys.GamesTotal, totalGames);
            UpdateTrendIcon(TrendCompletedGames, StatsTrendService.Keys.GamesCompleted, finishedGames);
            UpdateTrendIcon(TrendPlayingGames, StatsTrendService.Keys.GamesPlaying, playingGames);
            UpdateTrendIcon(TrendPlayTime, StatsTrendService.Keys.GamesPlayTime, totalPlayTime);
            UpdateTrendIcon(TrendRating, StatsTrendService.Keys.GamesRating, avgRating);
            
            // Yeni değerleri kaydet
            _trendService.SaveValue(StatsTrendService.Keys.GamesTotal, totalGames);
            _trendService.SaveValue(StatsTrendService.Keys.GamesCompleted, finishedGames);
            _trendService.SaveValue(StatsTrendService.Keys.GamesPlaying, playingGames);
            _trendService.SaveValue(StatsTrendService.Keys.GamesPlayTime, totalPlayTime);
            _trendService.SaveValue(StatsTrendService.Keys.GamesRating, avgRating);
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
        
        private void StatsText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DetailedStatsPanel.Visibility == Visibility.Visible)
                DetailedStatsPanel.Visibility = Visibility.Collapsed;
            else
                DetailedStatsPanel.Visibility = Visibility.Visible;
        }
        
        // --- TOPLU İŞLEMLER ---
        private void BtnBulkActions_Click(object sender, RoutedEventArgs e)
        {
            _isSelectionMode = !_isSelectionMode;
            _selectedGameIds.Clear();
            
            if (_isSelectionMode)
            {
                BulkActionsToolbar.Visibility = Visibility.Visible;
                BtnBulkActions.Content = "İptal";
                ShowCheckboxes(true);
            }
            else
            {
                BulkActionsToolbar.Visibility = Visibility.Collapsed;
                BtnBulkActions.Content = "Toplu İşlem";
                ShowCheckboxes(false);
            }
            UpdateSelectedCount();
        }
        
        private void ShowCheckboxes(bool show)
        {
            if (GamesGridControl != null)
            {
                foreach (var item in GamesGridControl.Items)
                {
                    if (item is Game game)
                    {
                        var container = GamesGridControl.ItemContainerGenerator.ContainerFromItem(item);
                        if (container != null)
                        {
                            var contentControl = FindVisualChild<ContentControl>(container);
                            if (contentControl != null)
                            {
                                var border = FindVisualChild<Border>(contentControl);
                                if (border != null)
                                {
                                    var checkbox = FindVisualChild<System.Windows.Controls.CheckBox>(border);
                                    if (checkbox != null)
                                    {
                                        checkbox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T) return (T)child;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }
        
        private void GameCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Game game)
            {
                _selectedGameIds.Add(game.Id);
                UpdateSelectedCount();
            }
        }
        
        private void GameCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Game game)
            {
                _selectedGameIds.Remove(game.Id);
                UpdateSelectedCount();
            }
        }
        
        private void GameCheckBox_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
        
        private void UpdateSelectedCount()
        {
            if (TxtSelectedCount != null) TxtSelectedCount.Text = $"{_selectedGameIds.Count} seçili";
        }
        
        private async void BtnBulkAddFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGameIds.Count == 0) return;
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                foreach (var gameId in _selectedGameIds)
                {
                    var game = _allGames.FirstOrDefault(g => g.Id == gameId);
                    if (game != null)
                    {
                        game.IsFavorite = true;
                        await _firebaseService.UpdateGameAsync(game);
                    }
                }
                Mouse.OverrideCursor = null;
                await LoadGamesAsync();
                DXMessageBox.Show($"{_selectedGameIds.Count} oyun favorilere eklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                Mouse.OverrideCursor = null;
                DXMessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnBulkRemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGameIds.Count == 0) return;
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                foreach (var gameId in _selectedGameIds)
                {
                    var game = _allGames.FirstOrDefault(g => g.Id == gameId);
                    if (game != null)
                    {
                        game.IsFavorite = false;
                        await _firebaseService.UpdateGameAsync(game);
                    }
                }
                Mouse.OverrideCursor = null;
                await LoadGamesAsync();
                DXMessageBox.Show($"{_selectedGameIds.Count} oyun favorilerden çıkarıldı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                Mouse.OverrideCursor = null;
                DXMessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnBulkDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGameIds.Count == 0) return;
            var result = DXMessageBox.Show($"{_selectedGameIds.Count} oyunu silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    foreach (var gameId in _selectedGameIds)
                    {
                        await _firebaseService.DeleteGameAsync(gameId);
                    }
                    Mouse.OverrideCursor = null;
                    await LoadGamesAsync();
                    DXMessageBox.Show($"{_selectedGameIds.Count} oyun silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnBulkActions_Click(sender, e);
                }
                catch (System.Exception ex)
                {
                    Mouse.OverrideCursor = null;
                    DXMessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void BtnBulkCancel_Click(object sender, RoutedEventArgs e)
        {
            BtnBulkActions_Click(sender, e);
        }

        // --- ARAMA ve FİLTRELEME UI ---
        private void TxtSearch_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb)
            {
                if (rb.Tag != null) _currentFilter = rb.Tag.ToString();
                else if (rb.Content != null) _currentFilter = rb.Content.ToString();
                ApplyFilters();
            }
        }

        private void UpdateActiveSegment()
        {
            // Event handler'ları geçici olarak kaldır (sonsuz döngüyü önlemek için)
            if (RadioTumu != null) RadioTumu.Checked -= FilterRadioButton_Checked;
            if (RadioDevamEdenler != null) RadioDevamEdenler.Checked -= FilterRadioButton_Checked;
            if (RadioTamamlandi != null) RadioTamamlandi.Checked -= FilterRadioButton_Checked;
            if (RadioBirakildi != null) RadioBirakildi.Checked -= FilterRadioButton_Checked;
            if (RadioIstekListesi != null) RadioIstekListesi.Checked -= FilterRadioButton_Checked;

            // Tüm RadioButton'ları false yap
            if (RadioTumu != null) RadioTumu.IsChecked = false;
            if (RadioDevamEdenler != null) RadioDevamEdenler.IsChecked = false;
            if (RadioTamamlandi != null) RadioTamamlandi.IsChecked = false;
            if (RadioBirakildi != null) RadioBirakildi.IsChecked = false;
            if (RadioIstekListesi != null) RadioIstekListesi.IsChecked = false;

            // İlgili RadioButton'ı true yap
            switch (_currentFilter)
            {
                case "Tümü": if (RadioTumu != null) RadioTumu.IsChecked = true; break;
                case "Devam Edenler": if (RadioDevamEdenler != null) RadioDevamEdenler.IsChecked = true; break;
                case "Tamamlandı": if (RadioTamamlandi != null) RadioTamamlandi.IsChecked = true; break;
                case "Bırakıldı": if (RadioBirakildi != null) RadioBirakildi.IsChecked = true; break;
                case "İstek Listesi": if (RadioIstekListesi != null) RadioIstekListesi.IsChecked = true; break;
            }

            // Event handler'ları tekrar ekle
            if (RadioTumu != null) RadioTumu.Checked += FilterRadioButton_Checked;
            if (RadioDevamEdenler != null) RadioDevamEdenler.Checked += FilterRadioButton_Checked;
            if (RadioTamamlandi != null) RadioTamamlandi.Checked += FilterRadioButton_Checked;
            if (RadioBirakildi != null) RadioBirakildi.Checked += FilterRadioButton_Checked;
            if (RadioIstekListesi != null) RadioIstekListesi.Checked += FilterRadioButton_Checked;
        }


        // --- YENİ OYUN EKLEME ---
        private async void BtnAddGame_Click(object sender, RoutedEventArgs e)
        {
            // 1. Ekleme penceresini aç
            AddGameWindow addWindow = new AddGameWindow();
            bool? result = addWindow.ShowDialog();

            // 2. Kullanıcı "Kaydet" dediyse
            if (result == true)
            {
                var newGame = addWindow.ResultGame;
                
                // 3. Firebase'e kaydet
                try
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    string saveResult = await _firebaseService.AddGameAsync(newGame);
                    Mouse.OverrideCursor = null;

                    if (saveResult == "OK")
                    {
                        Helpers.ToastNotification.Show($"'{newGame.Title}' başarıyla kütüphaneye eklendi!", Helpers.ToastType.Success);
                        // Listeyi yenile
                        await LoadGamesAsync();
                    }
                    else
                    {
                        DXMessageBox.Show(saveResult, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    Mouse.OverrideCursor = null;
                    DXMessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- OYUN DÜZENLEME ---
        private async void GameCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Seçim modu aktifse, düzenleme penceresini açma
            if (_isSelectionMode)
            {
                return;
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("GameCard tıklandı");
            #endif
            
            // Tıklanan Border'dan Game nesnesini al
            // PreviewMouseDown tunneling event olduğu için Border'dan başlar
            if (sender is System.Windows.Controls.Border border)
            {
                // DataContext'i Border'dan veya parent'tan al
                var gameToEdit = border.DataContext as Game;
                
                // Eğer Border'da DataContext yoksa, parent'tan al
                if (gameToEdit == null)
                {
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(border) as System.Windows.FrameworkElement;
                    while (parent != null && gameToEdit == null)
                    {
                        gameToEdit = parent.DataContext as Game;
                        parent = System.Windows.Media.VisualTreeHelper.GetParent(parent) as System.Windows.FrameworkElement;
                    }
                }
                
                if (gameToEdit != null)
            {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Oyun seçildi: {gameToEdit.Title}");
                    #endif
                // Oyunun bir kopyasını oluştur (binding'in orijinal nesneyi değiştirmemesi için)
                var gameCopy = new Game
                {
                    Id = gameToEdit.Id,
                    Title = gameToEdit.Title,
                    Developer = gameToEdit.Developer,
                    ReleaseDate = gameToEdit.ReleaseDate,
                    Status = gameToEdit.Status,
                    Platform = gameToEdit.Platform,
                    UserRating = gameToEdit.UserRating,
                    PlayTime = gameToEdit.PlayTime,
                    MetacriticScore = gameToEdit.MetacriticScore,
                    CoverImageUrl = gameToEdit.CoverImageUrl,
                    Description = gameToEdit.Description,
                    UserNotes = gameToEdit.UserNotes,
                    AddedDate = gameToEdit.AddedDate,
                    IsFavorite = gameToEdit.IsFavorite,
                    Genres = gameToEdit.Genres,
                    RawgId = gameToEdit.RawgId
                };

                // 1. Düzenleme penceresini, bu oyun verisiyle aç
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("EditGameWindow açılıyor...");
                #endif
                EditGameWindow editWindow = new EditGameWindow(gameCopy);

                // Pencerenin sahibini (Owner) belirtmek iyi olabilir ama UserControl içinde olduğumuz için
                // Window.GetWindow(this) ile ana pencereyi bulabiliriz.
                editWindow.Owner = Window.GetWindow(this);

                bool? result = editWindow.ShowDialog();
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"EditGameWindow kapandı, result: {result}");
                #endif

                // 2. Pencere kapandığında ne oldu?
                if (result == true)
                {
                    if (editWindow.IsDeleted)
                    {
                        // Kullanıcı "Oyunu Sil" butonuna bastı
                        try
                        {
                            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                            string deleteResult = await _firebaseService.DeleteGameAsync(gameCopy.Id);
                            Mouse.OverrideCursor = null;

                            if (deleteResult == "OK")
                            {
                                Helpers.ToastNotification.Show($"{gameCopy.Title} kütüphaneden silindi!", Helpers.ToastType.Success);
                                // Listeyi yenile
                                await LoadGamesAsync();
                            }
                            else
                            {
                                DXMessageBox.Show(deleteResult, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Mouse.OverrideCursor = null;
                            DXMessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        // Kullanıcı "Kaydet" butonuna bastı.
                        // Data Binding sayesinde 'gameCopy' nesnesi zaten güncellendi!
                        try
                        {
                            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                            string updateResult = await _firebaseService.UpdateGameAsync(gameCopy);
                            Mouse.OverrideCursor = null;

                            if (updateResult == "OK")
                            {
                                Helpers.ToastNotification.Show($"{gameCopy.Title} başarıyla güncellendi!", Helpers.ToastType.Success);
                                // Listeyi yenile
                                await LoadGamesAsync();
                            }
                            else
                            {
                                DXMessageBox.Show(updateResult, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Mouse.OverrideCursor = null;
                            DXMessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("GameCard tıklaması: DataContext Game değil");
                    #endif
                }
            }
            else
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("GameCard tıklaması: Sender Border değil");
                #endif
            }
        }

        // --- GÖRSEL YÜKLEME ---
        private async void GameImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Image image)
            {
                // DataContext'i al - GameCardTemplate içinde DataContext Game olmalı
                var game = image.DataContext as Game;
                
                // Eğer Image'de DataContext yoksa, parent'tan al
                if (game == null)
                {
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(image) as System.Windows.FrameworkElement;
                    while (parent != null && game == null)
                    {
                        game = parent.DataContext as Game;
                        parent = System.Windows.Media.VisualTreeHelper.GetParent(parent) as System.Windows.FrameworkElement;
                    }
                }
                
                if (game != null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Görsel yükleme başlatılıyor: {game.Title}, URL: {game.CoverImageUrl}");
                    #endif
                    await LoadGameImageAsync(image, game.CoverImageUrl);
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("GameImage_Loaded: DataContext Game değil!");
                    #endif
                }
            }
        }

        private async System.Threading.Tasks.Task LoadGameImageAsync(System.Windows.Controls.Image imageControl, string imageUrl)
        {
            try
            {
                // ImageCacheService kullanarak async ve önbellekli yükleme
                var fallbackPath = "pack://application:,,,/ZImages/sample_cover.jpg";
                var image = await Services.ImageCacheService.GetImageAsync(imageUrl, fallbackPath);
                
                // UI thread'de resmi set et
                await this.Dispatcher.InvokeAsync(() =>
                {
                    if (image != null)
                    {
                        imageControl.Source = image;
                    }
                });
            }
            catch (System.Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"LoadGameImageAsync hatası: {ex.Message}");
                #endif
            }
        }

        // --- GÖRSEL YÜKLEME HATASI YÖNETİMİ ---
        private void GameImage_ImageFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            // Görsel yüklenemezse varsayılan görsele fallback yap
            if (sender is System.Windows.Controls.Image image)
            {
                try
                {
                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new System.Uri("pack://application:,,,/ZImages/sample_cover.jpg"));
                }
                catch (System.Exception ex)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Görsel fallback hatası: {ex.Message}");
                    #endif
                }
            }
        }
        

        // --- GÖRÜNÜM MODU DEĞİŞTİRME ---
        private void BtnViewGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_viewMode != "Grid")
            {
                _viewMode = "Grid";
                UpdateViewMode();
            }
        }

        private void BtnViewList_Click(object sender, RoutedEventArgs e)
        {
            if (_viewMode != "List")
            {
                _viewMode = "List";
                UpdateViewMode();
            }
        }

        private void UpdateViewMode()
        {
            // Buton Görünümlerini Güncelle
            if (BtnViewGrid != null && BtnViewList != null)
            {
                if (_viewMode == "List")
                {
                    BtnViewGrid.Opacity = 0.5;
                    BtnViewList.Opacity = 1.0;
                }
                else
                {
                    BtnViewGrid.Opacity = 1.0;
                    BtnViewList.Opacity = 0.5;
                }
            }
            
            // Görünümleri değiştir
            if (_viewMode == "List")
            {
                if (GamesGridControl != null) GamesGridControl.Visibility = System.Windows.Visibility.Collapsed;
                if (GamesListControl != null) GamesListControl.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                if (GamesGridControl != null) GamesGridControl.Visibility = System.Windows.Visibility.Visible;
                if (GamesListControl != null) GamesListControl.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }
}