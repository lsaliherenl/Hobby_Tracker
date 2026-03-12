namespace HobbyTracker.Models
{
    /// <summary>
    /// Kullanıcının film veya dizi izleme durumu.
    /// </summary>
    public enum WatchStatus
    {
        /// <summary>
        /// İzleme listesinde.
        /// </summary>
        PlanToWatch,
        
        /// <summary>
        /// Şu anda izleniyor.
        /// </summary>
        InProgress,
        
        /// <summary>
        /// Tamamlandı.
        /// </summary>
        Completed,
        
        /// <summary>
        /// Yarım bırakıldı.
        /// </summary>
        Dropped
    }

    /// <summary>
    /// Filmin vizyon durumu.
    /// </summary>
    public enum ReleaseStatus
    {
        /// <summary>
        /// Vizyonda veya çıktı.
        /// </summary>
        Released,
        
        /// <summary>
        /// Gelecek.
        /// </summary>
        Upcoming,
        
        /// <summary>
        /// Yapım aşamasında.
        /// </summary>
        InProduction,
        
        /// <summary>
        /// Söylenti.
        /// </summary>
        Rumored
    }
}