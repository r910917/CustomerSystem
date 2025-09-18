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

    private readonly string apiKey = "ec6af86faamsha2876b46c79bf20p1a58edjsnfd66f07e897f";  // �b RapidAPI ���o�� API Key
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
        string url = "https://www.macromicro.me/series/489/tw-cpi-yoy";  // �x�W CPI ����

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return "�L�k�s��������A�еy��A�աC";
        }

        string htmlContent = await response.Content.ReadAsStringAsync();

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(htmlContent);

        // �M����������q���v�ƾ�
        var node = document.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[3]/div[1]/span[1]");
        var nodedate = document.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[2]/time");
        if (node != null)
        {
            inflationRate = node.InnerText.Trim();
            string currentDate = nodedate.InnerText.Trim();  // ��e��Ʈɶ�
            return $"�G{inflationRate}% - {currentDate}��";
        }
        else
        {
            return "�䤣��q���v��ơC";
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
        string url = "https://www.macromicro.me/series/5307/tw-monthly-real-earnings-all-employees-industry-services-yoy";  // �x�W CPI ����

        HttpClient httpClientGrowth = new HttpClient();
        httpClientGrowth.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        HttpResponseMessage responseGrowth = await httpClientGrowth.GetAsync(url);
        if (!responseGrowth.IsSuccessStatusCode)
        {
            return "�L�k�s��������A�еy��A�աC";
        }

        string htmlContentGrowth = await responseGrowth.Content.ReadAsStringAsync();

        HtmlDocument documentGrowth = new HtmlDocument();
        documentGrowth.LoadHtml(htmlContentGrowth);

        // �M����������q���v�ƾ�
        var itemName = documentGrowth.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[1]/h1/a");
        var nodeGrowth = documentGrowth.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[3]/div[1]/span[1]");
        var nodedateGrowth = documentGrowth.DocumentNode.SelectSingleNode("//*[@id=\"panel\"]/main/div/div[1]/div/div[2]/time");
        if (nodeGrowth != null && itemName != null)
        {
            GrowthRate = nodeGrowth.InnerText.Trim();
            itemNameLabel.Text = itemName.InnerText.Trim();
            string currentDate = nodedateGrowth.InnerText.Trim();  // ��e��Ʈɶ�
            return $"�G{GrowthRate}% - {currentDate}��";
        }
        else
        {
            return $"�䤣��{itemNameLabel.Text}��ơC";
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

        // ��s Picker ���
        StockPickerPicker.ItemsSource = StockPickerItems;
    }

    private async Task InsertETFData(string name, string symbol, string type)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

        // �ˬd��Ʈw���O�_�s�b�ۦP�� returnsymbol
        string checkQuery = "SELECT COUNT(*) FROM etf_list WHERE symbol = @symbol";
        using (var checkCommand = new MySqlCommand(checkQuery, connection))
        {
            checkCommand.Parameters.AddWithValue("@symbol", symbol);
            long count = (long)await checkCommand.ExecuteScalarAsync();

            if (count > 0)
            {
                return; // ������k�A�����洡�J�ާ@
            }
        }

        // ���J�s���
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
        return double.TryParse(text, out _); // �^�� true �p�G�i�H�ഫ���Ʀr�A�_�h false
    }
    private CancellationTokenSource _debounceTokenSource;
    private async void OnETFTextChanged(object sender,EventArgs e)
    {
        // �p�G���e���i�椤���ާ@�A�h������
        _debounceTokenSource?.Cancel();
        // �Ыطs�� CancellationTokenSource �ӳB�z����
        _debounceTokenSource = new CancellationTokenSource();
        var token = _debounceTokenSource.Token;
        try
        {
            // ���� 2 ���ݿ�J����
            await Task.Delay(3000, token);

            // �p�G���Q�����A����ާ@
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
                            // �I�s Yahoo Finance API
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
                                    enterETF.Text = "�L�k���o���";
                                }
                            }
                            else
                            {
                                enterETF.Text = "�L�k���o��ơI";
                            }

                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("���~", $"�L�k���o��ơG{ex.Message}", "�T�w");
                        }
                    }

                }
            }
        }
        catch (TaskCanceledException)
        {
            enterETF.Text="���b���ݿ�J����...";
        }
        
    }
    public HtmlNode itemName { get; set; }
    string returnPercentage;
    private async void Getpercent(string stockSymbol)
    {

        try
        {
            // ���o�N��

            if (string.IsNullOrEmpty(stockSymbol))
            {
                await DisplayAlert("���~", "�䤣��������Ѳ��N��", "�T�w");
                return;
            }

            string url = $"https://tw.stock.yahoo.com/quote/{stockSymbol}/performance";  // �x�W CPI ����

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            HttpResponseMessage responsenow = await httpClient.GetAsync(url);
            if (!responsenow.IsSuccessStatusCode)
            {
                ReturnOnInvestmentLabel.Text = "�L�k�s��������A�еy��A�աC";
            }
            else
            {
                string htmlContent = await responsenow.Content.ReadAsStringAsync();

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(htmlContent);

                // �M����������q���v�ƾ�
                itemName = document.DocumentNode.SelectSingleNode("//*[@id=\"main-2-QuotePerformance-Proxy\"]/div/div/div[1]/div[2]/div/div[2]/div/div/ul/li[1]/div/div[2]/span/text()");
                if (itemName != null)
                {
                    ReturnOnInvestmentLabel.Text = $"�����S�v�G {itemName.InnerText.Trim()}";
                    if (RetirementAgeEntry.Text != "" && ChildEducationBudgetEntry.Text != "" && MarriageBudgetEntry.Text != "" && HouseBudgetEntry.Text != "")
                    {
                        await DisplayGoalCompletionAsync(_username);
                    }
                }
                else
                {
                    ReturnOnInvestmentLabel.Text = "�L�k���o�����S�v";
                    // �I�s Yahoo Finance API
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

                        // �N JSON ����ഫ������
                        JObject jsonObj = JObject.Parse(body);

                        // �������S�v�ʤ���
                        returnPercentage = jsonObj["quoteSummary"]?["result"]?[0]?["fundPerformance"]?["performanceOverview"]?["oneYearTotalReturn"]?["fmt"]?.ToString();
                        var returnDate = jsonObj["quoteSummary"]?["result"]?[0]?["fundPerformance"]?["performanceOverview"]?["asOfDate"]?["fmt"]?.ToString();

                        if (!string.IsNullOrEmpty(returnPercentage))
                        {
                            ReturnOnInvestmentLabel.Text = $"{returnDate} �����S�v�G{returnPercentage}";
                            if(RetirementAgeEntry.Text != "" && ChildEducationBudgetEntry.Text != "" && MarriageBudgetEntry.Text != "" && HouseBudgetEntry.Text != "")
                            {
                                await DisplayGoalCompletionAsync(_username);
                            }
                        }
                        else
                        {
                            ReturnOnInvestmentLabel.Text = "�L�k���o�����S�v";
                        }
                    }
                }
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("���~", $"�L�k���o��ơG{ex.Message}", "�T�w");
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
                await DisplayAlert("���~", "�п�ܪѲ��� ETF", "�T�w");
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
            return selectedStock.Substring(startIndex + 1, endIndex - startIndex - 1); // ���o () ������r
        }
        return "�L�k�����N�X";
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
                // ���ƨӷ������ŮɡA�H����ܤ@�ӿﶵ

                    // �H����ܤ@�ӿﶵ
                    Random random = new Random();
                    int randomIndex = random.Next(StockPicker.ItemsSource.Count);
                    StockPicker.SelectedIndex = randomIndex; // �۰ʳ]�w�H����w����
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("���~", $"�L�k���J�~�ָ�ơG{ex.Message}", "�T�w");
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
            if (birthday.Date > today.AddYears(-currentAge)) currentAge--;  // �p�G�|���L�ͤ�A�~�ִ� 1
        }
        else
        {
            throw new Exception("�L�k���o�ͤ���");
        }

        return currentAge;
    }


    private void OnAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = sender as Entry;
        if (entry == null || string.IsNullOrWhiteSpace(entry.Text)) return;

        // �N�Ʀr�榡�Ʀ��f���榡
        if (decimal.TryParse(entry.Text.Replace(",", "").Replace("NT$", ""), out decimal amount))
        {
            // �ۭq�c�餤�媺�d����榡
            var customFormat = new CultureInfo("zh-TW", false).NumberFormat;
            customFormat.CurrencySymbol = "NT$";  // �O���f���Ÿ�
            customFormat.CurrencyGroupSeparator = ",";  // �]�w�d����Ÿ����r��
            customFormat.CurrencyDecimalDigits = 0;  // �L�p���I

            entry.Text = string.Format(customFormat, "{0:C}", amount);
        }
    }
    // ��ϥΪ̿�J�w�p�~�֮ɦ۰ʭp��~�֮t
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
                                HouseAgeDiffEntry.Text = ageDiff.ToString() + "�~";
                            }
                            else
                            {
                                HouseAgeDiffEntry.Text = "�~�֮t�����T";
                            }
                            
                            break;
                        case "MarriageAgeEntry":
                            if(ageDiff > 0)
                                MarriageAgeDiffEntry.Text = ageDiff.ToString() + "�~";
                            else
                                MarriageAgeDiffEntry.Text = "�~�֮t�����T";
                            break;
                        case "ChildEducationAgeEntry":
                            if(ageDiff > 0)
                                ChildEducationAgeDiffEntry.Text = ageDiff.ToString() + "�~";
                            else
                                ChildEducationAgeDiffEntry.Text = "�~�֮t�����T";
                            break;
                        case "RetirementAgeEntry":
                            if (ageDiff > 0)
                                RetirementAgeDiffEntry.Text = ageDiff.ToString() + "�~";
                            else
                                RetirementAgeDiffEntry.Text = "�~�֮t�����T";
                            break;
                    }
                }
            }
        }
    }

    // �e�X���s�ƥ�
    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        try
        {
            string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // �ˬd��ƬO�_�s�b
            string checkQuery = "SELECT COUNT(*) FROM financial_goals WHERE username = @username;";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@username", _username);

            int recordCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            string query;

            if (recordCount > 0)
            {
                // �ϥ� UPDATE ��s
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
                // �ϥ� INSERT �s�W
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

            await DisplayAlert("���\", "�]�ȥؼФw�x�s�C", "�T�w");
        }
        catch (Exception ex)
        {
            await DisplayAlert("���~", $"�x�s��ƮɥX�{���D�G{ex.Message}", "�T�w");
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

            //�q��
            double inflationRatehouseCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(houseBudget), currentAge, Convert.ToInt32(houseAge), Convert.ToDouble(inflationRate) / 100);
            double inflationRatemarriageCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(marriageBudget), currentAge, Convert.ToInt32(marriageAge), Convert.ToDouble(inflationRate) / 100);
            double inflationRatechildEducationCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(childEducationBudget), currentAge, Convert.ToInt32(childEducationAge), Convert.ToDouble(inflationRate) / 100);
            double inflationRateretirementCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(retirementBudget), currentAge, Convert.ToInt32(retirementAge), Convert.ToDouble(inflationRate) / 100);

            HouseInflationLabel.Text = $"{inflationRatehouseCompletionRate:0.##}%";
            MarriageInflationLabel.Text = $"{inflationRatemarriageCompletionRate:0.##}%";
            ChildEducationInflationLabel.Text = $"{inflationRatechildEducationCompletionRate:0.##}%";
            RetirementInflationLabel.Text = $"{inflationRateretirementCompletionRate:0.##}%";

            //�~�ꦨ���v
            double GrowthRateRatehouseCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(houseBudget), currentAge, Convert.ToInt32(houseAge), Convert.ToDouble(GrowthRate) / 100);
            double GrowthRateRatemarriageCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(marriageBudget), currentAge, Convert.ToInt32(marriageAge), Convert.ToDouble(GrowthRate) / 100);
            double GrowthRateRatechildEducationCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(childEducationBudget), currentAge, Convert.ToInt32(childEducationAge), Convert.ToDouble(GrowthRate) / 100);
            double GrowthRateRateretirementCompletionRate = CalculateGoalCompletioninflationRate(availableFunds, Convert.ToDouble(retirementBudget), currentAge, Convert.ToInt32(retirementAge), Convert.ToDouble(GrowthRate) / 100);

            HouseIncomeGrowthLabel.Text = $"{GrowthRateRatehouseCompletionRate:0.##}%";
            MarriageIncomeGrowthLabel.Text = $"{GrowthRateRatemarriageCompletionRate:0.##}%";
            ChildEducationIncomeGrowthLabel.Text = $"{GrowthRateRatechildEducationCompletionRate:0.##}%";
            RetirementIncomeGrowthLabel.Text = $"{GrowthRateRateretirementCompletionRate:0.##}%";

            //�~�ꦨ���v
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
        double completionRate = (monthlyContribution / goalAmount) * 1; // �F���v�ʤ���
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
    //��������
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