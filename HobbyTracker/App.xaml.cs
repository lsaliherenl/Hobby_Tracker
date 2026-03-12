using System.Windows;
using DevExpress.Xpf.Core;

namespace HobbyTracker
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            ApplicationThemeHelper.ApplicationThemeName = Theme.Win11DarkName;
        }
    }
}