# Jewochron - Jewish Calendar Digital Sign

A beautiful WinUI 3 desktop application that displays Jewish calendar information, Halachic times, Torah readings, and more.

<img width="1833" height="1983" alt="image" src="https://github.com/user-attachments/assets/e067c7bb-2d89-4bc9-90ec-62e018ac9bdf" />

## Features

- **📅 Dates**: Display current date in English and Hebrew (both transliterated and in Hebrew characters)
- **🌆 Jerusalem Skyline**: Animated Jerusalem skyline with dynamic time-of-day visuals (dawn, sunrise, day, sunset, dusk, night)
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
- Animated sun and moon that move across the sky based on real Jerusalem time
- Dynamic sky colors transitioning through dawn, sunrise, day, sunset, dusk, and night
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

The app uses a responsive grid layout that automatically adjusts to window size:
- **Small windows**: Cards stack vertically in a single column
- **Medium windows**: Cards flow in 2 columns
- **Large windows**: Cards spread across multiple columns with a max width of 1400px

Each card is 380px wide and adapts to available space.

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
