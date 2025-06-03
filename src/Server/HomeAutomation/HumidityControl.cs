using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class HumidityControl
    {
        public static readonly Dictionary<string, double> RoomHumidity;
        public static readonly Dictionary<string, double?> RoomSetpoints;
        public static readonly Dictionary<string, HumidityControlState> RoomStates;
        private static readonly Dictionary<string, Timer> roomTimers;
        private static readonly Lock humidityLock = new();
        private static bool isDisposed;

        static HumidityControl()
        {
            RoomHumidity = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => GetCurrentMonthlyHumidity(), StringComparer.OrdinalIgnoreCase);
            RoomSetpoints = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => (double?)null, StringComparer.OrdinalIgnoreCase);
            RoomStates = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => HumidityControlState.Off, StringComparer.OrdinalIgnoreCase);
            roomTimers = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => new Timer(_ => SimulateHumidity(room), null, TimeSpan.Zero, TimeSpan.FromMinutes(1)), StringComparer.OrdinalIgnoreCase);
        }

        private static double GetCurrentMonthlyHumidity()
        {
            int currentMonth = DateTime.UtcNow.Month; // 1-based
            return Environment.LondonMonthlyHumidity[currentMonth - 1];
        }

        private static void SimulateHumidity(string room)
        {
            if (isDisposed) return;
            lock (humidityLock)
            {
                var state = RoomStates[room];
                double current = RoomHumidity[room];
                double setpoint = RoomSetpoints[room] ?? 50.0;
                double seasonal = GetCurrentMonthlyHumidity();
                double changeRate = 2.0; // % per minute
                if (state == HumidityControlState.Humidifying)
                {
                    if (current < setpoint)
                    {
                        RoomHumidity[room] = Math.Min(current + changeRate, setpoint);
                        if (RoomHumidity[room] >= setpoint)
                        {
                            RoomStates[room] = HumidityControlState.Paused;
                        }
                    }
                    else
                    {
                        RoomStates[room] = HumidityControlState.Paused;
                    }
                }
                else if (state == HumidityControlState.Dehumidifying)
                {
                    if (current > setpoint)
                    {
                        RoomHumidity[room] = Math.Max(current - changeRate, setpoint);
                        if (RoomHumidity[room] <= setpoint)
                        {
                            RoomStates[room] = HumidityControlState.Paused;
                        }
                    }
                    else
                    {
                        RoomStates[room] = HumidityControlState.Paused;
                    }
                }
                else if (state == HumidityControlState.Paused)
                {
                    // If setpoint matches seasonal, drift by nature
                    if (Math.Abs(setpoint - seasonal) < 0.01)
                    {
                        DriftTowardsSeasonal(room, current, seasonal);
                    }
                    else if (current < setpoint)
                    {
                        RoomStates[room] = HumidityControlState.Humidifying;
                    }
                    else if (current > setpoint)
                    {
                        RoomStates[room] = HumidityControlState.Dehumidifying;
                    }
                }
                else // Off
                {
                    DriftTowardsSeasonal(room, current, seasonal);
                }
            }

            static void DriftTowardsSeasonal(string roomName, double current, double seasonal)
            {
                if (current < seasonal)
                {
                    RoomHumidity[roomName] = Math.Min(current + 1.0, seasonal);
                }
                else if (current > seasonal)
                {
                    RoomHumidity[roomName] = Math.Max(current - 1.0, seasonal);
                }
            }
        }

        [McpServerTool(Name = "humiditycontrol_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the status of the humidity control in a room.")]
        public static async Task<string> GetStatus(string room)
        {
            if (!RoomHumidity.ContainsKey(room))
                return "Room not found";
            var state = RoomStates[room];
            var setpoint = RoomSetpoints[room];
            return $"Humidity control is {state}. Current humidity in {room}: {RoomHumidity[room]:F1}%, Setpoint: {(setpoint.HasValue ? setpoint.Value.ToString("F1") + "%" : "Never set")}";
        }

        [McpServerTool(Name = "humiditycontrol_set_power", Destructive = false, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Turns the humidity control on or off in a room.")]
        public static async Task<string> SetPower(string room, bool on)
        {
            if (!RoomStates.ContainsKey(room))
                return "Room not found";
            lock (humidityLock)
            {
                double setpoint = RoomSetpoints[room] ?? 50.0;
                double current = RoomHumidity[room];
                double seasonal = GetCurrentMonthlyHumidity();
                if (on)
                {
                    if (RoomStates[room] != HumidityControlState.Off)
                        return $"Humidity control in {room} is already on.";
                    if (!RoomSetpoints[room].HasValue)
                        RoomSetpoints[room] = 50.0;
                    if (Math.Abs(setpoint - seasonal) < 0.01)
                    {
                        RoomStates[room] = HumidityControlState.Paused;
                        return $"Humidity control in {room} is paused to drift by nature (setpoint matches monthly average).";
                    }
                    else if (current < setpoint)
                    {
                        RoomStates[room] = HumidityControlState.Humidifying;
                        return $"Humidity control in {room} is now humidifying. Setpoint is {setpoint:F1}%.";
                    }
                    else if (current > setpoint)
                    {
                        RoomStates[room] = HumidityControlState.Dehumidifying;
                        return $"Humidity control in {room} is now dehumidifying. Setpoint is {setpoint:F1}%.";
                    }
                    else
                    {
                        RoomStates[room] = HumidityControlState.Paused;
                        return $"Humidity control in {room} is paused (already at setpoint).";
                    }
                }
                else
                {
                    if (RoomStates[room] == HumidityControlState.Off)
                        return $"Humidity control in {room} is already off.";
                    RoomStates[room] = HumidityControlState.Off;
                    return $"Humidity control in {room} is now off.";
                }
            }
        }

        [McpServerTool(Name = "humiditycontrol_set_humidity", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the humidity setpoint of the humidity control in a room. Turns it on if it's off.")]
        public static async Task<string> SetHumidity(string room, double humidity)
        {
            if (!RoomSetpoints.ContainsKey(room))
                return "Room not found";
            lock (humidityLock)
            {
                RoomSetpoints[room] = humidity;
                double current = RoomHumidity[room];
                double seasonal = GetCurrentMonthlyHumidity();
                if (RoomStates[room] == HumidityControlState.Off)
                {
                    if (Math.Abs(humidity - seasonal) < 0.01)
                    {
                        RoomStates[room] = HumidityControlState.Paused;
                        return $"Setpoint for {room} set to {humidity:F1}% and humidity control is paused to drift by nature (setpoint matches monthly average).";
                    }
                    else if (current < humidity)
                    {
                        RoomStates[room] = HumidityControlState.Humidifying;
                        return $"Setpoint for {room} set to {humidity:F1}% and humidity control is now Humidifying.";
                    }
                    else if (current > humidity)
                    {
                        RoomStates[room] = HumidityControlState.Dehumidifying;
                        return $"Setpoint for {room} set to {humidity:F1}% and humidity control is now Dehumidifying.";
                    }
                    else
                    {
                        RoomStates[room] = HumidityControlState.Paused;
                        return $"Setpoint for {room} set to {humidity:F1}% and humidity control is paused (already at setpoint).";
                    }
                }
                else
                {
                    return $"Setpoint for {room} set to {humidity:F1}%.";
                }
            }
        }

        [McpServerTool(Name = "humiditycontrol_get_current_humidity", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the current humidity in a room.")]
        public static async Task<double?> GetCurrentHumidity(string room)
        {
            if (!RoomHumidity.ContainsKey(room))
                return null;
            return RoomHumidity[room];
        }

        public static void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            foreach (var timer in roomTimers.Values)
            {
                timer.Dispose();
            }
            roomTimers.Clear();
        }
    }

    public enum HumidityControlState
    {
        Off,
        Humidifying,
        Dehumidifying,
        Paused
    }
}
