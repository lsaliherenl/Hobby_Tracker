using System;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Oyun modeli. RAWG API'den gelen veriler ve kullanıcı oynama bilgilerini içerir.
    /// </summary>
    public class Game : HobbyItem
    {
        /// <summary>
        /// RAWG API'den gelen benzersiz oyun kimliği.
        /// </summary>
        public int RawgId { get; set; }
        
        /// <summary>
        /// Oyunu geliştiren firma.
        /// </summary>
        public string Developer { get; set; } = string.Empty;
        
        /// <summary>
        /// Çıkış tarihi (format: "2022-02-25").
        /// </summary>
        public string ReleaseDate { get; set; } = string.Empty;
        
        /// <summary>
        /// Metacritic puanı (0-100 arası).
        /// </summary>
        public double MetacriticScore { get; set; }
        
        /// <summary>
        /// Oyun açıklaması.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Oyunun platformu (PC, PS5, Xbox, vb.).
        /// </summary>
        public string Platform { get; set; } = string.Empty;
        
        /// <summary>
        /// Kullanıcının oynadığı toplam saat.
        /// </summary>
        public int PlayTime { get; set; }

        private string _genres = string.Empty;
        
        /// <summary>
        /// Oyun türleri (virgülle ayrılmış).
        /// </summary>
        public string Genres
        {
            get => _genres;
            set
            {
                _genres = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Kullanıcının verdiği puan (0-10 arası).
        /// </summary>
        public double UserRating { get; set; }

        /// <summary>
        /// Çıkış yılı. ReleaseDate'den ilk 4 karakteri alır.
        /// </summary>
        public string ReleaseYear
        {
            get
            {
                if (string.IsNullOrEmpty(ReleaseDate)) return "N/A";
                return ReleaseDate.Length >= 4 ? ReleaseDate.Substring(0, 4) : ReleaseDate;
            }
        }

        /// <summary>
        /// Platforma göre ikon yolu döndürür.
        /// </summary>
        public string PlatformIcon
        {
            get
            {
                if (string.IsNullOrEmpty(Platform)) return "/ZImages/controller.svg";
                string p = Platform.ToLower();
                if (p.Contains("pc") || p.Contains("windows")) return "/ZImages/laptop.svg";
                if (p.Contains("playstation") || p.Contains("ps")) return "/ZImages/playstation.svg";
                if (p.Contains("xbox")) return "/ZImages/xbox.svg";
                if (p.Contains("nintendo") || p.Contains("switch")) return "/ZImages/switch.svg";
                return "/ZImages/controller.svg";
            }
        }

        /// <summary>
        /// Duruma göre renk kodu döndürür. UI rozet renkleri için kullanılır.
        /// </summary>
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Oynuyor" => "#2b6cee",
                    "Tamamlandı" => "#10b981",
                    "Bırakıldı" => "#ef4444",
                    "İstek Listesi" => "#64748b",
                    _ => "#2b6cee"
                };
            }
        }
    }
}