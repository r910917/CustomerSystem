using MySql.Data.MySqlClient;
using System.Globalization;

namespace CustomerSystem.Pages;

public partial class BasicInfoPage : ContentPage
{
    private bool _isEditingPersonalInfo = false;
    private bool _isEditingFinancialInfo = false;
    private string _username = Preferences.Get("SavedUsername", string.Empty);



    public BasicInfoPage()
	{
		InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        if (!string.IsNullOrEmpty(_username))
        {
            LoadUserData();
            LoadFinancialData();
        }
        else
        {
            DisplayAlert("錯誤", "無法取得使用者帳號", "確定");
        }
    }

    
    // 從資料庫讀取資料並填充格子
    private async void LoadUserData()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string query = "SELECT * FROM users WHERE Username = @Username";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", _username);
        using var reader = await command.ExecuteReaderAsync();

        if (reader.Read())
        {
            // 第一個區域資料
            FullNameEntry.Text = reader["FullName"].ToString();
            NicknameEntry.Text = reader["Nickname"].ToString();
            EmailEntry.Text = reader["Email"].ToString();
            PhoneEntry.Text = reader["Phone"].ToString();
            AddressEntry.Text = reader["Address"].ToString();
        }
    }

    // 編輯個人資料
    private void OnEditPersonalInfo01Clicked(object sender, EventArgs e)
    {
        _isEditingPersonalInfo = !_isEditingPersonalInfo;
        SetPersonalInfoReadOnly(!_isEditingPersonalInfo);
        UpdatePersonalInfo01Button.IsVisible = _isEditingPersonalInfo;
        EditPersonalInfo01Button.Text = _isEditingPersonalInfo ? "取消" : "編輯";
        EditPersonalInfo01Button.BackgroundColor = _isEditingPersonalInfo ? Colors.Red : Colors.Blue;

        if (!_isEditingPersonalInfo)  // 若點擊取消，重新加載資料
        {
            LoadUserData();
        }
    }


    // 設定個人資料輸入框狀態
    private void SetPersonalInfoReadOnly(bool isReadOnly)
    {
        FullNameEntry.IsReadOnly = isReadOnly;
        NicknameEntry.IsReadOnly = isReadOnly;
        EmailEntry.IsReadOnly = isReadOnly;
        PhoneEntry.IsReadOnly = isReadOnly;
        AddressEntry.IsReadOnly = isReadOnly;

        // 設置背景顏色
        FullNameEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        NicknameEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        EmailEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        PhoneEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        AddressEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
    }
    // 儲存更新後的資料
    private async void OnUpdatePersonalInfo01Clicked(object sender, EventArgs e)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string updateQuery = @"
            UPDATE users
            SET FullName = @FullName, Nickname = @Nickname, 
                Email = @Email, Phone = @Phone, Address = @Address
            WHERE Username = @Username";

        using var command = new MySqlCommand(updateQuery, connection);
        command.Parameters.AddWithValue("@Username", _username);
        command.Parameters.AddWithValue("@FullName", FullNameEntry.Text);
        command.Parameters.AddWithValue("@Nickname", NicknameEntry.Text);
        command.Parameters.AddWithValue("@Email", EmailEntry.Text);
        command.Parameters.AddWithValue("@Phone", PhoneEntry.Text);
        command.Parameters.AddWithValue("@Address", AddressEntry.Text);

        await command.ExecuteNonQueryAsync();
        await DisplayAlert("成功", "個人資料已更新！", "確定");

        _isEditingPersonalInfo = false;
        UpdatePersonalInfo01Button.IsVisible = false;
        EditPersonalInfo01Button.Text = _isEditingPersonalInfo ? "取消" : "編輯";
        EditPersonalInfo01Button.BackgroundColor = _isEditingPersonalInfo ? Colors.Red : Colors.Blue;
        SetPersonalInfoReadOnly(true);
        LoadUserData();  // 更新後重新顯示新資料
    }


    // 2. 從 financial_data 表讀取理財資料
    private async void LoadFinancialData()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string query = "SELECT * FROM financial_data WHERE Username = @Username";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", _username);
        using var reader = await command.ExecuteReaderAsync();

        if (reader.Read())
        {
            if (reader["birthday"] != DBNull.Value && DateTime.TryParse(reader["birthday"].ToString(), out DateTime birthDate))
            {
                BirthDatePicker.Date = birthDate;  // 將轉換後的 DateTime 賦值給 DatePicker
            }
            else
            {
                BirthDatePicker.Date = DateTime.Today;  // 若無生日資料，預設為今天
            }
            OccupationEntry.Text = reader["Occupation"].ToString();
            // 格式化每月收入
            if (decimal.TryParse(reader["MonthlyIncome"]?.ToString(), out decimal monthlyIncome))
            {
                MonthlyIncomeEntry.Text = monthlyIncome.ToString("N0", CultureInfo.InvariantCulture);
            }

            // 格式化每月支出
            if (decimal.TryParse(reader["MonthlyExpense"]?.ToString(), out decimal monthlyExpense))
            {
                MonthlyExpenseEntry.Text = monthlyExpense.ToString("N0", CultureInfo.InvariantCulture);
            }

            // 格式化薪資儲蓄率
            if (decimal.TryParse(reader["SavingRate"]?.ToString(), out decimal savingRate))
            {
                SavingRateEntry.Text = savingRate.ToString("N2", CultureInfo.InvariantCulture);  // 百分比形式顯示到小數點後兩位
            }

            // 格式化活存
            if (decimal.TryParse(reader["CashSaving"]?.ToString(), out decimal cashSaving))
            {
                CashSavingEntry.Text = cashSaving.ToString("N0", CultureInfo.InvariantCulture);
            }

            // 格式化保本
            if (decimal.TryParse(reader["FixedDeposit"]?.ToString(), out decimal fixedDeposit))
            {
                FixedDepositEntry.Text = fixedDeposit.ToString("N0", CultureInfo.InvariantCulture);
            }

            // 格式化投資
            if (decimal.TryParse(reader["Investment"]?.ToString(), out decimal investment))
            {
                InvestmentEntry.Text = investment.ToString("N0", CultureInfo.InvariantCulture);
            }
        }
    }

    // 3. 當使用者手動選擇出生年月日時自動計算年齡
    private void OnBirthDateSelected(object sender, DateChangedEventArgs e)
    {
        int age = CalculateAge(e.NewDate);
        AgeEntry.Text = age.ToString();  // 更新年齡
    }

    // 3. 計算年齡
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    // 5. 編輯理財資料
    private void OnEditFinancialInfoClicked(object sender, EventArgs e)
    {
        _isEditingFinancialInfo = !_isEditingFinancialInfo;
        SetFinancialInfoReadOnly(!_isEditingFinancialInfo);
        EditFinancialInfoButton.Text = _isEditingFinancialInfo ? "取消" : "編輯";
        EditFinancialInfoButton.BackgroundColor = _isEditingFinancialInfo ? Colors.Red : Colors.LightGray;
        UpdateFinancialInfoButton.IsVisible = _isEditingFinancialInfo;
    }

    private void SetFinancialInfoReadOnly(bool isReadOnly)
    {
        OccupationEntry.IsReadOnly = isReadOnly;
        MonthlyIncomeEntry.IsReadOnly = isReadOnly;
        MonthlyExpenseEntry.IsReadOnly = isReadOnly;
        SavingRateEntry.IsReadOnly = isReadOnly;
        CashSavingEntry.IsReadOnly = isReadOnly;
        FixedDepositEntry.IsReadOnly = isReadOnly;
        InvestmentEntry.IsReadOnly = isReadOnly;

        var backgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        BirthDatePicker.BackgroundColor = backgroundColor;
        OccupationEntry.BackgroundColor = backgroundColor;
        MonthlyIncomeEntry.BackgroundColor = backgroundColor;
        MonthlyExpenseEntry.BackgroundColor = backgroundColor;
        SavingRateEntry.BackgroundColor = backgroundColor;
        CashSavingEntry.BackgroundColor = backgroundColor;
        FixedDepositEntry.BackgroundColor = backgroundColor;
        InvestmentEntry.BackgroundColor = backgroundColor;
    }

    private async void OnUpdateFinancialInfoClicked(object sender, EventArgs e)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 檢查資料表中是否有該 `Username`
        string checkQuery = "SELECT COUNT(*) FROM financial_data WHERE Username = @Username";
        using var checkCommand = new MySqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@Username", _username);
        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());  // 回傳行數

        string query;
        if (count == 0)  // 如果不存在，則插入新資料
        {
            query = @"
            INSERT INTO financial_data (Username, Occupation, Birthday, MonthlyIncome, MonthlyExpense, SavingRate, CashSaving, FixedDeposit, Investment)
            VALUES (@Username, @Occupation, @BirthDate, @MonthlyIncome, @MonthlyExpense, @SavingRate, @CashSaving, @FixedDeposit, @Investment)";
        }
        else  // 如果存在，則更新資料
        {
            query = @"
            UPDATE financial_data
            SET Occupation = @Occupation, Birthday = @BirthDate, MonthlyIncome = @MonthlyIncome, MonthlyExpense = @MonthlyExpense,
                SavingRate = @SavingRate, CashSaving = @CashSaving,
                FixedDeposit = @FixedDeposit, Investment = @Investment
            WHERE Username = @Username";
        }

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", _username);
        command.Parameters.AddWithValue("@BirthDate", BirthDatePicker.Date);
        command.Parameters.AddWithValue("@Occupation", OccupationEntry.Text);
        command.Parameters.AddWithValue("@MonthlyIncome", ConvertCurrencyToDecimal(MonthlyIncomeEntry.Text));
        command.Parameters.AddWithValue("@MonthlyExpense", ConvertCurrencyToDecimal(MonthlyExpenseEntry.Text));
        command.Parameters.AddWithValue("@SavingRate", ConvertCurrencyToDecimal(SavingRateEntry.Text));
        command.Parameters.AddWithValue("@CashSaving", ConvertCurrencyToDecimal(CashSavingEntry.Text));
        command.Parameters.AddWithValue("@FixedDeposit", ConvertCurrencyToDecimal(FixedDepositEntry.Text));
        command.Parameters.AddWithValue("@Investment", ConvertCurrencyToDecimal(InvestmentEntry.Text));

        await command.ExecuteNonQueryAsync();
        await DisplayAlert("成功", "理財資料已更新！", "確定");

        _isEditingFinancialInfo = false;
        UpdateFinancialInfoButton.IsVisible = false;
        EditFinancialInfoButton.Text = "編輯";
        EditFinancialInfoButton.BackgroundColor = Colors.LightGray;
        SetFinancialInfoReadOnly(true);
    }

    private void OnEntryCompleted(object sender, EventArgs e)
    {
        FormatCurrency((Entry)sender);  // 當使用者完成輸入時格式化為貨幣格式
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        FormatCurrency((Entry)sender);  // 當輸入框失去焦點時格式化為貨幣格式
    }

    private void FormatCurrency(Entry entry)
    {
        if (decimal.TryParse(entry.Text, out decimal value))
        {
            entry.Text = value.ToString("N0", CultureInfo.InvariantCulture);  // 格式化為 1,000 形式
        }
        else
        {
            entry.Text = string.Empty;  // 若輸入無效，清空輸入框
        }
    }

    private decimal ConvertCurrencyToDecimal(string currencyText)
    {
        if (string.IsNullOrWhiteSpace(currencyText))
            return 0;

        // 去除逗號和貨幣符號
        string cleanedText = currencyText.Replace(",", "").Replace("$", "").Trim();

        // 將清理後的字串轉換為 decimal
        if (decimal.TryParse(cleanedText, out decimal result))
            return result;
        else
            return 0;  // 如果轉換失敗，回傳 0
    }

    private async void OnBasicInfoPageClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.BasicInfoPage());
    }

    private async void OnAccountingClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.AccountingPage());
    }

    private async void OnViewOrdersClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.FinancialAnalysisPage());
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.HomePage());
    }

    // 登出按鈕點擊事件
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool isLogout = await DisplayAlert("登出", "確定要登出嗎？", "是", "否");
        if (isLogout)
        {
            // 清除儲存的使用者資訊並返回登入頁面
            Preferences.Clear();  // 清除偏好設定中的使用者資訊
            await Navigation.PushAsync(new MainPage());  // 導回登入頁面
        }
    }

}