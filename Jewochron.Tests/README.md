# Jewochron Test Suite

This test project contains comprehensive unit tests for all Jewochron services.

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run with detailed output
```bash
dotnet test --verbosity detailed
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~HebrewCalendarServiceTests"
```

### Run tests with coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage

### Services Tested:
- ? **HebrewCalendarService** - Hebrew date conversion, month names, numerals, leap year detection
- ? **MoonPhaseService** - Moon phase calculation, illumination percentage, lunar age
- ? **DafYomiService** - Daily Talmud page calculation across cycles
- ? **HalachicTimesService** - Prayer times, sunrise/sunset, halachic hours
- ? **JewishHolidaysService** - Holiday calculations and countdown
- ? **MoladService** - New moon (Molad) calculations with halachic times
- ? **TorahPortionService** - Weekly Torah portion (Parsha) by Hebrew date
- ? **LocationService** - IP-based geolocation for prayer time calculations

## Test Categories

### Unit Tests
All tests are unit tests that validate individual service methods in isolation.

### Integration Points
Some tests verify:
- Date conversions between Hebrew and Gregorian calendars
- Coordination between services (e.g., TorahPortionService uses HebrewCalendarService)
- Real-world date calculations for known holidays and events

## Known Test Data

Tests use well-known dates for validation:
- **Rosh Hashanah 5785**: October 3, 2024
- **Yom Kippur 5785**: October 12, 2024
- **Chanukah 5785**: December 25, 2024
- **New Moon (Molad)**: January 11, 2024
- **Full Moon**: January 25, 2024

## Test Assertions

Tests verify:
- ? Correct date calculations
- ? Valid Hebrew month names and translations
- ? Accurate moon phase percentages
- ? Proper Daf Yomi cycle progression
- ? Realistic halachic time calculations
- ? Holiday countdown accuracy
- ? Valid geolocation coordinates
- ? Hebrew text contains Hebrew characters
- ? Consistent results for same inputs

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- No external dependencies required (except network for LocationService)
- Fast execution (< 30 seconds for full suite)
- Deterministic results for date-based tests
- Clear, descriptive test names

## Adding New Tests

When adding new tests:
1. Create a test class in `Services/` folder
2. Name it `[ServiceName]Tests.cs`
3. Initialize the service in the constructor
4. Use `[Fact]` for single test cases
5. Use `[Theory]` with `[InlineData]` for parameterized tests
6. Include both positive and negative test cases
7. Test edge cases (boundaries, transitions, special dates)
8. Verify Hebrew output contains Hebrew characters

## Test Framework

- **Framework**: xUnit 2.9.3
- **.NET Version**: .NET 10.0
- **Code Coverage**: coverlet.collector
- **Test Runner**: Visual Studio Test Platform

## Example Test

```csharp
[Fact]
public void GetHebrewDate_RoshHashanah2024_ReturnsCorrectDate()
{
    // Arrange
    var gregorianDate = new DateTime(2024, 10, 3);

    // Act
    var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = 
        _service.GetHebrewDate(gregorianDate);

    // Assert
    Assert.Equal(5785, hebrewYear);
    Assert.Equal(7, hebrewMonth); // Tishrei
    Assert.Equal(1, hebrewDay);
    Assert.False(isLeapYear);
}
```

## Test Results

Run tests to see:
- Total tests passed/failed
- Execution time per test
- Code coverage percentage
- Detailed failure messages

Happy testing! ????
