using HobbyTracker.Models;
using HobbyTracker.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HobbyTracker.ViewModels
{
    /// <summary>
    /// Dashboard görünümü için view model. İstatistikler, son eklenenler ve grafik verilerini yönetir.
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly SFirebase _firebaseService;

        /// <summary>
        /// Özellik değişikliği olayı.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// Özellik değişikliğini bildirir.
        /// </summary>
        /// <param name="propertyName">Değişen özelliğin adı.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private UserStats _stats;
        
        /// <summary>
        /// Kullanıcı istatistikleri.
        /// </summary>
        public UserStats Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        private ObservableCollection<RecentItem> _recentItems;
        
        /// <summary>
        /// Son eklenen içerikler listesi.
        /// </summary>
        public ObservableCollection<RecentItem> RecentItems
        {
            get => _recentItems;
            set { _recentItems = value; OnPropertyChanged(); }
        }

        private ObservableCollection<RecentItem> _continueItems;
        
        /// <summary>
        /// Devam edilen içerikler listesi.
        /// </summary>
        public ObservableCollection<RecentItem> ContinueItems
        {
            get => _continueItems;
            set { _continueItems = value; OnPropertyChanged(); }
        }

        private List<int> _chartValues;
        
        /// <summary>
        /// Haftalık aktivite grafiği için bar yükseklikleri.
        /// </summary>
        public List<int> ChartValues
        {
            get => _chartValues;
            set { _chartValues = value; OnPropertyChanged(); }
        }

        private string _selectedTab = "GenelBakis";
        
        /// <summary>
        /// Seçili sekme adı.
        /// </summary>
        public string SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// DashboardViewModel'i başlatır ve varsayılan değerleri ayarlar.
        /// </summary>
        public DashboardViewModel()
        {
            _firebaseService = new SFirebase();
            Stats = new UserStats();
            RecentItems = new ObservableCollection<RecentItem>();
            ContinueItems = new ObservableCollection<RecentItem>();
            ChartValues = new List<int> { 0, 0, 0, 0, 0, 0, 0 };
        }

        /// <summary>
        /// İstatistikleri, son eklenenleri ve devam edilenleri yükler.
        /// </summary>
        public async Task LoadStatsAsync()
        {
            Stats = await _firebaseService.GetUserStatsAsync();

            var recentItems = await _firebaseService.GetRecentItemsAsync(5);
            RecentItems = new ObservableCollection<RecentItem>(recentItems);

            var continueItems = await _firebaseService.GetContinueItemsAsync(4);
            ContinueItems = new ObservableCollection<RecentItem>(continueItems);

            UpdateChartData();
        }

        /// <summary>
        /// Haftalık aktivite verilerinden grafik değerlerini günceller.
        /// </summary>
        private void UpdateChartData()
        {
            if (Stats?.WeeklyActivity == null || Stats.WeeklyActivity.Count == 0)
                return;

            var values = new List<int>();
            foreach (var activity in Stats.WeeklyActivity)
            {
                values.Add(activity.ActivityCount);
            }
            ChartValues = values;
        }
    }
}
