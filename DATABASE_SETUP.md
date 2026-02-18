# Yahrzeit Database Setup

## SQLite Database

The application uses **SQLite**, a lightweight, embedded database that requires **NO installation or configuration**!

### Automatic Setup

The database is **automatically created** when you run the application for the first time. No setup required!

### Database Location

The SQLite database file is stored at:
```
%LocalAppData%\Jewochron\yahrzeits.db
```

**Example path:**
```
C:\Users\YourUsername\AppData\Local\Jewochron\yahrzeits.db
```

### Benefits of SQLite

✅ **Zero Configuration** - No server to install or configure  
✅ **Embedded** - Database file stored locally with your app  
✅ **Portable** - Copy the .db file to backup or transfer data  
✅ **Fast** - Perfect performance for single-user applications  
✅ **Reliable** - Battle-tested, used by millions of applications  

## Database Schema

The application automatically creates the `yahrzeits` table with the following structure:

```sql
CREATE TABLE yahrzeits (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    HebrewMonth INTEGER NOT NULL,
    HebrewDay INTEGER NOT NULL,
    HebrewYear INTEGER NOT NULL,
    NameEnglish TEXT NOT NULL,
    NameHebrew TEXT NOT NULL,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX idx_hebrew_date ON yahrzeits(HebrewMonth, HebrewDay);
```

## Accessing the Web Interface

Once the application is running, open your web browser and navigate to:

**http://localhost:5555**

The web interface provides:
- Add new yahrzeit entries
- View all saved yahrzeits
- Edit existing entries
- Delete entries

## Backup Your Data

### Manual Backup
Simply copy the database file:
```
Copy from: %LocalAppData%\Jewochron\yahrzeits.db
Copy to: Your backup location
```

### Restore from Backup
Replace the database file with your backup:
```
Copy your backup file to: %LocalAppData%\Jewochron\yahrzeits.db
```

## Viewing the Database

You can view/edit the SQLite database using these free tools:

1. **DB Browser for SQLite** (Recommended)
   - Download: https://sqlitebrowser.org/
   - Open: File → Open Database → Browse to yahrzeits.db

2. **SQLite Studio**
   - Download: https://sqlitestudio.pl/

3. **Visual Studio Code** with SQLite extension
   - Install "SQLite" extension
   - Right-click .db file → Open Database

## Troubleshooting

### Database locked error
- Close any SQLite browser tools
- Only one application can write to SQLite at a time

### Can't find database file
- Check the Debug output in Visual Studio for the exact path
- Look for: "Database location: C:\Users\..."

### Reset database
Delete the file and restart the app:
```
Delete: %LocalAppData%\Jewochron\yahrzeits.db
```
The app will create a fresh database on next launch.

## Advanced: Custom Database Location

To use a custom database location, modify `App.xaml.cs`:

```csharp
// Current (automatic):
string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
string dbPath = Path.Combine(appDataPath, "Jewochron", "yahrzeits.db");

// Custom location example:
string dbPath = @"D:\MyData\yahrzeits.db";
```
