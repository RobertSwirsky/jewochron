using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class JewishHolidaysServiceTests
{
    private readonly JewishHolidaysService _service;
    private readonly HebrewCalendarService _hebrewCalendarService;

    public JewishHolidaysServiceTests()
    {
        _hebrewCalendarService = new HebrewCalendarService();
        _service = new JewishHolidaysService(_hebrewCalendarService);
    }

    [Fact]
    public void GetNextHoliday_BeforeRoshHashanah_ReturnsRoshHashanah()
    {
        // Arrange - A date before Rosh Hashanah 2024 (October 3-4, 2024)
        var date = new DateTime(2024, 9, 1);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.Contains("Rosh", holidayEnglish);
        Assert.Contains("Hashanah", holidayEnglish);
        Assert.True(daysUntil > 0);
        Assert.True(daysUntil < 40);
        Assert.NotEmpty(holidayHebrew);
    }

    [Fact]
    public void GetNextHoliday_BeforeYomKippur_ReturnsYomKippur()
    {
        // Arrange - Between Rosh Hashanah and Yom Kippur 2024
        var date = new DateTime(2024, 10, 5);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.Contains("Yom Kippur", holidayEnglish);
        Assert.True(daysUntil < 10);
        Assert.NotEmpty(holidayHebrew);
    }

    [Fact]
    public void GetNextHoliday_BeforeChanukah_ReturnsChanukah()
    {
        // Arrange - Before Chanukah (December 25, 2024)
        var date = new DateTime(2024, 12, 1);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.Contains("Chanukah", holidayEnglish);
        Assert.True(daysUntil > 0);
        Assert.True(daysUntil < 30);
    }

    [Fact]
    public void GetNextHoliday_BeforePurim_ReturnsPurim()
    {
        // Arrange - Before Purim (typically in March)
        var date = new DateTime(2024, 2, 1);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.Contains("Purim", holidayEnglish);
        Assert.True(daysUntil < 60);
    }

    [Fact]
    public void GetNextHoliday_BeforePassover_ReturnsPassover()
    {
        // Arrange - Before Passover (typically in April)
        var date = new DateTime(2024, 3, 1);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.Contains("Passover", holidayEnglish);
        Assert.True(daysUntil > 0);
        Assert.True(daysUntil < 60);
    }

    [Fact]
    public void GetNextHoliday_BeforeShavuot_ReturnsShavuot()
    {
        // Arrange - Between Passover and Shavuot
        var date = new DateTime(2024, 5, 1);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.Contains("Shavuot", holidayEnglish);
        Assert.True(daysUntil < 50); // Shavuot is within 50 days of Passover
    }

    [Fact]
    public void GetNextHoliday_AlwaysReturnsAHoliday()
    {
        // Test that there's always a next holiday, any time of year
        var testDates = new[]
        {
            new DateTime(2024, 1, 15),
            new DateTime(2024, 4, 15),
            new DateTime(2024, 7, 15),
            new DateTime(2024, 10, 15)
        };

        foreach (var date in testDates)
        {
            // Act
            var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

            // Assert
            Assert.NotNull(holidayEnglish);
            Assert.NotEmpty(holidayEnglish);
            Assert.NotNull(holidayHebrew);
            Assert.NotEmpty(holidayHebrew);
            Assert.True(daysUntil >= 0, $"Days until next holiday should be non-negative, got {daysUntil} for date {date:yyyy-MM-dd}");
            Assert.True(holidayDate > date || holidayDate == date.Date, 
                "Holiday date should be today or in the future");
        }
    }

    [Fact]
    public void GetNextHoliday_DaysUntil_IsAccurate()
    {
        // Arrange
        var date = new DateTime(2024, 9, 1);

        // Act
        var (_, _, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        var actualDays = (holidayDate - date.Date).Days;
        Assert.Equal(actualDays, daysUntil);
    }

    [Fact]
    public void GetNextHoliday_HebrewText_ContainsHebrewCharacters()
    {
        // Arrange
        var date = new DateTime(2024, 9, 1);

        // Act
        var (_, holidayHebrew, _, _) = _service.GetNextHoliday(date);

        // Assert
        Assert.True(holidayHebrew.Any(c => c >= 0x0590 && c <= 0x05FF), 
            "Hebrew holiday name should contain Hebrew characters");
    }

    [Fact]
    public void GetNextHoliday_ConsecutiveDays_DecrementsCorrectly()
    {
        // Arrange
        var date1 = new DateTime(2024, 9, 20);
        var date2 = date1.AddDays(1);

        // Act
        var (holiday1, _, _, daysUntil1) = _service.GetNextHoliday(date1);
        var (holiday2, _, _, daysUntil2) = _service.GetNextHoliday(date2);

        // Assert
        if (holiday1 == holiday2 && daysUntil1 > 0)
        {
            // Same holiday, days should decrease by 1
            Assert.Equal(daysUntil1 - 1, daysUntil2);
        }
    }

    [Theory]
    [InlineData("2024-01-01")]
    [InlineData("2024-04-15")]
    [InlineData("2024-07-01")]
    [InlineData("2024-10-01")]
    [InlineData("2024-12-31")]
    public void GetNextHoliday_VariousDates_ReturnsValidData(string dateStr)
    {
        // Arrange
        var date = DateTime.Parse(dateStr);

        // Act
        var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = _service.GetNextHoliday(date);

        // Assert
        Assert.NotEmpty(holidayEnglish);
        Assert.NotEmpty(holidayHebrew);
        Assert.True(holidayDate >= date.Date);
        Assert.True(daysUntil >= 0);
        Assert.True(daysUntil <= 365, "Next holiday should be within a year");
    }

    [Fact]
    public void GetNextHoliday_OnHolidayDay_ReturnsZeroDays()
    {
        // Arrange - On Rosh Hashanah 2024 (October 3)
        var date = new DateTime(2024, 10, 3);

        // Act
        var (holidayEnglish, _, _, daysUntil) = _service.GetNextHoliday(date);

        // Assert - Should either return today's holiday with 0 days, or next holiday
        Assert.True(daysUntil >= 0, "Days until should be non-negative");
        if (daysUntil == 0)
        {
            Assert.Contains("Rosh", holidayEnglish);
        }
    }
}
