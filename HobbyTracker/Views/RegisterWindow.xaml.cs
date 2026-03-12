using DevExpress.Xpf.Core;
using System.Windows;
using System.Windows.Input;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class RegisterWindow : ThemedWindow
    {
        private bool isPasswordVisible = false;
        private bool isConfirmVisible = false;

        public RegisterWindow()
        {
            InitializeComponent();
        }

        // Pencere Sürükleme
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNameSurname.Text;
            string email = txtUsername.Text;
            // Şifreyi, hangi kutu görünürse oradan al
            string pass1 = isPasswordVisible ? txtVisiblePassword.Text : txtPassword.Password;
            string pass2 = isConfirmVisible ? txtVisibleConfirm.Text : txtConfirm.Password;

            // 1. Boş Alan Kontrolü
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass1))
            {
                DXMessageBox.Show("Lütfen tüm alanları doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Şifre Kontrolleri
            if (pass1 != pass2)
            {
                DXMessageBox.Show("Şifreler uyuşmuyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (pass1.Length < 6)
            {
                DXMessageBox.Show("Şifre en az 6 karakter olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. FIREBASE KAYIT İŞLEMİ BAŞLIYOR 🚀
            try
            {
                // Kullanıcıya işlemin başladığını hissettirmek için imleci bekletme moduna alalım
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                SFirebase service = new SFirebase();
                string result = await service.RegisterUserAsync(name, email, pass1);

                Mouse.OverrideCursor = null; // İmleci normale döndür

                if (result == "OK")
                {
                    DXMessageBox.Show("Kayıt başarılı! Hoşgeldiniz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    MainWindow main = new MainWindow();
                    main.Show();
                    this.Close();
                }
                else
                {
                    // Hata mesajını göster (Örn: "Bu e-posta zaten kullanılıyor")
                    DXMessageBox.Show(result, "Kayıt Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                Mouse.OverrideCursor = null;
                DXMessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtLogin_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // Şifre 1 Gizle/Göster
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

        // Şifre 2 (Tekrar) Gizle/Göster
        private void BtnToggleConfirm_Click(object sender, RoutedEventArgs e)
        {
            isConfirmVisible = !isConfirmVisible;

            if (isConfirmVisible)
            {
                txtVisibleConfirm.Text = txtConfirm.Password;
                txtConfirm.Visibility = Visibility.Collapsed;
                txtVisibleConfirm.Visibility = Visibility.Visible;
                imgEyeOpenConfirm.Visibility = Visibility.Collapsed;
                imgEyeClosedConfirm.Visibility = Visibility.Visible;
            }
            else
            {
                txtConfirm.Password = txtVisibleConfirm.Text;
                txtVisibleConfirm.Visibility = Visibility.Collapsed;
                txtConfirm.Visibility = Visibility.Visible;
                imgEyeOpenConfirm.Visibility = Visibility.Visible;
                imgEyeClosedConfirm.Visibility = Visibility.Collapsed;
            }
        }
    }
}