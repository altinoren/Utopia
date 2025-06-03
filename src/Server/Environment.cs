using ModelContextProtocol.Server;
using System.Text.Json;
using static Utopia.Transportation.AutonomousElectricVehicle;

namespace Utopia
{
    [McpServerResourceType]
    public class Environment
    {
        public const string resourceProtocol = "utopia://server";
        private static string roomsResource;
        private static string locationsResource;

        public const double BaseTemperatureChangeRate = 0.5; // 0.5°C per minute for a 10m² room
        public const double NaturalTemperatureChangeRate = 0.1; // 0.1°C per minute for a 10m² room when system is off

        public static Dictionary<string, double> RoomsWithArea = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "Kitchen", 9.3 },
            { "Bedroom", 14.2 },
            { "Living Room", 18.3 },
            { "Bathroom", 4.5 },
            { "Hallway", 5.4 }
        };

        // London's average monthly temperatures in °C
        public static readonly double[] LondonMonthlyTemperatures = new[]
        {
            6.0,  // January
            6.1,  // February
            8.3,  // March
            11.0, // April
            14.1, // May
            17.4, // June
            19.4, // July
            19.1, // August
            16.5, // September
            12.8, // October
            9.1,  // November
            6.7   // December
        };

        // London's average monthly humidity in % (relative humidity)
        public static readonly double[] LondonMonthlyHumidity = new[]
        {
            81.0,  // January
            78.0,  // February
            75.0,  // March
            72.0,  // April
            71.0,  // May
            70.0,  // June
            68.0,  // July
            68.0,  // August
            72.0,  // September
            76.0,  // October
            80.0,  // November
            82.0   // December
        };

        public record LocationInfo(double DistanceKm, double Latitude, double Longitude);
        public static readonly Dictionary<string, LocationInfo> KnownLocations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Home", new LocationInfo(0, 51.5034, -0.1276) }, // 10 Downing Street
            { "Office", new LocationInfo(20, 51.4995, -0.1248) }, // Parliament
            { "Mall", new LocationInfo(10, 51.5079, -0.2217) }, // Westfield London
            { "Airport", new LocationInfo(50, 51.4700, -0.4543) }, // Heathrow
            { "Supermarket", new LocationInfo(5, 51.4908, -0.1426) }, // Sainsbury’s, 91-93 Warwick Way
            { "Gym", new LocationInfo( 8, 51.4941, -0.1436) } // PureGym London Victoria
        };

        [McpServerResource(Name = "Rooms", MimeType = "application/json", UriTemplate = $"{resourceProtocol}/home/rooms")]
        public static string RoomsResource()
        {
            if(roomsResource == null)
            {
                roomsResource = JsonSerializer.Serialize(RoomsWithArea.Keys.ToArray());
            }
            return roomsResource;
        }

        [McpServerResource(Name = "Locations", MimeType = "application/json", UriTemplate = $"{resourceProtocol}/transportation/locations")]
        public static string LocationsResource()
        {
            if(locationsResource == null)
            {
                locationsResource = JsonSerializer.Serialize(KnownLocations);
            }
            return locationsResource;
        }
    }
}
