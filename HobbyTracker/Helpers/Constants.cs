namespace HobbyTracker.Helpers
{
    /// <summary>
    /// Uygulama genelinde kullanılan sabit değerler.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Oyun başlığı gerekli mesajı.
        /// </summary>
        public const string MSG_GAME_TITLE_REQUIRED = "• Lütfen bir oyun ismi girin.";
        
        /// <summary>
        /// Durum seçimi gerekli mesajı.
        /// </summary>
        public const string MSG_STATUS_REQUIRED = "• Lütfen bir durum seçin.";
        
        /// <summary>
        /// Platform seçimi gerekli mesajı.
        /// </summary>
        public const string MSG_PLATFORM_REQUIRED = "• Lütfen bir platform seçin.";
        
        /// <summary>
        /// Arama sonucu bulunamadı mesajı.
        /// </summary>
        public const string MSG_SEARCH_NO_RESULTS = "Arama sonucu bulunamadı. Lütfen farklı bir oyun adı deneyin.";
        
        /// <summary>
        /// Arama hatası mesajı formatı.
        /// </summary>
        public const string MSG_SEARCH_ERROR = "Arama sırasında bir hata oluştu: {0}";
        
        /// <summary>
        /// Oyun detayları yükleme hatası mesajı formatı.
        /// </summary>
        public const string MSG_GAME_DETAILS_ERROR = "Oyun detayları yüklenirken bir hata oluştu: {0}";
        
        /// <summary>
        /// Kaydetme hatası mesajı formatı.
        /// </summary>
        public const string MSG_SAVE_ERROR = "Oyun kaydedilirken bir hata oluştu: {0}";
        
        /// <summary>
        /// Eksik bilgi başlığı.
        /// </summary>
        public const string TITLE_MISSING_INFO = "Eksik Bilgi";
        
        /// <summary>
        /// Sonuç bulunamadı başlığı.
        /// </summary>
        public const string TITLE_NO_RESULTS = "Sonuç Bulunamadı";
        
        /// <summary>
        /// Hata başlığı.
        /// </summary>
        public const string TITLE_ERROR = "Hata";
        
        /// <summary>
        /// Varsayılan oyun başlığı.
        /// </summary>
        public const string DEFAULT_GAME_TITLE = "Oyun Başlığı";
        
        /// <summary>
        /// Varsayılan geliştirici adı.
        /// </summary>
        public const string DEFAULT_DEVELOPER = "Geliştirici";
        
        /// <summary>
        /// Varsayılan yıl metni.
        /// </summary>
        public const string DEFAULT_YEAR = "Yıl";
        
        /// <summary>
        /// Varsayılan puan değeri.
        /// </summary>
        public const string DEFAULT_RATING = "0";
        
        /// <summary>
        /// Varsayılan açıklama metni.
        /// </summary>
        public const string DEFAULT_DESCRIPTION = "Oyun açıklaması burada görünecek...";
        
        /// <summary>
        /// Varsayılan kapak resmi yolu.
        /// </summary>
        public const string DEFAULT_COVER_IMAGE = "pack://application:,,,/ZImages/defBook.jpg";
        
        /// <summary>
        /// Arama için minimum karakter uzunluğu.
        /// </summary>
        public const int SEARCH_MIN_LENGTH = 3;
        
        /// <summary>
        /// Arama debounce süresi (milisaniye).
        /// </summary>
        public const int SEARCH_DEBOUNCE_MS = 500;
        
        /// <summary>
        /// Resim yükleme zaman aşımı (milisaniye).
        /// </summary>
        public const int IMAGE_LOAD_TIMEOUT_MS = 10000;
    }
}

