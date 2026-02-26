# Holiday Card Enhancement - Fast Times & Dual Dates

## Summary

Enhanced the "Next Holiday" card to show Hebrew and Gregorian dates plus automatic fast time calculation with distinction between 24-hour and dawn-to-dusk fasts.

## Changes Made

### 1. Service Layer Updates

**File**: `Services\JewishHolidaysService.cs`

**Enhanced Return Type:**
```csharp
public (string englishName, string hebrewName, DateTime date, int daysUntil, bool isFast, bool is24HourFast) GetNextHoliday(DateTime currentDate)
```

**Fast Day Classifications:**

#### 24-Hour Fasts (Sunset to Nightfall)
- ✡️ **Yom Kippur** (יום כיפור) - 10 Tishrei
- ✡️ **Tisha B'Av** (תשעה באב) - 9 Av

#### Dawn-to-Dusk Fasts (Alot HaShachar to Tzait)
- ⏰ **Fast of Gedaliah** (צום גדליה) - 3 Tishrei
- ⏰ **10th of Tevet** (צום עשרה בטבת) - 10 Tevet
- ⏰ **Fast of Esther** (תענית אסתר) - 13 Adar (or Adar II)
- ⏰ **17th of Tammuz** (צום שבעה עשר בתמוז) - 17 Tammuz

### 2. Display Layer Updates

**File**: `Views\MainPage.xaml.cs`

**Hebrew and English Dates:**
```csharp
// Get Hebrew date for the holiday
var (holidayHebrewYear, holidayHebrewMonth, holidayHebrewDay, holidayIsLeapYear) = hebrewCalendarService.GetHebrewDate(holidayDate);
string holidayHebrewMonthName = hebrewCalendarService.GetHebrewMonthNameInHebrew(holidayHebrewMonth, holidayIsLeapYear);
string holidayHebrewDayStr = hebrewCalendarService.ConvertToHebrewNumber(holidayHebrewDay);
string hebrewHolidayDate = $"{holidayHebrewDayStr} {holidayHebrewMonthName}";

// Format Gregorian date
string englishHolidayDate = holidayDate.ToString("MMMM d");

// Display both Hebrew and English dates
txtHolidayDate.Text = $"{hebrewHolidayDate} • {englishHolidayDate}";
```

**Fast Time Calculation:**

**24-Hour Fast Logic:**
```csharp
if (is24HourFast)
{
    // 24-hour fast: sunset to nightfall (Yom Kippur, Tisha B'Av)
    DateTime fastStart = holidayTimes.sunset.AddDays(-1); // Previous evening
    DateTime fastEnd = holidayTimes.tzait; // Nightfall (tzait)
    
    txtFastTimes.Text = $"⏰ Fast: {fastStart:dddd h:mm tt} - {fastEnd:dddd h:mm tt}";
    txtFastTimes.Visibility = Visibility.Visible;
}
```

**Dawn-to-Dusk Fast Logic:**
```csharp
else
{
    // Dawn-to-dusk fast: alot hashachar to nightfall
    DateTime fastStart = holidayTimes.alotHaShachar; // Dawn
    DateTime fastEnd = holidayTimes.tzait; // Nightfall
    
    txtFastTimes.Text = $"⏰ Fast: {fastStart:h:mm tt} - {fastEnd:h:mm tt}";
    txtFastTimes.Visibility = Visibility.Visible;
}
```

### 3. UI Layer Updates

**File**: `Views\MainPage.xaml`

**Added Fast Times Element:**
```xaml
<!-- Fast times (only shown for fast days) -->
<TextBlock
    x:Name="txtFastTimes"
    FontSize="14"
    HorizontalAlignment="Center"
    Foreground="#FFA07A"
    Margin="0,8,0,0"
    TextWrapping="Wrap"
    Visibility="Collapsed"
    Text="" />
```

**Styling:**
- Color: `#FFA07A` (Light orange - draws attention without being alarming)
- Initially hidden (`Visibility="Collapsed"`)
- Only shown when the next holiday is a fast day
- Wraps text for long time ranges

## Visual Results

### Example 1: Regular Holiday (Chanukah)
```
🎉 Next Holiday • חג הבא

Chanukah (1st candle)
חנוכה

42 days

Tuesday
כ״ה כסלו • December 25
```
*No fast times shown*

### Example 2: Dawn-to-Dusk Fast (Fast of Esther)
```
🎉 Next Holiday • חג הבא

Fast of Esther
תענית אסתר

5 days

Thursday
יג׳ אדר • March 13

⏰ Fast: 5:15 AM - 7:38 PM
```
*Same-day fast, time-only format*

### Example 3: 24-Hour Fast (Yom Kippur)
```
🎉 Next Holiday • חג הבא

Yom Kippur
יום כיפור

187 days

Tuesday
י׳ תשרי • October 12

⏰ Fast: Monday 6:42 PM - Tuesday 7:28 PM
```
*Multi-day fast with day names*

### Example 4: 24-Hour Fast (Tisha B'Av)
```
🎉 Next Holiday • חג הבא

Tisha B'Av
תשעה באב

289 days

Sunday
ט׳ אב • August 2

⏰ Fast: Saturday 8:15 PM - Sunday 9:03 PM
```
*Starts Saturday night, ends Sunday night*

## Technical Details

### Fast Time Calculations

**Times Used:**
- **Alot HaShachar** (Dawn): 72 minutes before sunrise
- **Tzait** (Nightfall): 42 minutes after sunset
- **Sunset**: Calculated for holiday date and location

**24-Hour Fast Calculation:**
```
Start: Previous day's sunset (holiday.date - 1 day)
End: Holiday day's nightfall (tzait)
Duration: ~25 hours (sunset to nightfall next day)
```

**Dawn-to-Dusk Fast Calculation:**
```
Start: Holiday day's dawn (alot hashachar)
End: Holiday day's nightfall (tzait)
Duration: ~14-15 hours (varies by season)
```

### Date Format Consistency

Matches the Shabbat card format:
```
Hebrew numeral + Hebrew month • English month + day
```

Examples:
- `ט׳ אדר • March 8`
- `י׳ תשרי • October 12`
- `יג׳ אדר • March 13`

## Jewish Law (Halacha) Accuracy

✅ **Yom Kippur**: Correctly shows ~25 hour fast (evening before to nightfall)
✅ **Tisha B'Av**: Correctly shows ~25 hour fast (evening before to nightfall)
✅ **Minor Fasts**: Correctly show dawn-to-nightfall (not sunrise to sunset)
✅ **Fast of Gedaliah**: Begins at dawn, not sunrise
✅ **Tzom Tammuz**: Begins at dawn, not sunrise
✅ **Tzom Tevet**: Begins at dawn, not sunrise
✅ **Ta'anit Esther**: Begins at dawn, not sunrise

## Benefits

1. **Clear Communication**: Users know exactly when to start and stop fasting
2. **Dual Dates**: No confusion about which date the holiday is on
3. **Automatic**: No manual configuration needed
4. **Accurate**: Uses proper halachic times (alot hashachar, tzait)
5. **Distinction**: 24-hour vs dawn-to-dusk clearly differentiated
6. **Location-Aware**: Times calculated for user's latitude/longitude

## Future Enhancements (Optional)

Possible additions:
- Time zone indicator for fasts when traveling
- Pre-fast meal reminder (seudah mafseket)
- Fast difficulty indicator based on day length
- Link to fast day prayers/readings
- Countdown to fast start/end

The current implementation provides all essential fast day information in a clear, halachically accurate format!
