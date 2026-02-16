using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class HalachicTimesServiceTests
{
    private readonly HalachicTimesService _service;

    public HalachicTimesServiceTests()
    {
        _service = new HalachicTimesService();
    }

    [Fact]
    public void CalculateTimes_Jerusalem_ReturnsSensibleTimes()
    {
        // Arrange - Jerusalem coordinates
        var date = new DateTime(2024, 6, 15); // Summer day
        var latitude = 31.7683;
        var longitude = 35.2137;

        // Act
        var (alotHaShachar, sunrise, sunset, tzait, chatzot, minGedolah, plagHaMincha) = 
            _service.CalculateTimes(date, latitude, longitude);

        // Assert - Basic sanity checks
        Assert.True(alotHaShachar < sunrise, "Alot HaShachar should be before sunrise");
        Assert.True(sunrise < chatzot, "Sunrise should be before chatzot (noon)");
        Assert.True(chatzot < sunset, "Chatzot should be before sunset");
        Assert.True(sunset < tzait, "Sunset should be before tzait");
        
        // Check that times are on the same day
        Assert.Equal(date.Date, sunrise.Date);
        Assert.Equal(date.Date, sunset.Date);
    }

    [Fact]
    public void CalculateTimes_NewYork_ReturnsSensibleTimes()
    {
        // Arrange - New York coordinates
        var date = new DateTime(2024, 6, 15);
        var latitude = 40.7128;
        var longitude = -74.0060;

        // Act
        var (alotHaShachar, sunrise, sunset, tzait, chatzot, minGedolah, plagHaMincha) = 
            _service.CalculateTimes(date, latitude, longitude);

        // Assert
        Assert.True(alotHaShachar < sunrise);
        Assert.True(sunrise < sunset);
        Assert.True(sunset < tzait);
        Assert.Equal(date.Date, sunrise.Date);
    }

    [Fact]
    public void CalculateTimes_Winter_HasShorterDayThanSummer()
    {
        // Arrange
        var summerDate = new DateTime(2024, 6, 21); // Summer solstice
        var winterDate = new DateTime(2024, 12, 21); // Winter solstice
        var latitude = 40.7128; // New York
        var longitude = -74.0060;

        // Act
        var (_, summerSunrise, summerSunset, _, _, _, _) = 
            _service.CalculateTimes(summerDate, latitude, longitude);
        var (_, winterSunrise, winterSunset, _, _, _, _) = 
            _service.CalculateTimes(winterDate, latitude, longitude);

        var summerDayLength = summerSunset - summerSunrise;
        var winterDayLength = winterSunset - winterSunrise;

        // Assert
        Assert.True(summerDayLength > winterDayLength, 
            $"Summer day ({summerDayLength.TotalHours:F2}h) should be longer than winter day ({winterDayLength.TotalHours:F2}h)");
    }

    [Fact]
    public void CalculateTimes_Chatzot_IsApproximatelyMiddayLocal()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var latitude = 40.7128; // New York
        var longitude = -74.0060;

        // Act
        var (_, _, _, _, chatzot, _, _) = _service.CalculateTimes(date, latitude, longitude);

        // Assert - Chatzot should be around noon (11am-1pm accounting for timezone and solar noon)
        Assert.True(chatzot.Hour >= 11 && chatzot.Hour <= 14, 
            $"Chatzot {chatzot:HH:mm} should be around midday");
    }

    [Fact]
    public void CalculateTimes_MinGedolah_IsAfterChatzot()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var latitude = 31.7683;
        var longitude = 35.2137;

        // Act
        var (_, _, _, _, chatzot, minGedolah, _) = 
            _service.CalculateTimes(date, latitude, longitude);

        // Assert
        Assert.True(minGedolah > chatzot, "Mincha Gedolah should be after Chatzot");
    }

    [Fact]
    public void CalculateTimes_PlagHaMincha_IsBeforeSunset()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var latitude = 31.7683;
        var longitude = 35.2137;

        // Act
        var (_, _, sunset, _, _, _, plagHaMincha) = 
            _service.CalculateTimes(date, latitude, longitude);

        // Assert
        Assert.True(plagHaMincha < sunset, "Plag HaMincha should be before sunset");
        Assert.True(plagHaMincha > sunset.AddHours(-2), "Plag HaMincha should be within 2 hours of sunset");
    }

    [Theory]
    [InlineData(31.7683, 35.2137)]  // Jerusalem
    [InlineData(40.7128, -74.0060)] // New York
    [InlineData(51.5074, -0.1278)]  // London
    [InlineData(-33.8688, 151.2093)] // Sydney
    public void CalculateTimes_VariousLocations_AllTimesAreOrdered(double latitude, double longitude)
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var (alotHaShachar, sunrise, sunset, tzait, chatzot, minGedolah, plagHaMincha) = 
            _service.CalculateTimes(date, latitude, longitude);

        // Assert - Verify chronological order
        Assert.True(alotHaShachar < sunrise, "Alot < Sunrise");
        Assert.True(sunrise < minGedolah, "Sunrise < Mincha Gedolah");
        Assert.True(minGedolah < plagHaMincha, "Mincha Gedolah < Plag HaMincha");
        Assert.True(plagHaMincha < sunset, "Plag HaMincha < Sunset");
        Assert.True(sunset < tzait, "Sunset < Tzait");
    }

    [Fact]
    public void CalculateTimes_Equator_HasNearEqualDayAndNight()
    {
        // Arrange - Coordinates near equator
        var date = new DateTime(2024, 3, 20); // Equinox
        var latitude = 0.0;
        var longitude = 0.0;

        // Act
        var (_, sunrise, sunset, _, _, _, _) = 
            _service.CalculateTimes(date, latitude, longitude);

        var dayLength = sunset - sunrise;

        // Assert - Day length should be close to 12 hours at equator on equinox
        Assert.True(Math.Abs(dayLength.TotalHours - 12) < 1, 
            $"Day length {dayLength.TotalHours:F2}h should be close to 12 hours at equator");
    }

    [Fact]
    public void CalculateTimes_ConsecutiveDays_TimesVaryGradually()
    {
        // Arrange
        var date1 = new DateTime(2024, 6, 15);
        var date2 = date1.AddDays(1);
        var latitude = 40.7128;
        var longitude = -74.0060;

        // Act
        var (_, sunrise1, sunset1, _, _, _, _) = _service.CalculateTimes(date1, latitude, longitude);
        var (_, sunrise2, sunset2, _, _, _, _) = _service.CalculateTimes(date2, latitude, longitude);

        var sunriseChange = Math.Abs((sunrise2 - sunrise1).TotalMinutes);
        var sunsetChange = Math.Abs((sunset2 - sunset1).TotalMinutes);

        // Assert - Changes should be gradual (less than 5 minutes per day)
        Assert.True(sunriseChange < 5, $"Sunrise should change gradually, changed {sunriseChange:F2} minutes");
        Assert.True(sunsetChange < 5, $"Sunset should change gradually, changed {sunsetChange:F2} minutes");
    }
}
