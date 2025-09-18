using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microcharts;
using SkiaSharp;
using System.Linq;
using CustomerSystem.Models;
using CustomerSystem.services;
namespace CustomerSystem.Pages;
using Syncfusion.Maui.Charts;
using Syncfusion.Maui.ToolKit;

using Org.BouncyCastle.Asn1.Cms;
using System.Globalization;
using Syncfusion.Maui.Toolkit.Charts;

public partial class HomePage : ContentPage
{
    public double ImageHeight { get; set; }
    public double ImageWidth { get; set; }
    public string CurrentDate { get; set; }
    public double InitialBalance { get; set; }
    private readonly DatabaseService _databaseService;
    private string _username = Preferences.Get("SavedUsername", string.Empty);

    public HomePage()
    {
        InitializeComponent();
        CurrentDate = DateTime.Now.ToString("yyyy/MM/dd");
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = this;
        _databaseService = new DatabaseService();  // 初始化 DatabaseService

    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrEmpty(_username))
        {
            LoadUserData();
            await LoadFinancialSummaryAsync(); // 畫面載入時更新財務摘要
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator2.IsRunning = true;
            LoadingIndicator2.IsVisible = true;
            LoadingIndicator3.IsRunning = true;
            LoadingIndicator3.IsVisible = true;
            LoadBalanceChart();
            LoadMonthlyFinancialData();  // 加載數據
        }
        else
        {
            await DisplayAlert("錯誤", "無法取得使用者帳號", "確定");
        }
    }
    private async void LoadUserData()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 查詢使用者資料
        string query = @"
            SELECT u.FullName, f.CashSaving, f.FixedDeposit, f.Investment, f.MonthlyIncome, f.MonthlyExpense 
            FROM users u 
            JOIN financial_data f ON u.Username = f.Username 
            WHERE u.Username = @Username"; 
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", _username);

        using var reader = await command.ExecuteReaderAsync();
        if (reader.Read())
        {
            // 讀取使用者姓名並顯示問候語
            string fullName = reader["FullName"].ToString();
            string firstName = ExtractFirstName(fullName);
            GreetingLabel.Text = $"Hello，{firstName}！";
        }
    }

    private async Task LoadFinancialSummaryAsync()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        int year = DateTime.Now.Year;
        int month = DateTime.Now.Month;
        // 月份收入總額查詢
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '收入' AND YEAR(date) = @year AND MONTH(date) = @month;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@year", year);
        incomeCommand.Parameters.AddWithValue("@month", month);
        double totalIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // 月份支出總額查詢
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '支出' AND YEAR(date) = @year AND MONTH(date) = @month;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@year", year);
        expenseCommand.Parameters.AddWithValue("@month", month);
        double totalExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // SQL 查詢：計算收入和設定的總和
        string finallyincomeQuery = @"
        SELECT IFNULL(SUM(amount), 0) 
        FROM transactions 
        WHERE username = @username AND (type = '收入' OR type = '設定');";
        using var finallyincomeCommand = new MySqlCommand(finallyincomeQuery, connection);
        finallyincomeCommand.Parameters.AddWithValue("@username", _username);
        double finallytotalIncome = Convert.ToDouble(await finallyincomeCommand.ExecuteScalarAsync());

        // SQL 查詢：計算支出總和
        string finallyexpenseQuery = @"
        SELECT IFNULL(SUM(amount), 0) 
        FROM transactions 
        WHERE username = @username AND type = '支出';";
        using var finallyexpenseCommand = new MySqlCommand(finallyexpenseQuery, connection);
        finallyexpenseCommand.Parameters.AddWithValue("@username", _username);
        double finallytotalExpense = Convert.ToDouble(await finallyexpenseCommand.ExecuteScalarAsync());

        // 計算總資產
        double FinalBalance = finallytotalIncome - finallytotalExpense;

        // 更新 UI
        TotalAssetsLabel.Text = $"{FinalBalance:C0}";  // 顯示金額格式
        TotalAssetsLabel.TextColor = FinalBalance < 0 ? Colors.Red : Colors.Blue;  // 設定文字顏色
        MonthlyIncomeLabel.Text = $"{totalIncome:C0}";  // 顯示金額格式
        MonthlyExpensesLabel.Text = $"{totalExpense:C0}";  // 顯示金額格式
    }

    // 提取名字（去掉姓氏）
    private string ExtractFirstName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return string.Empty;

        // 如果姓名長度大於 1 個字，則去掉第一個字作為姓氏
        return fullName.Length > 1 ? fullName.Substring(1) : fullName;
    }

    public List<string> Announcements { get; set; }

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

    //顯示圖表
    private async void LoadBalanceChart()
    {
        try
        {
            var dailyBalances = await _databaseService.GetDailyBalancesAsync(_username);
            var monthlyAverages = await _databaseService.GetMonthlyDataAsync(_username);
            var dailyData = await _databaseService.GetDailyFinancialDataAsync(_username);

            //BalanceChart.Chart = new LineChart
            //{
            //    Entries = entries,
            //    LineMode = LineMode.Straight,
            //    LineSize = 4,
            //    PointMode = PointMode.Circle,
            //    PointSize = 8,
            //    LabelTextSize = 12,
            //    LabelOrientation = Orientation.Horizontal,  // 垂直方向顯示標籤
            //    ValueLabelOrientation = Orientation.Horizontal,  // 垂直顯示金額標籤
            //    BackgroundColor = SKColors.Transparent
            //};

            await Task.Delay(1000);
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator2.IsRunning = false;
            LoadingIndicator2.IsVisible = false;
            LoadingIndicator3.IsRunning = false;
            LoadingIndicator3.IsVisible = false;
            await Task.Delay(500);
            BalanceChart.IsVisible = true;  // 顯示圖表
            BalanceChartSF.IsVisible = true;
            DailyBalanceChart.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法載入折線圖：{ex.Message}", "確定");
        }
    }

   private async void LoadMonthlyFinancialData()
   {
       var data = await _databaseService.GetMonthlyFinancialDataAsync(_username);  // 從資料庫獲取資料
       var monthlyAverages = await _databaseService.GetMonthlyDataAsync(_username);
       var dailyData = await _databaseService.GetDailyFinancialDataAsync(_username);

        // 將資料綁定至折線圖
        BalanceLineSeries.ItemsSource = data;  // 綁定資料
        BalanceLineSeriesmonth.ItemsSource = monthlyAverages;
        DailyBalanceLineSeries.ItemsSource = dailyData;

        BalanceChart.WidthRequest = Math.Max(600, monthlyAverages.Count * 100);
        BalanceChartSF.WidthRequest = Math.Max(600, data.Count * 100);
        DailyBalanceChart.WidthRequest = Math.Max(600, dailyData.Count * 100);
    }
}

