using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class MoladServiceTests
{
    private readonly MoladService _service;
    private readonly HebrewCalendarService _hebrewCalendarService;

    public MoladServiceTests()
    {
        _hebrewCalendarService = new HebrewCalendarService();
        _service = new MoladService(_hebrewCalendarService);
    }

    [Fact]
    public void GetNextMolad_ReturnsValidData()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var (moladDateTime, dayOfWeek, moladHour, moladChalakim, moladDayName) = _service.GetNextMolad(date);

        // Assert
        Assert.True(moladDateTime > date, "Molad should be in the future");
        Assert.True(moladHour >= 0 && moladHour < 24, $"Molad hour {moladHour} should be 0-23");
        Assert.True(moladChalakim >= 0 && moladChalakim < 1080, $"Chalakim {moladChalakim} should be 0-1079");
        Assert.NotEmpty(moladDayName);
        Assert.True(dayOfWeek >= DayOfWeek.Sunday && dayOfWeek <= DayOfWeek.Saturday);
    }

    [Fact]
    public void GetNextMolad_ConsecutiveMonths_AreApproximately29DaysApart()
    {
        // Arrange
        var date = new DateTime(2024, 6, 1);

        // Act
        var (molad1, _, _, _, _) = _service.GetNextMolad(date);
        var (molad2, _, _, _, _) = _service.GetNextMolad(molad1.AddDays(1));

        var daysBetween = (molad2 - molad1).TotalDays;

        // Assert - Molad occurs approximately every 29.5 days
        Assert.True(daysBetween >= 28 && daysBetween <= 31, 
            $"Molads should be ~29.5 days apart, got {daysBetween:F2} days");
    }

    [Fact]
    public void GetNextMolad_Chalakim_IsWithinValidRange()
    {
        // Test multiple dates to ensure chalakim is always valid
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
            var (_, _, _, moladChalakim, _) = _service.GetNextMolad(date);

            // Assert - Chalakim is parts of an hour (1080 parts = 1 hour)
            Assert.True(moladChalakim >= 0 && moladChalakim < 1080, 
                $"Chalakim {moladChalakim} for {date:yyyy-MM-dd} should be 0-1079");
        }
    }

    [Fact]
    public void GetNextMolad_Hour_IsWithinValidRange()
    {
        // Test multiple dates
        var testDates = new[]
        {
            new DateTime(2024, 1, 15),
            new DateTime(2024, 6, 15),
            new DateTime(2024, 12, 15)
        };

        foreach (var date in testDates)
        {
            // Act
            var (_, _, moladHour, _, _) = _service.GetNextMolad(date);

            // Assert
            Assert.True(moladHour >= 0 && moladHour < 24, 
                $"Hour {moladHour} for {date:yyyy-MM-dd} should be 0-23");
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, "Sunday")]
    [InlineData(DayOfWeek.Monday, "Monday")]
    [InlineData(DayOfWeek.Tuesday, "Tuesday")]
    [InlineData(DayOfWeek.Wednesday, "Wednesday")]
    [InlineData(DayOfWeek.Thursday, "Thursday")]
    [InlineData(DayOfWeek.Friday, "Friday")]
    [InlineData(DayOfWeek.Saturday, "Saturday")]
    public void GetHebrewDayName_ReturnsCorrectHebrewName(DayOfWeek dayOfWeek, string expectedEnglish)
    {
        // Act
        var hebrewName = _service.GetHebrewDayName(dayOfWeek);

        // Assert
        Assert.NotEmpty(hebrewName);
        Assert.True(hebrewName.Any(c => c >= 0x0590 && c <= 0x05FF), 
            $"Hebrew name for {expectedEnglish} should contain Hebrew characters");
    }

    [Fact]
    public void GetNextMolad_DayName_MatchesDayOfWeek()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var (moladDateTime, dayOfWeek, _, _, moladDayName) = _service.GetNextMolad(date);

        // Assert
        var expectedDayName = moladDateTime.DayOfWeek.ToString();
        Assert.Equal(expectedDayName, moladDayName);
        Assert.Equal(moladDateTime.DayOfWeek, dayOfWeek);
    }

    [Fact]
    public void GetNextMolad_SameDay_ReturnsSameResult()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15, 10, 30, 0);

        // Act
        var molad1 = _service.GetNextMolad(date);
        var molad2 = _service.GetNextMolad(date);

        // Assert
        Assert.Equal(molad1.moladDateTime, molad2.moladDateTime);
        Assert.Equal(molad1.moladHour, molad2.moladHour);
        Assert.Equal(molad1.moladChalakim, molad2.moladChalakim);
    }

    [Fact]
    public void GetNextMolad_DifferentYears_AllReturnValidMolads()
    {
        // Test across multiple years
        var years = new[] { 2023, 2024, 2025, 2026 };

        foreach (var year in years)
        {
            var date = new DateTime(year, 6, 15);

            // Act
            var (moladDateTime, _, moladHour, moladChalakim, _) = _service.GetNextMolad(date);

            // Assert
            Assert.True(moladDateTime > date);
            Assert.True(moladHour >= 0 && moladHour < 24);
            Assert.True(moladChalakim >= 0 && moladChalakim < 1080);
        }
    }

    [Fact]
    public void GetNextMolad_BeforeAndAfterMolad_ReturnsDifferentMolads()
    {
        // Arrange - Get a molad, then test dates before and after it
        var initialDate = new DateTime(2024, 6, 1);
        var (firstMolad, _, _, _, _) = _service.GetNextMolad(initialDate);

        var dateBeforeMolad = firstMolad.AddDays(-1);
        var dateAfterMolad = firstMolad.AddDays(1);

        // Act
        var moladFromBefore = _service.GetNextMolad(dateBeforeMolad);
        var moladFromAfter = _service.GetNextMolad(dateAfterMolad);

        // Assert
        Assert.Equal(firstMolad, moladFromBefore.moladDateTime);
        Assert.NotEqual(firstMolad, moladFromAfter.moladDateTime);
        Assert.True(moladFromAfter.moladDateTime > firstMolad);
    }

    [Fact]
    public void GetNextMolad_MoladDateTime_IsInUtc()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var (moladDateTime, _, _, _, _) = _service.GetNextMolad(date);

        // Assert
        Assert.Equal(DateTimeKind.Utc, moladDateTime.Kind);
    }
}
