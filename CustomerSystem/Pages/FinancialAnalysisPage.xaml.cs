using CustomerSystem.Models;
using CustomerSystem.services;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text.Json;
using HtmlAgilityPack;
using System.Net.Http;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Maui;
using System.Data;
using Syncfusion.Maui.Graphics.Internals;


namespace CustomerSystem.Pages;

public partial class FinancialAnalysisPage : ContentPage
{

    private readonly string apiKey = "ec6af86faamsha2876b46c79bf20p1a58edjsnfd66f07e897f";  // 在 RapidAPI 取得的 API Key
    string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
    private string _username = Preferences.Get("SavedUsername", string.Empty);

    public FinancialAnalysisPage()
	{
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = this;
        InitializeComponent();
        OnFetchInflationRateClicked(null, null);
        OnGrowthRateLabelClicked(null, null);
    }

    string inflationRate;
    private async Task<string> GetInflationRateFromWebAsync()
    {
        string url = "https://www.macromicro.me/series/489/tw-cpi-yoy";  // 台灣 CPI 網頁

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return "無法連接到網站，請稍後再試。";
        }

        string htmlContent = await response.Content.ReadAsStringAsync();

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(htmlContent);

        // 尋找網頁中的通膨率數據
        var node = document.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[3]/div[1]/span[1]");
        var nodedate = document.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[2]/time");
        if (node != null)
        {
            inflationRate = node.InnerText.Trim();
            string currentDate = nodedate.InnerText.Trim();  // 當前資料時間
            return $"：{inflationRate}% - {currentDate}月";
        }
        else
        {
            return "找不到通膨率資料。";
        }
    }

    private async void OnFetchInflationRateClicked(object sender, EventArgs e)
    {
        string inflationRate = await GetInflationRateFromWebAsync();
        InflationRateLabel.Text = inflationRate;
    }

    string GrowthRate;
    private async Task<string> GetGrowthRateLabelFromWebAsync()
    {
        string url = "https://www.macromicro.me/series/5307/tw-monthly-real-earnings-all-employees-industry-services-yoy";  // 台灣 CPI 網頁

        HttpClient httpClientGrowth = new HttpClient();
        httpClientGrowth.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        HttpResponseMessage responseGrowth = await httpClientGrowth.GetAsync(url);
        if (!responseGrowth.IsSuccessStatusCode)
        {
            return "無法連接到網站，請稍後再試。";
        }

        string htmlContentGrowth = await responseGrowth.Content.ReadAsStringAsync();

        HtmlDocument documentGrowth = new HtmlDocument();
        documentGrowth.LoadHtml(htmlContentGrowth);

        // 尋找網頁中的通膨率數據
        var itemName = documentGrowth.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[1]/h1/a");
        var nodeGrowth = documentGrowth.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[3]/div[1]/span[1]");
        var nodedateGrowth = documentGrowth.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[2]/time");
        if (nodeGrowth != null && itemName != null)
        {
            GrowthRate = nodeGrowth.InnerText.Trim();
            itemNameLabel.Text = itemName.InnerText.Trim();
            string currentDate = nodedateGrowth.InnerText.Trim();  // 當前資料時間
            return $"：{GrowthRate}% - {currentDate}月";
        }
        else
        {
            return $"找不到{itemNameLabel.Text}資料。";
        }
    }

    private async Task LoadETFListToPicker(Picker StockPickerPicker)
    {
        List<string> StockPickerItems = new List<string>();
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using (var connection = new MySqlConnection(connectionString))
        {
            string query = "SELECT CONCAT(name, ' (', symbol, ')') AS DisplayText FROM etf_list ORDER BY name;";
            await connection.OpenAsync();

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    string displayText = reader.GetString("DisplayText");
                    StockPickerItems.Add(displayText);
                }
            }
        }

        // 更新 Picker 選單
        StockPickerPicker.ItemsSource = StockPickerItems;
    }

    private async Task InsertETFData(string name, string symbol, string type)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

        // 檢查資料庫中是否存在相同的 returnsymbol
        string checkQuery = "SELECT COUNT(*) FROM etf_list WHERE symbol = @symbol";
        using (var checkCommand = new MySqlCommand(checkQuery, connection))
        {
            checkCommand.Parameters.AddWithValue("@symbol", symbol);
            long count = (long)await checkCommand.ExecuteScalarAsync();

            if (count > 0)
            {
                return; // 結束方法，不執行插入操作
            }
        }

        // 插入新資料
        string insertQuery = "INSERT INTO etf_list (name, symbol, type) VALUES (@name, @symbol, @type)";
        using (var insertCommand = new MySqlCommand(insertQuery, connection))
        {
            insertCommand.Parameters.AddWithValue("@name", name);
            insertCommand.Parameters.AddWithValue("@symbol", symbol);
            insertCommand.Parameters.AddWithValue("@type", type);

            await insertCommand.ExecuteNonQueryAsync();
        }
        }
    }
    private bool IsNumeric(string text)
    {
        return double.TryParse(text, out _); // 回傳 true 如果可以轉換為數字，否則 false
    }
    private CancellationTokenSource _debounceTokenSource;
    private async void OnETFTextChanged(object sender,EventArgs e)
    {
        // 如果之前有進行中的操作，則取消它
        _debounceTokenSource?.Cancel();
        // 創建新的 CancellationTokenSource 來處理延遲
        _debounceTokenSource = new CancellationTokenSource();
        var token = _debounceTokenSource.Token;
        try
        {
            // 延遲 2 秒等待輸入停止
            await Task.Delay(3000, token);

            // 如果未被取消，執行操作
            if (!token.IsCancellationRequested)
            {
                var ETFText = sender as Entry;
                if (ETFText.Text.Length >= 4)
                {
                    if (IsNumeric(ETFText.Text))
                    {
                        enterETF.Text = ETFText.Text.Trim();
                        try
                        {
                            // 呼叫 Yahoo Finance API
                            string apiUrl = $"https://yahoo-finance166.p.rapidapi.com/api/stock/get-price?region=TW&symbol={enterETF.Text}.TW";

                            using var client = new HttpClient();
                            var request = new HttpRequestMessage
                            {
                                Method = HttpMethod.Get,
                                RequestUri = new Uri(apiUrl),
                                Headers =
    {
        { "x-rapidapi-key", apiKey },
        { "x-rapidapi-host", "yahoo-finance166.p.rapidapi.com" },
    },
                            };

                            using var response = await client.SendAsync(request);
                            if (response.IsSuccessStatusCode)
                            {
                                var body = await response.Content.ReadAsStringAsync();

                                JObject jsonObj = JObject.Parse(body);

                                var returnName = jsonObj["quoteSummary"]?["result"]?[0]?["price"]?["longName"]?.ToString();
                                var returnsymbol = jsonObj["quoteSummary"]?["result"]?[0]?["price"]?["symbol"]?.ToString();
                                var returntype = jsonObj["quoteSummary"]?["result"]?[0]?["price"]?["quoteType"]?.ToString();

                                if (!string.IsNullOrEmpty(returnName))
                                {
                                    enterETF.Text = $"{returnName} ({returnsymbol})";
                                    if (returntype == "ETF")
                                    {
                                        await InsertETFData(returnName, returnsymbol, returntype);
                                        Getpercent(returnsymbol);
                                        await LoadETFListToPicker(StockPicker);
                                    }
                                }
                                else
                                {
                                    enterETF.Text = "無法取得資料";
                                }
                            }
                            else
                            {
                                enterETF.Text = "無法取得資料！";
                            }

                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("錯誤", $"無法取得資料：{ex.Message}", "確定");
                        }
                    }

                }
            }
        }
        catch (TaskCanceledException)
        {
            enterETF.Text="正在等待輸入完成...";
        }
        
    }
    public HtmlNode itemName { get; set; }
    string returnPercentage;
    private async void Getpercent(string stockSymbol)
    {

        try
        {
            // 取得代號

            if (string.IsNullOrEmpty(stockSymbol))
            {
                await DisplayAlert("錯誤", "找不到對應的股票代號", "確定");
                return;
            }

            string url = $"https://tw.stock.yahoo.com/quote/{stockSymbol}/performance";  // 台灣 CPI 網頁

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            HttpResponseMessage responsenow = await httpClient.GetAsync(url);
            if (!responsenow.IsSuccessStatusCode)
            {
                ReturnOnInvestmentLabel.Text = "無法連接到網站，請稍後再試。";
            }
            else
            {
                string htmlContent = await responsenow.Content.ReadAsStringAsync();

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(htmlContent);

                // 尋找網頁中的通膨率數據
                itemName = document.DocumentNode.SelectSingleNode("//*[@id=\"main-2-QuotePerformance-Proxy\"]/div/div/div[1]/div[2]/div/div[2]/div/div/ul/li[1]/div/div[2]/span/text()");
                if (itemName != null)
                {
                    ReturnOnInvestmentLabel.Text = $"投資報酬率： {itemName.InnerText.Trim()}";
                    if (RetirementAgeEntry.Text != "" && ChildEducationBudgetEntry.Text != "" && MarriageBudgetEntry.Text != "" && HouseBudgetEntry.Text != "")
                    {
                        await DisplayGoalCompletionAsync(_username);
                    }
                }
                else
                {
                    ReturnOnInvestmentLabel.Text = "無法取得投資報酬率";
                    // 呼叫 Yahoo Finance API
                    string apiUrl = $"https://yahoo-finance166.p.rapidapi.com/api/stock/get-fund-performance?symbol={stockSymbol}";

                    using var client = new HttpClient();
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(apiUrl),
                        Headers =
    {
        { "x-rapidapi-key", apiKey },
        { "x-rapidapi-host", "yahoo-finance166.p.rapidapi.com" },
    },
                    };

                    using var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();

                        // 將 JSON 資料轉換成物件
                        JObject jsonObj = JObject.Parse(body);

                        // 提取報酬率百分比
                        returnPercentage = jsonObj["quoteSummary"]?["result"]?[0]?["fundPerformance"]?["performanceOverview"]?["oneYearTotalReturn"]?["fmt"]?.ToString();
                        var returnDate = jsonObj["quoteSummary"]?["result"]?[0]?["fundPerformance"]?["performanceOverview"]?["asOfDate"]?["fmt"]?.ToString();

                        if (!string.IsNullOrEmpty(returnPercentage))
                        {
                            ReturnOnInvestmentLabel.Text = $"{returnDate} 投資報酬率：{returnPercentage}";
                            if(RetirementAgeEntry.Text != "" && ChildEducationBudgetEntry.Text != "" && MarriageBudgetEntry.Text != "" && HouseBudgetEntry.Text != "")
                            {
                                await DisplayGoalCompletionAsync(_username);
                            }
                        }
                        else
                        {
                            ReturnOnInvestmentLabel.Text = "無法取得投資報酬率";
                        }
                    }
                }
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法取得資料：{ex.Message}", "確定");
        }
        finally
        {

        }
    }
    private async void OnStockPickerSelectionChanged(object sender, EventArgs e)
    {
        string selectedStock;
        if (e != null)
        {
            var picker = sender as Picker;
            selectedStock = picker.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedStock))
            {
                await DisplayAlert("錯誤", "請選擇股票或 ETF", "確定");
                return;
            }
            string stockSymbol = GetStockSymbol(selectedStock);
            Getpercent(stockSymbol);
        }
        
    
    }

    private string GetStockSymbol(string selectedStock)
    {
        int startIndex = selectedStock.IndexOf('(');
        int endIndex = selectedStock.IndexOf(')');
        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            return selectedStock.Substring(startIndex + 1, endIndex - startIndex - 1); // 取得 () 中的文字
        }
        return "無法提取代碼";
    }




    private async void OnGrowthRateLabelClicked(object sender, EventArgs e)
    {
        string GrowthRateLabelRate = await GetGrowthRateLabelFromWebAsync();
        GrowthRateLabel.Text = GrowthRateLabelRate;
    }
    int currentAge;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (!string.IsNullOrEmpty(_username))
            {
                int currentAge = await GetCurrentAgeAsync(_username);
                await LoadFinancialGoalsAsync();
                await LoadETFListToPicker(StockPicker);
                // 當資料來源不為空時，隨機選擇一個選項

                    // 隨機選擇一個選項
                    Random random = new Random();
                    int randomIndex = random.Next(StockPicker.ItemsSource.Count);
                    StockPicker.SelectedIndex = randomIndex; // 自動設定隨機選定項目
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"無法載入年齡資料：{ex.Message}", "確定");
        }
    }

    private async Task LoadFinancialGoalsAsync()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string query = "SELECT * FROM financial_goals WHERE username = @username;";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", Preferences.Get("SavedUsername", ""));

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            HouseBudgetEntry.Text = reader["house_budget"].ToString();
            HouseAgeEntry.Text = reader["house_age"].ToString();
            MarriageBudgetEntry.Text = reader["marriage_budget"].ToString();
            MarriageAgeEntry.Text = reader["marriage_age"].ToString();
            ChildEducationBudgetEntry.Text = reader["child_education_budget"].ToString();
            ChildEducationAgeEntry.Text = reader["child_education_age"].ToString();
            RetirementBudgetEntry.Text = reader["retirement_budget"].ToString();
            RetirementAgeEntry.Text = reader["retirement_age"].ToString();
        }
    }


    private async Task<int> GetCurrentAgeAsync(string username)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        string query = "SELECT birthday FROM financial_data WHERE Username = @username;";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", username);

        var result = await command.ExecuteScalarAsync();

        if (result != null && DateTime.TryParse(result.ToString(), out DateTime birthday))
        {
            DateTime today = DateTime.Today;
            currentAge = today.Year - birthday.Year;
            if (birthday.Date > today.AddYears(-currentAge)) currentAge--;  // 如果尚未過生日，年齡減 1
        }
        else
        {
            throw new Exception("無法取得生日資料");
        }

        return currentAge;
    }


    private void OnAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = sender as Entry;
        if (entry == null || string.IsNullOrWhiteSpace(entry.Text)) return;

        // 將數字格式化成貨幣格式
        if (decimal.TryParse(entry.Text.Replace(",", "").Replace("NT$", ""), out decimal amount))
        {
            // 自訂繁體中文的千分位格式
            var customFormat = new CultureInfo("zh-TW", false).NumberFormat;
            customFormat.CurrencySymbol = "NT$";  // 保持貨幣符號
            customFormat.CurrencyGroupSeparator = ",";  // 設定千分位符號為逗號
            customFormat.CurrencyDecimalDigits = 0;  // 無小數點

            entry.Text = string.Format(customFormat, "{0:C}", amount);
        }
    }
    // 當使用者輸入預計年齡時自動計算年齡差
    private async void OnAgeTextChanged(object sender, TextChangedEventArgs e)
    {

        int CurrentAge = await GetCurrentAgeAsync(_username);
        if (int.TryParse(((Entry)sender).Text, out int targetAge))
        {
            if(sender is Entry entry)
            {
                
                string styleId = entry.StyleId;
                int ageDiff = targetAge - CurrentAge;
                if (!string.IsNullOrEmpty(styleId))
                {
                    switch (styleId)
                    {
                        case "HouseAgeEntry":
                            if(ageDiff > 0)
                            {
                                HouseAgeDiffEntry.Text = ageDiff.ToString() + "年";
                            }
                            else
                            {
                                HouseAgeDiffEntry.Text = "年齡差不正確";
                            }
                            
                            break;
                        case "MarriageAgeEntry":
                            if(ageDiff > 0)
                                MarriageAgeDiffEntry.Text = ageDiff.ToString() + "年";
                            else
                                MarriageAgeDiffEntry.Text = "年齡差不正確";
                            break;
                        case "ChildEducationAgeEntry":
                            if(ageDiff > 0)
                                ChildEducationAgeDiffEntry.Text = ageDiff.ToString() + "年";
                            else
                                ChildEducationAgeDiffEntry.Text = "年齡差不正確";
                            break;
                        case "RetirementAgeEntry":
                            if (ageDiff > 0)
                                RetirementAgeDiffEntry.Text = ageDiff.ToString() + "年";
                            else
                                RetirementAgeDiffEntry.Text = "年齡差不正確";
                            break;
                    }
                }
            }
        }
    }

    // 送出按鈕事件
    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        try
        {
            string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // 檢查資料是否存在
            string checkQuery = "SELECT COUNT(*) FROM financial_goals WHERE username = @username;";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@username", _username);

            int recordCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            string query;

            if (recordCount > 0)
            {
                // 使用 UPDATE 更新
                query = @"
                UPDATE financial_goals
                SET house_budget = @house_budget, house_age = @house_age,
                    marriage_budget = @marriage_budget, marriage_age = @marriage_age,
                    child_education_budget = @child_education_budget, child_education_age = @child_education_age,
                    retirement_budget = @retirement_budget, retirement_age = @retirement_age
                WHERE username = @username;";
            }
            else
            {
                // 使用 INSERT 新增
                query = @"
                INSERT INTO financial_goals (username, house_budget, house_age, marriage_budget, marriage_age,
                                             child_education_budget, child_education_age, retirement_budget, retirement_age)
                VALUES (@username, @house_budget, @house_age, @marriage_budget, @marriage_age,
                        @child_education_budget, @child_education_age, @retirement_budget, @retirement_age);";
            }

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", _username);
            command.Parameters.AddWithValue("@house_budget", HouseBudgetEntry.Text.Replace("NT$", "").Replace(",", ""));
            command.Parameters.AddWithValue("@house_age", HouseAgeEntry.Text);
            command.Parameters.AddWithValue("@marriage_budget", MarriageBudgetEntry.Text.Replace("NT$", "").Replace(",", ""));
            command.Parameters.AddWithValue("@marriage_age", MarriageAgeEntry.Text);
            command.Parameters.AddWithValue("@child_education_budget", ChildEducationBudgetEntry.Text.Replace("NT$", "").Replace(",", ""));
            command.Parameters.AddWithValue("@child_education_age", ChildEducationAgeEntry.Text);
            command.Parameters.AddWithValue("@retirement_budget", RetirementBudgetEntry.Text.Replace("NT$", "").Replace(",", ""));
            command.Parameters.AddWithValue("@retirement_age", RetirementAgeEntry.Text);

            await command.ExecuteNonQueryAsync();

            await DisplayAlert("成功", "財務目標已儲存。", "確定");
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"儲存資料時出現問題：{ex.Message}", "確定");
        }
    }















    public async Task DisplayGoalCompletionAsync(string username)
    {
        var (birthday, cashSaving, fixedDeposit, investment) = await GetFinancialDataAsync(_username);
        double availableFunds = cashSaving + fixedDeposit + investment;
        int currentAge = DateTime.Now.Year - birthday.Year;
        if (HouseBudgetEntry.Text != null)
        {
            var houseBudget = HouseBudgetEntry.Text.Replace("NT$", "").Replace(",", "");
            var houseAge = HouseAgeEntry.Text;
            var marriageBudget = MarriageBudgetEntry.Text.Replace("NT$", "").Replace(",", "");
            var marriageAge = MarriageAgeEntry.Text;
            var childEducationBudget = ChildEducationBudgetEntry.Text.Replace("NT$", "").Replace(",", "");
            var childEducationAge = ChildEducationAgeEntry.Text;
            var retirementBudget = RetirementBudgetEntry.Text.Replace("NT$", "").Replace(",", "");
            var retirementAge = RetirementAgeEntry.Text;

            //通膨
            double inflationRatehouseCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(houseBudget), currentAge, Convert.ToInt32(houseAge), Convert.ToDouble(inflationRate) / 100);
            double inflationRatemarriageCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(marriageBudget), currentAge, Convert.ToInt32(marriageAge), Convert.ToDouble(inflationRate) / 100);
            double inflationRatechildEducationCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(childEducationBudget), currentAge, Convert.ToInt32(childEducationAge), Convert.ToDouble(inflationRate) / 100);
            double inflationRateretirementCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(retirementBudget), currentAge, Convert.ToInt32(retirementAge), Convert.ToDouble(inflationRate) / 100);

            HouseInflationLabel.Text = $"{inflationRatehouseCompletionRate:0.##}%";
            MarriageInflationLabel.Text = $"{inflationRatemarriageCompletionRate:0.##}%";
            ChildEducationInflationLabel.Text = $"{inflationRatechildEducationCompletionRate:0.##}%";
            RetirementInflationLabel.Text = $"{inflationRateretirementCompletionRate:0.##}%";

            //薪資成長率
            double GrowthRateRatehouseCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(houseBudget), currentAge, Convert.ToInt32(houseAge), Convert.ToDouble(GrowthRate) / 100);
            double GrowthRateRatemarriageCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(marriageBudget), currentAge, Convert.ToInt32(marriageAge), Convert.ToDouble(GrowthRate) / 100);
            double GrowthRateRatechildEducationCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(childEducationBudget), currentAge, Convert.ToInt32(childEducationAge), Convert.ToDouble(GrowthRate) / 100);
            double GrowthRateRateretirementCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(retirementBudget), currentAge, Convert.ToInt32(retirementAge), Convert.ToDouble(GrowthRate) / 100);

            HouseIncomeGrowthLabel.Text = $"{GrowthRateRatehouseCompletionRate:0.##}%";
            MarriageIncomeGrowthLabel.Text = $"{GrowthRateRatemarriageCompletionRate:0.##}%";
            ChildEducationIncomeGrowthLabel.Text = $"{GrowthRateRatechildEducationCompletionRate:0.##}%";
            RetirementIncomeGrowthLabel.Text = $"{GrowthRateRateretirementCompletionRate:0.##}%";

            //薪資成長率
            double itemNameRatehouseCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(houseBudget), currentAge, Convert.ToInt32(houseAge), Convert.ToDouble(itemName.InnerText.Trim().Replace("%", "")) / 100);
            double itemNameRatemarriageCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(marriageBudget), currentAge, Convert.ToInt32(marriageAge), Convert.ToDouble(itemName.InnerText.Trim().Replace("%", "")) / 100);
            double itemNameRatechildEducationCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(childEducationBudget), currentAge, Convert.ToInt32(childEducationAge), Convert.ToDouble(itemName.InnerText.Trim().Replace("%", "")) / 100);
            double itemNameRateretirementCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(retirementBudget), currentAge, Convert.ToInt32(retirementAge), Convert.ToDouble(itemName.InnerText.Trim().Replace("%", "")) / 100);

            HouseInvestmentReturnLabel.Text = $"{itemNameRatehouseCompletionRate:0.##}%";
            MarriageInvestmentReturnLabel.Text = $"{itemNameRatemarriageCompletionRate:0.##}%";
            ChildEducationInvestmentReturnLabel.Text = $"{itemNameRatechildEducationCompletionRate:0.##}%";
            RetirementInvestmentReturnLabel.Text = $"{itemNameRateretirementCompletionRate:0.##}%";
        }
    }

    public double CalculateGoalCompletioninflationRate(double availableFunds, double goalAmount, int currentAge, int targetAge, double inflationRate)
    {
        double monthsRemaining = availableFunds * (1 - inflationRate);
        double Age = (targetAge - currentAge) * 12;
        double monthlyContribution = monthsRemaining * Age;
        double completionRate = (monthlyContribution / goalAmount) * 1; // 達成率百分比
        return completionRate;
    }

    public async Task<(DateTime birthday, double cashSaving, double fixedDeposit, double investment)> GetFinancialDataAsync(string username)
    {
        string query = "SELECT Birthday, CashSaving, FixedDeposit, Investment FROM financial_data WHERE username = @username";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@username", username);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        DateTime birthday = reader.GetDateTime("Birthday");
                        double cashSaving = reader.GetDouble("CashSaving");
                        double fixedDeposit = reader.GetDouble("FixedDeposit");
                        double investment = reader.GetDouble("Investment");
                        return (birthday, cashSaving, fixedDeposit, investment);
                    }
                }
            }
        }
        return (DateTime.MinValue, 0, 0, 0);
    }

    public async Task<(double houseBudget, int houseAge, double marriageBudget, int marriageAge, double childEducationBudget, int childEducationAge, double retirementBudget, int retirementAge)> GetFinancialGoalsAsync(string username)
    {
        string query = "SELECT house_budget, house_age, marriage_budget, marriage_age, child_education_budget, child_education_age, retirement_budget, retirement_age FROM financial_goals WHERE username = @username";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@username", username);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        double houseBudget = reader.GetDouble("house_budget");
                        int houseAge = reader.GetInt32("house_age");
                        double marriageBudget = reader.GetDouble("marriage_budget");
                        int marriageAge = reader.GetInt32("marriage_age");
                        double childEducationBudget = reader.GetDouble("child_education_budget");
                        int childEducationAge = reader.GetInt32("child_education_age");
                        double retirementBudget = reader.GetDouble("retirement_budget");
                        int retirementAge = reader.GetInt32("retirement_age");
                        return (houseBudget, houseAge, marriageBudget, marriageAge, childEducationBudget, childEducationAge, retirementBudget, retirementAge);
                    }
                }
            }
        }
        return (0, 0, 0, 0, 0, 0, 0, 0);
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

}