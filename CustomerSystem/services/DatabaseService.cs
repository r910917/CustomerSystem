using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using CustomerSystem.Models;
using Microsoft.Maui.Controls;
using System.Data;

namespace CustomerSystem.services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        double finalBalance;
        // 初始化連接字串
        public DatabaseService()
        {
            _connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
        }

        // 通用查詢方法 (SELECT)
        public async Task<List<T>> ExecuteQueryAsync<T>(string query, Dictionary<string, object> parameters, Func<MySqlDataReader, T> mapFunction)
        {
            List<T> result = new List<T>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);

            // 添加參數
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(mapFunction((MySqlDataReader)reader));  // 使用 mapping 函數轉換資料列為物件
            }

            return result;
        }

        // 插入/更新/刪除方法 (非查詢)
        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);

            // 添加參數
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }
            }

            return await command.ExecuteNonQueryAsync();  // 回傳受影響的行數
        }

        public async Task<object> ExecuteScalarAsync(string query, Dictionary<string, object> parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }
            return await command.ExecuteScalarAsync();
        }

        // **新增交易並更新每日與每月結餘**
        public async Task InsertTransactionAsync(Transactiondata transaction)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transactionScope = await connection.BeginTransactionAsync();
            // 先檢查每日結餘是否存在
            string checkDailyExistQuery = @"SELECT COUNT(*) FROM daily_assets WHERE username = @username AND date = @date;";
            int dailyCount = Convert.ToInt32(await ExecuteScalarAsync(checkDailyExistQuery, new Dictionary<string, object>
                {
                    { "@username", transaction.Username },
                    { "@date", transaction.Date }
                }));
            // 先檢查每月結餘是否存在
            string checkMonthlyExistQuery = @"SELECT COUNT(*) FROM monthly_assets WHERE username = @username AND year = @Year AND month = @Month;";
            int monthlyCount = Convert.ToInt32(await ExecuteScalarAsync(checkMonthlyExistQuery, new Dictionary<string, object>
                {
                    { "@username", transaction.Username },
                    { "@Year", transaction.Date.Year },
                    { "@Month", transaction.Date.Month }
                }));
            try
            {
                // 更新每日結餘
                if (dailyCount > 0)
                {
                    string updateDailyQuery = @"
                    UPDATE daily_assets SET
                        total_income = total_income + IF(@type = '收入' OR @type = '設定', @amount, 0),
                        total_expense = total_expense + IF(@type = '支出', @amount, 0),
                        balance = IF(@type = '收入' OR @type = '設定', @balance+@amount, @balance-@amount)
                        WHERE username = @username AND date = @date;";
                    await ExecuteNonQueryAsync(updateDailyQuery, new Dictionary<string, object>
                    {
                        { "@username", transaction.Username },
                        { "@date", transaction.Date },
                        { "@amount", transaction.Amount },
                        { "@type", transaction.Type },
                        { "@balance", transaction.Balance}
                    });
                }
                else
                {
                    string insertDailyQuery = @"
                    INSERT INTO daily_assets (username, date, total_income, total_expense, balance)
                    VALUES (@username, @date,
                        IF(@type = '收入' OR @type = '設定', @amount, 0),
                        IF(@type = '支出', @amount, 0),
                        IF(@type = '收入' OR @type = '設定', @balance+@amount, @balance-@amount));";
                    await ExecuteNonQueryAsync(insertDailyQuery, new Dictionary<string, object>
                    {
                        { "@username", transaction.Username },
                        { "@date", transaction.Date },
                        { "@amount", transaction.Amount },
                        { "@type", transaction.Type },
                        { "@balance", transaction.Balance }
                    });
                }
                // 更新每月結餘
                if (monthlyCount > 0)
                {
                    string updateMonthlyQuery = @"UPDATE monthly_assets SET 
                                                  total_income = total_income + IF(@type = '收入' OR @type = '設定', @amount, 0),
                                                  total_expense = total_expense + IF(@type = '支出', @amount, 0),
                                                  balance = @balance + IF(@type = '收入' OR @type = '設定', @amount, -@amount)
                                                  WHERE username = @username AND year = @Year AND month = @Month;";
                    await ExecuteNonQueryAsync(updateMonthlyQuery, new Dictionary<string, object>
                        {
                            { "@username", transaction.Username },
                            { "@Year", transaction.Date.Year },
                            { "@Month", transaction.Date.Month },
                            { "@amount", transaction.Amount },
                            { "@type", transaction.Type },
                            { "@balance", transaction.Balance}
                        });
                }
                else{ 
                
                string insertMonthlyQuery = @"
                    INSERT INTO monthly_assets (username, year, month, total_income, total_expense, balance)
                    VALUES (@username, @Year, @Month,
                        IF(@type = '收入' OR @type = '設定', @amount, 0),
                        IF(@type = '支出', @amount, 0),
                        IF(@type = '收入' OR @type = '設定', @balance+@amount, @balance-@amount));";
                    await ExecuteNonQueryAsync(insertMonthlyQuery, new Dictionary<string, object>
                    {
                        { "@username", transaction.Username },
                        { "@Year", transaction.Date.Year },
                        { "@Month", transaction.Date.Month },
                        { "@amount", transaction.Amount },
                        { "@type", transaction.Type },
                        { "@balance", transaction.Balance }
                    });
                }

                await transactionScope.CommitAsync();
            }
            catch (Exception ex)
            {
                await transactionScope.RollbackAsync();
                throw new Exception($"新增交易失敗：{ex.Message}");
            }
        }

        // **刪除交易並更新每日與每月結餘**
        public async Task DeleteTransactionAsync(int transactionId, string username, DateTime date)
        {
            // 更新每日和每月結餘
            await UpdateDailyAndMonthlyAssets(username, date);
        }

        private async Task UpdateDailyAndMonthlyAssets(string username, DateTime date)
        {
            string dailyUpdateQuery = @"
                UPDATE daily_assets
                SET total_income = (SELECT SUM(CASE WHEN type = '收入' THEN amount ELSE 0 END) FROM transactions WHERE username = @username AND date = @date),
                    total_expense = (SELECT SUM(CASE WHEN type = '支出' THEN amount ELSE 0 END) FROM transactions WHERE username = @username AND date = @date),
                    balance = balance + total_income - total_expense
                WHERE username = @username AND date = @date;";
            await ExecuteNonQueryAsync(dailyUpdateQuery, new Dictionary<string, object> { { "@username", username }, { "@date", date } });

            string monthlyUpdateQuery = @"
                UPDATE monthly_assets
                SET total_income = (SELECT SUM(CASE WHEN type = '收入' THEN amount ELSE 0 END) FROM transactions WHERE username = @username AND YEAR(date) = YEAR(@date) AND MONTH(date) = MONTH(@date)),
                    total_expense = (SELECT SUM(CASE WHEN type = '支出' THEN amount ELSE 0 END) FROM transactions WHERE username = @username AND YEAR(date) = YEAR(@date) AND MONTH(date) = MONTH(@date)),
                    balance = balance + total_income - total_expense
                WHERE username = @username AND year = YEAR(@date) AND month = MONTH(@date);";
            await ExecuteNonQueryAsync(monthlyUpdateQuery, new Dictionary<string, object> { { "@username", username }, { "@date", date } });
        }


        public async Task<List<(string Date, double Balance)>> GetDailyBalancesAsync(string username)
        {
            List<(string Date, double Balance)> dailyBalances = new List<(string Date, double Balance)>();
            string query = "SELECT date, balance FROM daily_assets WHERE username = @username ORDER BY date;";
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string dateLabel = reader.GetDateTime("date").ToString("yyyy/MM/dd");
                double balance = reader.GetDouble("balance");
                dailyBalances.Add((dateLabel, balance));
            }

            return dailyBalances;
        }

        public class FinancialData
        {
            public required string Month { get; set; }  // X軸顯示的月份
            public double Balance { get; set; }  // Y軸顯示的平均資產餘額
        }
        //日均每月
        public async Task<List<FinancialData>> GetMonthlyFinancialDataAsync(string username)
        {
            List<FinancialData> data = new List<FinancialData>();
            string query = @"
        SELECT CONCAT(year, '/' , LPAD(month, 2, '0')) AS Month, AVG(balance) AS AverageBalance
        FROM daily_assets
        WHERE username = @username
        GROUP BY year, month
        ORDER BY year, month;";
        
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
        
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
        
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                data.Add(new FinancialData
                {
                    Month = reader.GetString("Month"),
                    Balance = reader.GetDouble("AverageBalance")
                });
            }
            return data;
        }
        public class MonthlyBalanceData
        {
            public required string Month { get; set; }  // X軸顯示的月份
            public double Balance { get; set; }  // Y軸顯示的平均資產餘額
        }
        //每月
        public async Task<List<MonthlyBalanceData>> GetMonthlyDataAsync(string username)
        {
            string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
            var monthlyBalances = new List<MonthlyBalanceData>();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // 直接從 monthly_assets 表格中取得每個月份的結餘
            string query = @"
        SELECT CONCAT(year, '/', LPAD(month, 2, '0')) AS Month, balance AS Balance
        FROM monthly_assets
        WHERE username = @username
        ORDER BY year, month;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                monthlyBalances.Add(new MonthlyBalanceData
                {
                    Month = reader.GetString("Month"),  // e.g., "2025/01"
                    Balance = reader.GetDouble("Balance")  // 該月的結餘金額
                });
            }

            return monthlyBalances;
        }

        public class DailyBalanceData
        {
            public string Day { get; set; }  // 日期 e.g., "01"
            public double Balance { get; set; }  // 當日結餘
        }

        //每日
        public async Task<List<DailyBalanceData>> GetDailyFinancialDataAsync(string username)
        {
            string connectionString = "Server=220.134.162.210;Database=clientsystem;User=orsp0118;Password=orsp0118;";
            var dailyBalances = new List<DailyBalanceData>();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // 查詢該使用者特定月份的每日結餘資料
            string query = @"
        SELECT DATE_FORMAT(date, '%Y/%m/%d') AS Day, balance AS Balance
        FROM daily_assets
        WHERE username = @username
        ORDER BY date;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dailyBalances.Add(new DailyBalanceData
                {
                    Day = reader.GetString("Day"),  // e.g., "01", "02"
                    Balance = reader.GetDouble("Balance")  // 每日結餘金額
                });
            }

            return dailyBalances;
        }



    }
}
