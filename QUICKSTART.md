# Quick Start Guide - Yahrzeit Web Interface

## 🚀 Get Started in 1 Minute!

### That's It! (Seriously)

SQLite is embedded - **no database installation needed!**

Just follow these steps:

### Step 1: Run the App

1. **Build**: Press `Ctrl+Shift+B` in Visual Studio
2. **Run**: Press `F5`
3. **Open Browser**: Navigate to `http://localhost:5555`

### Step 2: Add Your First Yahrzeit

1. Select Hebrew Month (e.g., "Tishrei")
2. Select Hebrew Day (e.g., "10")
3. Enter Hebrew Year (e.g., "5784")
4. Enter Name in English (e.g., "Abraham Cohen")
5. Enter Name in Hebrew (e.g., "אברהם בן יצחק")
6. Click "Save Yahrzeit"

**Done!** 🎉

The database is automatically created at:
```
%LocalAppData%\Jewochron\yahrzeits.db
```

## 📝 Example Data

Try adding these sample entries:

### Entry 1
- Month: **Tishrei (1)**
- Day: **10**
- Year: **5783**
- English: **Sarah Goldberg**
- Hebrew: **שרה בת משה**

### Entry 2
- Month: **Shevat (5)**
- Day: **15**
- Year: **5780**
- English: **David Levi**
- Hebrew: **דוד בן אברהם**

### Entry 3
- Month: **Adar (6)**
- Day: **7**
- Year: **5775**
- English: **Rachel Schwartz**
- Hebrew: **רחל בת יעקב**

## ⚡ Pro Tips

1. **Hebrew Input**: Click in the Hebrew name field - it automatically switches to right-to-left text!

2. **Edit Entries**: Click the "Edit" button next to any yahrzeit to modify it

3. **Quick Delete**: Click "Delete" to remove an entry (it will ask for confirmation)

4. **Sorted List**: All yahrzeits are automatically sorted by Hebrew month and day

## 🛠️ Troubleshooting

### Port 5555 already in use
Edit `Services/YahrzeitWebServer.cs` line 18:
```csharp
private readonly int _port = 5556; // Changed from 5555
```

### Where is my database?
Check the Debug output window in Visual Studio for the exact path, or navigate to:
```
%LocalAppData%\Jewochron\yahrzeits.db
```

### Want to backup your data?
Simply copy the `yahrzeits.db` file to your backup location!

## 🎯 What's Next?

- Add all your family yahrzeits
- Keep the app running to access the web interface anytime
- Share the URL with family members on your network: `http://YOUR_IP:5555`

## 📞 Need Help?

Check these files for more details:
- `DATABASE_SETUP.md` - Detailed database configuration
- `YAHRZEIT_WEBUI_SUMMARY.md` - Complete feature documentation

---

**Enjoy managing your Yahrzeits! 🕯️**
