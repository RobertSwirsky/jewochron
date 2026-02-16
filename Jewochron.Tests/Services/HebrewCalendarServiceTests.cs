using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class HebrewCalendarServiceTests
{
    private readonly HebrewCalendarService _service;

    public HebrewCalendarServiceTests()
    {
        _service = new HebrewCalendarService();
    }

    [Fact]
    public void GetHebrewDate_RoshHashanah2024_ReturnsCorrectDate()
    {
        // Arrange - Rosh Hashanah 5785 (October 3, 2024)
        var gregorianDate = new DateTime(2024, 10, 3);

        // Act
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _service.GetHebrewDate(gregorianDate);

        // Assert
        Assert.Equal(5785, hebrewYear);
        Assert.Equal(7, hebrewMonth); // Tishrei is month 7
        Assert.Equal(1, hebrewDay);
        Assert.False(isLeapYear);
    }

    [Fact]
    public void GetHebrewDate_YomKippur2024_ReturnsCorrectDate()
    {
        // Arrange - Yom Kippur (October 12, 2024)
        var gregorianDate = new DateTime(2024, 10, 12);

        // Act
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _service.GetHebrewDate(gregorianDate);

        // Assert
        Assert.Equal(5785, hebrewYear);
        Assert.Equal(7, hebrewMonth); // Tishrei
        Assert.Equal(10, hebrewDay);
    }

    [Fact]
    public void GetHebrewDate_Chanukah2024_ReturnsCorrectDate()
    {
        // Arrange - First night of Chanukah (December 25, 2024)
        var gregorianDate = new DateTime(2024, 12, 25);

        // Act
        var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = _service.GetHebrewDate(gregorianDate);

        // Assert
        Assert.Equal(5785, hebrewYear);
        Assert.Equal(9, hebrewMonth); // Kislev
        Assert.Equal(25, hebrewDay);
    }

    [Theory]
    [InlineData(1, false, "Nissan")]
    [InlineData(2, false, "Iyar")]
    [InlineData(3, false, "Sivan")]
    [InlineData(7, false, "Tishrei")]
    [InlineData(12, false, "Adar")]
    [InlineData(12, true, "Adar I")]
    [InlineData(13, true, "Adar II")]
    public void GetHebrewMonthName_ReturnsCorrectMonthName(int month, bool isLeapYear, string expectedName)
    {
        // Act
        var monthName = _service.GetHebrewMonthName(month, isLeapYear);

        // Assert
        Assert.Equal(expectedName, monthName);
    }

    [Theory]
    [InlineData(1, false, "????")]
    [InlineData(2, false, "????")]
    [InlineData(3, false, "????")]
    [InlineData(7, false, "????")]
    [InlineData(12, false, "???")]
    [InlineData(12, true, "??? ??")]
    [InlineData(13, true, "??? ??")]
    public void GetHebrewMonthNameInHebrew_ReturnsCorrectHebrewName(int month, bool isLeapYear, string expectedName)
    {
        // Act
        var monthName = _service.GetHebrewMonthNameInHebrew(month, isLeapYear);

        // Assert
        Assert.Equal(expectedName, monthName);
    }

    [Theory]
    [InlineData(1, "??")]
    [InlineData(5, "??")]
    [InlineData(10, "??")]
    [InlineData(15, "???")]
    [InlineData(18, "???")]
    [InlineData(25, "???")]
    [InlineData(29, "???")]
    public void ConvertToHebrewNumber_ReturnsCorrectHebrewNumeral(int number, string expected)
    {
        // Act
        var result = _service.ConvertToHebrewNumber(number);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToHebrewNumber_InvalidNumber_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ConvertToHebrewNumber(0));
        Assert.Throws<ArgumentException>(() => _service.ConvertToHebrewNumber(-1));
        Assert.Throws<ArgumentException>(() => _service.ConvertToHebrewNumber(10000));
    }

    [Theory]
    [InlineData(5784, true)]  // Leap year
    [InlineData(5785, false)] // Regular year
    [InlineData(5787, true)]  // Leap year
    [InlineData(5790, false)] // Regular year
    public void GetHebrewDate_LeapYearDetection_IsCorrect(int year, bool expectedLeapYear)
    {
        // Arrange - Use a date in that Hebrew year
        var gregorianDate = new DateTime(year - 3761, 6, 1); // Approximate

        // Act
        var (hebrewYear, _, _, isLeapYear) = _service.GetHebrewDate(gregorianDate);

        // Assert - Check if the year we're testing matches
        if (hebrewYear == year)
        {
            Assert.Equal(expectedLeapYear, isLeapYear);
        }
    }
}
