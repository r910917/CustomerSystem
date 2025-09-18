using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
namespace CustomerSystem.Pages
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        public class AddressValidator
        {
            private const string ApiKey = "AIzaSyA8_Yk_hVPcNQNF9l6leTGZLGQms5HOsV0";
            private const string BaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";

            public static async Task<bool> ValidateAddressAsync(string address)
            {
                using var client = new HttpClient();
                var response = await client.GetFromJsonAsync<GeocodeResponse>($"{BaseUrl}?address={Uri.EscapeDataString(address)}&key={ApiKey}");
                if (response == null || response.Results == null || response.Results.Length == 0)
                {
                    return false;
                }

                // 判斷是否返回有效地址
                return response.Status == "OK";
            }
        }

        public class GeocodeResponse
        {
            public string Status { get; set; }
            public GeocodeResult[] Results { get; set; }
        }

        public class GeocodeResult
        {
            public string FormattedAddress { get; set; }
            public Geometry Geometry { get; set; }
        }

        public class Geometry
        {
            public Location Location { get; set; }
        }

        public class Location
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }
        // 密碼加密工具
        public static class PasswordHelper
        {
            public static (string hash, string salt) HashPassword(string password)
            {
                // 產生隨機鹽值
                var saltBytes = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(saltBytes);
                }
                var salt = Convert.ToBase64String(saltBytes);

                // 密碼加鹽後雜湊
                var passwordWithSalt = password + salt;
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
                    var hash = Convert.ToBase64String(hashBytes);
                    return (hash, salt);
                }
            }
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            // 獲取輸入資料
            var fullName = FullNameEntry.Text;
            var nickname = NicknameEntry.Text ?? string.Empty; // 選填
            var email = EmailEntry.Text;
            var username = UsernameEntry.Text;
            var password = PasswordEntry.Text;
            var phone = PhoneEntry.Text;
            var address = AddressEntry.Text;

            // 驗證必填項
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("錯誤", "請填寫所有必填欄位", "確定");
                return;
            }

            try
            {
                // 建立 MySQL 連線
                string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // 檢查電子郵件是否已註冊
                string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                using var checkEmailCommand = new MySqlCommand(checkEmailQuery, connection);
                checkEmailCommand.Parameters.AddWithValue("@Email", email);
                var emailExists = Convert.ToInt32(await checkEmailCommand.ExecuteScalarAsync()) > 0;

                if (emailExists)
                {
                    await DisplayAlert("錯誤", "該電子郵件已被註冊", "確定");
                    return;
                }

                // 檢查手機號碼是否已註冊
                string checkPhoneQuery = "SELECT COUNT(*) FROM Users WHERE Phone = @Phone";
                using var checkPhoneCommand = new MySqlCommand(checkPhoneQuery, connection);
                checkPhoneCommand.Parameters.AddWithValue("@Phone", phone);
                var phoneExists = Convert.ToInt32(await checkPhoneCommand.ExecuteScalarAsync()) > 0;

                if (phoneExists)
                {
                    await DisplayAlert("錯誤", "該手機號碼已被註冊", "確定");
                    return;
                }

                // 檢查使用者ID是否已註冊
                string checkusernameQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using var checkusernameCommand = new MySqlCommand(checkusernameQuery, connection);
                checkusernameCommand.Parameters.AddWithValue("@Username", username);
                var usernameExists = Convert.ToInt32(await checkusernameCommand.ExecuteScalarAsync()) > 0;

                if (usernameExists)
                {
                    await DisplayAlert("錯誤", "該手機號碼已被註冊", "確定");
                    return;
                }

                // 驗證地址有效性
                if (!await AddressValidator.ValidateAddressAsync(address))
                {
                    await DisplayAlert("錯誤", "地址無效，請檢查並重新輸入。", "確定");
                    return;
                }

                // 加密密碼
                var (passwordHash, salt) = PasswordHelper.HashPassword(password);

                // 插入資料
                string query = @"
                    INSERT INTO Users 
                    (FullName, Nickname, Email, Username, Password, Salt, Phone, Address) 
                    VALUES 
                    (@FullName, @Nickname, @Email, @Username, @PasswordHash, @Salt, @Phone, @Address)";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Nickname", nickname);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash); // 加密後的密碼
                command.Parameters.AddWithValue("@Salt", salt); // 加密使用的鹽值
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@Address", address);
                await command.ExecuteNonQueryAsync();

                await DisplayAlert("成功", "註冊成功！", "確定");
                await Navigation.PopAsync(); // 返回上一頁
            }
            catch (Exception ex)
            {
                await DisplayAlert("錯誤", $"資料庫操作失敗：{ex.Message}", "確定");
            }
        }
    }

}
