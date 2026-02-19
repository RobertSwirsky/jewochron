# Jewochron - Jewish Calendar Digital Sign

A beautiful WinUI 3 desktop application that displays Jewish calendar information, Halachic times, Torah readings, Yahrzeits and more.

<img width="1642" height="1952" alt="image" src="https://github.com/user-attachments/assets/98f30c49-c436-442b-8940-e80398c17649" />



## Features

- **📅 Dates**: Display current date in English and Hebrew (both transliterated and in Hebrew characters)
- **🌆 Jerusalem Skyline**: Animated Jerusalem skyline with dynamic time-of-day visuals (dawn, sunrise, day, sunset, dusk, night)
  - Responsive scaling via Viewbox - fits any screen width while maintaining aspect ratio
  - Phase-accurate moon that reflects actual lunar illumination percentage
  - Sun and moon positioned high in the sky with natural arc movement
- **⏰ Live Clocks**: Real-time display of local time and Jerusalem time side-by-side
- **📍 Location**: Automatic location detection (City, State) with user permission
- **📖 Torah Portion**: Current week's Parsha in English and Hebrew with decorative Torah scroll illustration
- **📚 Daf Yomi**: Today's Talmud page in English and Hebrew with Hebrew numerals
- **🕐 Halachic Times**: Location-based calculations for:
  - Alot HaShachar (Dawn)
  - Sunrise (נץ החמה)
  - Sunset (שקיעה)
  - Tzait HaKochavim (Nightfall)
- **🌙 Molad & Moon Phase**: Next Molad (new moon) with exact time and Chalakim, plus current lunar phase details
- **🎉 Next Holiday**: Countdown to the next Jewish holiday with days remaining
- **✡️ Prayer Times**: Visual prayer times with animated praying man illustration
  - Shacharit, Mincha, and Maariv times with current prayer indicator
  - Detailed illustration showing man holding Siddur (prayer book)
  - Dynamic display of Tallit (prayer shawl) during Shacharit
  - Dynamic display of Tefillin (phylacteries) during weekday Shacharit
- **🎨 Dark Mode**: Beautiful dark theme with responsive card layout
- **📖 Yahrzeit Manager**: Built-in web interface for managing yahrzeit dates (memorial anniversaries)
  - Accessible at `http://localhost:5555` when app is running
  - Add, edit, and delete yahrzeit entries
  - Store Hebrew dates with bilingual names
  - SQLite database (zero configuration, embedded)

## Yahrzeit Web Interface

The application includes an embedded web server for managing yahrzeit (memorial anniversary) dates.

### Accessing the WebUI

1. **Run the Jewochron application** (Press F5 in Visual Studio)
2. **Open your web browser** (Chrome, Edge, Firefox, Safari)
3. **Navigate to:** `http://localhost:5555`

### Features

- ✅ **Add Yahrzeits**: Enter Hebrew month, day, year, and names in English and Hebrew
- ✅ **View All**: See all saved yahrzeits sorted by Hebrew date
- ✅ **Edit Entries**: Modify existing yahrzeit information
- ✅ **Delete Entries**: Remove yahrzeits with confirmation
- ✅ **Beautiful UI**: Modern purple-gradient design with responsive layout
- ✅ **Hebrew Support**: Right-to-left text input for Hebrew names
- ✅ **Zero Setup**: No database installation required - uses embedded SQLite

### Database Location

The SQLite database is automatically created at:
```
%LocalAppData%\Jewochron\yahrzeits.db

Example: C:\Users\YourName\AppData\Local\Jewochron\yahrzeits.db
```

### Backup Your Data

Simply copy the database file to backup your yahrzeits:
```powershell
Copy-Item "$env:LOCALAPPDATA\Jewochron\yahrzeits.db" "D:\Backups\"
```

### API Endpoints

The web server provides a REST API:
- `GET /api/yahrzeits` - Get all yahrzeits
- `GET /api/yahrzeits/{id}` - Get specific yahrzeit
- `POST /api/yahrzeits` - Create new yahrzeit
- `PUT /api/yahrzeits/{id}` - Update yahrzeit
- `DELETE /api/yahrzeits/{id}` - Delete yahrzeit

For more details, see:
- **QUICKSTART.md** - Quick setup guide
- **DATABASE_SETUP.md** - Database information
- **YAHRZEIT_WEBUI_SUMMARY.md** - Complete feature documentation
- **SQLITE_MIGRATION.md** - Technical details about SQLite

## Architecture

The application has been refactored into a modular architecture:

### Services Layer (`Services/`)

- **HebrewCalendarService.cs**: Hebrew date conversions, Hebrew numeral formatting, and Shabbat detection
- **TorahPortionService.cs**: Calculates current Torah portion based on Hebrew calendar
- **DafYomiService.cs**: Calculates current Daf Yomi page with Hebrew formatting
- **HalachicTimesService.cs**: Location-based Halachic time calculations (sunrise, sunset, dawn, nightfall)
- **MoonPhaseService.cs**: Detailed lunar phase calculations with illumination percentage
- **MoladService.cs**: Calculates next Molad (new moon) with exact time, Chalakim, and Rosh Chodesh information
- **JewishHolidaysService.cs**: Tracks upcoming Jewish holidays with countdown
- **LocationService.cs**: Geolocation and reverse geocoding

### UI Layer (`Views/`)

- **MainPage.xaml**: 
  - Responsive card-based layout with adaptive visual states
  - Detailed Jerusalem skyline graphic with iconic landmarks (Dome of the Rock, Western Wall, Temple Mount, etc.)
  - Dynamic sky colors and celestial objects (sun, moon, stars) that change based on Jerusalem time
  - Decorative Torah scroll illustration with Ashkenazi-style design
  - Realistic praying man illustration with detailed Siddur (prayer book), Tallit, and Tefillin
- **MainPage.xaml.cs**: 
  - View logic coordinating all services
  - Real-time clock updates (1-second intervals)
  - Dynamic skyline animation based on Jerusalem time of day
  - Prayer time-based attire updates (Tallit/Tefillin visibility)
  - Data refresh on date change and prayer period transitions

## Visual Highlights

### Jerusalem Skyline
- Detailed architectural illustration featuring iconic landmarks
- **Responsive scaling**: Automatically scales to fit any screen width using Viewbox
- **Phase-accurate moon**: Displays actual lunar phase with shadow overlay based on real illumination percentage
- Animated sun and moon that move across the sky based on real Jerusalem time
- Celestial bodies positioned high in sky with proper arc movement throughout day/night
- Dynamic sky colors transitioning through dawn, sunrise, day, sunset, dusk, and night
- Stars of David visible at night, fading during dawn/dusk transitions
- Star of David decorations on buildings
- Traditional Jewish symbols integrated throughout

### Prayer Man Illustration
- Realistic figure with proper proportions and anatomy
- Prominently displayed burgundy Siddur (prayer book) with:
  - Gold Star of David emblem on cover
  - Visible cream-colored pages with Hebrew text lines
  - Detailed leather-like texture
- Enhanced facial features including closed eyes in prayer, nose, smile, and detailed beard
- Professional attire with suit, tie, and proper hand positioning
- Dynamic religious items that appear based on prayer time:
  - **Tallit** (prayer shawl): Shown during Shacharit with traditional blue stripes and Tzitzit fringes
  - **Tefillin** (phylacteries): Shown during weekday Shacharit with embossed Shin letter and proper strap positioning

## Responsive Design

The app features **aspect-ratio aware responsive design** specifically optimized for synagogue digital displays:

### Portrait Mode (9:16)
- **Use case**: Vertical entrance signs, hallway displays
- **Layout**: Single-column card stack
- **Skyline**: Scaled down (50%) to fit vertical space
- **Time display**: Stacks vertically (Local time above Jerusalem time)
- **Cards**: Full width, compact padding
- **Font sizes**: Optimized for vertical reading

### Landscape Narrow (16:9 at smaller sizes)
- **Use case**: Smaller displays, windowed mode, testing
- **Resolution**: Width < 1400px
- **Layout**: Three-column card grid
- **Skyline**: Responsive scaling to fit available width
- **Time display**: Side-by-side horizontal layout
- **Cards**: Standard padding and fonts

### Landscape Wide (16:9 at larger sizes)
- **Use case**: Large sanctuary displays, social hall screens
- **Resolution**: 1920x1080, 2560x1440, 3840x2160 (width >= 1400px)
- **Layout**: Three-column card grid with enhanced spacing
- **Skyline**: Full size (1200px) with optimal visibility
- **Font sizes**: Larger for far-viewing (56px clocks, 42px dates)
- **Padding**: Generous spacing for comfortable viewing from distance

### Adaptive Features
- **Automatic detection**: Layout switches automatically based on window size
- **Smooth transitions**: Visual state changes animate smoothly
- **Handles all ratios**: Also adapts to 4:3, square, and other aspect ratios
- **Real-time adjustment**: Updates immediately when display orientation or size changes

The app uses **VisualStateManager** with custom **aspect ratio detection** to ensure optimal display in any configuration.

## Usage for Synagogue Digital Displays

### Recommended Display Configurations

**Landscape (16:9) - Most Common**
- **Resolution**: 1920x1080 (Full HD), 2560x1440 (2K), or 3840x2160 (4K)
- **Placement**: Sanctuary, social hall, or main entrance
- **Layout**: Three-column card grid with large fonts
- **Optimal viewing distance**: 6-20 feet

**Portrait (9:16) - Vertical Displays**
- **Resolution**: 1080x1920 (rotate 1080p display)
- **Placement**: Entrance hallways, narrow wall spaces
- **Layout**: Single-column stack, compact design
- **Optimal viewing distance**: 3-10 feet

### Setup Instructions for Kiosk Mode

1. **Set display to run full-screen**:
   - Press `F11` in the app or use Windows kiosk mode
   - Configure display settings to match physical orientation

2. **Prevent screen sleep**:
   - Windows Settings → System → Power & Sleep
   - Set "Screen" to "Never" when plugged in

3. **Auto-start on boot** (optional):
   - Place shortcut in: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`

4. **Hide taskbar** (optional):
   - Right-click taskbar → Taskbar settings → "Automatically hide taskbar"

## Permissions Required

- **Location**: For accurate Halachic times calculation
- **Internet**: For reverse geocoding (city/state lookup)

## Technologies

- **.NET 10** (latest)
- **WinUI 3** (Windows App SDK)
- **MSIX Packaging** for deployment
- **C# 14** with latest language features

## Building

1. Ensure you have Visual Studio 2022 or later with:
   - .NET 10 SDK
   - Windows App SDK
   - Windows 10 SDK (10.0.19041.0 or later)

2. Open `Jewochron.slnx`

3. Set platform to `x64` or `x86`

4. Build and run (F5)

## First Run

On first run, the app will request location permission. You can:
- **Allow**: Get accurate Halachic times for your location
- **Deny**: App will default to New York, NY

## Notes

- Torah portion calculations are simplified and may not account for all calendar variations
- Halachic times use standard astronomical calculations
- For production use, consider integrating a dedicated Jewish calendar library like KosherJava

## License

MIT License - Feel free to use and modify
