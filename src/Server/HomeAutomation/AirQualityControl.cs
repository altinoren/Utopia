using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class AirQualityControl
    {
        public static Dictionary<string, AirQuality> RoomAirQuality { get; private set; }
        public static Dictionary<string, OperationMode> RoomModes { get; private set; }
        public static Dictionary<string, bool> RoomPowerStates { get; private set; }
        public static Dictionary<string, Timer> RoomTimers { get; private set; }
        private static readonly Lock airQualityLock = new();
        private static bool isDisposed;


        static AirQualityControl()
        {
            RoomAirQuality = Environment.RoomsWithArea.Keys.ToDictionary(
                room => room,
                room => AirQuality.Good,
                StringComparer.OrdinalIgnoreCase);
            RoomModes = Environment.RoomsWithArea.Keys.ToDictionary(
                room => room,
                room => OperationMode.Normal,
                StringComparer.OrdinalIgnoreCase);
            RoomPowerStates = Environment.RoomsWithArea.Keys.ToDictionary(
                room => room,
                room => false,
                StringComparer.OrdinalIgnoreCase);
            RoomTimers = Environment.RoomsWithArea.Keys.ToDictionary(
                room => room,
                room => new Timer(_ => SimulateAirQuality(room), null, TimeSpan.Zero, TimeSpan.FromMinutes(1)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void SimulateAirQuality(string room)
        {
            lock (airQualityLock)
            {
                if (!RoomPowerStates[room])
                    return;

                var currentQuality = RoomAirQuality[room];
                if (currentQuality == AirQuality.Good)
                    return;

                var mode = RoomModes[room];
                var improveMinutes = mode == OperationMode.Normal ? 1.0 : 0.5; // Quiet mode takes twice as long

                // Calculate time needed for each transition
                var totalMinutesNeeded = currentQuality == AirQuality.VeryUnhealthy ? 300.0 : 120.0; // 5 hours or 2 hours
                var progressPerMinute = 100.0 / totalMinutesNeeded;
                
                // Improve air quality based on time passing
                var qualityProgress = improveMinutes * progressPerMinute;
                if (qualityProgress >= 100.0 || 
                    (currentQuality == AirQuality.Moderate && qualityProgress >= 50.0))
                {
                    RoomAirQuality[room] = currentQuality == AirQuality.VeryUnhealthy ? 
                        AirQuality.Moderate : AirQuality.Good;
                }
            }
        }

        [McpServerTool(Name = "airquality_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the status of the air quality control in a room.")]
        public static async Task<string> GetStatus(string room)
        {
            if (!RoomAirQuality.ContainsKey(room))
                return "Room not found";

            return $"Air quality in {room} is {RoomAirQuality[room]}. " +
                   $"Unit is {(RoomPowerStates[room] ? "On" : "Off")} " +
                   $"in {RoomModes[room]} mode.";
        }

        [McpServerTool(Name = "airquality_set_power", Destructive = false, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Turns the air quality control unit on or off in a room.")]
        public static async Task<string> SetPower(string room, bool on)
        {
            if (!RoomPowerStates.ContainsKey(room))
                return "Room not found";

            lock (airQualityLock)
            {
                if (RoomPowerStates[room] == on)
                    return $"Air quality control in {room} is already {(on ? "on" : "off")}.";

                RoomPowerStates[room] = on;
                return $"Air quality control in {room} is now {(on ? "on" : "off")}.";
            }
        }

        [McpServerTool(Name = "airquality_set_mode", Destructive = false, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the operation mode (Normal or Quiet) for the air quality control in a room.")]
        public static async Task<string> SetMode(string room, OperationMode mode)
        {
            if (!RoomModes.ContainsKey(room))
                return "Room not found";

            lock (airQualityLock)
            {
                if (RoomModes[room] == mode)
                    return $"Air quality control in {room} is already in {mode} mode.";

                RoomModes[room] = mode;
                return $"Air quality control in {room} is now in {mode} mode.";
            }
        }

        [McpServerTool(Name = "airquality_degrade", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Development tool to simulate air quality degradation in a room.")]
        public static async Task<string> SimulateDegradation(string room, AirQuality targetQuality)
        {
            if (!RoomAirQuality.ContainsKey(room))
                return "Room not found";

            if (targetQuality == AirQuality.Good)
                return "Cannot degrade to Good quality";

            lock (airQualityLock)
            {
                RoomAirQuality[room] = targetQuality;
                return $"Air quality in {room} is now {targetQuality}.";
            }
        }

        public static void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            if (RoomTimers != null)
            {
                foreach (var timer in RoomTimers.Values)
                {
                    timer.Dispose();
                }
                RoomTimers.Clear();
            }
        }
    }

    public enum AirQuality
    {
        Good,
        Moderate,
        VeryUnhealthy
    }

    public enum OperationMode
    {
        Normal,
        Quiet
    }
}