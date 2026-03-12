using DevExpress.Xpf.Core;
using System.Windows;
using System.Windows.Input;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class LoginWindow : ThemedWindow
    {
        private bool isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();

            // --- BENİ HATIRLA ÖZELLİĞİ: BAŞLANGIÇTA YÜKLE ---
            // Eğer daha önce "Beni Hatırla" denmişse, kutuları doldur.
            if (HobbyTracker.Properties.Settings.Default.IsRemembered == true)
            {
                txtUsername.Text = HobbyTracker.Properties.Settings.Default.SavedEmail;
                txtPassword.Password = HobbyTracker.Properties.Settings.Default.SavedPassword;

                // Görünür şifre kutusunu da senkronize et (Göz ikonuna basılırsa boş çıkmasın)
                txtVisiblePassword.Text = HobbyTracker.Properties.Settings.Default.SavedPassword;

                // Kutucuğu işaretli getir
                chkRememberMe.IsChecked = true;
            }
        }

        // Pencere Sürükleme
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtUsername.Text;
            string password = isPasswordVisible ? txtVisiblePassword.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                DXMessageBox.Show("Lütfen e-posta ve şifrenizi girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // FIREBASE GİRİŞ İŞLEMİ 🚀
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                SFirebase service = new SFirebase();
                string result = await service.LoginUserAsync(email, password);

                Mouse.OverrideCursor = null;

                if (result == "OK")
                {
                    // --- BENİ HATIRLA ÖZELLİĞİ: VERİLERİ KAYDET ---
                    if (chkRememberMe.IsChecked == true)
                    {
                        // Kullanıcı beni hatırla dediyse ayarları kaydet
                        HobbyTracker.Properties.Settings.Default.SavedEmail = email;
                        HobbyTracker.Properties.Settings.Default.SavedPassword = password;
                        HobbyTracker.Properties.Settings.Default.IsRemembered = true;
                        HobbyTracker.Properties.Settings.Default.Save(); // Kalıcı yap
                    }
                    else
                    {
                        // İşaretli değilse hafızayı temizle
                        HobbyTracker.Properties.Settings.Default.SavedEmail = "";
                        HobbyTracker.Properties.Settings.Default.SavedPassword = "";
                        HobbyTracker.Properties.Settings.Default.IsRemembered = false;
                        HobbyTracker.Properties.Settings.Default.Save();
                    }

                    // Giriş Başarılı! Ana Ekrana Git
                    MainWindow main = new MainWindow();
                    main.Show();
                    this.Close();
                }
                else
                {
                    // Hata (Örn: "Hatalı şifre" veya "Kullanıcı bulunamadı")
                    DXMessageBox.Show(result, "Giriş Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                Mouse.OverrideCursor = null;
                DXMessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRegister_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

        // Şifremi Unuttum (Geçici Mesaj)
        private void TxtForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            DXMessageBox.Show("Şifre sıfırlama özelliği yakında eklenecek!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // Şifre Göster/Gizle
        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                txtVisiblePassword.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtVisiblePassword.Visibility = Visibility.Visible;
                imgEyeOpen.Visibility = Visibility.Collapsed;
                imgEyeClosed.Visibility = Visibility.Visible;
            }
            else
            {
                txtPassword.Password = txtVisiblePassword.Text;
                txtVisiblePassword.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                imgEyeOpen.Visibility = Visibility.Visible;
                imgEyeClosed.Visibility = Visibility.Collapsed;
            }
        }
    }
}