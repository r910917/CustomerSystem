using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Calendar.Models;
using System.Transactions;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using System.Data;
using CustomerSystem.Models;
using CustomerSystem.services;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CustomerSystem.Pages;

public partial class AccountingPage : ContentPage
{
    public double TotalAssets { get; set; }
    public double TotalIncome { get; set; }
    public double TotalExpense { get; set; }
    public double InitialBalance { get; set; }
    public double NetIncome => TotalIncome - TotalExpense;
    public double FinalBalance => InitialBalance + NetIncome;
    private double _selectedIncome;
    private double _selectedExpense;
    private double _selectedNetIncome;
    private CancellationTokenSource _animationCancellationTokenSource;

    private bool _isTransactionFormVisible = false;  // 表單顯示狀態
    private bool _isCardDetailVisible = false;  // 表單顯示狀態
    //private Transactiondata _selectedTransaction;
    private readonly DatabaseService _databaseService = new DatabaseService();
    private DateTime _selectedDate;
    private DateTime _shownDate;
    public Command<string> MenuCommand { get; }
    public ICommand DayTappedCommand { get; }  // 點選日期事件
    public ICommand OnShownDateChangedCommand { get; }  // 顯示月份改變事件
    public ObservableCollection<Transactiondata> Transactions { get; set; } = new ObservableCollection<Transactiondata>();
    public string SelectedDateText => $"選擇日期：{_selectedDate:yyyy/MM/dd}";
    public string ShownMonthText => $"顯示月份：{_shownDate:yyyy/MM}";
    private string _username = Preferences.Get("SavedUsername", string.Empty);
    private List<string> _defaultCategories = new List<string> { "餐飲", "交通", "娛樂", "購物", "其他", "薪資" };

    public AccountingPage()
	{
		InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        // 初始化命令
        DayTappedCommand = new Command<DateTime>(OnDayTapped);

        BindingContext = this;

        // 預設顯示當前月份和日期
        _selectedDate = DateTime.Today;
        _shownDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    }

    public EventCollection Events { get; set; }
    private async Task LoadTransactionDatesAsync()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 查詢所有有交易資料的日期
        string query = "SELECT DISTINCT date FROM transactions WHERE username = @username;";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", _username);

        using var reader = await command.ExecuteReaderAsync();
        var events = new EventCollection();  // 使用 EventCollection

        while (await reader.ReadAsync())
        {
            DateTime date = reader.GetDateTime("date");
            events.Add(date, null);
        }

        TransactionCalendar.Events = events;  // 將事件列表綁定到日曆
    }
    private async Task LoadFinancialSummaryAsync()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 初始餘額查詢
        string initialBalanceQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '設定' AND category = '初始餘額設定';";
        using var initialBalanceCommand = new MySqlCommand(initialBalanceQuery, connection);
        initialBalanceCommand.Parameters.AddWithValue("@username", _username);
        InitialBalance = Convert.ToDouble(await initialBalanceCommand.ExecuteScalarAsync());

        // 獲取收入總額
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '收入';";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        double totalIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // 獲取支出總額
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '支出';";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        double totalExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // 計算總資產
        double totalAssets = totalIncome - totalExpense;
        double NetIncome = totalIncome - totalExpense;
        double FinalBalance = InitialBalance + NetIncome;
        // 計算總收支和餘額
        NetIncomeLabel.Text = NetIncome < 0 ? "支出" : "收入";
        FinalBalanceLabel.Text = $"{FinalBalance:C0}";
        FinalBalanceLabel.TextColor = FinalBalance < 0 ? Colors.Red : Colors.Blue;

        // 更新 UI
        TotalAssetsLabel.Text = $"{totalAssets:C0}";  // 顯示金額格式
        TotalAssetsLabel.TextColor = totalAssets < 0 ? Colors.Red : Colors.Blue;  // 設定文字顏色
        TotalAssetsLabelFirst.Text = $"{totalAssets:C0}";  // 顯示金額格式
        TotalAssetsLabelFirst.TextColor = totalAssets < 0 ? Colors.Red : Colors.Blue;  // 設定文字顏色
        totalIncomeLabel.Text = $"{totalIncome:C0}";  // 顯示金額格式
        totalExpenseLabel.Text = $"{totalExpense:C0}";  // 顯示金額格式
        InitialBalanceLabel.Text = $"{InitialBalance:C0}";

        SelectedIncomeLabel.Text = $"{totalIncome:C0}";  // 無小數點
        SelectedExpenseLabel.Text = $"{totalExpense:C0}";  // 無小數點

        SelectedNetIncomeLabel.Text = NetIncome < 0 ? "支出" : "收入";
        SelectedNetIncomeLabel.TextColor = NetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{NetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = NetIncome < 0 ? Colors.Red : Colors.Blue;
    }

    private async void OnMenuButtonClicked(object sender, EventArgs e)
    {

        if (sender is Button button && button.BindingContext is Transactiondata transaction)
        {
            // 確保 transaction 為非空
            if (transaction == null)
            {
                await DisplayAlert("錯誤", "無法取得交易資料。", "確定");
                return;
            }

            string action = await DisplayActionSheet("選擇操作", "取消", null, "編輯", "刪除");
            switch (action)
            {
                case "編輯":
                    ShowEditPopup(transaction);
                    break;
                case "刪除":
                    bool confirm = await DisplayAlert("刪除確認", $"是否要刪除該筆交易資料？\n日期：{transaction.Date:yyyy/MM/dd}\n類型：{transaction.Type}\n金額：{transaction.Amount:C}", "是", "否");
                    if (confirm)
                    {
                        await DeleteTransactionAsync(transaction.Id, transaction.Username, transaction.Date);
                        OnShowAllDataClicked(null, null);
                    }
                    break;
            }
        }
        else
        {
            await DisplayAlert("錯誤", "無法取得交易資料。", "確定");
        }
    }
    int transID;
    private void ShowEditPopup(Transactiondata transaction)
    {
        // 填充現有交易資料
        EditAmountEntry.Text = transaction.Amount.ToString("0.00");
        EditNoteEditor.Text = transaction.Note;
        transID = transaction.Id;

        EditTransactionPopup.IsVisible = true;  // 顯示彈出視窗
    }
    private void OnCancelEditClicked(object sender, EventArgs e)
    {
        EditTransactionPopup.IsVisible = false;  // 隱藏彈出視窗
    }
    private async void OnUpdateTransactionClicked(object sender, EventArgs e)
    {
        // 更新資料庫
        string query = "UPDATE transactions SET amount = @amount, note = @note WHERE id = @id;";
        var parameters = new Dictionary<string, object>
    {
        { "@amount", EditAmountEntry.Text },
        { "@note", EditNoteEditor.Text },
        { "@id", transID }
    };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);
        //await _databaseService.InsertTransactionAsync(transaction);

        EditTransactionPopup.IsVisible = false;  // 隱藏彈出視窗
        await DisplayAlert("成功", "交易資料已更新。", "確定");

        OnShowAllDataClicked(null, null);  // 重新載入資料
    }

    private async Task DeleteTransactionAsync(int transactionId,string username,DateTime SelDate)
    {
        string query = "DELETE FROM transactions WHERE id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", transactionId } };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);
        await _databaseService.DeleteTransactionAsync(transactionId, username, SelDate);
    }
    // 載入類別清單
    private async Task LoadCategoriesAsync()
    {
        try
        {
            string query = "SELECT name FROM category WHERE username = @username;";
            var parameters = new Dictionary<string, object> { { "@username", _username } };

            var categories = await _databaseService.ExecuteQueryAsync(query, parameters, reader => reader.GetString("name"));

            // 如果資料庫沒有類別，使用預設類別
            if (categories == null || categories.Count == 0)
            {
                CategoryPicker.ItemsSource = _defaultCategories;
            }
            else
            {
                CategoryPicker.ItemsSource = categories;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法載入類別：{ex.Message}", "確定");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategoriesAsync();  // 當頁面顯示時載入類別清單
        await LoadFinancialSummaryAsync(); // 畫面載入時更新財務摘要
        await LoadTransactionDatesAsync();  // 載入有交易資料的日期
        OnShowAllDataClicked(null, null);
    }


    private void OnToggleTransactionFormClicked(object sender, EventArgs e)
    {
        // 切換表單顯示狀態
        _isTransactionFormVisible = !_isTransactionFormVisible;
        TransactionForm.IsVisible = _isTransactionFormVisible;

        // 更新按鈕文字
        ToggleTransactionFormButton.Text = _isTransactionFormVisible ? "取消" : "新增記帳";
        ToggleTransactionFormButton.BackgroundColor = _isTransactionFormVisible ? Color.FromArgb("#90FF5252") : Color.FromArgb("#904CAF50");
    }

    private void OnCardDetailClicked(object sender, EventArgs e)
    {
        // 切換表單顯示狀態
        _isCardDetailVisible = !_isCardDetailVisible;
        CardDetail.IsVisible = _isCardDetailVisible;

        // 更新按鈕文字
        CardDetailButton.Text = _isCardDetailVisible ? "隱藏統計" : "檢視統計";
        CardDetailButton.BackgroundColor = _isCardDetailVisible ? Color.FromArgb("#90FF5252") : Color.FromArgb("#90864EAD");
    }

    private async void OnSubmitTransactionClicked(object sender, EventArgs e)
    {
        // 驗證輸入資料
        if (TypePicker.SelectedItem == null || string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            await DisplayAlert("錯誤", "請選擇類型並輸入金額。", "確定");
            return;
        }
        string balanceText = FinalBalanceLabel.Text.Replace("NT","").Replace("$", "").Replace(",", "").Trim();
        // 將資料轉換並新增到資料庫
        var transaction = new Transactiondata
        {
            Username = _username,
            Type = TypePicker.SelectedItem.ToString(),
            Category = string.IsNullOrWhiteSpace(CategoryPicker.SelectedItem?.ToString()) ? null : CategoryPicker.SelectedItem?.ToString(),
            Amount = decimal.Parse(AmountEntry.Text),
            Date = TransactionCalendar.SelectedDate ?? DateTime.Today,  // 預設為今日日期
            Balance = Convert.ToDouble(balanceText)
        };

        await AddTransactionAsync(transaction);
        await DisplayAlert("成功", "記帳資料已新增！", "確定");

        // 清空輸入欄位
        TypePicker.SelectedItem = null;
        CategoryPicker.SelectedItem = null;
        AmountEntry.Text = string.Empty;
        NoteEntry.Text = string.Empty;

        // 重新加載資料
        OnShowAllDataClicked(null, null);
    }

    private async Task AddTransactionAsync(Transactiondata transaction)
    {
        string query = @"INSERT INTO transactions (username, type, category, amount, initial_balance, date, note)
                     VALUES (@username, @type, @category, @amount, @initialBalance, @date, @note);";

        var parameters = new Dictionary<string, object>
        {
            { "@username", transaction.Username },
            { "@type", transaction.Type },
            { "@category", transaction.Category },
            { "@amount", transaction.Amount },
            { "@initialBalance", transaction.InitialBalance },
            { "@date", transaction.Date },
            { "@note", transaction.Note }
        };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);
        await _databaseService.InsertTransactionAsync(transaction);
    }

    // 使用者點選日期時
    private async void OnDayTapped(DateTime selectedDate)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 查詢該日期的收入總額
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '收入' AND date = @date;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@date", selectedDate.Date);
        _selectedIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // 查詢該日期的支出總額
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '支出' AND date = @date;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@date", selectedDate.Date);
        _selectedExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // 計算選取到的總收支
        _selectedNetIncome = _selectedIncome - _selectedExpense;

        // 更新 UI
        SelectedIncomeLabel.Text = $"{_selectedIncome:C0}";  // 無小數點
        SelectedExpenseLabel.Text = $"{_selectedExpense:C0}";  // 無小數點

        SelectedNetIncomeLabel.Text = _selectedNetIncome < 0 ? "支出" : "收入";
        SelectedNetIncomeLabel.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{_selectedNetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        _selectedDate = selectedDate;
        DateLabel.Text = $"選擇日期：{_selectedDate:yyyy/MM/dd}";
        try
        {
            string query = "SELECT * FROM transactions WHERE username = @username AND date = @date ORDER BY date DESC;";
            var parameters = new Dictionary<string, object>
        {
            { "@username", _username },
            { "@date", selectedDate.Date }
        };

            var transactions = await _databaseService.ExecuteQueryAsync(query, parameters, reader => new Transactiondata
            {
                Id = reader.GetInt32("id"),
                Username = reader.GetString("username"),
                Type = reader.GetString("type"),
                Category = reader["category"] != DBNull.Value ? reader.GetString("category") : null,
                Amount = reader.GetDecimal("amount"),
                InitialBalance = reader.GetDecimal("initial_balance"),
                Date = reader.GetDateTime("date"),
                Note = reader["note"] != DBNull.Value ? reader.GetString("note") : null,
                CreatedAt = reader.GetDateTime("created_at")
            });

            TransactionList.ItemsSource = transactions;

            // 顯示提示訊息
            if (transactions.Count == 0)
            {
                
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法載入資料：{ex.Message}", "確定");
        }
    }

    private async void OnShowMonthDataClicked(object sender, EventArgs e)
    {
        int selectedYear = TransactionCalendar.ShownDate.Year;  // 取得日曆顯示的年份
        int selectedMonth = TransactionCalendar.ShownDate.Month;  // 取得日曆顯示的月份
        if (TransactionCalendar.SelectedDate != null)
        {
            DateTime selectedDate = TransactionCalendar.SelectedDate.Value;
            int year = selectedDate.Year;
            int month = selectedDate.Month;

            await LoadMonthlyFinancialSummaryAsync(year, month);
        }
        try
        {
            string query = "SELECT * FROM transactions WHERE username = @username AND YEAR(date) = @year AND MONTH(date) = @month ORDER BY date DESC;";
            var parameters = new Dictionary<string, object>
        {
            { "@username", _username },
            { "@year", selectedYear },
            { "@month", selectedMonth }
        };

            var transactions = await _databaseService.ExecuteQueryAsync(query, parameters, reader => new Transactiondata
            {
                Id = reader.GetInt32("id"),
                Username = reader.GetString("username"),
                Type = reader.GetString("type"),
                Category = reader["category"] != DBNull.Value ? reader.GetString("category") : null,
                Amount = reader.GetDecimal("amount"),
                InitialBalance = reader.GetDecimal("initial_balance"),
                Date = reader.GetDateTime("date"),
                Note = reader["note"] != DBNull.Value ? reader.GetString("note") : null,
                CreatedAt = reader.GetDateTime("created_at")
            });

            TransactionList.ItemsSource = transactions;

            if (transactions.Count == 0)
            {
                await DisplayAlert("提示", $"所選月份 {selectedYear} 年 {selectedMonth} 月 無交易記錄。", "確定");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法載入資料：{ex.Message}", "確定");
        }
    }

    private async Task LoadMonthlyFinancialSummaryAsync(int year, int month)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 月份收入總額查詢
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '收入' AND YEAR(date) = @year AND MONTH(date) = @month;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@year", year);
        incomeCommand.Parameters.AddWithValue("@month", month);
        _selectedIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // 月份支出總額查詢
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '支出' AND YEAR(date) = @year AND MONTH(date) = @month;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@year", year);
        expenseCommand.Parameters.AddWithValue("@month", month);
        _selectedExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // 計算總收支
        _selectedNetIncome = _selectedIncome - _selectedExpense;

        // 更新 UI
        SelectedIncomeLabel.Text = $"{_selectedIncome:C0}";  // 無小數點
        SelectedExpenseLabel.Text = $"{_selectedExpense:C0}";  // 無小數點

        SelectedNetIncomeLabel.Text = _selectedNetIncome < 0 ? "支出" : "收入";
        SelectedNetIncomeLabel.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{_selectedNetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
    }

    private async void OnShowYearDataClicked(object sender, EventArgs e)
    {
        int selectedYear = TransactionCalendar.ShownDate.Year;  // 取得日曆顯示的年份
        if (TransactionCalendar.SelectedDate != null)
        {
            int year = TransactionCalendar.SelectedDate.Value.Year;

            await LoadYearlyFinancialSummaryAsync(year);
        }
        try
        {
            string query = "SELECT * FROM transactions WHERE username = @username AND YEAR(date) = @year ORDER BY date DESC;";
            var parameters = new Dictionary<string, object>
        {
            { "@username", _username },
            { "@year", selectedYear }
        };

            var transactions = await _databaseService.ExecuteQueryAsync(query, parameters, reader => new Transactiondata
            {
                Id = reader.GetInt32("id"),
                Username = reader.GetString("username"),
                Type = reader.GetString("type"),
                Category = reader["category"] != DBNull.Value ? reader.GetString("category") : null,
                Amount = reader.GetDecimal("amount"),
                InitialBalance = reader.GetDecimal("initial_balance"),
                Date = reader.GetDateTime("date"),
                Note = reader["note"] != DBNull.Value ? reader.GetString("note") : null,
                CreatedAt = reader.GetDateTime("created_at")
            });

            TransactionList.ItemsSource = transactions;

            if (transactions.Count == 0)
            {
                await DisplayAlert("提示", $"所選年份 {selectedYear} 無交易記錄。", "確定");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法載入資料：{ex.Message}", "確定");
        }
    }

    private async Task LoadYearlyFinancialSummaryAsync(int year)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 年份收入總額查詢
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '收入' AND YEAR(date) = @year;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@year", year);
        _selectedIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // 年份支出總額查詢
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '支出' AND YEAR(date) = @year;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@year", year);
        _selectedExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // 計算總收支
        _selectedNetIncome = _selectedIncome - _selectedExpense;

        // 更新 UI
        SelectedIncomeLabel.Text = $"{_selectedIncome:C0}";  // 無小數點
        SelectedExpenseLabel.Text = $"{_selectedExpense:C0}";  // 無小數點

        SelectedNetIncomeLabel.Text = _selectedNetIncome < 0 ? "支出" : "收入";
        SelectedNetIncomeLabel.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{_selectedNetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
    }

    private async void OnShowAllDataClicked(object sender, EventArgs e)
    {
            try
            {
                string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM transactions WHERE username = @username ORDER BY date DESC;";
                using var command = new MySqlCommand(query, connection);

                // 添加參數
                command.Parameters.AddWithValue("@username", _username);

                using var reader = await command.ExecuteReaderAsync();
                List<Transactiondata> transactions = new List<Transactiondata>();

                while (await reader.ReadAsync())
                {
                    transactions.Add(new Transactiondata
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("username"),
                        Type = reader.GetString("type"),
                        Category = reader["category"] != DBNull.Value ? reader.GetString("category") : null,
                        Amount = reader.GetDecimal("amount"),
                        InitialBalance = reader.GetDecimal("initial_balance"),
                        Date = reader.GetDateTime("date"),
                        Note = reader["note"] != DBNull.Value ? reader.GetString("note") : null,
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }
                await LoadFinancialSummaryAsync();
            await LoadTransactionDatesAsync();  // 載入有交易資料的日期
            TransactionList.ItemsSource = transactions;  // 將資料綁定到 UI
            }
            catch (Exception ex)
            {
                await DisplayAlert("錯誤", $"無法載入資料：{ex.Message}", "確定");
            }
        }

    private async void OnSetInitialBalanceClicked(object sender, EventArgs e)
    {
        // 彈出輸入對話框讓使用者輸入初始餘額
        string result = await DisplayPromptAsync("初始餘額設定", "請輸入初始餘額金額：", "確定", "取消", keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        if (!decimal.TryParse(result, out decimal initialBalance) || initialBalance < 0)
        {
            await DisplayAlert("錯誤", "請輸入正確的金額。", "確定");
            return;
        }
        string balanceText = FinalBalanceLabel.Text.Replace("NT", "").Replace("$", "").Replace(",", "").Trim();
        // 建立初始餘額設定的 TransactionData
        var transaction = new Transactiondata
        {
            Username = _username,
            Type = "設定",  // 設定類型
            Category = "初始餘額設定",  // 類別寫 "初始餘額設定"
            Amount = initialBalance,
            InitialBalance = initialBalance,
            Date = DateTime.Today,  // 預設為今日
            Note = "設定初始餘額",
            Balance = Convert.ToDouble(balanceText)
        };

        await AddTransactionAsync(transaction);
        await DisplayAlert("成功", "初始餘額已成功設定！", "確定");

        // 重新加載資料以更新 UI
        OnShowAllDataClicked(null, null);
    }


    //頁面跳轉
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
    //頁面跳轉
}