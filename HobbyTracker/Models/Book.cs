using System;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Kitap modeli. Google Books API'den gelen veriler ve kullanıcı ilerleme bilgilerini içerir.
    /// </summary>
    public class Book : HobbyItem
    {
        /// <summary>
        /// Google Books API'den gelen benzersiz kitap kimliği.
        /// </summary>
        public string GoogleBooksId { get; set; } = string.Empty;
        
        /// <summary>
        /// Kitabın yazarları.
        /// </summary>
        public string Authors { get; set; } = string.Empty;
        
        /// <summary>
        /// Yayınevi bilgisi.
        /// </summary>
        public string Publisher { get; set; } = string.Empty;
        
        /// <summary>
        /// Basım yılı.
        /// </summary>
        public string PublishedDate { get; set; } = string.Empty;
        
        /// <summary>
        /// Kitap özeti.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Kitabın ISBN numarası.
        /// </summary>
        public string ISBN { get; set; } = string.Empty;
        
        /// <summary>
        /// Kitabın toplam sayfa sayısı.
        /// </summary>
        public int PageCount { get; set; }

        private int _currentPage;
        
        /// <summary>
        /// Kullanıcının okuduğu sayfa numarası. Değiştiğinde ilerleme bilgilerini günceller.
        /// </summary>
        public int CurrentPage 
        { 
            get => _currentPage; 
            set 
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }
        
        /// <summary>
        /// Kullanıcının verdiği puan (0-10 arası).
        /// </summary>
        public double UserRating { get; set; }
        
        /// <summary>
        /// Okumaya başlama tarihi.
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Okumayı bitirme tarihi.
        /// </summary>
        public DateTime? FinishDate { get; set; }
        
        /// <summary>
        /// Kitabın dili. Varsayılan değer: "Türkçe".
        /// </summary>
        public string Language { get; set; } = "Türkçe";

        /// <summary>
        /// Okuma ilerlemesi yüzdesi (0-100). Progress Bar için kullanılır.
        /// </summary>
        public int ProgressPercentage
        {
            get
            {
                if (Status == "Bitti" || Status == "Okundu") return 100;
                if (PageCount <= 0) return 0;
                if (CurrentPage >= PageCount) return 100;
                return (int)((double)CurrentPage / PageCount * 100);
            }
        }

        /// <summary>
        /// İlerleme metni formatı (örn: "120 / 400").
        /// </summary>
        public string ProgressText => $"{CurrentPage} / {PageCount}";

        /// <summary>
        /// Basım yılı. PublishedDate'den ilk 4 karakteri alır.
        /// </summary>
        public string PublishYear
        {
            get
            {
                if (string.IsNullOrEmpty(PublishedDate)) return "N/A";
                return PublishedDate.Length >= 4 ? PublishedDate.Substring(0, 4) : PublishedDate;
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
                    "Okunuyor" => "#3b82f6",
                    "Okundu" or "Bitti" => "#ef4444",
                    "Okunacak" => "#10b981",
                    "Yarım Bırakıldı" => "#6b7280",
                    _ => "#3b82f6"
                };
            }
        }
        
        /// <summary>
        /// Kart üzerinde gösterilecek durum metni. "Bitti" durumu "Okundu" olarak gösterilir.
        /// </summary>
        public string StatusDisplay
        {
            get
            {
                if (Status == "Bitti") return "Okundu";
                return Status;
            }
        }

        /// <summary>
        /// Durum rozeti arka plan rengi.
        /// </summary>
        public string StatusBadgeBackground
        {
            get => StatusColor;
        }
    }
}