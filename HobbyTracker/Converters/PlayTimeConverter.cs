using System;
using System.Globalization;
using System.Windows.Data;

namespace HobbyTracker.Converters
{
    /// <summary>
    /// Oynama süresini string'e dönüştüren converter. 0 değeri için "?" gösterir.
    /// </summary>
    public class PlayTimeConverter : IValueConverter
    {
        /// <summary>
        /// Oynama süresini string formatına dönüştürür.
        /// </summary>
        /// <param name="value">Oynama süresi (int veya double).</param>
        /// <param name="targetType">Hedef tip.</param>
        /// <param name="parameter">Parametre (kullanılmıyor).</param>
        /// <param name="culture">Kültür bilgisi.</param>
        /// <returns>Oynama süresi string'i. 0 ise "?" döner.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int playTime)
            {
                return playTime == 0 ? "?" : playTime.ToString();
            }
            
            if (value is double playTimeDouble)
            {
                var playTimeInt = (int)playTimeDouble;
                return playTimeInt == 0 ? "?" : playTimeInt.ToString();
            }
            
            return "?";
        }
        
        /// <summary>
        /// Geri dönüşüm desteklenmiyor.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

