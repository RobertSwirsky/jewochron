using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Jewochron.Data;
using System.Diagnostics;
using System.Text;

namespace Jewochron.Services
{
    /// <summary>
    /// Embedded web server for managing Yahrzeit data
    /// </summary>
    public class YahrzeitWebServer
    {
        private WebApplication? _app;
        private readonly int _port = 5555;
        private readonly string _databasePath;

        /// <summary>
        /// Event raised when yahrzeit data is added, updated, or deleted
        /// </summary>
        public event EventHandler? YahrzeitDataChanged;

        public YahrzeitWebServer(string databasePath)
        {
            _databasePath = databasePath;
        }

        /// <summary>
        /// Raises the YahrzeitDataChanged event
        /// </summary>
        protected virtual void OnYahrzeitDataChanged()
        {
            Debug.WriteLine("[YAHRZEIT] OnYahrzeitDataChanged triggered - raising event");
            YahrzeitDataChanged?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine($"[YAHRZEIT] Event raised, handlers attached: {YahrzeitDataChanged != null}");
        }

        public async Task StartAsync()
        {
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Configure services
                builder.Services.AddDbContext<YahrzeitDbContext>(options =>
                    options.UseSqlite($"Data Source={_databasePath}"));

                builder.Services.AddDbContext<SimchaDbContext>(options =>
                    options.UseSqlite($"Data Source={_databasePath.Replace("yahrzeits.db", "simchas.db")}"));

                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });

                // Configure Kestrel to listen on specific port
                builder.WebHost.UseUrls($"http://localhost:{_port}");

                _app = builder.Build();

                // Ensure database is created and schema is up to date
                using (var scope = _app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<YahrzeitDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();

                    // Ensure Simcha database is also created
                    var simchaDbContext = scope.ServiceProvider.GetRequiredService<SimchaDbContext>();
                    await simchaDbContext.Database.EnsureCreatedAsync();

                    // Check if Gender column exists and add it if missing
                    try
                    {
                        var connection = dbContext.Database.GetDbConnection();
                        await connection.OpenAsync();
                        using var command = connection.CreateCommand();
                        command.CommandText = "PRAGMA table_info(yahrzeits)";
                        using var reader = await command.ExecuteReaderAsync();

                        bool hasGenderColumn = false;
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1) == "Gender")
                            {
                                hasGenderColumn = true;
                                break;
                            }
                        }

                        reader.Close();

                        if (!hasGenderColumn)
                        {
                            Debug.WriteLine("Gender column not found. Adding it to the database...");
                            command.CommandText = "ALTER TABLE yahrzeits ADD COLUMN Gender TEXT NOT NULL DEFAULT 'M'";
                            await command.ExecuteNonQueryAsync();
                            Debug.WriteLine("Gender column added successfully.");
                        }

                        await connection.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking/adding Gender column: {ex.Message}");
                        throw;
                    }
                }

                _app.UseCors();

                // API Endpoints
                MapApiEndpoints(_app);

                // Serve the main settings menu
                _app.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(GetSettingsMenuHtml());
                });

                // Serve the Yahrzeit management page
                _app.MapGet("/yahrzeits", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(GetYahrzeitFormHtml());
                });

                // Serve the Simchas management page
                _app.MapGet("/simchas", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(GetSimchasFormHtml());
                });

                await _app.StartAsync();
                Debug.WriteLine($"Yahrzeit Web Server started at http://localhost:{_port}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start web server: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }

        private void MapApiEndpoints(WebApplication app)
        {
            // GET all yahrzeits
            app.MapGet("/api/yahrzeits", async (YahrzeitDbContext db) =>
            {
                var yahrzeits = await db.Yahrzeits
                    .OrderBy(y => y.HebrewMonth)
                    .ThenBy(y => y.HebrewDay)
                    .ToListAsync();
                return Results.Ok(yahrzeits);
            });

            // GET yahrzeit by ID
            app.MapGet("/api/yahrzeits/{id}", async (int id, YahrzeitDbContext db) =>
            {
                var yahrzeit = await db.Yahrzeits.FindAsync(id);
                return yahrzeit != null ? Results.Ok(yahrzeit) : Results.NotFound();
            });

            // POST new yahrzeit
            app.MapPost("/api/yahrzeits", async (Models.Yahrzeit yahrzeit, YahrzeitDbContext db) =>
            {
                yahrzeit.CreatedAt = DateTime.UtcNow;
                yahrzeit.UpdatedAt = DateTime.UtcNow;
                db.Yahrzeits.Add(yahrzeit);
                await db.SaveChangesAsync();
                OnYahrzeitDataChanged();
                return Results.Created($"/api/yahrzeits/{yahrzeit.Id}", yahrzeit);
            });

            // PUT update yahrzeit
            app.MapPut("/api/yahrzeits/{id}", async (int id, Models.Yahrzeit updatedYahrzeit, YahrzeitDbContext db) =>
            {
                var yahrzeit = await db.Yahrzeits.FindAsync(id);
                if (yahrzeit == null) return Results.NotFound();

                yahrzeit.HebrewMonth = updatedYahrzeit.HebrewMonth;
                yahrzeit.HebrewDay = updatedYahrzeit.HebrewDay;
                yahrzeit.HebrewYear = updatedYahrzeit.HebrewYear;
                yahrzeit.NameEnglish = updatedYahrzeit.NameEnglish;
                yahrzeit.NameHebrew = updatedYahrzeit.NameHebrew;
                yahrzeit.Gender = updatedYahrzeit.Gender;
                yahrzeit.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                OnYahrzeitDataChanged();
                return Results.Ok(yahrzeit);
            });

            // DELETE yahrzeit
            app.MapDelete("/api/yahrzeits/{id}", async (int id, YahrzeitDbContext db) =>
            {
                var yahrzeit = await db.Yahrzeits.FindAsync(id);
                if (yahrzeit == null) return Results.NotFound();

                db.Yahrzeits.Remove(yahrzeit);
                await db.SaveChangesAsync();
                OnYahrzeitDataChanged();
                return Results.NoContent();
            });

            // Simchas API Endpoints

            // GET all simchas
            app.MapGet("/api/simchas", async (SimchaDbContext db) =>
            {
                var simchas = await db.Simchas
                    .OrderBy(s => s.HebrewMonth)
                    .ThenBy(s => s.HebrewDay)
                    .ToListAsync();
                return Results.Ok(simchas);
            });

            // GET simcha by ID
            app.MapGet("/api/simchas/{id}", async (int id, SimchaDbContext db) =>
            {
                var simcha = await db.Simchas.FindAsync(id);
                return simcha != null ? Results.Ok(simcha) : Results.NotFound();
            });

            // POST new simcha
            app.MapPost("/api/simchas", async (Models.Simcha simcha, SimchaDbContext db) =>
            {
                simcha.CreatedDate = DateTime.UtcNow;
                db.Simchas.Add(simcha);
                await db.SaveChangesAsync();
                return Results.Created($"/api/simchas/{simcha.Id}", simcha);
            });

            // PUT update simcha
            app.MapPut("/api/simchas/{id}", async (int id, Models.Simcha updatedSimcha, SimchaDbContext db) =>
            {
                var simcha = await db.Simchas.FindAsync(id);
                if (simcha == null) return Results.NotFound();

                simcha.Name = updatedSimcha.Name;
                simcha.Type = updatedSimcha.Type;
                simcha.HebrewDay = updatedSimcha.HebrewDay;
                simcha.HebrewMonth = updatedSimcha.HebrewMonth;
                simcha.HebrewYear = updatedSimcha.HebrewYear;
                simcha.HebrewDate = updatedSimcha.HebrewDate;
                simcha.EnglishDate = updatedSimcha.EnglishDate;
                simcha.IsRecurring = updatedSimcha.IsRecurring;
                simcha.Notes = updatedSimcha.Notes;

                await db.SaveChangesAsync();
                return Results.Ok(simcha);
            });

            // DELETE simcha
            app.MapDelete("/api/simchas/{id}", async (int id, SimchaDbContext db) =>
            {
                var simcha = await db.Simchas.FindAsync(id);
                if (simcha == null) return Results.NotFound();

                db.Simchas.Remove(simcha);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // Convert Gregorian date to Hebrew date
            app.MapPost("/api/convert-to-hebrew", (DateConversionRequest request) =>
            {
                try
                {
                    var gregorianDate = DateTime.Parse(request.GregorianDate);
                    var hebrewCalendar = new System.Globalization.HebrewCalendar();

                    int hebrewYear = hebrewCalendar.GetYear(gregorianDate);
                    int hebrewMonth = hebrewCalendar.GetMonth(gregorianDate);
                    int hebrewDay = hebrewCalendar.GetDayOfMonth(gregorianDate);

                    return Results.Ok(new { hebrewDay, hebrewMonth, hebrewYear });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });
        }

        private record DateConversionRequest(string GregorianDate);

        private string GetSettingsMenuHtml()
        {
            return """
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Jewochron Settings</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .container {
            max-width: 800px;
            width: 100%;
        }

        h1 {
            color: white;
            text-align: center;
            margin-bottom: 40px;
            font-size: 3em;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }

        .settings-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 30px;
            padding: 20px;
        }

        .setting-card {
            background: white;
            border-radius: 15px;
            padding: 40px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            transition: transform 0.3s, box-shadow 0.3s;
            cursor: pointer;
            text-decoration: none;
            display: block;
        }

        .setting-card:hover {
            transform: translateY(-10px);
            box-shadow: 0 15px 50px rgba(0,0,0,0.3);
        }

        .setting-icon {
            font-size: 4em;
            margin-bottom: 20px;
        }

        .setting-title {
            font-size: 1.8em;
            font-weight: 600;
            color: #333;
            margin-bottom: 15px;
        }

        .setting-description {
            color: #666;
            font-size: 1em;
            line-height: 1.6;
        }

        .yahrzeit-card {
            background: linear-gradient(135deg, #8B7355 0%, #CD853F 50%, #8B7355 100%);
        }

        .yahrzeit-card .setting-title,
        .yahrzeit-card .setting-description {
            color: white;
            text-shadow: 1px 1px 2px rgba(0,0,0,0.3);
        }

        .simcha-card {
            background: linear-gradient(135deg, #FF6B9D 0%, #FFC371 100%);
        }

        .simcha-card .setting-title,
        .simcha-card .setting-description {
            color: white;
            text-shadow: 1px 1px 2px rgba(0,0,0,0.3);
        }
    </style>
</head>
<body>
    <div class='container'>
        <h1>⚙️ Jewochron Settings</h1>
        <div class='settings-grid'>
            <a href='/yahrzeits' class='setting-card yahrzeit-card'>
                <div class='setting-icon'>🕯️</div>
                <div class='setting-title'>Yahrzeits</div>
                <div class='setting-description'>
                    Manage memorial dates and anniversaries for loved ones
                </div>
            </a>
            <a href='/simchas' class='setting-card simcha-card'>
                <div class='setting-icon'>🎉</div>
                <div class='setting-title'>Simchas</div>
                <div class='setting-description'>
                    Track Hebrew birthdays, bar mitzvahs, weddings, and joyous occasions
                </div>
            </a>
        </div>
    </div>
</body>
</html>
""";
        }

        private string GetYahrzeitFormHtml()
        {
            // Read the HTML from embedded resource or return inline HTML
            // Using raw string literal to avoid escaping issues
            return """
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Yahrzeit Manager - Jewochron</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            min-height: 100vh;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 30px;
        }

        .back-btn {
            background: white;
            color: #667eea;
            padding: 10px 20px;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            cursor: pointer;
            text-decoration: none;
            display: inline-block;
            font-weight: 600;
            transition: transform 0.2s;
        }

        .back-btn:hover {
            transform: translateX(-5px);
        }

        h1 {
            color: white;
            text-align: center;
            font-size: 2.5em;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
            flex: 1;
        }

        .card {
            background: white;
            border-radius: 15px;
            padding: 30px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            margin-bottom: 30px;
        }

        .form-group {
            margin-bottom: 20px;
        }

        label {
            display: block;
            margin-bottom: 8px;
            font-weight: 600;
            color: #333;
        }

        input, select {
            width: 100%;
            padding: 12px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 16px;
            transition: border-color 0.3s;
        }

        input:focus, select:focus {
            outline: none;
            border-color: #667eea;
        }

        .hebrew-input {
            direction: rtl;
            text-align: right;
            font-size: 18px;
        }

        .hebrew-name-container {
            display: flex;
            align-items: center;
            gap: 10px;
            flex-wrap: wrap;
            flex-direction: row-reverse; /* Right-to-left layout for Hebrew */
        }

        .hebrew-name-part {
            flex: 1;
            min-width: 200px;
        }

        .ben-bat-selector {
            display: flex;
            gap: 15px;
            padding: 10px;
            background: #f8f9fa;
            border-radius: 8px;
            border: 2px solid #e0e0e0;
        }

        .radio-option {
            display: flex;
            align-items: center;
            gap: 5px;
            cursor: pointer;
            padding: 5px 10px;
            border-radius: 5px;
            transition: background 0.2s;
        }

        .radio-option:hover {
            background: rgba(102, 126, 234, 0.1);
        }

        .radio-option input[type='radio'] {
            width: auto;
            cursor: pointer;
        }

        .radio-option .hebrew-text {
            font-size: 20px;
            font-weight: bold;
            color: #333;
        }

        .radio-option .english-text {
            font-size: 12px;
            color: #666;
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 15px;
        }

        .btn {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 14px 30px;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s;
            width: 100%;
            margin-top: 10px;
        }

        .btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 20px rgba(102, 126, 234, 0.4);
        }

        .btn:active {
            transform: translateY(0);
        }

        .btn-secondary {
            background: #6c757d;
            margin-left: 10px;
        }

        .btn-danger {
            background: #dc3545;
        }

        .yahrzeit-list {
            margin-top: 30px;
        }

        .yahrzeit-item {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 10px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            transition: background 0.2s;
        }

        .yahrzeit-item:hover {
            background: #e9ecef;
        }

        .yahrzeit-info {
            flex: 1;
        }

        .yahrzeit-name {
            font-weight: 600;
            font-size: 18px;
            color: #333;
            margin-bottom: 5px;
        }

        .yahrzeit-date {
            color: #666;
            font-size: 14px;
        }

        .yahrzeit-actions {
            display: flex;
            gap: 10px;
        }

        .btn-small {
            padding: 8px 16px;
            font-size: 14px;
            width: auto;
            margin: 0;
        }

        .message {
            padding: 12px;
            border-radius: 8px;
            margin-bottom: 20px;
            display: none;
        }

        .message.success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }

        .message.error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .hebrew-name-container {
                flex-direction: column;
                align-items: stretch;
            }

            .hebrew-name-part {
                min-width: 100%;
            }

            .ben-bat-selector {
                justify-content: center;
            }

            .yahrzeit-item {
                flex-direction: column;
                align-items: flex-start;
            }

            .yahrzeit-actions {
                margin-top: 10px;
                width: 100%;
            }

            .btn-small {
                flex: 1;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <a href='/' class='back-btn'>← Back to Settings</a>
            <h1>📖 Yahrzeit Manager</h1>
            <div style='width: 180px;'></div>
        </div>

        <div class='card'>
            <h2 id='formTitle'>Add New Yahrzeit</h2>
            <div id='message' class='message'></div>
            
            <form id='yahrzeitForm'>
                <input type='hidden' id='editId' value=''>
                
                <div class='form-row'>
                    <div class='form-group'>
                        <label for='hebrewMonth'>Hebrew Month</label>
                        <select id='hebrewMonth' required>
                            <option value=''>Select Month</option>
                            <option value='1'>Tishrei</option>
                            <option value='2'>Cheshvan</option>
                            <option value='3'>Kislev</option>
                            <option value='4'>Tevet</option>
                            <option value='5'>Shevat</option>
                            <option value='6'>Adar (or Adar I)</option>
                            <option value='7'>Adar II (Leap Year)</option>
                            <option value='8'>Nisan</option>
                            <option value='9'>Iyar</option>
                            <option value='10'>Sivan</option>
                            <option value='11'>Tammuz</option>
                            <option value='12'>Av</option>
                            <option value='13'>Elul</option>
                        </select>
                    </div>
                    
                    <div class='form-group'>
                        <label for='hebrewDay'>Hebrew Day</label>
                        <select id='hebrewDay' required>
                            <option value=''>Select Day</option>
                        </select>
                    </div>
                    
                    <div class='form-group'>
                        <label for='hebrewYear'>Hebrew Year</label>
                        <input type='number' id='hebrewYear' min='5000' max='6000' required 
                               placeholder='e.g., 5784'>
                    </div>
                </div>
                
                <div class='form-group'>
                    <label for='nameEnglish'>Name (English)</label>
                    <input type='text' id='nameEnglish' required 
                           placeholder='Enter name in English'>
                </div>

                <div class='form-group'>
                    <label>Hebrew Name (Format: [First Name] בן/בת [Father's Name])</label>
                    <div class='hebrew-name-container'>
                        <input type='text' id='hebrewFirstName' class='hebrew-input hebrew-name-part' required 
                               placeholder='שם פרטי' title='First Name (appears on right)'>

                        <div class='ben-bat-selector'>
                            <label class='radio-option'>
                                <input type='radio' name='benBat' value='M' checked>
                                <span class='hebrew-text'>בן</span>
                                <span class='english-text'>(Ben)</span>
                            </label>
                            <label class='radio-option'>
                                <input type='radio' name='benBat' value='F'>
                                <span class='hebrew-text'>בת</span>
                                <span class='english-text'>(Bat)</span>
                            </label>
                        </div>

                        <input type='text' id='hebrewFatherName' class='hebrew-input hebrew-name-part' required 
                               placeholder='שם האב' title="Father's Name (appears on left)">
                    </div>
                    <input type='hidden' id='nameHebrew'>
                    <input type='hidden' id='gender'>
                </div>

                <button type='submit' class='btn'>Save Yahrzeit</button>
                <button type='button' class='btn btn-secondary' id='cancelEdit' style='display:none;'>Cancel Edit</button>
            </form>
        </div>
        
        <div class='card'>
            <h2>Saved Yahrzeits</h2>
            <div id='yahrzeitList' class='yahrzeit-list'></div>
        </div>
    </div>

    <script>
        const daySelect = document.getElementById('hebrewDay');
        for (let i = 1; i <= 30; i++) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = i;
            daySelect.appendChild(option);
        }

        const monthNames = {
            1: 'Tishrei', 2: 'Cheshvan', 3: 'Kislev', 4: 'Tevet',
            5: 'Shevat', 6: 'Adar/Adar I', 7: 'Adar II', 8: 'Nisan',
            9: 'Iyar', 10: 'Sivan', 11: 'Tammuz', 12: 'Av', 13: 'Elul'
        };

        function showMessage(text, type) {
            const message = document.getElementById('message');
            message.textContent = text;
            message.className = 'message ' + type;
            message.style.display = 'block';
            setTimeout(() => message.style.display = 'none', 5000);
        }

        async function loadYahrzeits() {
            try {
                const response = await fetch('/api/yahrzeits');
                const yahrzeits = await response.json();
                
                const listDiv = document.getElementById('yahrzeitList');
                if (yahrzeits.length === 0) {
                    listDiv.innerHTML = '<p style="color: #666;">No yahrzeits recorded yet.</p>';
                    return;
                }
                
                listDiv.innerHTML = yahrzeits.map(y => {
                    return '<div class="yahrzeit-item">' +
                        '<div class="yahrzeit-info">' +
                        '<div class="yahrzeit-name">' + y.nameEnglish + ' | ' + y.nameHebrew + '</div>' +
                        '<div class="yahrzeit-date">' + monthNames[y.hebrewMonth] + ' ' + y.hebrewDay + ', ' + y.hebrewYear + '</div>' +
                        '</div>' +
                        '<div class="yahrzeit-actions">' +
                        '<button class="btn btn-small btn-secondary" onclick="editYahrzeit(' + y.id + ')">Edit</button>' +
                        '<button class="btn btn-small btn-danger" onclick="deleteYahrzeit(' + y.id + ')">Delete</button>' +
                        '</div>' +
                        '</div>';
                }).join('');
            } catch (error) {
                showMessage('Failed to load yahrzeits: ' + error.message, 'error');
            }
        }

        async function editYahrzeit(id) {
            try {
                const response = await fetch('/api/yahrzeits/' + id);
                const yahrzeit = await response.json();

                document.getElementById('editId').value = id;
                document.getElementById('hebrewMonth').value = yahrzeit.hebrewMonth;
                document.getElementById('hebrewDay').value = yahrzeit.hebrewDay;
                document.getElementById('hebrewYear').value = yahrzeit.hebrewYear;
                document.getElementById('nameEnglish').value = yahrzeit.nameEnglish;

                // Parse Hebrew name back into parts
                const hebrewName = yahrzeit.nameHebrew;
                const benIndex = hebrewName.indexOf(' בן ');
                const batIndex = hebrewName.indexOf(' בת ');

                if (benIndex !== -1) {
                    document.getElementById('hebrewFirstName').value = hebrewName.substring(0, benIndex);
                    document.getElementById('hebrewFatherName').value = hebrewName.substring(benIndex + 4);
                    document.querySelector('input[name="benBat"][value="M"]').checked = true;
                } else if (batIndex !== -1) {
                    document.getElementById('hebrewFirstName').value = hebrewName.substring(0, batIndex);
                    document.getElementById('hebrewFatherName').value = hebrewName.substring(batIndex + 4);
                    document.querySelector('input[name="benBat"][value="F"]').checked = true;
                } else {
                    // Fallback if name doesn't match expected format
                    document.getElementById('hebrewFirstName').value = hebrewName;
                    document.getElementById('hebrewFatherName').value = '';
                    const gender = yahrzeit.gender || 'M';
                    document.querySelector('input[name="benBat"][value="' + gender + '"]').checked = true;
                }

                document.getElementById('formTitle').textContent = 'Edit Yahrzeit';
                document.getElementById('cancelEdit').style.display = 'inline-block';

                window.scrollTo({ top: 0, behavior: 'smooth' });
            } catch (error) {
                showMessage('Failed to load yahrzeit: ' + error.message, 'error');
            }
        }

        async function deleteYahrzeit(id) {
            if (!confirm('Are you sure you want to delete this yahrzeit?')) return;
            
            try {
                const response = await fetch('/api/yahrzeits/' + id, {
                    method: 'DELETE'
                });
                
                if (response.ok) {
                    showMessage('Yahrzeit deleted successfully', 'success');
                    loadYahrzeits();
                } else {
                    showMessage('Failed to delete yahrzeit', 'error');
                }
            } catch (error) {
                showMessage('Error: ' + error.message, 'error');
            }
        }

        document.getElementById('cancelEdit').addEventListener('click', () => {
            document.getElementById('yahrzeitForm').reset();
            document.getElementById('editId').value = '';
            document.getElementById('formTitle').textContent = 'Add New Yahrzeit';
            document.getElementById('cancelEdit').style.display = 'none';
        });

        document.getElementById('yahrzeitForm').addEventListener('submit', async (e) => {
            e.preventDefault();

            // Construct full Hebrew name from parts
            const hebrewFirstName = document.getElementById('hebrewFirstName').value.trim();
            const hebrewFatherName = document.getElementById('hebrewFatherName').value.trim();
            const benBat = document.querySelector('input[name="benBat"]:checked').value;
            const benBatText = benBat === 'M' ? 'בן' : 'בת';
            const fullHebrewName = hebrewFirstName + ' ' + benBatText + ' ' + hebrewFatherName;

            const editId = document.getElementById('editId').value;
            const yahrzeit = {
                hebrewMonth: parseInt(document.getElementById('hebrewMonth').value),
                hebrewDay: parseInt(document.getElementById('hebrewDay').value),
                hebrewYear: parseInt(document.getElementById('hebrewYear').value),
                nameEnglish: document.getElementById('nameEnglish').value,
                nameHebrew: fullHebrewName,
                gender: benBat
            };

            try {
                const url = editId ? '/api/yahrzeits/' + editId : '/api/yahrzeits';
                const method = editId ? 'PUT' : 'POST';

                const response = await fetch(url, {
                    method: method,
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(yahrzeit)
                });

                if (response.ok) {
                    showMessage(editId ? 'Yahrzeit updated successfully' : 'Yahrzeit added successfully', 'success');
                    document.getElementById('yahrzeitForm').reset();
                    document.getElementById('editId').value = '';
                    document.getElementById('formTitle').textContent = 'Add New Yahrzeit';
                    document.getElementById('cancelEdit').style.display = 'none';
                    loadYahrzeits();
                } else {
                    showMessage('Failed to save yahrzeit', 'error');
                }
            } catch (error) {
                showMessage('Error: ' + error.message, 'error');
            }
        });

        loadYahrzeits();
    </script>
</body>
</html>
""";
        }

        private string GetSimchasFormHtml()
        {
            return """
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Simchas Manager - Jewochron</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #FF6B9D 0%, #FFC371 100%);
            padding: 20px;
            min-height: 100vh;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 30px;
        }

        .back-btn {
            background: white;
            color: #FF6B9D;
            padding: 10px 20px;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            cursor: pointer;
            text-decoration: none;
            display: inline-block;
            font-weight: 600;
            transition: transform 0.2s;
        }

        .back-btn:hover {
            transform: translateX(-5px);
        }

        h1 {
            color: white;
            text-align: center;
            font-size: 2.5em;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
            flex: 1;
        }

        .card {
            background: white;
            border-radius: 15px;
            padding: 30px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            margin-bottom: 30px;
        }

        .form-group {
            margin-bottom: 20px;
        }

        label {
            display: block;
            margin-bottom: 8px;
            font-weight: 600;
            color: #333;
        }

        input, select, textarea {
            width: 100%;
            padding: 12px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 16px;
            transition: border-color 0.3s;
        }

        input:focus, select:focus, textarea:focus {
            outline: none;
            border-color: #FF6B9D;
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 15px;
        }

        .date-type-selector {
            display: flex;
            gap: 15px;
            margin-bottom: 20px;
            padding: 15px;
            background: #fff5f8;
            border-radius: 8px;
            justify-content: center;
        }

        .date-type-btn {
            padding: 12px 24px;
            border: 2px solid #FF6B9D;
            background: white;
            color: #FF6B9D;
            border-radius: 8px;
            cursor: pointer;
            font-weight: 600;
            font-size: 15px;
            transition: all 0.3s;
            min-width: 180px;
        }

        .date-type-btn:hover {
            background: #fff0f5;
            transform: translateY(-2px);
        }

        .date-type-btn.active {
            background: linear-gradient(135deg, #FF6B9D 0%, #FFC371 100%);
            color: white;
            border-color: transparent;
            box-shadow: 0 4px 15px rgba(255, 107, 157, 0.3);
        }

        .date-input-section {
            display: none;
            animation: fadeIn 0.3s ease-in;
        }

        .date-input-section.active {
            display: block;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(-10px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .hebrew-datepicker {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 15px;
            padding: 20px;
            background: linear-gradient(135deg, #fff5f8 0%, #ffe8f0 100%);
            border-radius: 12px;
            border: 2px solid #ffd4e5;
        }

        .datepicker-field {
            display: flex;
            flex-direction: column;
        }

        .datepicker-field label {
            font-size: 13px;
            font-weight: 600;
            color: #FF6B9D;
            margin-bottom: 8px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .datepicker-select,
        .datepicker-input {
            padding: 12px;
            border: 2px solid #ffd4e5;
            border-radius: 8px;
            font-size: 16px;
            background: white;
            transition: all 0.3s;
        }

        .datepicker-select:focus,
        .datepicker-input:focus {
            outline: none;
            border-color: #FF6B9D;
            box-shadow: 0 0 0 3px rgba(255, 107, 157, 0.1);
        }

        .gregorian-datepicker {
            padding: 20px;
            background: linear-gradient(135deg, #fff5f8 0%, #ffe8f0 100%);
            border-radius: 12px;
            border: 2px solid #ffd4e5;
        }

        .datepicker-field-full {
            display: flex;
            flex-direction: column;
        }

        .datepicker-field-full label {
            font-size: 13px;
            font-weight: 600;
            color: #FF6B9D;
            margin-bottom: 8px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .datepicker-input-full {
            padding: 14px;
            border: 2px solid #ffd4e5;
            border-radius: 8px;
            font-size: 16px;
            background: white;
            transition: all 0.3s;
        }

        .datepicker-input-full:focus {
            outline: none;
            border-color: #FF6B9D;
            box-shadow: 0 0 0 3px rgba(255, 107, 157, 0.1);
        }

        .date-preview {
            margin-top: 15px;
            padding: 12px;
            background: #e7f3ff;
            border-left: 4px solid #2196F3;
            border-radius: 4px;
            font-size: 15px;
            color: #0d47a1;
            font-weight: 500;
            display: none;
        }

        .date-preview.show {
            display: block;
        }

        .btn {
            background: linear-gradient(135deg, #FF6B9D 0%, #FFC371 100%);
            color: white;
            padding: 14px 30px;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s;
            width: 100%;
            margin-top: 10px;
        }

        .btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 20px rgba(255, 107, 157, 0.4);
        }

        .btn-small {
            padding: 8px 16px;
            font-size: 14px;
            width: auto;
            margin: 0 5px;
        }

        .btn-secondary {
            background: #6c757d;
        }

        .btn-danger {
            background: #dc3545;
        }

        .simcha-list {
            margin-top: 30px;
        }

        .simcha-item {
            background: #fff5f8;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 10px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-left: 4px solid #FF6B9D;
        }

        .simcha-info {
            flex: 1;
        }

        .simcha-name {
            font-weight: 600;
            font-size: 18px;
            color: #333;
            margin-bottom: 5px;
        }

        .simcha-type {
            color: #FF6B9D;
            font-weight: 600;
            margin-bottom: 5px;
        }

        .simcha-date {
            color: #666;
            font-size: 14px;
        }

        .simcha-actions {
            display: flex;
            gap: 10px;
        }

        .message {
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
            display: none;
        }

        .message.success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }

        .message.error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }

        .info-box {
            background: #e7f3ff;
            border-left: 4px solid #2196F3;
            padding: 12px;
            margin-bottom: 15px;
            border-radius: 4px;
            font-size: 14px;
            color: #0d47a1;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .hebrew-datepicker {
                grid-template-columns: 1fr;
            }

            .date-type-selector {
                flex-direction: column;
                gap: 10px;
            }

            .date-type-btn {
                width: 100%;
            }

            .simcha-item {
                flex-direction: column;
                align-items: flex-start;
            }

            .simcha-actions {
                margin-top: 10px;
                width: 100%;
            }

            .simcha-actions button {
                flex: 1;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <a href='/' class='back-btn'>← Back to Settings</a>
            <h1>🎉 Simchas Manager</h1>
            <div style='width: 140px;'></div>
        </div>

        <div id='message' class='message'></div>

        <div class='card'>
            <h2 id='formTitle'>Add New Simcha</h2>
            <form id='simchaForm'>
                <input type='hidden' id='editId' />
                <input type='hidden' id='hebrewDay' />
                <input type='hidden' id='hebrewMonth' />
                <input type='hidden' id='hebrewYear' />
                <input type='hidden' id='hebrewDateString' />
                <input type='hidden' id='englishDate' />

                <div class='form-group'>
                    <label for='name'>Name</label>
                    <input type='text' id='name' required placeholder='Enter name' />
                </div>

                <div class='form-group'>
                    <label for='type'>Type</label>
                    <select id='type' required>
                        <option value=''>Select type...</option>
                        <option value='Hebrew Birthday'>🎂 Hebrew Birthday</option>
                        <option value='Bar Mitzvah'>📜 Bar Mitzvah</option>
                        <option value='Bat Mitzvah'>📜 Bat Mitzvah</option>
                        <option value='Wedding'>💒 Wedding</option>
                        <option value='Engagement'>💍 Engagement</option>
                        <option value='Brit Milah'>👶 Brit Milah</option>
                        <option value='Pidyon HaBen'>🕊️ Pidyon HaBen</option>
                        <option value='Upsherin'>✂️ Upsherin</option>
                        <option value='Anniversary'>💝 Anniversary</option>
                        <option value='Other'>🎉 Other</option>
                    </select>
                </div>

                <div class='form-group'>
                    <label>Date Entry Method</label>
                    <div class='date-type-selector'>
                        <button type='button' class='date-type-btn active' id='hebrewBtn'>
                            📅 Hebrew Calendar
                        </button>
                        <button type='button' class='date-type-btn' id='gregorianBtn'>
                            📆 Gregorian Calendar
                        </button>
                    </div>
                </div>

                <div id='hebrewDateSection' class='date-input-section active'>
                    <div class='info-box'>
                        📅 Enter the date in the Hebrew calendar (e.g., 15 Nisan 5784)
                    </div>
                    <div class='hebrew-datepicker'>
                        <div class='datepicker-field'>
                            <label for='hebrewDayInput'>Day</label>
                            <select id='hebrewDayInput' class='datepicker-select'></select>
                        </div>
                        <div class='datepicker-field'>
                            <label for='hebrewMonthInput'>Month</label>
                            <select id='hebrewMonthInput' class='datepicker-select'>
                                <option value=''>Select month...</option>
                                <option value='1'>Tishrei (תִּשְׁרֵי)</option>
                                <option value='2'>Cheshvan (חֶשְׁוָן)</option>
                                <option value='3'>Kislev (כִּסְלֵו)</option>
                                <option value='4'>Tevet (טֵבֵת)</option>
                                <option value='5'>Shevat (שְׁבָט)</option>
                                <option value='6'>Adar (אֲדָר)</option>
                                <option value='7'>Nisan (נִיסָן)</option>
                                <option value='8'>Iyar (אִיָּר)</option>
                                <option value='9'>Sivan (סִיוָן)</option>
                                <option value='10'>Tammuz (תַּמּוּז)</option>
                                <option value='11'>Av (אָב)</option>
                                <option value='12'>Elul (אֱלוּל)</option>
                            </select>
                        </div>
                        <div class='datepicker-field'>
                            <label for='hebrewYearInput'>Year</label>
                            <input type='number' id='hebrewYearInput' class='datepicker-input' placeholder='e.g., 5784' min='5000' max='6000' />
                        </div>
                    </div>
                    <div id='hebrewDatePreview' class='date-preview'></div>
                </div>

                <div id='gregorianDateSection' class='date-input-section'>
                    <div class='info-box'>
                        📆 Enter the date in the Gregorian calendar (e.g., January 15, 2024). It will be automatically converted to the Hebrew calendar for storage.
                    </div>
                    <div class='gregorian-datepicker'>
                        <div class='datepicker-field-full'>
                            <label for='gregorianDateInput'>Date</label>
                            <input type='date' id='gregorianDateInput' class='datepicker-input-full' />
                        </div>
                    </div>
                    <div id='gregorianDatePreview' class='date-preview'></div>
                </div>

                <div class='form-group'>
                    <label for='notes'>Notes (Optional)</label>
                    <textarea id='notes' rows='3' placeholder='Add any additional information'></textarea>
                </div>

                <button type='submit' class='btn'>Save Simcha</button>
                <button type='button' id='cancelEdit' class='btn btn-secondary' style='display:none;'>Cancel</button>
            </form>
        </div>

        <div class='card'>
            <h2>Saved Simchas</h2>
            <div id='simchaList' class='simcha-list'></div>
        </div>
    </div>

    <script>
        let currentDateType = 'hebrew';

        // Populate day dropdown
        const daySelect = document.getElementById('hebrewDayInput');
        for (let i = 1; i <= 30; i++) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = i;
            daySelect.appendChild(option);
        }

        const monthNames = {
            1: 'Tishrei', 2: 'Cheshvan', 3: 'Kislev', 4: 'Tevet',
            5: 'Shevat', 6: 'Adar', 7: 'Nisan', 8: 'Iyar',
            9: 'Sivan', 10: 'Tammuz', 11: 'Av', 12: 'Elul'
        };

        // Setup date type buttons with event listeners
        document.getElementById('hebrewBtn').addEventListener('click', function() {
            switchDateType('hebrew');
        });

        document.getElementById('gregorianBtn').addEventListener('click', function() {
            switchDateType('gregorian');
        });

        function switchDateType(type) {
            currentDateType = type;

            // Update button states
            document.getElementById('hebrewBtn').classList.toggle('active', type === 'hebrew');
            document.getElementById('gregorianBtn').classList.toggle('active', type === 'gregorian');

            // Show/hide date sections with animation
            document.getElementById('hebrewDateSection').classList.toggle('active', type === 'hebrew');
            document.getElementById('gregorianDateSection').classList.toggle('active', type === 'gregorian');
        }

        // Hebrew date preview
        function updateHebrewDatePreview() {
            const day = document.getElementById('hebrewDayInput').value;
            const month = document.getElementById('hebrewMonthInput').value;
            const year = document.getElementById('hebrewYearInput').value;
            const preview = document.getElementById('hebrewDatePreview');

            if (day && month && year) {
                preview.textContent = '📅 Selected: ' + day + ' ' + monthNames[parseInt(month)] + ' ' + year;
                preview.classList.add('show');
            } else {
                preview.classList.remove('show');
            }
        }

        document.getElementById('hebrewDayInput').addEventListener('change', updateHebrewDatePreview);
        document.getElementById('hebrewMonthInput').addEventListener('change', updateHebrewDatePreview);
        document.getElementById('hebrewYearInput').addEventListener('input', updateHebrewDatePreview);

        // Gregorian date preview
        function updateGregorianDatePreview() {
            const dateInput = document.getElementById('gregorianDateInput').value;
            const preview = document.getElementById('gregorianDatePreview');

            if (dateInput) {
                const date = new Date(dateInput);
                const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
                const gregorianDisplay = date.toLocaleDateString('en-US', options);

                // Convert to Hebrew date for preview
                fetch('/api/convert-to-hebrew', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ gregorianDate: dateInput })
                })
                .then(response => response.json())
                .then(hebrewDate => {
                    const hebrewDisplay = hebrewDate.hebrewDay + ' ' + monthNames[hebrewDate.hebrewMonth] + ' ' + hebrewDate.hebrewYear;
                    preview.innerHTML = '📆 Selected: ' + gregorianDisplay + '<br/>🔄 Converts to: ' + hebrewDisplay + ' (Hebrew)';
                    preview.classList.add('show');
                })
                .catch(() => {
                    preview.textContent = '📆 Selected: ' + gregorianDisplay;
                    preview.classList.add('show');
                });
            } else {
                preview.classList.remove('show');
            }
        }

        document.getElementById('gregorianDateInput').addEventListener('change', updateGregorianDatePreview);

        function showMessage(text, type) {
            const message = document.getElementById('message');
            message.textContent = text;
            message.className = 'message ' + type;
            message.style.display = 'block';
            setTimeout(() => message.style.display = 'none', 5000);
        }

        async function loadSimchas() {
            try {
                const response = await fetch('/api/simchas');
                const simchas = await response.json();

                const listDiv = document.getElementById('simchaList');
                if (simchas.length === 0) {
                    listDiv.innerHTML = '<p style="color: #666;">No simchas recorded yet. Add your first joyous occasion above!</p>';
                    return;
                }

                listDiv.innerHTML = simchas.map(s => {
                    const dateDisplay = s.hebrewDate || 'No date';
                    return '<div class="simcha-item">' +
                        '<div class="simcha-info">' +
                        '<div class="simcha-name">' + s.name + '</div>' +
                        '<div class="simcha-type">' + s.type + '</div>' +
                        '<div class="simcha-date">📅 ' + dateDisplay + '</div>' +
                        (s.notes ? '<div class="simcha-date" style="margin-top: 5px; font-style: italic;">' + s.notes + '</div>' : '') +
                        '</div>' +
                        '<div class="simcha-actions">' +
                        '<button class="btn btn-small btn-secondary" onclick="editSimcha(' + s.id + ')">Edit</button>' +
                        '<button class="btn btn-small btn-danger" onclick="deleteSimcha(' + s.id + ')">Delete</button>' +
                        '</div>' +
                        '</div>';
                }).join('');
            } catch (error) {
                showMessage('Failed to load simchas: ' + error.message, 'error');
            }
        }

        async function editSimcha(id) {
            try {
                const response = await fetch('/api/simchas/' + id);
                const simcha = await response.json();

                document.getElementById('editId').value = id;
                document.getElementById('name').value = simcha.name;
                document.getElementById('type').value = simcha.type;
                document.getElementById('notes').value = simcha.notes || '';

                // Always use Hebrew date since that's what we store
                if (simcha.hebrewDay && simcha.hebrewMonth && simcha.hebrewYear) {
                    switchDateType('hebrew');
                    document.getElementById('hebrewDayInput').value = simcha.hebrewDay;
                    document.getElementById('hebrewMonthInput').value = simcha.hebrewMonth;
                    document.getElementById('hebrewYearInput').value = simcha.hebrewYear;
                    updateHebrewDatePreview();
                }

                document.getElementById('formTitle').textContent = 'Edit Simcha';
                document.getElementById('cancelEdit').style.display = 'inline-block';

                window.scrollTo({ top: 0, behavior: 'smooth' });
            } catch (error) {
                showMessage('Failed to load simcha: ' + error.message, 'error');
            }
        }

        async function deleteSimcha(id) {
            if (!confirm('Are you sure you want to delete this simcha?')) return;

            try {
                const response = await fetch('/api/simchas/' + id, {
                    method: 'DELETE'
                });

                if (response.ok) {
                    showMessage('Simcha deleted successfully', 'success');
                    loadSimchas();
                } else {
                    showMessage('Failed to delete simcha', 'error');
                }
            } catch (error) {
                showMessage('Error: ' + error.message, 'error');
            }
        }

        document.getElementById('cancelEdit').addEventListener('click', () => {
            document.getElementById('simchaForm').reset();
            document.getElementById('editId').value = '';
            document.getElementById('formTitle').textContent = 'Add New Simcha';
            document.getElementById('cancelEdit').style.display = 'none';
        });

        document.getElementById('simchaForm').addEventListener('submit', async (e) => {
            e.preventDefault();

            const editId = document.getElementById('editId').value;

            // Build simcha object based on current date type
            const simcha = {
                name: document.getElementById('name').value.trim(),
                type: document.getElementById('type').value,
                notes: document.getElementById('notes').value.trim(),
                isRecurring: true
            };

            if (currentDateType === 'hebrew') {
                const day = parseInt(document.getElementById('hebrewDayInput').value);
                const month = parseInt(document.getElementById('hebrewMonthInput').value);
                const year = parseInt(document.getElementById('hebrewYearInput').value);

                if (!day || !month || !year) {
                    showMessage('Please enter all Hebrew date fields', 'error');
                    return;
                }

                simcha.hebrewDay = day;
                simcha.hebrewMonth = month;
                simcha.hebrewYear = year;
                simcha.hebrewDate = day + ' ' + monthNames[month] + ' ' + year;
                simcha.englishDate = null;
            } else {
                // Gregorian mode - convert to Hebrew first
                const gregorianDate = document.getElementById('gregorianDateInput').value;
                if (!gregorianDate) {
                    showMessage('Please enter a Gregorian date', 'error');
                    return;
                }

                // Convert Gregorian to Hebrew via API
                try {
                    const convertResponse = await fetch('/api/convert-to-hebrew', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ gregorianDate: gregorianDate })
                    });

                    if (!convertResponse.ok) {
                        showMessage('Failed to convert date to Hebrew calendar', 'error');
                        return;
                    }

                    const hebrewDate = await convertResponse.json();

                    simcha.hebrewDay = hebrewDate.hebrewDay;
                    simcha.hebrewMonth = hebrewDate.hebrewMonth;
                    simcha.hebrewYear = hebrewDate.hebrewYear;
                    simcha.hebrewDate = hebrewDate.hebrewDay + ' ' + monthNames[hebrewDate.hebrewMonth] + ' ' + hebrewDate.hebrewYear;
                    simcha.englishDate = null;
                } catch (error) {
                    showMessage('Error converting date: ' + error.message, 'error');
                    return;
                }
            }

            try {
                const url = editId ? '/api/simchas/' + editId : '/api/simchas';
                const method = editId ? 'PUT' : 'POST';

                const response = await fetch(url, {
                    method: method,
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(simcha)
                });

                if (response.ok) {
                    showMessage(editId ? 'Simcha updated successfully!' : 'Simcha added successfully!', 'success');
                    document.getElementById('simchaForm').reset();
                    document.getElementById('editId').value = '';
                    document.getElementById('formTitle').textContent = 'Add New Simcha';
                    document.getElementById('cancelEdit').style.display = 'none';
                    switchDateType('hebrew');
                    loadSimchas();
                } else {
                    showMessage('Failed to save simcha', 'error');
                }
            } catch (error) {
                showMessage('Error: ' + error.message, 'error');
            }
        });

        loadSimchas();
    </script>
</body>
</html>
""";
        }
    }
}
