using Jewochron.Services;
using System;

// Quick test runner for moon phase verification
var moonPhaseService = new MoonPhaseService();

Console.WriteLine("=== Moon Phase Calculation Verification ===\n");
Console.WriteLine("Testing against known astronomical data:\n");

// Known moon phases from NASA/USNO data
var tests = new[]
{
    // Reference new moon
    (new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc), "New Moon", 0.0),
    
    // 2024 phases
    (new DateTime(2024, 1, 11, 11, 57, 0, DateTimeKind.Utc), "New Moon", 0.0),
    (new DateTime(2024, 1, 18, 3, 53, 0, DateTimeKind.Utc), "First Quarter", 50.0),
    (new DateTime(2024, 1, 25, 17, 54, 0, DateTimeKind.Utc), "Full Moon", 100.0),
    (new DateTime(2024, 12, 1, 6, 21, 0, DateTimeKind.Utc), "New Moon", 0.5),
    (new DateTime(2024, 12, 15, 9, 2, 0, DateTimeKind.Utc), "Full Moon", 100.0),
    
    // 2025 phases
    (new DateTime(2025, 1, 6, 23, 56, 0, DateTimeKind.Utc), "First Quarter", 50.0),
    (new DateTime(2025, 1, 13, 22, 27, 0, DateTimeKind.Utc), "Full Moon", 99.9),
    (new DateTime(2025, 1, 21, 20, 31, 0, DateTimeKind.Utc), "Last Quarter", 50.0),
    (new DateTime(2025, 1, 29, 12, 36, 0, DateTimeKind.Utc), "New Moon", 0.0),
};

int passed = 0;
int failed = 0;

foreach (var (date, expectedPhase, expectedIllumination) in tests)
{
    var (emoji, phaseName, illumination, age) = moonPhaseService.GetDetailedMoonPhase(date);
    
    string dateStr = date.ToString("yyyy-MM-dd HH:mm UTC");
    Console.WriteLine($"Date: {dateStr}");
    Console.WriteLine($"  Expected: {expectedPhase} @ {expectedIllumination:F1}%");
    Console.WriteLine($"  Calculated: {phaseName} {emoji} @ {illumination:F1}%");
    Console.WriteLine($"  Moon Age: {age:F2} days");
    
    // Validate with reasonable tolerance
    // New/Full moons: ±5%
    // Quarter moons: ±10% (harder to pinpoint exact 50%)
    double tolerance = expectedPhase.Contains("Quarter") ? 10.0 : 5.0;
    double difference = Math.Abs(illumination - expectedIllumination);
    
    bool phaseMatches = phaseName == expectedPhase || 
                       (expectedPhase == "New Moon" && illumination < 5) ||
                       (expectedPhase == "Full Moon" && illumination > 95);
    
    if (difference <= tolerance && phaseMatches)
    {
        Console.WriteLine($"  ✓ PASS (difference: {difference:F1}%, within ±{tolerance}%)");
        passed++;
    }
    else
    {
        Console.WriteLine($"  ✗ FAIL (difference: {difference:F1}%, expected ±{tolerance}%)");
        failed++;
    }
    
    Console.WriteLine();
}

Console.WriteLine($"\n=== Results: {passed} passed, {failed} failed out of {passed + failed} tests ===\n");

// Test current date
Console.WriteLine("Current Moon Phase:");
var current = moonPhaseService.GetDetailedMoonPhase(DateTime.UtcNow);
Console.WriteLine($"  {current.name} {current.emoji}");
Console.WriteLine($"  {current.illumination:F1}% illuminated");
Console.WriteLine($"  Day {Math.Floor(current.age) + 1} of lunar cycle");
Console.WriteLine($"  Age: {current.age:F2} days");
