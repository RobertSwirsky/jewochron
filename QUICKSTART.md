# Quick Start Guide - Yahrzeit Web Interface

## 🚀 Get Started in 5 Minutes!

### Step 1: Install MySQL (Choose ONE option)

**Option A: MySQL Installer (Windows)**
```
1. Download from: https://dev.mysql.com/downloads/installer/
2. Run installer, choose "Developer Default"
3. Set root password (remember this!)
4. Complete installation
```

**Option B: Docker (Any Platform)**
```bash
docker run --name mysql-jewochron -e MYSQL_ROOT_PASSWORD=jewochron123 -p 3306:3306 -d mysql:latest
```

### Step 2: Update Connection String

Open `App.xaml.cs` and find this line (~line 41):

```csharp
string connectionString = "server=localhost;port=3306;database=jewochron;user=root;password=your_password";
```

**Update it with YOUR password:**
```csharp
// If you used Docker:
string connectionString = "server=localhost;port=3306;database=jewochron;user=root;password=jewochron123";

// If you used MySQL Installer:
string connectionString = "server=localhost;port=3306;database=jewochron;user=root;password=YOUR_MYSQL_PASSWORD";
```

### Step 3: Run the App

1. **Build**: Press `Ctrl+Shift+B` in Visual Studio
2. **Run**: Press `F5`
3. **Open Browser**: Navigate to `http://localhost:5555`

### Step 4: Add Your First Yahrzeit

1. Select Hebrew Month (e.g., "Tishrei")
2. Select Hebrew Day (e.g., "10")
3. Enter Hebrew Year (e.g., "5784")
4. Enter Name in English (e.g., "Abraham Cohen")
5. Enter Name in Hebrew (e.g., "אברהם בן יצחק")
6. Click "Save Yahrzeit"

**Done!** 🎉

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

### "Connection failed" error
```bash
# Check if MySQL is running:
mysql -u root -p

# Or for Docker:
docker ps | grep mysql-jewochron
```

### Port 5555 already in use
Edit `Services/YahrzeitWebServer.cs` line 18:
```csharp
private readonly int _port = 5556; // Changed from 5555
```

### Database doesn't exist
The app creates it automatically! Just make sure MySQL is running.

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
