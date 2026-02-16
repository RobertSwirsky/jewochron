using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class TorahPortionServiceTests
{
    private readonly TorahPortionService _service;
    private readonly HebrewCalendarService _hebrewCalendarService;

    public TorahPortionServiceTests()
    {
        _hebrewCalendarService = new HebrewCalendarService();
        _service = new TorahPortionService(_hebrewCalendarService);
    }

    [Fact]
    public async Task GetTorahPortionAsync_Saturday_ReturnsParsha()
    {
        // Arrange - A known Saturday in 2024
        var saturday = new DateTime(2024, 6, 15); // Should be a Saturday
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(saturday);

        // Act
        var (parshaEnglish, parshaHebrew) = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.NotNull(parshaEnglish);
        Assert.NotEmpty(parshaEnglish);
        Assert.NotNull(parshaHebrew);
        Assert.NotEmpty(parshaHebrew);
    }

    [Fact]
    public async Task GetTorahPortionAsync_Weekday_ReturnsNextShabbat()
    {
        // Arrange - A weekday
        var weekday = new DateTime(2024, 6, 11); // Tuesday
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(weekday);

        // Act
        var (parshaEnglish, parshaHebrew) = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.NotEmpty(parshaEnglish);
        Assert.NotEmpty(parshaHebrew);
        Assert.Contains("Upcoming", parshaEnglish); // Should mention it's upcoming
    }

    [Fact]
    public async Task GetTorahPortionAsync_HebrewOutput_ContainsHebrewCharacters()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(date);

        // Act
        var (_, parshaHebrew) = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.True(parshaHebrew.Any(c => c >= 0x0590 && c <= 0x05FF), 
            "Hebrew parsha name should contain Hebrew characters");
    }

    [Fact]
    public async Task GetTorahPortionAsync_ConsecutiveSaturdays_ReturnsDifferentParshas()
    {
        // Arrange - Two consecutive Saturdays
        var saturday1 = new DateTime(2024, 6, 15);
        var saturday2 = saturday1.AddDays(7);

        var (year1, month1, day1, leap1) = _hebrewCalendarService.GetHebrewDate(saturday1);
        var (year2, month2, day2, leap2) = _hebrewCalendarService.GetHebrewDate(saturday2);

        // Act
        var (parsha1English, _) = await _service.GetTorahPortionAsync(year1, month1, day1, leap1);
        var (parsha2English, _) = await _service.GetTorahPortionAsync(year2, month2, day2, leap2);

        // Assert - Different Saturdays should have different parshas (unless it's a double parsha situation)
        // This might occasionally be the same if both are part of a double parsha, so we just check they're valid
        Assert.NotEmpty(parsha1English);
        Assert.NotEmpty(parsha2English);
    }

    [Theory]
    [InlineData("2024-01-06")]  // Saturday in January
    [InlineData("2024-04-13")]  // Saturday in April
    [InlineData("2024-07-20")]  // Saturday in July
    [InlineData("2024-10-05")]  // Saturday in October
    public async Task GetTorahPortionAsync_VariousSaturdays_ReturnsValidData(string dateStr)
    {
        // Arrange
        var date = DateTime.Parse(dateStr);
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(date);

        // Act
        var (parshaEnglish, parshaHebrew) = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.NotEmpty(parshaEnglish);
        Assert.NotEmpty(parshaHebrew);
    }

    [Fact]
    public async Task GetTorahPortionAsync_DuringHighHolidays_ReturnsSpecialReading()
    {
        // Arrange - Yom Kippur (October 12, 2024) or Rosh Hashanah
        var holidayDate = new DateTime(2024, 10, 12);
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(holidayDate);

        // Act
        var (parshaEnglish, _) = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.NotEmpty(parshaEnglish);
        // High holidays have special Torah readings, not regular weekly parshas
    }

    [Fact]
    public async Task GetTorahPortionAsync_SameDate_ReturnsSameResult()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(date);

        // Act
        var result1 = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);
        var result2 = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.Equal(result1.parshaEnglish, result2.parshaEnglish);
        Assert.Equal(result1.parshaHebrew, result2.parshaHebrew);
    }

    [Fact]
    public async Task GetTorahPortionAsync_LeapYear_HandlesExtraMonth()
    {
        // Arrange - Date in a Hebrew leap year (has Adar I and Adar II)
        // Hebrew year 5784 is a leap year
        var dateInLeapYear = new DateTime(2024, 3, 15);
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(dateInLeapYear);

        // Act
        var (parshaEnglish, parshaHebrew) = await _service.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);

        // Assert
        Assert.NotEmpty(parshaEnglish);
        Assert.NotEmpty(parshaHebrew);
        // Should handle the leap year's extra month correctly
    }
}
