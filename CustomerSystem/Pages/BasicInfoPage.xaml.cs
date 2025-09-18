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
            DisplayAlert("���~", "�L�k���o�ϥΪ̱b��", "�T�w");
        }
    }

    
    // �q��ƮwŪ����ƨö�R��l
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
            // �Ĥ@�Ӱϰ���
            FullNameEntry.Text = reader["FullName"].ToString();
            NicknameEntry.Text = reader["Nickname"].ToString();
            EmailEntry.Text = reader["Email"].ToString();
            PhoneEntry.Text = reader["Phone"].ToString();
            AddressEntry.Text = reader["Address"].ToString();
        }
    }

    // �s��ӤH���
    private void OnEditPersonalInfo01Clicked(object sender, EventArgs e)
    {
        _isEditingPersonalInfo = !_isEditingPersonalInfo;
        SetPersonalInfoReadOnly(!_isEditingPersonalInfo);
        UpdatePersonalInfo01Button.IsVisible = _isEditingPersonalInfo;
        EditPersonalInfo01Button.Text = _isEditingPersonalInfo ? "����" : "�s��";
        EditPersonalInfo01Button.BackgroundColor = _isEditingPersonalInfo ? Colors.Red : Colors.Blue;

        if (!_isEditingPersonalInfo)  // �Y�I�������A���s�[�����
        {
            LoadUserData();
        }
    }


    // �]�w�ӤH��ƿ�J�ت��A
    private void SetPersonalInfoReadOnly(bool isReadOnly)
    {
        FullNameEntry.IsReadOnly = isReadOnly;
        NicknameEntry.IsReadOnly = isReadOnly;
        EmailEntry.IsReadOnly = isReadOnly;
        PhoneEntry.IsReadOnly = isReadOnly;
        AddressEntry.IsReadOnly = isReadOnly;

        // �]�m�I���C��
        FullNameEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        NicknameEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        EmailEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        PhoneEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
        AddressEntry.BackgroundColor = isReadOnly ? Colors.LightGray : Colors.White;
    }
    // �x�s��s�᪺���
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
        await DisplayAlert("���\", "�ӤH��Ƥw��s�I", "�T�w");

        _isEditingPersonalInfo = false;
        UpdatePersonalInfo01Button.IsVisible = false;
        EditPersonalInfo01Button.Text = _isEditingPersonalInfo ? "����" : "�s��";
        EditPersonalInfo01Button.BackgroundColor = _isEditingPersonalInfo ? Colors.Red : Colors.Blue;
        SetPersonalInfoReadOnly(true);
        LoadUserData();  // ��s�᭫�s��ܷs���
    }


    // 2. �q financial_data ��Ū���z�]���
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
                BirthDatePicker.Date = birthDate;  // �N�ഫ�᪺ DateTime ��ȵ� DatePicker
            }
            else
            {
                BirthDatePicker.Date = DateTime.Today;  // �Y�L�ͤ��ơA�w�]������
            }
            OccupationEntry.Text = reader["Occupation"].ToString();
            // �榡�ƨC�리�J
            if (decimal.TryParse(reader["MonthlyIncome"]?.ToString(), out decimal monthlyIncome))
            {
                MonthlyIncomeEntry.Text = monthlyIncome.ToString("N0", CultureInfo.InvariantCulture);
            }

            // �榡�ƨC���X
            if (decimal.TryParse(reader["MonthlyExpense"]?.ToString(), out decimal monthlyExpense))
            {
                MonthlyExpenseEntry.Text = monthlyExpense.ToString("N0", CultureInfo.InvariantCulture);
            }

            // �榡���~���x�W�v
            if (decimal.TryParse(reader["SavingRate"]?.ToString(), out decimal savingRate))
            {
                SavingRateEntry.Text = savingRate.ToString("N2", CultureInfo.InvariantCulture);  // �ʤ���Φ���ܨ�p���I����
            }

            // �榡�Ƭ��s
            if (decimal.TryParse(reader["CashSaving"]?.ToString(), out decimal cashSaving))
            {
                CashSavingEntry.Text = cashSaving.ToString("N0", CultureInfo.InvariantCulture);
            }

            // �榡�ƫO��
            if (decimal.TryParse(reader["FixedDeposit"]?.ToString(), out decimal fixedDeposit))
            {
                FixedDepositEntry.Text = fixedDeposit.ToString("N0", CultureInfo.InvariantCulture);
            }

            // �榡�Ƨ��
            if (decimal.TryParse(reader["Investment"]?.ToString(), out decimal investment))
            {
                InvestmentEntry.Text = investment.ToString("N0", CultureInfo.InvariantCulture);
            }
        }
    }

    // 3. ��ϥΪ̤�ʿ�ܥX�ͦ~���ɦ۰ʭp��~��
    private void OnBirthDateSelected(object sender, DateChangedEventArgs e)
    {
        int age = CalculateAge(e.NewDate);
        AgeEntry.Text = age.ToString();  // ��s�~��
    }

    // 3. �p��~��
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    // 5. �s��z�]���
    private void OnEditFinancialInfoClicked(object sender, EventArgs e)
    {
        _isEditingFinancialInfo = !_isEditingFinancialInfo;
        SetFinancialInfoReadOnly(!_isEditingFinancialInfo);
        EditFinancialInfoButton.Text = _isEditingFinancialInfo ? "����" : "�s��";
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

        // �ˬd��ƪ��O�_���� `Username`
        string checkQuery = "SELECT COUNT(*) FROM financial_data WHERE Username = @Username";
        using var checkCommand = new MySqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@Username", _username);
        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());  // �^�Ǧ��

        string query;
        if (count == 0)  // �p�G���s�b�A�h���J�s���
        {
            query = @"
            INSERT INTO financial_data (Username, Occupation, Birthday, MonthlyIncome, MonthlyExpense, SavingRate, CashSaving, FixedDeposit, Investment)
            VALUES (@Username, @Occupation, @BirthDate, @MonthlyIncome, @MonthlyExpense, @SavingRate, @CashSaving, @FixedDeposit, @Investment)";
        }
        else  // �p�G�s�b�A�h��s���
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
        await DisplayAlert("���\", "�z�]��Ƥw��s�I", "�T�w");

        _isEditingFinancialInfo = false;
        UpdateFinancialInfoButton.IsVisible = false;
        EditFinancialInfoButton.Text = "�s��";
        EditFinancialInfoButton.BackgroundColor = Colors.LightGray;
        SetFinancialInfoReadOnly(true);
    }

    private void OnEntryCompleted(object sender, EventArgs e)
    {
        FormatCurrency((Entry)sender);  // ��ϥΪ̧�����J�ɮ榡�Ƭ��f���榡
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        FormatCurrency((Entry)sender);  // ���J�إ��h�J�I�ɮ榡�Ƭ��f���榡
    }

    private void FormatCurrency(Entry entry)
    {
        if (decimal.TryParse(entry.Text, out decimal value))
        {
            entry.Text = value.ToString("N0", CultureInfo.InvariantCulture);  // �榡�Ƭ� 1,000 �Φ�
        }
        else
        {
            entry.Text = string.Empty;  // �Y��J�L�ġA�M�ſ�J��
        }
    }

    private decimal ConvertCurrencyToDecimal(string currencyText)
    {
        if (string.IsNullOrWhiteSpace(currencyText))
            return 0;

        // �h���r���M�f���Ÿ�
        string cleanedText = currencyText.Replace(",", "").Replace("$", "").Trim();

        // �N�M�z�᪺�r���ഫ�� decimal
        if (decimal.TryParse(cleanedText, out decimal result))
            return result;
        else
            return 0;  // �p�G�ഫ���ѡA�^�� 0
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

    // �n�X���s�I���ƥ�
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool isLogout = await DisplayAlert("�n�X", "�T�w�n�n�X�ܡH", "�O", "�_");
        if (isLogout)
        {
            // �M���x�s���ϥΪ̸�T�ê�^�n�J����
            Preferences.Clear();  // �M�����n�]�w�����ϥΪ̸�T
            await Navigation.PushAsync(new MainPage());  // �ɦ^�n�J����
        }
    }

}