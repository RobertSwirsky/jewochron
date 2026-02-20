using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Jewochron.Models;
using Jewochron.Services;

namespace Jewochron.Services
{
    public class SimchaService
    {
        private readonly string connectionString;
        private readonly HebrewCalendarService hebrewCalendarService;

        public SimchaService(string databasePath, HebrewCalendarService hebrewCalendarService)
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            connectionString = $"Data Source={databasePath}";
            this.hebrewCalendarService = hebrewCalendarService;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Simchas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    HebrewDate TEXT NOT NULL,
                    HebrewDay INTEGER NOT NULL,
                    HebrewMonth INTEGER NOT NULL,
                    HebrewYear INTEGER NOT NULL,
                    EnglishDate TEXT,
                    IsRecurring INTEGER NOT NULL DEFAULT 1,
                    Notes TEXT,
                    CreatedDate TEXT NOT NULL
                )";
            createTableCommand.ExecuteNonQuery();
        }

        public async Task<ObservableCollection<Simcha>> GetAllSimchasAsync()
        {
            var simchas = new ObservableCollection<Simcha>();

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Simchas ORDER BY HebrewMonth, HebrewDay, Name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var simcha = new Simcha
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Type = reader.GetString(reader.GetOrdinal("Type")),
                    HebrewDate = reader.GetString(reader.GetOrdinal("HebrewDate")),
                    HebrewDay = reader.GetInt32(reader.GetOrdinal("HebrewDay")),
                    HebrewMonth = reader.GetInt32(reader.GetOrdinal("HebrewMonth")),
                    HebrewYear = reader.GetInt32(reader.GetOrdinal("HebrewYear")),
                    EnglishDate = reader.IsDBNull(reader.GetOrdinal("EnglishDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("EnglishDate"))),
                    IsRecurring = reader.GetInt32(reader.GetOrdinal("IsRecurring")) == 1,
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString(reader.GetOrdinal("Notes")),
                    CreatedDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedDate")))
                };
                simchas.Add(simcha);
            }

            return simchas;
        }

        public async Task<bool> AddSimchaAsync(Simcha simcha)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Simchas (Name, Type, HebrewDate, HebrewDay, HebrewMonth, HebrewYear, EnglishDate, IsRecurring, Notes, CreatedDate)
                    VALUES (@name, @type, @hebrewDate, @hebrewDay, @hebrewMonth, @hebrewYear, @englishDate, @isRecurring, @notes, @createdDate)";

                command.Parameters.AddWithValue("@name", simcha.Name);
                command.Parameters.AddWithValue("@type", simcha.Type);
                command.Parameters.AddWithValue("@hebrewDate", simcha.HebrewDate);
                command.Parameters.AddWithValue("@hebrewDay", simcha.HebrewDay);
                command.Parameters.AddWithValue("@hebrewMonth", simcha.HebrewMonth);
                command.Parameters.AddWithValue("@hebrewYear", simcha.HebrewYear);
                command.Parameters.AddWithValue("@englishDate", simcha.EnglishDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@isRecurring", simcha.IsRecurring ? 1 : 0);
                command.Parameters.AddWithValue("@notes", simcha.Notes ?? "");
                command.Parameters.AddWithValue("@createdDate", simcha.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateSimchaAsync(Simcha simcha)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Simchas 
                    SET Name = @name, Type = @type, HebrewDate = @hebrewDate, HebrewDay = @hebrewDay, 
                        HebrewMonth = @hebrewMonth, HebrewYear = @hebrewYear, EnglishDate = @englishDate, 
                        IsRecurring = @isRecurring, Notes = @notes
                    WHERE Id = @id";

                command.Parameters.AddWithValue("@id", simcha.Id);
                command.Parameters.AddWithValue("@name", simcha.Name);
                command.Parameters.AddWithValue("@type", simcha.Type);
                command.Parameters.AddWithValue("@hebrewDate", simcha.HebrewDate);
                command.Parameters.AddWithValue("@hebrewDay", simcha.HebrewDay);
                command.Parameters.AddWithValue("@hebrewMonth", simcha.HebrewMonth);
                command.Parameters.AddWithValue("@hebrewYear", simcha.HebrewYear);
                command.Parameters.AddWithValue("@englishDate", simcha.EnglishDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@isRecurring", simcha.IsRecurring ? 1 : 0);
                command.Parameters.AddWithValue("@notes", simcha.Notes ?? "");

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteSimchaAsync(int id)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Simchas WHERE Id = @id";
                command.Parameters.AddWithValue("@id", id);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        // Get upcoming Simchas within the next 30 days
        public async Task<List<Simcha>> GetUpcomingSimchasAsync(int days = 30)
        {
            var allSimchas = await GetAllSimchasAsync();
            var upcomingSimchas = new List<Simcha>();
            var cutoffDate = DateTime.Now.AddDays(days);

            foreach (var simcha in allSimchas)
            {
                var nextOccurrence = simcha.GetNextOccurrence(hebrewCalendarService);
                if (nextOccurrence.HasValue && nextOccurrence.Value <= cutoffDate)
                {
                    upcomingSimchas.Add(simcha);
                }
            }

            return upcomingSimchas.OrderBy(s => s.GetNextOccurrence(hebrewCalendarService)).ToList();
        }
    }
}