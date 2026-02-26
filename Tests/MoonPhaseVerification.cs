using Jewochron.Services;
using System;

namespace Jewochron.Tests
{
    /// <summary>
    /// Verification of moon phase calculations against known astronomical data
    /// </summary>
    public class MoonPhaseVerification
    {
        public static void TestKnownMoonPhases()
        {
            var moonPhaseService = new MoonPhaseService();
            
            Console.WriteLine("=== Moon Phase Calculation Verification ===\n");
            Console.WriteLine("Testing against known astronomical data:\n");

            // Known moon phases from NASA/USNO data
            // Format: (Date, Expected Phase Name, Expected Illumination %)
            var knownPhases = new[]
            {
                // New Moons
                (new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc), "New Moon", 0.0),
                (new DateTime(2024, 1, 11, 11, 57, 0, DateTimeKind.Utc), "New Moon", 0.0),
                (new DateTime(2024, 12, 1, 6, 21, 0, DateTimeKind.Utc), "New Moon", 0.5),
                (new DateTime(2025, 1, 29, 12, 36, 0, DateTimeKind.Utc), "New Moon", 0.0),
                
                // Full Moons
                (new DateTime(2024, 1, 25, 17, 54, 0, DateTimeKind.Utc), "Full Moon", 100.0),
                (new DateTime(2024, 12, 15, 9, 2, 0, DateTimeKind.Utc), "Full Moon", 100.0),
                (new DateTime(2025, 1, 13, 22, 27, 0, DateTimeKind.Utc), "Full Moon", 99.9),
                
                // First Quarter
                (new DateTime(2024, 1, 18, 3, 53, 0, DateTimeKind.Utc), "First Quarter", 50.0),
                (new DateTime(2025, 1, 6, 23, 56, 0, DateTimeKind.Utc), "First Quarter", 50.0),
                
                // Last Quarter
                (new DateTime(2024, 1, 4, 3, 30, 0, DateTimeKind.Utc), "Last Quarter", 50.0),
                (new DateTime(2025, 1, 21, 20, 31, 0, DateTimeKind.Utc), "Last Quarter", 50.0),
                
                // Today's date (for current verification)
                (DateTime.UtcNow, "Current", -1.0) // -1 = don't check exact value
            };

            foreach (var (date, expectedPhase, expectedIllumination) in knownPhases)
            {
                var (emoji, phaseName, illumination, age) = moonPhaseService.GetDetailedMoonPhase(date);
                
                string dateStr = date.ToString("yyyy-MM-dd HH:mm UTC");
                Console.WriteLine($"Date: {dateStr}");
                Console.WriteLine($"  Expected: {expectedPhase}");
                Console.WriteLine($"  Calculated: {phaseName} {emoji}");
                Console.WriteLine($"  Illumination: {illumination:F1}%");
                Console.WriteLine($"  Moon Age: {age:F2} days");
                
                // Validate
                if (expectedIllumination >= 0)
                {
                    double tolerance = expectedPhase.Contains("Quarter") ? 10.0 : 5.0;
                    double difference = Math.Abs(illumination - expectedIllumination);
                    
                    if (difference <= tolerance)
                    {
                        Console.WriteLine($"  ✓ PASS (within {tolerance}% tolerance)");
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ FAIL (difference: {difference:F1}%, expected ±{tolerance}%)");
                    }
                }
                
                Console.WriteLine();
            }
            
            Console.WriteLine("\n=== Additional Validation ===\n");
            
            // Test synodic month length
            DateTime startNew = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
            DateTime nextNew = new DateTime(2000, 2, 5, 13, 3, 0, DateTimeKind.Utc);
            double actualSynodicMonth = (nextNew - startNew).TotalDays;
            Console.WriteLine($"Synodic Month Length:");
            Console.WriteLine($"  Reference calculation: {actualSynodicMonth:F5} days");
            Console.WriteLine($"  Code uses: 29.53058867 days");
            Console.WriteLine($"  Difference: {Math.Abs(actualSynodicMonth - 29.53058867):F5} days");
            Console.WriteLine();
            
            // Test phase symmetry
            Console.WriteLine("Phase Symmetry Test (illumination at same offset from new/full):");
            var testDate1 = new DateTime(2024, 1, 14, 0, 0, 0, DateTimeKind.Utc); // ~7 days after new
            var testDate2 = new DateTime(2024, 1, 32, 0, 0, 0, DateTimeKind.Utc); // ~7 days before next new
            var result1 = moonPhaseService.GetDetailedMoonPhase(testDate1);
            var result2 = moonPhaseService.GetDetailedMoonPhase(testDate2);
            Console.WriteLine($"  7 days after new moon: {result1.illumination:F1}% ({result1.name})");
            Console.WriteLine($"  7 days before new moon: {result2.illumination:F1}% ({result2.name})");
            Console.WriteLine($"  Should be roughly equal in magnitude");
            Console.WriteLine();
            
            Console.WriteLine("=== Test Complete ===");
        }
    }
}
