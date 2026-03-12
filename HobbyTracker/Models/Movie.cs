using System;
using System.Collections.Generic;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Film modeli. TMDB API'den gelen veriler ve kullanıcı izleme bilgilerini içerir.
    /// </summary>
    public class Movie
    {
        /// <summary>
        /// Film kimliği.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Film başlığı.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Film özeti.
        /// </summary>
        public string Synopsis { get; set; } = string.Empty;
        
        /// <summary>
        /// Yönetmen adı.
        /// </summary>
        public string Director { get; set; } = string.Empty;
        
        /// <summary>
        /// Çıkış yılı.
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// MPAA yaş sınırı derecelendirmesi.
        /// </summary>
        public string MpaaRating { get; set; } = string.Empty;
        
        /// <summary>
        /// Global puan (TMDB'den).
        /// </summary>
        public double GlobalRating { get; set; }
        
        /// <summary>
        /// Orijinal dil kodu.
        /// </summary>
        public string OriginalLanguage { get; set; } = string.Empty;
        
        /// <summary>
        /// İzlenme tarihi.
        /// </summary>
        public DateTime? WatchedDate { get; set; }
        
        /// <summary>
        /// Koleksiyona eklenme tarihi.
        /// </summary>
        public DateTime AddedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Film türleri listesi.
        /// </summary>
        public List<string> Genres { get; set; } = new List<string>();

        /// <summary>
        /// Poster resmi URL'i.
        /// </summary>
        public string PosterUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Arka plan resmi URL'i.
        /// </summary>
        public string BackdropUrl { get; set; } = string.Empty;

        /// <summary>
        /// Film bütçesi.
        /// </summary>
        public decimal Budget { get; set; }
        
        /// <summary>
        /// Film geliri.
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Oyuncu kadrosu.
        /// </summary>
        public List<CastMember> Cast { get; set; } = new List<CastMember>();

        /// <summary>
        /// Kullanıcının izleme durumu.
        /// </summary>
        public WatchStatus UserStatus { get; set; } = WatchStatus.PlanToWatch;
        
        /// <summary>
        /// Favori olarak işaretlenip işaretlenmediği.
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// Kullanıcının verdiği puan (0-10 arası).
        /// </summary>
        public double? UserRating { get; set; }
        
        /// <summary>
        /// Kullanıcının eklediği kişisel notlar.
        /// </summary>
        public string PersonalNote { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının eklediği özel etiketler.
        /// </summary>
        public List<string> UserTags { get; set; } = new List<string>();

        /// <summary>
        /// Film süresi (dakika).
        /// </summary>
        public int DurationMinutes { get; set; }
        
        /// <summary>
        /// İzlenen süre (dakika).
        /// </summary>
        public int WatchedMinutes { get; set; }

        /// <summary>
        /// Film süresini "2h 49m" formatında döndürür.
        /// </summary>
        public string DurationFormatted
        {
            get
            {
                TimeSpan ts = TimeSpan.FromMinutes(DurationMinutes);
                return $"{ts.Hours}h {ts.Minutes}m";
            }
        }

        /// <summary>
        /// İzleme ilerlemesi yüzdesi (0-100). Progress Bar için kullanılır.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                if (DurationMinutes == 0) return 0;
                return ((double)WatchedMinutes / DurationMinutes) * 100;
            }
        }

        /// <summary>
        /// İzlenen süreyi "2h 15m watched" formatında döndürür.
        /// </summary>
        public string WatchedFormatted
        {
            get
            {
                TimeSpan ts = TimeSpan.FromMinutes(WatchedMinutes);
                return $"{ts.Hours}h {ts.Minutes}m watched";
            }
        }

        /// <summary>
        /// Kalan süreyi "34m left" formatında döndürür.
        /// </summary>
        public string RemainingFormatted
        {
            get
            {
                int remaining = DurationMinutes - WatchedMinutes;
                if (remaining <= 0) return "Completed";

                TimeSpan ts = TimeSpan.FromMinutes(remaining);
                return ts.Hours > 0 ? $"{ts.Hours}h {ts.Minutes}m left" : $"{ts.Minutes}m left";
            }
        }

        /// <summary>
        /// Durum gösterim metni. Kartlarda kullanılır.
        /// </summary>
        public string StatusDisplay
        {
            get
            {
                return UserStatus switch
                {
                    WatchStatus.Completed => "İzlendi",
                    WatchStatus.InProgress => "İzleniyor",
                    WatchStatus.PlanToWatch => "Listem",
                    WatchStatus.Dropped => "Yarım Bırakıldı",
                    _ => "Bilinmiyor"
                };
            }
        }

        /// <summary>
        /// Durum rozeti arka plan rengi.
        /// </summary>
        public System.Windows.Media.SolidColorBrush StatusBadgeBackground
        {
            get
            {
                return UserStatus switch
                {
                    WatchStatus.Completed => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)), // Red
                    WatchStatus.InProgress => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), // Blue
                    WatchStatus.PlanToWatch => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), // Green
                    WatchStatus.Dropped => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), // Gray
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)) // Gray
                };
            }
        }
    }
}