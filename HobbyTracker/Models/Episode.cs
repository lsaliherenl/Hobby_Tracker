using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Dizi bölümü modeli. TMDB API'den gelen bölüm bilgilerini içerir.
    /// </summary>
    public class Episode : INotifyPropertyChanged
    {
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
        
        /// <summary>
        /// Sezon numarası.
        /// </summary>
        public int SeasonNumber { get; set; }
        
        /// <summary>
        /// Bölüm numarası.
        /// </summary>
        public int EpisodeNumber { get; set; }
        
        /// <summary>
        /// Bölüm adı.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Bölüm özeti.
        /// </summary>
        public string Overview { get; set; } = string.Empty;
        
        /// <summary>
        /// Yayın tarihi.
        /// </summary>
        public string AirDate { get; set; } = string.Empty;
        
        /// <summary>
        /// Bölüm süresi (dakika).
        /// </summary>
        public int Runtime { get; set; }
        
        /// <summary>
        /// Bölüm küçük resmi URL'i.
        /// </summary>
        public string StillPath { get; set; } = string.Empty;
        
        private bool _isWatched;
        
        /// <summary>
        /// Bölümün izlenip izlenmediği. Değiştiğinde PropertyChanged event'ini tetikler.
        /// </summary>
        public bool IsWatched
        {
            get => _isWatched;
            set
            {
                if (_isWatched != value)
                {
                    _isWatched = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// Bölüm kodu formatı (örn: "S01E05").
        /// </summary>
        public string EpisodeCode => $"S{SeasonNumber:D2}E{EpisodeNumber:D2}";
        
        /// <summary>
        /// Bölüm süresini metin formatında döndürür (örn: "45m").
        /// </summary>
        public string RuntimeText => Runtime > 0 ? $"{Runtime}m" : "";
    }
}
