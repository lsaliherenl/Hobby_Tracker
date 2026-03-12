using System;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Tüm hobi öğeleri için temel sınıf. Oyun, Kitap, Film ve Dizi sınıfları bu sınıftan türer.
    /// </summary>
    public abstract class HobbyItem : System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        /// Özellik değişikliği olayı.
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// Özellik değişikliğini bildirir.
        /// </summary>
        /// <param name="propertyName">Değişen özelliğin adı. Otomatik olarak çağıran metodun adından alınır.</param>
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Firebase veritabanındaki benzersiz kimlik.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Öğenin başlığı veya adı.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Öğenin durumu (Oynuyor, Okunuyor, İzlendi, vb.).
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Kullanıcının verdiği puan (1-10 arası).
        /// </summary>
        public int Score { get; set; }
        
        /// <summary>
        /// Kapak resminin URL'i.
        /// </summary>
        public string CoverImageUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Kullanıcının eklediği notlar.
        /// </summary>
        public string UserNotes { get; set; } = string.Empty;
        
        /// <summary>
        /// Öğenin koleksiyona eklenme tarihi.
        /// </summary>
        public DateTime AddedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Öğenin favori olarak işaretlenip işaretlenmediği.
        /// </summary>
        public bool IsFavorite { get; set; }
    }
}