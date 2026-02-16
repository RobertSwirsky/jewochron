using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class MoonPhaseServiceTests
{
    private readonly MoonPhaseService _service;

    public MoonPhaseServiceTests()
    {
        _service = new MoonPhaseService();
    }

    [Fact]
    public void GetMoonPhase_NewMoonDate_ReturnsNewMoon()
    {
        // Arrange - Known new moon date (January 11, 2024)
        var date = new DateTime(2024, 1, 11);

        // Act
        var (emoji, name) = _service.GetMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("New Moon", name);
    }

    [Fact]
    public void GetMoonPhase_FullMoonDate_ReturnsFullMoon()
    {
        // Arrange - Known full moon date (January 25, 2024)
        var date = new DateTime(2024, 1, 25);

        // Act
        var (emoji, name) = _service.GetMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("Full Moon", name);
    }

    [Fact]
    public void GetMoonPhase_FirstQuarter_ReturnsFirstQuarter()
    {
        // Arrange - Known first quarter date (January 18, 2024)
        var date = new DateTime(2024, 1, 18);

        // Act
        var (emoji, name) = _service.GetMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("First Quarter", name);
    }

    [Fact]
    public void GetDetailedMoonPhase_NewMoon_ReturnsZeroIllumination()
    {
        // Arrange - Known new moon date
        var date = new DateTime(2024, 1, 11);

        // Act
        var (emoji, name, illumination, age) = _service.GetDetailedMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("New Moon", name);
        Assert.True(illumination < 5, $"Expected illumination < 5% for new moon, got {illumination}%");
        Assert.True(age >= 0 && age < 29.6, "Moon age should be within lunar month");
    }

    [Fact]
    public void GetDetailedMoonPhase_FullMoon_ReturnsHighIllumination()
    {
        // Arrange - Known full moon date
        var date = new DateTime(2024, 1, 25);

        // Act
        var (emoji, name, illumination, age) = _service.GetDetailedMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("Full Moon", name);
        Assert.True(illumination > 95, $"Expected illumination > 95% for full moon, got {illumination}%");
        Assert.True(age > 13 && age < 16, $"Full moon should be around day 14-15, got {age}");
    }

    [Fact]
    public void GetDetailedMoonPhase_FirstQuarter_ReturnsHalfIllumination()
    {
        // Arrange - First quarter moon
        var date = new DateTime(2024, 1, 18);

        // Act
        var (emoji, name, illumination, age) = _service.GetDetailedMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("First Quarter", name);
        Assert.True(illumination > 40 && illumination < 60, 
            $"Expected illumination ~50% for first quarter, got {illumination}%");
        Assert.True(age > 6 && age < 9, $"First quarter should be around day 7-8, got {age}");
    }

    [Fact]
    public void GetDetailedMoonPhase_WaxingCrescent_ReturnsCorrectPhase()
    {
        // Arrange - A few days after new moon
        var date = new DateTime(2024, 1, 14);

        // Act
        var (emoji, name, illumination, age) = _service.GetDetailedMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("Waxing Crescent", name);
        Assert.True(illumination > 5 && illumination < 40, 
            $"Waxing crescent should be 5-40% illuminated, got {illumination}%");
    }

    [Fact]
    public void GetDetailedMoonPhase_WaxingGibbous_ReturnsCorrectPhase()
    {
        // Arrange - Between first quarter and full moon
        var date = new DateTime(2024, 1, 22);

        // Act
        var (emoji, name, illumination, age) = _service.GetDetailedMoonPhase(date);

        // Assert
        Assert.Equal("??", emoji);
        Assert.Equal("Waxing Gibbous", name);
        Assert.True(illumination > 60 && illumination < 95, 
            $"Waxing gibbous should be 60-95% illuminated, got {illumination}%");
    }

    [Theory]
    [InlineData("2024-01-11")] // New Moon
    [InlineData("2024-01-18")] // First Quarter
    [InlineData("2024-01-25")] // Full Moon
    [InlineData("2024-02-02")] // Last Quarter
    public void GetMoonPhase_KnownDates_ReturnsValidPhase(string dateStr)
    {
        // Arrange
        var date = DateTime.Parse(dateStr);

        // Act
        var (emoji, name) = _service.GetMoonPhase(date);

        // Assert
        Assert.NotNull(emoji);
        Assert.NotEmpty(emoji);
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void GetDetailedMoonPhase_MoonAge_IsWithinLunarMonth()
    {
        // Arrange
        var testDates = new[]
        {
            new DateTime(2024, 1, 15),
            new DateTime(2024, 6, 15),
            new DateTime(2024, 12, 15)
        };

        foreach (var date in testDates)
        {
            // Act
            var (_, _, _, age) = _service.GetDetailedMoonPhase(date);

            // Assert
            Assert.True(age >= 0 && age < 29.6, 
                $"Moon age {age} for {date:yyyy-MM-dd} should be between 0 and 29.6 days");
        }
    }

    [Fact]
    public void GetDetailedMoonPhase_Illumination_IsWithinValidRange()
    {
        // Arrange
        var testDates = new[]
        {
            new DateTime(2024, 1, 15),
            new DateTime(2024, 6, 15),
            new DateTime(2024, 12, 15)
        };

        foreach (var date in testDates)
        {
            // Act
            var (_, _, illumination, _) = _service.GetDetailedMoonPhase(date);

            // Assert
            Assert.True(illumination >= 0 && illumination <= 100, 
                $"Illumination {illumination}% for {date:yyyy-MM-dd} should be between 0 and 100");
        }
    }
}
