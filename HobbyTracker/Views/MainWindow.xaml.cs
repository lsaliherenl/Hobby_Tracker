using DevExpress.Xpf.Core;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HobbyTracker.Models; // Modeller için
using HobbyTracker.Views;  // Views (EditGameWindow) için

namespace HobbyTracker.Views
{
    public partial class MainWindow : ThemedWindow
    {
        // Sayfa önbelleği - her sayfa bir kez oluşturulur ve yeniden kullanılır
        private readonly Dictionary<string, System.Windows.Controls.UserControl> _pageCache = new();
        
        public MainWindow()
        {
            InitializeComponent();

            // Uygulama açıldığında Dashboard göster
            NavigateTo("Dashboard");

            // Kullanıcı ismini göster
            if (!string.IsNullOrEmpty(Models.UserSession.CurrentUserName))
            {
                TxtCurrentUser.Text = Models.UserSession.CurrentUserName;
            }

            // Başlangıçta Dashboard butonunu aktif (mavi) yap
            // (Eğer tasarımda BtnDashboard tanımlıysa null kontrolü yapalım)
            if (BtnDashboard != null)
                SetActiveMenu(BtnDashboard);
        }

        // --- SAYFA NAVİGASYONU (ÖNBELLEKLİ) ---
        // --- SAYFA NAVİGASYONU (ÖNBELLEKLİ) ---
        private void NavigateTo(string pageName)
        {
            // Önbellekte yoksa oluştur
            if (!_pageCache.ContainsKey(pageName))
            {
                _pageCache[pageName] = pageName switch
                {
                    "Dashboard" => new DashboardView(),
                    "Games" => new GamesView(),
                    "Books" => new BooksView(),
                    "Movies" => new MoviesView(), 
                    "Series" => new SeriesView(), 
                    _ => null
                };
            }

            // Önbellekten al ve göster
            var page = _pageCache[pageName];
            if (page != null)
            {
                MainContent.Content = page;
            }
        }

        // --- MENÜ RENK YÖNETİMİ ---
        private void SetActiveMenu(SimpleButton activeButton)
        {
            if (activeButton == null) return;

            // Renk dönüştürücüleri ve fırçalar (System.Windows.Media kullanarak)
            var converter = new System.Windows.Media.BrushConverter();
            var defaultForeground = (System.Windows.Media.Brush)converter.ConvertFrom("#cbd5e1"); // Soluk gri
            var transparentBrush = System.Windows.Media.Brushes.Transparent;

            // 1. Tüm butonları varsayılan hale getir (Şeffaf arka plan, gri yazı)
            // (Eğer butonlar null değilse işlem yap)
            if (BtnDashboard != null) { BtnDashboard.Background = transparentBrush; BtnDashboard.Foreground = defaultForeground; }
            if (BtnGames != null) { BtnGames.Background = transparentBrush; BtnGames.Foreground = defaultForeground; }
            if (BtnBooks != null) { BtnBooks.Background = transparentBrush; BtnBooks.Foreground = defaultForeground; }
            if (BtnMovies != null) { BtnMovies.Background = transparentBrush; BtnMovies.Foreground = defaultForeground; }
            if (BtnSeries != null) { BtnSeries.Background = transparentBrush; BtnSeries.Foreground = defaultForeground; }

            // 2. Tıklanan butonu aktif (Mavi arka plan, Beyaz yazı) yap
            var activeBackground = (System.Windows.Media.Brush)converter.ConvertFrom("#3b82f6");
            activeButton.Background = activeBackground;
            activeButton.Foreground = System.Windows.Media.Brushes.White;
        }

        // --- MENÜ BUTON TIKLAMALARI ---
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("Dashboard");
            SetActiveMenu(BtnDashboard);
        }

        private void BtnGames_Click(object sender, RoutedEventArgs e)
        {
            SwitchToGames();
        }

        public void SwitchToGames()
        {
            NavigateTo("Games");
            SetActiveMenu(BtnGames);
        }

        private void BtnBooks_Click(object sender, RoutedEventArgs e)
        {
            SwitchToBooks();
        }

        public void SwitchToBooks()
        {
            NavigateTo("Books");
            SetActiveMenu(BtnBooks);
        }

        private void BtnMovies_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("Movies");
            SetActiveMenu(BtnMovies);
        }

        private void BtnSeries_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("Series");
            SetActiveMenu(BtnSeries);
        }

        // --- PUBLIC NAVİGASYON (Alt sayfalardan erişim için) ---
        public void NavigateToDashboardAndSelectTab(string tabName)
        {
            NavigateTo("Dashboard");
            SetActiveMenu(BtnDashboard);

            if (_pageCache.ContainsKey("Dashboard") && _pageCache["Dashboard"] is DashboardView dashboardView)
            {
                dashboardView.SelectTab(tabName);
            }
        }

        public void NavigateToMovieDetail(Movie movie)
        {
            var editPage = new EditMovieWindow();
            editPage.LoadMovie(movie);
            editPage.MovieUpdated += (updatedMovie) =>
            {
                // Film güncellendiğinde MoviesView'ı yenile
                if (_pageCache.ContainsKey("Movies") && _pageCache["Movies"] is MoviesView moviesView)
                {
                    // MoviesView'a geri dön ve listeyi güncelle
                }
            };
            MainContent.Content = editPage;
        }

        public void NavigateBackToMovies()
        {
            NavigateTo("Movies");
            SetActiveMenu(BtnMovies);
        }

        public void NavigateToSeriesDetail(Series series)
        {
            var editPage = new EditSeriesWindow();
            editPage.LoadSeries(series);
            editPage.SeriesUpdated += (updatedSeries) =>
            {
                // Dizi güncellendiğinde SeriesView'ı yenile
                if (_pageCache.ContainsKey("Series") && _pageCache["Series"] is SeriesView seriesView)
                {
                    seriesView.RefreshData();
                }
            };
            MainContent.Content = editPage;
        }

        public void NavigateBackToSeries()
        {
            NavigateTo("Series");
            SetActiveMenu(BtnSeries);
        }

        // --- PENCERE YÖNETİMİ (Kapat, Küçült, Büyüt, Sürükle) ---

        // Pencereyi sürüklemek için
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Kapatma Butonu
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // Küçültme (Minimize) Butonu
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Büyütme/Eski Haline Getirme (Maximize/Restore) Butonu
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal; // Pencereyi eski boyutuna döndür
            }
            else
            {
                this.WindowState = WindowState.Maximized; // Tam ekran yap
            }
        }
    }
}