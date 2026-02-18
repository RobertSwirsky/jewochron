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

        public YahrzeitWebServer(string databasePath)
        {
            _databasePath = databasePath;
        }

        public async Task StartAsync()
        {
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Configure services
                builder.Services.AddDbContext<YahrzeitDbContext>(options =>
                    options.UseSqlite($"Data Source={_databasePath}"));

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

                // Ensure database is created
                using (var scope = _app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<YahrzeitDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();
                }

                _app.UseCors();

                // API Endpoints
                MapApiEndpoints(_app);

                // Serve the HTML page
                _app.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(GetYahrzeitFormHtml());
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
                yahrzeit.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok(yahrzeit);
            });

            // DELETE yahrzeit
            app.MapDelete("/api/yahrzeits/{id}", async (int id, YahrzeitDbContext db) =>
            {
                var yahrzeit = await db.Yahrzeits.FindAsync(id);
                if (yahrzeit == null) return Results.NotFound();

                db.Yahrzeits.Remove(yahrzeit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
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

        h1 {
            color: white;
            text-align: center;
            margin-bottom: 30px;
            font-size: 2.5em;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
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
        <h1>📖 Yahrzeit Manager</h1>
        
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
                    <label for='nameHebrew'>Name (Hebrew)</label>
                    <input type='text' id='nameHebrew' class='hebrew-input' required 
                           placeholder='הזן שם בעברית'>
                </div>

                <div class='form-group'>
                    <label for='gender'>Gender</label>
                    <select id='gender' required>
                        <option value='M'>Male (זכר)</option>
                        <option value='F'>Female (נקבה)</option>
                    </select>
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
                document.getElementById('nameHebrew').value = yahrzeit.nameHebrew;
                document.getElementById('gender').value = yahrzeit.gender || 'M';

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
            
            const editId = document.getElementById('editId').value;
            const yahrzeit = {
                hebrewMonth: parseInt(document.getElementById('hebrewMonth').value),
                hebrewDay: parseInt(document.getElementById('hebrewDay').value),
                hebrewYear: parseInt(document.getElementById('hebrewYear').value),
                nameEnglish: document.getElementById('nameEnglish').value,
                nameHebrew: document.getElementById('nameHebrew').value,
                gender: document.getElementById('gender').value
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
    }
}
