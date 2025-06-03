using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class SmartBed
    {
        private static readonly Lock bedLock = new();
        private static double _currentTemperature = 18.0; // Default temperature in Celsius
        private static double _targetTemperature = 18.0;
        private static bool _isClimateOn = false;
        private static Timer? _climateTimer;
        private static Timer? _sleepSessionTimer;
        private static int _sleepSessionHours = 8;
        private static double _lastSleepQuality = 0.0;
        private static bool _isDisposed = false;

        [McpServerTool(Name = "bed_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the current status of the smart bed.")]
        public static Task<string> GetStatus()
        {
            lock (bedLock)
            {
                string status = _isClimateOn ? $"Climate control ON, Target: {_targetTemperature:F1}°C, Current: {_currentTemperature:F1}°C, Time left: {_sleepSessionHours}h" : $"Climate control OFF, Last setpoint: {_targetTemperature:F1}°C, Current: {_currentTemperature:F1}°C";
                return Task.FromResult(status);
            }
        }

        [McpServerTool(Name = "bed_set_for_sleep", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the bed for sleep: sets temperature and sleep duration (hours, default 8). Starts climate control.")]
        public static Task<string> SetForSleep(double temperature, int hours = 8)
        {
            lock (bedLock)
            {
                if (_isClimateOn)
                {
                    return Task.FromResult("Bed is already set for sleep. Please wait for the current session to finish or stop it first.");
                }
                _targetTemperature = temperature;
                _sleepSessionHours = hours;
                _isClimateOn = true;
                _climateTimer = new Timer(_ => SimulateClimate(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                _sleepSessionTimer = new Timer(_ => EndSleepSession(), null, TimeSpan.FromHours(hours), Timeout.InfiniteTimeSpan);
                return Task.FromResult($"Bed set for sleep: Target temperature {_targetTemperature:F1}°C for {_sleepSessionHours} hours.");
            }
        }

        [McpServerTool(Name = "bed_get_last_sleep_quality", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Returns the sleep quality of the last sleep session (simulated).")]
        public static Task<string> GetLastSleepQuality()
        {
            lock (bedLock)
            {
                if (_lastSleepQuality == 0.0)
                    return Task.FromResult("No sleep session recorded yet.");
                return Task.FromResult($"Last sleep quality: {_lastSleepQuality:F1}/100");
            }
        }

        [McpServerTool(Name = "bed_end_sleep_session", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Ends the current sleep session immediately and stops climate control.")]
        public static Task<string> EndSleepSessionTool()
        {
            lock (bedLock)
            {
                if (!_isClimateOn)
                {
                    return Task.FromResult("No sleep session is currently active.");
                }
                EndSleepSession();
                return Task.FromResult("Sleep session ended. Climate control is now off. Sleep quality has been recorded.");
            }
        }

        private static void SimulateClimate()
        {
            lock (bedLock)
            {
                if (!_isClimateOn || _isDisposed) return;
                // Simulate temperature moving towards target
                double changeRate = 0.2; // °C per minute
                if (Math.Abs(_currentTemperature - _targetTemperature) < changeRate)
                {
                    _currentTemperature = _targetTemperature;
                }
                else if (_currentTemperature < _targetTemperature)
                {
                    _currentTemperature += changeRate;
                }
                else if (_currentTemperature > _targetTemperature)
                {
                    _currentTemperature -= changeRate;
                }
            }
        }

        private static void EndSleepSession()
        {
            lock (bedLock)
            {
                _isClimateOn = false;
                _climateTimer?.Dispose();
                _climateTimer = null;
                _sleepSessionTimer?.Dispose();
                _sleepSessionTimer = null;
                // Simulate sleep quality (random for now)
                var rand = new Random();
                _lastSleepQuality = rand.NextDouble() * 40 + 60; // 60-100
            }
        }

        public static void Dispose()
        {
            lock (bedLock)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _climateTimer?.Dispose();
                _sleepSessionTimer?.Dispose();
            }
        }
    }
}
