using Windows.Devices.Geolocation;
using System.Net.Http;
using System.Text.Json;

namespace Jewochron.Services
{
    public class LocationService
    {
        public async Task<(string city, string state, double latitude, double longitude)> GetLocationAsync()
        {
            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator { DesiredAccuracyInMeters = 100 };
                    var position = await geolocator.GetGeopositionAsync();
                    
                    double latitude = position.Coordinate.Point.Position.Latitude;
                    double longitude = position.Coordinate.Point.Position.Longitude;

                    // Get city and state from reverse geocoding
                    var (city, state) = await GetCityStateFromCoordinatesAsync(latitude, longitude);
                    return (city, state, latitude, longitude);
                }
            }
            catch
            {
                // Fall back to default location if permission denied or error
            }

            // Default location: New York, NY
            return ("New York", "NY", 40.7128, -74.0060);
        }

        private async Task<(string city, string state)> GetCityStateFromCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                // Using nominatim reverse geocoding (free, no API key needed)
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Jewochron/1.0");

                string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}";
                var response = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var address = doc.RootElement.GetProperty("address");

                string city = "Unknown";
                if (address.TryGetProperty("city", out var cityElement))
                    city = cityElement.GetString() ?? "Unknown";
                else if (address.TryGetProperty("town", out var townElement))
                    city = townElement.GetString() ?? "Unknown";

                string state = address.TryGetProperty("state", out var stateElement) ? stateElement.GetString() ?? "Unknown" : "Unknown";

                return (city, state);
            }
            catch
            {
                return ("Unknown", "Unknown");
            }
        }
    }
}
