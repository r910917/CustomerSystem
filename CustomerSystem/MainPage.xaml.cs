using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Graphics;

namespace CustomerSystem
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            // 隱藏 Navigation Bar
            NavigationPage.SetHasNavigationBar(this, false);

            // 嘗試從本地記憶中載入帳號
            LoadSavedCredentials();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            await button.ScaleTo(0.95, 100);  // 縮小至 95%
            await button.ScaleTo(1, 100);     // 恢復至原始大小

        }

        public Command LoginCommand => new(async () =>
        {
            // 獲取使用者輸入
            var username = UsernameEntry.Text;
            var password = PasswordEntry.Text;

            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await Snackbar.Make("請輸入帳號與密碼", action: null, duration: TimeSpan.FromSeconds(2), visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb("#ed3ba7"), TextColor = Colors.White, Font = Microsoft.Maui.Font.Default, CornerRadius = new CornerRadius(10) }, anchor: Login).Show();
                return;
            }

            try
            {
                // 建立 MySQL 連線
                string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // 查詢使用者資料
                string query = "SELECT Password, Salt FROM Users WHERE Username = @Username";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    // 獲取資料庫中的密碼雜湊與鹽值
                    var storedHash = reader["Password"].ToString();
                    var storedSalt = reader["Salt"].ToString();


                    // 驗證密碼
                    if (VerifyPassword(password, storedHash, storedSalt))
                    {
                        // 保存帳號資訊到本地存儲
                        Preferences.Set("SavedUsername", username);
                        Preferences.Set("IsLoggedIn", true);

                        // 顯示 Toast 訊息（自動消失）
                        await Snackbar.Make("登入成功！", action: null, duration: TimeSpan.FromSeconds(2), visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb("#5bed3b"), TextColor = Colors.White, Font = Microsoft.Maui.Font.Default, CornerRadius = new CornerRadius(10) }, anchor: Login).Show();

                        // 跳轉到功能頁面（例如主功能頁）
                        await Task.Delay(500);  // 延遲2秒後跳轉
                        await Navigation.PushAsync(new Pages.HomePage());
                    }
                    else
                    {
                        await Snackbar.Make("密碼錯誤！", action: null, duration: TimeSpan.FromSeconds(2), visualOptions: new SnackbarOptions{BackgroundColor = Color.FromArgb("#FF7AFF"),TextColor = Colors.White, Font = Microsoft.Maui.Font.Default, CornerRadius = new CornerRadius(10)}, anchor: Login).Show();
                    }
                }
                else
                {
                    await Snackbar.Make("使用者不存在！", action: null, duration: TimeSpan.FromSeconds(2), visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb("#FF7AFF"), TextColor = Colors.White, Font = Microsoft.Maui.Font.Default, CornerRadius = new CornerRadius(10) }, anchor: Login).Show();
                }
            }
            catch (Exception ex)
            {
                await Snackbar.Make($"資料庫操作失敗：{ex.Message} ，請檢察網路狀態。", action: null, duration: TimeSpan.FromSeconds(2), visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb("#FF7AFF"), TextColor = Colors.White, Font = Microsoft.Maui.Font.Default, CornerRadius = new CornerRadius(10) }, anchor: Login).Show();
            }
        });

        // 驗證密碼
        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var passwordWithSalt = password + storedSalt;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
                var hash = Convert.ToBase64String(hashBytes);
                return hash == storedHash;
            }
        }

        public Command RegisterCommand => new(async () =>
        {
            //await DisplayAlert("註冊", "請聯繫管理員進行註冊。", "確定");
            await Navigation.PushAsync(new Pages.RegisterPage());
        });

        public Command AdminLoginCommand => new(async () =>
        {
            await DisplayAlert("管理員登入", "進入管理員登入介面。", "確定");
        });

        public Command ForgotPasswordCommand => new(async () =>
        {
            await DisplayAlert("忘記密碼", "請聯繫管理員重置密碼。", "確定");
        });

        private void SaveCredentials(string username)
        {
            Preferences.Set("SavedUsername", username);
        }

        private void LoadSavedCredentials()
        {
            if (Preferences.ContainsKey("SavedUsername"))
            {
                UsernameEntry.Text = Preferences.Get("SavedUsername", string.Empty);
            }
        }

    }

}
