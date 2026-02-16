# Jewochron - Jewish Calendar Digital Sign

A beautiful WinUI 3 desktop application that displays Jewish calendar information, Halachic times, Torah readings, and more.

<img width="1620" height="1885" alt="image" src="https://github.com/user-attachments/assets/8579ab75-510f-4909-8ef4-49e84d734176" />

## Features

- **📅 Dates**: Display current date in English and Hebrew (both transliterated and in Hebrew characters)
- **📍 Location**: Automatic location detection (City, State) with user permission
- **📖 Torah Portion**: Current week's Parsha in English and Hebrew
- **📚 Daf Yomi**: Today's Talmud page in English and Hebrew with Hebrew numerals
- **🕐 Halachic Times**: Location-based calculations for:
  - Alot HaShachar (Dawn)
  - Sunrise (נץ החמה)
  - Sunset (שקיעה)
  - Tzait HaKochavim (Nightfall)
- **🌙 Moon Phase**: Current lunar phase with emoji and name
- **🎨 Dark Mode**: Beautiful dark theme with responsive card layout

## Architecture

The application has been refactored into a modular architecture:

### Services Layer (`Services/`)

- **HebrewCalendarService.cs**: Hebrew date conversions and Hebrew numeral formatting
- **TorahPortionService.cs**: Calculates current Torah portion based on Hebrew calendar
- **DafYomiService.cs**: Calculates current Daf Yomi page
- **HalachicTimesService.cs**: Location-based Halachic time calculations
- **MoonPhaseService.cs**: Lunar phase calculations
- **LocationService.cs**: Geolocation and reverse geocoding

### UI Layer (`Views/`)

- **MainPage.xaml**: Responsive card-based layout using `VariableSizedWrapGrid`
- **MainPage.xaml.cs**: View logic that coordinates all services

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
