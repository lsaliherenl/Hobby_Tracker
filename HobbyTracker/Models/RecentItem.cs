using System;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Dashboard için son eklenen ve devam edilen içerikleri temsil eden model.
    /// </summary>
    public class RecentItem
    {
        /// <summary>
        /// İçerik kimliği.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// İçerik başlığı.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Kapak resmi URL'i.
        /// </summary>
        public string CoverUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// İçerik kategorisi (Oyun, Kitap, Film, Dizi).
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Kategori rozeti rengi (hex format: #8b5cf6, #f97316, vb.).
        /// </summary>
        public string CategoryColor { get; set; } = string.Empty;
        
        /// <summary>
        /// İçerik durumu (Oynanıyor, Okunuyor, İzleniyor, vb.).
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Koleksiyona eklenme tarihi.
        /// </summary>
        public DateTime AddedDate { get; set; }
        
        /// <summary>
        /// İlerleme yüzdesi (0-100 arası).
        /// </summary>
        public int Progress { get; set; }
        
        /// <summary>
        /// Oyunlar için oynama süresi (saat).
        /// </summary>
        public double PlayTimeHours { get; set; }
        
        /// <summary>
        /// İlerleme çubuğunun gösterilip gösterilmeyeceği. Oyunlar için false.
        /// </summary>
        public bool ShowProgressBar => Category != "Oyun";
        
        /// <summary>
        /// Oynama süresinin gösterilip gösterilmeyeceği. Sadece oyunlar için true.
        /// </summary>
        public bool ShowPlayTime => Category == "Oyun";
        
        /// <summary>
        /// Oyunlar için oynama süresi metni.
        /// </summary>
        public string PlayTimeText => PlayTimeHours > 0 ? $"🎮 {PlayTimeHours:F1} saat oynandı" : "🎮 Henüz oynanmadı";

        /// <summary>
        /// Eklenme tarihini formatlı gösterir (örn: "15 Oca 2024").
        /// </summary>
        public string AddedDateFormatted => AddedDate.ToString("dd MMM yyyy");
        
        /// <summary>
        /// Kısa tarih formatı (Bugün, Dün, X gün önce, veya tarih).
        /// </summary>
        public string AddedDateShort
        {
            get
            {
                var today = DateTime.Today;
                var diff = (today - AddedDate.Date).Days;

                if (diff == 0) return "Bugün";
                if (diff == 1) return "Dün";
                if (diff < 7) return $"{diff} gün önce";
                return AddedDate.ToString("dd MMM");
            }
        }

        /// <summary>
        /// İlerleme yüzdesi metni (örn: "%75").
        /// </summary>
        public string ProgressText => $"%{Progress}";
    }
}

