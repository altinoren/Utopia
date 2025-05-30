using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class SmartThermostat
    {
        public static Dictionary<string, double> RoomTemperatures { get; private set; }
        public static Dictionary<string, double?> RoomSetpoints { get; private set; }
        public static Dictionary<string, bool> RoomThermostatStates { get; private set; }
        private static Timer? temperatureSimulationTimer;
        private static readonly Lock temperatureLock = new();
        private static bool isDisposed;

        static SmartThermostat()
        {
            RoomTemperatures = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => 20.0, StringComparer.OrdinalIgnoreCase);
            RoomSetpoints = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => default(double?), StringComparer.OrdinalIgnoreCase);
            RoomThermostatStates = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => false, StringComparer.OrdinalIgnoreCase);
            temperatureSimulationTimer = new Timer(SimulateTemperatureChanges, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private static double GetCurrentSeasonalTemperature()
        {
            int currentMonth = DateTime.UtcNow.Month; // 1-based
            return Environment.LondonMonthlyTemperatures[currentMonth - 1];
        }

        private static void SimulateTemperatureChanges(object? state)
        {
            if (isDisposed) return;
            lock (temperatureLock)
            {
                var seasonalTemp = GetCurrentSeasonalTemperature();
                foreach (var room in Environment.RoomsWithArea.Keys)
                {
                    double currentTemp = RoomTemperatures[room];
                    double roomArea = Environment.RoomsWithArea[room];
                    if (RoomThermostatStates[room] && RoomSetpoints[room].HasValue)
                    {
                        double targetTemp = RoomSetpoints[room]!.Value;
                        double changeRate = (Environment.BaseTemperatureChangeRate * 10) / roomArea;
                        if (Math.Abs(targetTemp - currentTemp) < changeRate)
                        {
                            RoomTemperatures[room] = targetTemp;
                        }
                        else if (currentTemp < targetTemp)
                        {
                            RoomTemperatures[room] += changeRate;
                        }
                        else if (currentTemp > targetTemp)
                        {
                            RoomTemperatures[room] -= changeRate;
                        }
                    }
                    else
                    {
                        double changeRate = (Environment.NaturalTemperatureChangeRate * 10) / roomArea;
                        if (Math.Abs(seasonalTemp - currentTemp) < changeRate)
                        {
                            RoomTemperatures[room] = seasonalTemp;
                        }
                        else if (currentTemp < seasonalTemp)
                        {
                            RoomTemperatures[room] += changeRate;
                        }
                        else if (currentTemp > seasonalTemp)
                        {
                            RoomTemperatures[room] -= changeRate;
                        }
                    }
                }
            }
        }

        [McpServerTool(Name = "thermostat_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the status of the thermostat in a room.")]
        public static Task<string> GetStatus(string room)
        {
            lock (temperatureLock)
            {
                if (RoomTemperatures.ContainsKey(room))
                {
                    string status = RoomThermostatStates[room] ? "On" : "Off";
                    string setpointText = RoomSetpoints[room].HasValue ? $"{RoomSetpoints[room]:F1}°C" : "Never set";
                    string seasonalNote = !RoomThermostatStates[room] ? $" (Moving towards seasonal temperature: {GetCurrentSeasonalTemperature():F1}°C)" : "";
                    return Task.FromResult($"Thermostat is {status}. Current temperature in {room}: {RoomTemperatures[room]:F1}°C, Setpoint: {setpointText}{seasonalNote}");
                }
                return Task.FromResult("Room not found");
            }
        }

        [McpServerTool(Name = "thermostat_set_temperature", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the temperature of the thermostat in a room. Turns it on if it's in off state.")]
        public static Task<string> SetTemperature(string room, double temperature)
        {
            lock (temperatureLock)
            {
                if (!RoomSetpoints.ContainsKey(room))
                {
                    return Task.FromResult("Room not found");
                }
                RoomSetpoints[room] = temperature;
                if (!RoomThermostatStates[room])
                {
                    RoomThermostatStates[room] = true;
                    return Task.FromResult($"Setpoint for {room} set to {temperature:F1}°C and the thermostat is now On.");
                }
                else
                {
                    return Task.FromResult($"Setpoint for {room} set to {temperature:F1}°C.");
                }
            }
        }

        [McpServerTool(Name = "thermostat_set_power", Destructive = false, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Turns the thermostat on or off in a room.")]
        public static Task<string> SetPower(string room, bool on)
        {
            lock (temperatureLock)
            {
                if (!RoomThermostatStates.ContainsKey(room))
                {
                    return Task.FromResult("Room not found");
                }
                if (RoomThermostatStates[room] == on)
                {
                    return Task.FromResult($"Thermostat in {room} is already {(on ? "on" : "off")}");
                }
                RoomThermostatStates[room] = on;
                if (on)
                {
                    if (!RoomSetpoints[room].HasValue)
                    {
                        RoomSetpoints[room] = 20.0;
                    }
                    return Task.FromResult($"Thermostat in {room} is now on and the thermostat setpoint is now {RoomSetpoints[room].GetValueOrDefault():F1}°C.");
                }
                else
                {
                    return Task.FromResult($"Thermostat in {room} is now off. Setpoint is previously set to {RoomSetpoints[room].GetValueOrDefault():F1}°C");
                }
            }
        }

        public static void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                temperatureSimulationTimer?.Dispose();
                temperatureSimulationTimer = null;
            }
        }
    }
}

public enum ThermostatState
{
    Off,
    On
}
