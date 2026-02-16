using Xunit;
using Jewochron.Services;

namespace Jewochron.Tests.Services;

public class DafYomiServiceTests
{
    private readonly DafYomiService _service;
    private readonly HebrewCalendarService _hebrewCalendarService;

    public DafYomiServiceTests()
    {
        _hebrewCalendarService = new HebrewCalendarService();
        _service = new DafYomiService(_hebrewCalendarService);
    }

    [Fact]
    public void GetDafYomi_September5_1923_ReturnsBerachot2()
    {
        // Arrange - The start of the first Daf Yomi cycle
        var date = new DateTime(1923, 9, 11); // First day of Daf Yomi

        // Act
        var (dafYomiEnglish, dafYomiHebrew) = _service.GetDafYomi(date);

        // Assert
        Assert.Contains("Berachot", dafYomiEnglish);
        Assert.Contains("2", dafYomiEnglish);
    }

    [Fact]
    public void GetDafYomi_January2024_ReturnsValidDaf()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        // Act
        var (dafYomiEnglish, dafYomiHebrew) = _service.GetDafYomi(date);

        // Assert
        Assert.NotNull(dafYomiEnglish);
        Assert.NotEmpty(dafYomiEnglish);
        Assert.NotNull(dafYomiHebrew);
        Assert.NotEmpty(dafYomiHebrew);
        Assert.Contains(" ", dafYomiEnglish); // Should have tractate and page
    }

    [Fact]
    public void GetDafYomi_ConsecutiveDays_IncrementsByOne()
    {
        // Arrange
        var date1 = new DateTime(2024, 6, 1);
        var date2 = date1.AddDays(1);

        // Act
        var (daf1English, _) = _service.GetDafYomi(date1);
        var (daf2English, _) = _service.GetDafYomi(date2);

        // Assert
        Assert.NotEqual(daf1English, daf2English);
        // Extract page numbers (assuming format "Tractate Page")
        var page1 = int.Parse(daf1English.Split(' ')[^1]);
        var page2 = int.Parse(daf2English.Split(' ')[^1]);
        
        // Pages should increment by 1, or wrap to next tractate
        Assert.True(page2 == page1 + 1 || page2 == 2, 
            "Daf should increment by 1 or wrap to page 2 of next tractate");
    }

    [Fact]
    public void GetDafYomi_EndOfTractate_WrapsToNextTractate()
    {
        // This test verifies that when we reach the end of a tractate,
        // we properly move to the beginning of the next one
        // We'll test multiple days to catch a transition
        var foundTransition = false;
        var date = new DateTime(2024, 1, 1);

        for (int i = 0; i < 100; i++)
        {
            var currentDate = date.AddDays(i);
            var nextDate = currentDate.AddDays(1);

            var (currentDaf, _) = _service.GetDafYomi(currentDate);
            var (nextDaf, _) = _service.GetDafYomi(nextDate);

            var currentTractate = currentDaf.Split(' ')[0];
            var nextTractate = nextDaf.Split(' ')[0];

            if (currentTractate != nextTractate)
            {
                // Found a tractate transition
                var nextPage = int.Parse(nextDaf.Split(' ')[^1]);
                Assert.Equal(2, nextPage); // New tractate should start at page 2
                foundTransition = true;
                break;
            }
        }

        Assert.True(foundTransition, "Should find at least one tractate transition in 100 days");
    }

    [Theory]
    [InlineData("2024-01-01")]
    [InlineData("2024-06-15")]
    [InlineData("2024-12-31")]
    [InlineData("2023-05-10")]
    [InlineData("2025-03-20")]
    public void GetDafYomi_VariousDates_ReturnsConsistentFormat(string dateStr)
    {
        // Arrange
        var date = DateTime.Parse(dateStr);

        // Act
        var (dafYomiEnglish, dafYomiHebrew) = _service.GetDafYomi(date);

        // Assert - Check format
        Assert.Matches(@"^\w+ \d+$", dafYomiEnglish); // "Tractate Number" format
        Assert.NotEmpty(dafYomiHebrew);
    }

    [Fact]
    public void GetDafYomi_SameDate_ReturnsSameResult()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var (daf1English, daf1Hebrew) = _service.GetDafYomi(date);
        var (daf2English, daf2Hebrew) = _service.GetDafYomi(date);

        // Assert
        Assert.Equal(daf1English, daf2English);
        Assert.Equal(daf1Hebrew, daf2Hebrew);
    }

    [Fact]
    public void GetDafYomi_PageNumber_IsWithinValidRange()
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
            var (dafYomiEnglish, _) = _service.GetDafYomi(date);
            var pageStr = dafYomiEnglish.Split(' ')[^1];
            var page = int.Parse(pageStr);

            // Assert
            Assert.True(page >= 2 && page <= 157, 
                $"Page {page} for {date:yyyy-MM-dd} should be between 2 and 157 (valid Talmud pages)");
        }
    }

    [Fact]
    public void GetDafYomi_HebrewOutput_ContainsHebrewCharacters()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var (_, dafYomiHebrew) = _service.GetDafYomi(date);

        // Assert
        Assert.True(dafYomiHebrew.Any(c => c >= 0x0590 && c <= 0x05FF), 
            "Hebrew output should contain Hebrew characters");
    }
}
