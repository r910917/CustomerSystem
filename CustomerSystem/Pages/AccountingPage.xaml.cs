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

    private bool _isTransactionFormVisible = false;  // �����ܪ��A
    private bool _isCardDetailVisible = false;  // �����ܪ��A
    //private Transactiondata _selectedTransaction;
    private readonly DatabaseService _databaseService = new DatabaseService();
    private DateTime _selectedDate;
    private DateTime _shownDate;
    public Command<string> MenuCommand { get; }
    public ICommand DayTappedCommand { get; }  // �I�����ƥ�
    public ICommand OnShownDateChangedCommand { get; }  // ��ܤ�����ܨƥ�
    public ObservableCollection<Transactiondata> Transactions { get; set; } = new ObservableCollection<Transactiondata>();
    public string SelectedDateText => $"��ܤ���G{_selectedDate:yyyy/MM/dd}";
    public string ShownMonthText => $"��ܤ���G{_shownDate:yyyy/MM}";
    private string _username = Preferences.Get("SavedUsername", string.Empty);
    private List<string> _defaultCategories = new List<string> { "�\��", "��q", "�T��", "�ʪ�", "��L", "�~��" };

    public AccountingPage()
	{
		InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        // ��l�ƩR�O
        DayTappedCommand = new Command<DateTime>(OnDayTapped);

        BindingContext = this;

        // �w�]��ܷ�e����M���
        _selectedDate = DateTime.Today;
        _shownDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    }

    public EventCollection Events { get; set; }
    private async Task LoadTransactionDatesAsync()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // �d�ߩҦ��������ƪ����
        string query = "SELECT DISTINCT date FROM transactions WHERE username = @username;";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", _username);

        using var reader = await command.ExecuteReaderAsync();
        var events = new EventCollection();  // �ϥ� EventCollection

        while (await reader.ReadAsync())
        {
            DateTime date = reader.GetDateTime("date");
            events.Add(date, null);
        }

        TransactionCalendar.Events = events;  // �N�ƥ�C��j�w����
    }
    private async Task LoadFinancialSummaryAsync()
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // ��l�l�B�d��
        string initialBalanceQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '�]�w' AND category = '��l�l�B�]�w';";
        using var initialBalanceCommand = new MySqlCommand(initialBalanceQuery, connection);
        initialBalanceCommand.Parameters.AddWithValue("@username", _username);
        InitialBalance = Convert.ToDouble(await initialBalanceCommand.ExecuteScalarAsync());

        // ������J�`�B
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '���J';";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        double totalIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // �����X�`�B
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '��X';";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        double totalExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // �p���`�겣
        double totalAssets = totalIncome - totalExpense;
        double NetIncome = totalIncome - totalExpense;
        double FinalBalance = InitialBalance + NetIncome;
        // �p���`����M�l�B
        NetIncomeLabel.Text = NetIncome < 0 ? "��X" : "���J";
        FinalBalanceLabel.Text = $"{FinalBalance:C0}";
        FinalBalanceLabel.TextColor = FinalBalance < 0 ? Colors.Red : Colors.Blue;

        // ��s UI
        TotalAssetsLabel.Text = $"{totalAssets:C0}";  // ��ܪ��B�榡
        TotalAssetsLabel.TextColor = totalAssets < 0 ? Colors.Red : Colors.Blue;  // �]�w��r�C��
        TotalAssetsLabelFirst.Text = $"{totalAssets:C0}";  // ��ܪ��B�榡
        TotalAssetsLabelFirst.TextColor = totalAssets < 0 ? Colors.Red : Colors.Blue;  // �]�w��r�C��
        totalIncomeLabel.Text = $"{totalIncome:C0}";  // ��ܪ��B�榡
        totalExpenseLabel.Text = $"{totalExpense:C0}";  // ��ܪ��B�榡
        InitialBalanceLabel.Text = $"{InitialBalance:C0}";

        SelectedIncomeLabel.Text = $"{totalIncome:C0}";  // �L�p���I
        SelectedExpenseLabel.Text = $"{totalExpense:C0}";  // �L�p���I

        SelectedNetIncomeLabel.Text = NetIncome < 0 ? "��X" : "���J";
        SelectedNetIncomeLabel.TextColor = NetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{NetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = NetIncome < 0 ? Colors.Red : Colors.Blue;
    }

    private async void OnMenuButtonClicked(object sender, EventArgs e)
    {

        if (sender is Button button && button.BindingContext is Transactiondata transaction)
        {
            // �T�O transaction ���D��
            if (transaction == null)
            {
                await DisplayAlert("���~", "�L�k���o�����ơC", "�T�w");
                return;
            }

            string action = await DisplayActionSheet("��ܾާ@", "����", null, "�s��", "�R��");
            switch (action)
            {
                case "�s��":
                    ShowEditPopup(transaction);
                    break;
                case "�R��":
                    bool confirm = await DisplayAlert("�R���T�{", $"�O�_�n�R���ӵ������ơH\n����G{transaction.Date:yyyy/MM/dd}\n�����G{transaction.Type}\n���B�G{transaction.Amount:C}", "�O", "�_");
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
            await DisplayAlert("���~", "�L�k���o�����ơC", "�T�w");
        }
    }
    int transID;
    private void ShowEditPopup(Transactiondata transaction)
    {
        // ��R�{��������
        EditAmountEntry.Text = transaction.Amount.ToString("0.00");
        EditNoteEditor.Text = transaction.Note;
        transID = transaction.Id;

        EditTransactionPopup.IsVisible = true;  // ��ܼu�X����
    }
    private void OnCancelEditClicked(object sender, EventArgs e)
    {
        EditTransactionPopup.IsVisible = false;  // ���üu�X����
    }
    private async void OnUpdateTransactionClicked(object sender, EventArgs e)
    {
        // ��s��Ʈw
        string query = "UPDATE transactions SET amount = @amount, note = @note WHERE id = @id;";
        var parameters = new Dictionary<string, object>
    {
        { "@amount", EditAmountEntry.Text },
        { "@note", EditNoteEditor.Text },
        { "@id", transID }
    };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);
        //await _databaseService.InsertTransactionAsync(transaction);

        EditTransactionPopup.IsVisible = false;  // ���üu�X����
        await DisplayAlert("���\", "�����Ƥw��s�C", "�T�w");

        OnShowAllDataClicked(null, null);  // ���s���J���
    }

    private async Task DeleteTransactionAsync(int transactionId,string username,DateTime SelDate)
    {
        string query = "DELETE FROM transactions WHERE id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", transactionId } };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);
        await _databaseService.DeleteTransactionAsync(transactionId, username, SelDate);
    }
    // ���J���O�M��
    private async Task LoadCategoriesAsync()
    {
        try
        {
            string query = "SELECT name FROM category WHERE username = @username;";
            var parameters = new Dictionary<string, object> { { "@username", _username } };

            var categories = await _databaseService.ExecuteQueryAsync(query, parameters, reader => reader.GetString("name"));

            // �p�G��Ʈw�S�����O�A�ϥιw�]���O
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
            await DisplayAlert("���~", $"�L�k���J���O�G{ex.Message}", "�T�w");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategoriesAsync();  // ������ܮɸ��J���O�M��
        await LoadFinancialSummaryAsync(); // �e�����J�ɧ�s�]�ȺK�n
        await LoadTransactionDatesAsync();  // ���J�������ƪ����
        OnShowAllDataClicked(null, null);
    }


    private void OnToggleTransactionFormClicked(object sender, EventArgs e)
    {
        // ���������ܪ��A
        _isTransactionFormVisible = !_isTransactionFormVisible;
        TransactionForm.IsVisible = _isTransactionFormVisible;

        // ��s���s��r
        ToggleTransactionFormButton.Text = _isTransactionFormVisible ? "����" : "�s�W�O�b";
        ToggleTransactionFormButton.BackgroundColor = _isTransactionFormVisible ? Color.FromArgb("#90FF5252") : Color.FromArgb("#904CAF50");
    }

    private void OnCardDetailClicked(object sender, EventArgs e)
    {
        // ���������ܪ��A
        _isCardDetailVisible = !_isCardDetailVisible;
        CardDetail.IsVisible = _isCardDetailVisible;

        // ��s���s��r
        CardDetailButton.Text = _isCardDetailVisible ? "���òέp" : "�˵��έp";
        CardDetailButton.BackgroundColor = _isCardDetailVisible ? Color.FromArgb("#90FF5252") : Color.FromArgb("#90864EAD");
    }

    private async void OnSubmitTransactionClicked(object sender, EventArgs e)
    {
        // ���ҿ�J���
        if (TypePicker.SelectedItem == null || string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            await DisplayAlert("���~", "�п�������ÿ�J���B�C", "�T�w");
            return;
        }
        string balanceText = FinalBalanceLabel.Text.Replace("NT","").Replace("$", "").Replace(",", "").Trim();
        // �N����ഫ�÷s�W���Ʈw
        var transaction = new Transactiondata
        {
            Username = _username,
            Type = TypePicker.SelectedItem.ToString(),
            Category = string.IsNullOrWhiteSpace(CategoryPicker.SelectedItem?.ToString()) ? null : CategoryPicker.SelectedItem?.ToString(),
            Amount = decimal.Parse(AmountEntry.Text),
            Date = TransactionCalendar.SelectedDate ?? DateTime.Today,  // �w�]��������
            Balance = Convert.ToDouble(balanceText)
        };

        await AddTransactionAsync(transaction);
        await DisplayAlert("���\", "�O�b��Ƥw�s�W�I", "�T�w");

        // �M�ſ�J���
        TypePicker.SelectedItem = null;
        CategoryPicker.SelectedItem = null;
        AmountEntry.Text = string.Empty;
        NoteEntry.Text = string.Empty;

        // ���s�[�����
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

    // �ϥΪ��I������
    private async void OnDayTapped(DateTime selectedDate)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // �d�߸Ӥ�������J�`�B
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '���J' AND date = @date;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@date", selectedDate.Date);
        _selectedIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // �d�߸Ӥ������X�`�B
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '��X' AND date = @date;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@date", selectedDate.Date);
        _selectedExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // �p�����쪺�`����
        _selectedNetIncome = _selectedIncome - _selectedExpense;

        // ��s UI
        SelectedIncomeLabel.Text = $"{_selectedIncome:C0}";  // �L�p���I
        SelectedExpenseLabel.Text = $"{_selectedExpense:C0}";  // �L�p���I

        SelectedNetIncomeLabel.Text = _selectedNetIncome < 0 ? "��X" : "���J";
        SelectedNetIncomeLabel.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{_selectedNetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        _selectedDate = selectedDate;
        DateLabel.Text = $"��ܤ���G{_selectedDate:yyyy/MM/dd}";
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

            // ��ܴ��ܰT��
            if (transactions.Count == 0)
            {
                
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("���~", $"�L�k���J��ơG{ex.Message}", "�T�w");
        }
    }

    private async void OnShowMonthDataClicked(object sender, EventArgs e)
    {
        int selectedYear = TransactionCalendar.ShownDate.Year;  // ���o�����ܪ��~��
        int selectedMonth = TransactionCalendar.ShownDate.Month;  // ���o�����ܪ����
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
                await DisplayAlert("����", $"�ҿ��� {selectedYear} �~ {selectedMonth} �� �L����O���C", "�T�w");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("���~", $"�L�k���J��ơG{ex.Message}", "�T�w");
        }
    }

    private async Task LoadMonthlyFinancialSummaryAsync(int year, int month)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // ������J�`�B�d��
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '���J' AND YEAR(date) = @year AND MONTH(date) = @month;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@year", year);
        incomeCommand.Parameters.AddWithValue("@month", month);
        _selectedIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // �����X�`�B�d��
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '��X' AND YEAR(date) = @year AND MONTH(date) = @month;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@year", year);
        expenseCommand.Parameters.AddWithValue("@month", month);
        _selectedExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // �p���`����
        _selectedNetIncome = _selectedIncome - _selectedExpense;

        // ��s UI
        SelectedIncomeLabel.Text = $"{_selectedIncome:C0}";  // �L�p���I
        SelectedExpenseLabel.Text = $"{_selectedExpense:C0}";  // �L�p���I

        SelectedNetIncomeLabel.Text = _selectedNetIncome < 0 ? "��X" : "���J";
        SelectedNetIncomeLabel.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
        SelectedNetIncomeLabelNum.Text = $"{_selectedNetIncome:C0}";
        SelectedNetIncomeLabelNum.TextColor = _selectedNetIncome < 0 ? Colors.Red : Colors.Blue;
    }

    private async void OnShowYearDataClicked(object sender, EventArgs e)
    {
        int selectedYear = TransactionCalendar.ShownDate.Year;  // ���o�����ܪ��~��
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
                await DisplayAlert("����", $"�ҿ�~�� {selectedYear} �L����O���C", "�T�w");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("���~", $"�L�k���J��ơG{ex.Message}", "�T�w");
        }
    }

    private async Task LoadYearlyFinancialSummaryAsync(int year)
    {
        string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // �~�����J�`�B�d��
        string incomeQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '���J' AND YEAR(date) = @year;";
        using var incomeCommand = new MySqlCommand(incomeQuery, connection);
        incomeCommand.Parameters.AddWithValue("@username", _username);
        incomeCommand.Parameters.AddWithValue("@year", year);
        _selectedIncome = Convert.ToDouble(await incomeCommand.ExecuteScalarAsync());

        // �~����X�`�B�d��
        string expenseQuery = "SELECT IFNULL(SUM(amount), 0) FROM transactions WHERE username = @username AND type = '��X' AND YEAR(date) = @year;";
        using var expenseCommand = new MySqlCommand(expenseQuery, connection);
        expenseCommand.Parameters.AddWithValue("@username", _username);
        expenseCommand.Parameters.AddWithValue("@year", year);
        _selectedExpense = Convert.ToDouble(await expenseCommand.ExecuteScalarAsync());

        // �p���`����
        _selectedNetIncome = _selectedIncome - _selectedExpense;

        // ��s UI
        SelectedIncomeLabel.Text = $"{_selectedIncome:C0}";  // �L�p���I
        SelectedExpenseLabel.Text = $"{_selectedExpense:C0}";  // �L�p���I

        SelectedNetIncomeLabel.Text = _selectedNetIncome < 0 ? "��X" : "���J";
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

                // �K�[�Ѽ�
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
            await LoadTransactionDatesAsync();  // ���J�������ƪ����
            TransactionList.ItemsSource = transactions;  // �N��Ƹj�w�� UI
            }
            catch (Exception ex)
            {
                await DisplayAlert("���~", $"�L�k���J��ơG{ex.Message}", "�T�w");
            }
        }

    private async void OnSetInitialBalanceClicked(object sender, EventArgs e)
    {
        // �u�X��J��ܮ����ϥΪ̿�J��l�l�B
        string result = await DisplayPromptAsync("��l�l�B�]�w", "�п�J��l�l�B���B�G", "�T�w", "����", keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        if (!decimal.TryParse(result, out decimal initialBalance) || initialBalance < 0)
        {
            await DisplayAlert("���~", "�п�J���T�����B�C", "�T�w");
            return;
        }
        string balanceText = FinalBalanceLabel.Text.Replace("NT", "").Replace("$", "").Replace(",", "").Trim();
        // �إߪ�l�l�B�]�w�� TransactionData
        var transaction = new Transactiondata
        {
            Username = _username,
            Type = "�]�w",  // �]�w����
            Category = "��l�l�B�]�w",  // ���O�g "��l�l�B�]�w"
            Amount = initialBalance,
            InitialBalance = initialBalance,
            Date = DateTime.Today,  // �w�]������
            Note = "�]�w��l�l�B",
            Balance = Convert.ToDouble(balanceText)
        };

        await AddTransactionAsync(transaction);
        await DisplayAlert("���\", "��l�l�B�w���\�]�w�I", "�T�w");

        // ���s�[����ƥH��s UI
        OnShowAllDataClicked(null, null);
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
    //��������
}