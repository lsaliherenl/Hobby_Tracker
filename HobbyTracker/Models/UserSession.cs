namespace HobbyTracker.Models
{
    /// <summary>
    /// Kullanıcı oturum bilgilerini tutan static sınıf. Uygulama açık kaldığı sürece veriyi saklar.
    /// </summary>
    public static class UserSession
    {
        /// <summary>
        /// Mevcut kullanıcının benzersiz kimliği (Firebase UID).
        /// </summary>
        public static string? CurrentUserId { get; set; }

        /// <summary>
        /// Mevcut kullanıcının adı soyadı. UI'da "Hoşgeldin [Ad]" mesajı için kullanılır.
        /// </summary>
        public static string? CurrentUserName { get; set; }

        /// <summary>
        /// Mevcut kullanıcının e-posta adresi.
        /// </summary>
        public static string? CurrentUserEmail { get; set; }

        /// <summary>
        /// Firebase kimlik doğrulama token'ı (ID Token). Veritabanı erişimi için gereklidir.
        /// </summary>
        public static string? UserToken { get; set; }

        /// <summary>
        /// Kullanıcı çıkış yaptığında oturum verilerini temizler.
        /// </summary>
        public static void ClearSession()
        {
            CurrentUserId = null;
            CurrentUserName = null;
            CurrentUserEmail = null;
            UserToken = null;
        }
    }
}