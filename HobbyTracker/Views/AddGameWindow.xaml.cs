using DevExpress.Xpf.Core;
using HobbyTracker.Models;
using HobbyTracker.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace HobbyTracker.Views
{
    public partial class AddGameWindow : Window
    {
        // Yeni eklenen oyunu dışarıya aktarmak için Property
        public Game? ResultGame { get; private set; }

        private readonly RAWGService _rawgService;
        private List<RAWGGameResult> _searchResults = new List<RAWGGameResult>();
        private RAWGGameDetails? _selectedGameDetails;

        private RAWGGameResult? _selectedSearchResult;
        private double _userRating = 0;

        public AddGameWindow()
        {
            InitializeComponent();

            _rawgService = new RAWGService();

            // ComboBox'ların varsayılan seçimi
            if (CmbStatus != null) CmbStatus.SelectedIndex = 0;
            if (CmbPlatform != null) CmbPlatform.SelectedIndex = 0;

            // Başlangıçta EmptyState göster
            ShowEmptyState();
            
            // Yıldızları oluştur
            InitializeStars();
        }

        #region Panel Visibility Control

        private void ShowEmptyState()
        {
            if (EmptyStatePanel != null) EmptyStatePanel.Visibility = Visibility.Visible;
            if (SearchResultsPanel != null) SearchResultsPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowSearchResultsPanel()
        {
            if (EmptyStatePanel != null) EmptyStatePanel.Visibility = Visibility.Collapsed;
            if (SearchResultsPanel != null) SearchResultsPanel.Visibility = Visibility.Visible;
            if (DetailPanel != null) DetailPanel.Visibility = Visibility.Hidden;
            if (EmptyDetailPanel != null) EmptyDetailPanel.Visibility = Visibility.Visible;
        }

        private void ShowDetailPanel()
        {
            if (DetailPanel != null) DetailPanel.Visibility = Visibility.Visible;
            if (EmptyDetailPanel != null) EmptyDetailPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Search Events

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
            string searchText = TxtSearch?.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
            {
                DXMessageBox.Show("Lütfen en az 2 karakter girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Loading göster
                if (LoadingBar != null) LoadingBar.Visibility = Visibility.Visible;
                ShowSearchResultsPanel();

                // RAWG API'den ara
                _searchResults = await _rawgService.SearchGamesAsync(searchText);

                // Loading gizle
                if (LoadingBar != null) LoadingBar.Visibility = Visibility.Collapsed;

                if (_searchResults.Count > 0)
                {
                    // ListBox'a sonuçları bağla
                    if (LstGames != null)
                    {
                        LstGames.ItemsSource = _searchResults;
                    }
                }
                else
                {
                    DXMessageBox.Show("Arama sonucu bulunamadı. Lütfen farklı bir oyun adı deneyin.", "Sonuç Bulunamadı", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowEmptyState();
                }
            }
            catch (System.Exception ex)
            {
                if (LoadingBar != null) LoadingBar.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Arama hatası: {ex.Message}");
                DXMessageBox.Show($"Arama sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ListBox Selection

        private async void LstGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstGames?.SelectedItem is RAWGGameResult selectedGame)
            {
                _selectedSearchResult = selectedGame;
                
                // Loading göster
                if (LoadingBar != null) LoadingBar.Visibility = Visibility.Visible;

                try
                {
                    // Detayları yükle
                    _selectedGameDetails = await _rawgService.GetGameDetailsAsync(selectedGame.Id);

                    if (LoadingBar != null) LoadingBar.Visibility = Visibility.Collapsed;

                    if (_selectedGameDetails != null)
                    {
                        PopulateDetailPanel(_selectedGameDetails, selectedGame);
                    }
                    else
                    {
                        PopulateDetailPanelFromSearchResult(selectedGame);
                    }

                    ShowDetailPanel();
                }
                catch (System.Exception ex)
                {
                    if (LoadingBar != null) LoadingBar.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine($"Detay yükleme hatası: {ex.Message}");
                    
                    // Arama sonucundan bilgileri kullan
                    PopulateDetailPanelFromSearchResult(selectedGame);
                    ShowDetailPanel();
                }
            }
        }

        #endregion

        #region Populate Detail Panel

        private async void PopulateDetailPanel(RAWGGameDetails details, RAWGGameResult searchResult)
        {
            try
            {
                var game = _rawgService.ConvertToGame(details);
                if (game == null)
                {
                    PopulateDetailPanelFromSearchResult(searchResult);
                    return;
                }

                // Başlık
                if (TxtTitle != null) TxtTitle.Text = !string.IsNullOrWhiteSpace(game.Title) ? game.Title : "Oyun Seçin";

                // Türler (3 tane göster)
                if (details.Genres != null && details.Genres.Count > 0)
                {
                    var genres = details.Genres.Take(3).ToList();
                    if (TxtGenre1 != null) TxtGenre1.Text = genres.Count > 0 ? genres[0].Name : "";
                    if (TxtGenre2 != null) TxtGenre2.Text = genres.Count > 1 ? genres[1].Name : "";
                    if (TxtGenre3 != null) TxtGenre3.Text = genres.Count > 2 ? genres[2].Name : "";
                    
                    // Görünürlük ayarla
                    if (TxtGenre2 != null) ((System.Windows.FrameworkElement)TxtGenre2.Parent).Visibility = genres.Count > 1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    if (TxtGenre3 != null) ((System.Windows.FrameworkElement)TxtGenre3.Parent).Visibility = genres.Count > 2 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
                else
                {
                    if (TxtGenre1 != null) TxtGenre1.Text = "Oyun";
                    if (TxtGenre2 != null) ((System.Windows.FrameworkElement)TxtGenre2.Parent).Visibility = System.Windows.Visibility.Collapsed;
                    if (TxtGenre3 != null) ((System.Windows.FrameworkElement)TxtGenre3.Parent).Visibility = System.Windows.Visibility.Collapsed;
                }

                // Açıklama
                if (TxtDescription != null)
                {
                    string desc = !string.IsNullOrWhiteSpace(game.Description) ? game.Description : "Açıklama bulunamadı.";
                    // HTML temizle
                    desc = System.Text.RegularExpressions.Regex.Replace(desc, "<.*?>", string.Empty);
                    if (desc.Length > 200) desc = desc.Substring(0, 200) + "...";
                    TxtDescription.Text = desc;
                }

                // Poster resmi
                if (ImgPoster != null && !string.IsNullOrEmpty(game.CoverImageUrl))
                {
                    await LoadImageBrushAsync(ImgPoster, game.CoverImageUrl);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PopulateDetailPanel hatası: {ex.Message}");
                PopulateDetailPanelFromSearchResult(searchResult);
            }
        }

        private async void PopulateDetailPanelFromSearchResult(RAWGGameResult result)
        {
            if (result == null) return;

            // Başlık
            if (TxtTitle != null) TxtTitle.Text = !string.IsNullOrWhiteSpace(result.Name) ? result.Name : "Oyun Seçin";

            // Türler (arama sonucunda tür bilgisi varsa kullan)
            if (result.Genres != null && result.Genres.Count > 0)
            {
                var genres = result.Genres.Take(3).ToList();
                if (TxtGenre1 != null) TxtGenre1.Text = genres.Count > 0 ? genres[0].Name : "";
                if (TxtGenre2 != null) TxtGenre2.Text = genres.Count > 1 ? genres[1].Name : "";
                if (TxtGenre3 != null) TxtGenre3.Text = genres.Count > 2 ? genres[2].Name : "";
                
                // Görünürlük ayarla
                if (TxtGenre2 != null) ((System.Windows.FrameworkElement)TxtGenre2.Parent).Visibility = genres.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                if (TxtGenre3 != null) ((System.Windows.FrameworkElement)TxtGenre3.Parent).Visibility = genres.Count > 2 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                if (TxtGenre1 != null) TxtGenre1.Text = "Oyun";
                if (TxtGenre2 != null) ((System.Windows.FrameworkElement)TxtGenre2.Parent).Visibility = System.Windows.Visibility.Collapsed;
                if (TxtGenre3 != null) ((System.Windows.FrameworkElement)TxtGenre3.Parent).Visibility = System.Windows.Visibility.Collapsed;
            }

            // Açıklama
            if (TxtDescription != null)
            {
                TxtDescription.Text = "Detaylı açıklama yüklenemedi.";
            }

            // Poster resmi
            if (ImgPoster != null && !string.IsNullOrEmpty(result.BackgroundImage))
            {
                await LoadImageBrushAsync(ImgPoster, result.BackgroundImage);
            }
        }

        #endregion

        #region Save

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var validationErrors = ValidateForm();
            if (validationErrors.Count > 0)
            {
                string errorMessage = string.Join("\n", validationErrors);
                DXMessageBox.Show(errorMessage, "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveButton = sender as System.Windows.Controls.Button;
            if (saveButton != null) saveButton.IsEnabled = false;

            try
            {
                // Rating değeri
                double rating = _userRating;

                var genresList = _selectedGameDetails?.Genres?.Select(g => g.Name).ToList();
                
                // Eğer detaydan gelen genre yoksa, arama sonucundan al
                if ((genresList == null || genresList.Count == 0) && _selectedSearchResult?.Genres != null)
                {
                    genresList = _selectedSearchResult.Genres.Select(g => g.Name).ToList();
                }
                
                // Liste null ise boş liste oluştur
                genresList = genresList ?? new List<string>();
                
                var genresString = string.Join(", ", genresList);
                
                // Debug: Genres değerini kontrol et
                System.Diagnostics.Debug.WriteLine($"AddGameWindow - Genres kaydediliyor: '{genresString}' (Count: {genresList.Count})");
                
                ResultGame = new Game
                {
                    Title = TxtTitle?.Text ?? "",
                    Developer = _selectedGameDetails?.Developers?.FirstOrDefault()?.Name ?? "",
                    ReleaseDate = _selectedGameDetails?.Released?.Substring(0, 4) ?? _selectedSearchResult?.Released?.Substring(0, 4) ?? "",
                    Status = CmbStatus?.EditValue?.ToString() ?? CmbStatus?.Text ?? "Oynuyor",
                    Platform = CmbPlatform?.EditValue?.ToString() ?? CmbPlatform?.Text ?? "",
                    MetacriticScore = _selectedGameDetails?.Metacritic ?? 0,
                    UserRating = rating,
                    CoverImageUrl = _selectedGameDetails?.BackgroundImage ?? _selectedSearchResult?.BackgroundImage ?? "",
                    Description = _selectedGameDetails?.DescriptionRaw ?? "",
                    RawgId = _selectedGameDetails?.Id ?? _selectedSearchResult?.Id ?? 0,
                    AddedDate = System.DateTime.Now,
                    Genres = genresString
                };
                
                System.Diagnostics.Debug.WriteLine($"AddGameWindow - ResultGame.Genres: '{ResultGame.Genres}'");

                await System.Threading.Tasks.Task.Delay(300);

                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                DXMessageBox.Show($"Oyun kaydedilirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (saveButton != null) saveButton.IsEnabled = true;
            }
        }

        private List<string> ValidateForm()
        {
            var errors = new List<string>();

            if (_selectedSearchResult == null && _selectedGameDetails == null)
            {
                errors.Add("• Lütfen bir oyun seçin.");
            }

            if (CmbStatus?.SelectedItem == null)
            {
                errors.Add("• Lütfen bir durum seçin.");
            }

            if (CmbPlatform?.SelectedItem == null)
            {
                errors.Add("• Lütfen bir platform seçin.");
            }

            return errors;
        }

        #endregion

        #region Window Events

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Arka plana tıklayınca kapat - isteğe bağlı
        }

        #endregion

        #region Image Loading

        private async System.Threading.Tasks.Task LoadImageBrushAsync(System.Windows.Media.ImageBrush imageBrush, string imageUrl)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new System.Uri(imageUrl, System.UriKind.Absolute);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        if (imageBrush != null)
                        {
                            imageBrush.ImageSource = bitmap;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ImageBrush resim oluşturma hatası: {ex.Message}");
                    }
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageBrush resim yükleme hatası: {ex.Message}");
            }
        }

        #endregion

        #region Star Rating Logic

        private void InitializeStars()
        {
            if (StarContainer == null) return;
            
            StarContainer.Children.Clear();
            for (int i = 1; i <= 10; i++)
            {
                var star = new TextBlock
                {
                    Text = "★",
                    FontSize = 22,
                    Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748b")), // #64748b
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
                if (StarPopup != null) StarPopup.IsOpen = false;
                if (TxtStarRating != null) TxtStarRating.Text = $"{_userRating}/10";
            }
        }

        private void BtnStarSelect_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (StarPopup != null)
            {
                // Popup'ı ortala (Yaklaşık genişlik 310px varsayıldı)
                if (BtnStarSelect != null)
                {
                    double popupWidth = 310;
                    StarPopup.HorizontalOffset = (BtnStarSelect.ActualWidth - popupWidth) / 2;
                }
                StarPopup.IsOpen = !StarPopup.IsOpen;
            }
        }

        private void HighlightStars(int upToIndex)
        {
            if (StarContainer == null) return;
            
            foreach (var child in StarContainer.Children)
            {
                if (child is TextBlock star && star.Tag is int idx)
                {
                    star.Foreground = idx <= upToIndex 
                        ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fbbf24")) // #fbbf24 (gold)
                        : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748b")); // #64748b (gray)
                }
            }
        }

        private void UpdateStarDisplay()
        {
            HighlightStars((int)_userRating);
        }

        #endregion
    }
}