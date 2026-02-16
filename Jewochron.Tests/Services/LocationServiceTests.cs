using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class LocationServiceTests
{
    private readonly LocationService _service;

    public LocationServiceTests()
    {
        _service = new LocationService();
    }

    [Fact]
    public async Task GetLocationAsync_ReturnsValidData()
    {
        // Act
        var (city, state, latitude, longitude) = await _service.GetLocationAsync();

        // Assert
        Assert.NotNull(city);
        Assert.NotEmpty(city);
        Assert.NotNull(state);
        Assert.NotEmpty(state);
        
        // Latitude should be between -90 and 90
        Assert.True(latitude >= -90 && latitude <= 90, 
            $"Latitude {latitude} should be between -90 and 90");
        
        // Longitude should be between -180 and 180
        Assert.True(longitude >= -180 && longitude <= 180, 
            $"Longitude {longitude} should be between -180 and 180");
    }

    [Fact]
    public async Task GetLocationAsync_Coordinates_AreValid()
    {
        // Act
        var (_, _, latitude, longitude) = await _service.GetLocationAsync();

        // Assert
        Assert.NotEqual(0.0, latitude); // Should not be exactly 0
        Assert.NotEqual(0.0, longitude); // Should not be exactly 0
        Assert.True(Math.Abs(latitude) > 0.001, "Latitude should have meaningful precision");
        Assert.True(Math.Abs(longitude) > 0.001, "Longitude should have meaningful precision");
    }

    [Fact]
    public async Task GetLocationAsync_MultipleCalls_ReturnsSameLocation()
    {
        // Act
        var location1 = await _service.GetLocationAsync();
        var location2 = await _service.GetLocationAsync();

        // Assert - Location shouldn't change between calls
        Assert.Equal(location1.city, location2.city);
        Assert.Equal(location1.state, location2.state);
        Assert.Equal(location1.latitude, location2.latitude);
        Assert.Equal(location1.longitude, location2.longitude);
    }

    [Fact]
    public async Task GetLocationAsync_CityAndState_AreNotEmpty()
    {
        // Act
        var (city, state, _, _) = await _service.GetLocationAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(city), "City should not be null or whitespace");
        Assert.False(string.IsNullOrWhiteSpace(state), "State should not be null or whitespace");
        Assert.True(city.Length > 1, "City name should have at least 2 characters");
        Assert.True(state.Length >= 2, "State should have at least 2 characters (state code)");
    }

    [Fact]
    public async Task GetLocationAsync_CompletesInReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _service.GetLocationAsync();
        stopwatch.Stop();

        // Assert - Should complete within 10 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"Location lookup took {stopwatch.ElapsedMilliseconds}ms, should be < 10000ms");
    }

    [Fact]
    public async Task GetLocationAsync_CoordinatesPrecision_IsSufficient()
    {
        // Act
        var (_, _, latitude, longitude) = await _service.GetLocationAsync();

        // Convert to string to check decimal places
        var latStr = latitude.ToString("F6");
        var lonStr = longitude.ToString("F6");

        // Assert - Should have at least 2 decimal places for meaningful accuracy
        var latDecimals = latStr.Split('.')[1].TrimEnd('0');
        var lonDecimals = lonStr.Split('.')[1].TrimEnd('0');
        
        Assert.True(latDecimals.Length >= 2, $"Latitude {latitude} should have at least 2 decimal places");
        Assert.True(lonDecimals.Length >= 2, $"Longitude {longitude} should have at least 2 decimal places");
    }
}
