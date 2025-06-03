using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utopia.Transportation
{
    [McpServerToolType]
    public static class AutonomousElectricVehicle
    {
        public enum CarState { Parked, Driving, Charging }

        private static readonly Lock carLock = new();
        private const double SpeedKmh = 50.0;
        private const double ChargeRatePerMinute = 1.0; // percent per minute
        private const double MaxBattery = 100.0;
        private static CancellationTokenSource? drivingCts = null;
        private static CancellationTokenSource? chargingCts = null;
        private static (string destination, DateTime time)? scheduledTrip = null;
        private static CancellationTokenSource? scheduleCts = null;
        private static string location = "Home";

        public static CarState State { get; set; } = CarState.Parked;
        public static string Location {
            get => location;
            set => location = value;
        }
        public static double Battery { get; set; } = 80.0;
        public static string? DrivingDestination { get; set; } = null;
        public static (string destination, DateTime time)? ScheduledTrip
        {
            get { lock (carLock) { return scheduledTrip; } }
            set { lock (carLock) { scheduledTrip = value; } }
        }

        private static void StartScheduleMonitor()
        {
            scheduleCts?.Cancel();
            scheduleCts = new CancellationTokenSource();
            var token = scheduleCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    (string destination, DateTime time)? trip = null;
                    lock (carLock)
                    {
                        trip = scheduledTrip;
                    }
                    if (trip.HasValue)
                    {
                        var now = DateTime.Now;
                        if (now >= trip.Value.time)
                        {
                            lock (carLock)
                            {
                                if (scheduledTrip.HasValue && scheduledTrip.Value.time <= DateTime.Now)
                                {
                                    if (State == CarState.Parked)
                                    {
                                        if (Environment.KnownLocations.TryGetValue(trip.Value.destination, out var loc))
                                        {
                                            _ = DriveTo(loc.Latitude, loc.Longitude);
                                        }
                                        else if (trip.Value.destination.StartsWith("Custom:"))
                                        {
                                            var parts = trip.Value.destination.Split(':');
                                            var coords = parts[1].Split(',');
                                            double latitude = double.Parse(coords[0]);
                                            double longitude = double.Parse(coords[1]);
                                            _ = DriveTo(latitude, longitude);
                                        }
                                    }
                                    // In all cases, clear the schedule if time has come
                                    scheduledTrip = null;
                                }
                            }
                        }
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
            }, token);
        }

        static AutonomousElectricVehicle()
        {
            Location = "Home"; // Default starting location
            StartScheduleMonitor();
        }

        [McpServerTool(Name = "car_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Returns the current status of the EV, including whether it's parked, driving, or charging. Also include current location, battery level (percentage), and whether a trip is scheduled.")]
        public static Task<string> GetStatus()
        {
            lock (carLock)
            {
                string locStr = Location;
                if (Location.StartsWith("Custom:"))
                {
                    var coords = Location.Substring(7);
                    locStr = $"({coords})";
                }
                string status = $"Car is {State}. Location: {locStr}. Battery: {Battery:F1}%. ";
                if (State == CarState.Driving)
                    status += $"Driving to {DrivingDestination}. ";
                if (State == CarState.Charging)
                    status += "Charging. ";
                if (ScheduledTrip.HasValue)
                    status += $"Trip scheduled to {ScheduledTrip.Value.destination} at {ScheduledTrip.Value.time}. ";
                return Task.FromResult(status.Trim());
            }
        }

        [McpServerTool(Name = "car_get_info", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Returns information about the car, including brand, model, battery, state, and location.")]
        public static Task<string> GetCarInfo()
        {
            lock (carLock)
            {
                string brand = "ACMECar";
                string model = "Autonomous EV";
                string info = $"Brand: {brand}, Model: {model}, State: {State}, Location: {Location}, Battery: {Battery:F1}%";
                return Task.FromResult(info);
            }
        }

        [McpServerTool(Name = "car_drive_to", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Starts autonomous driving to a specified coordinates. Fails if the car is already in motion or charging. Update state accordingly and simulate trip time based on a fixed speed and a mock distance map.")]
        public static Task<string> DriveTo(double latitude, double longitude)
        {
            lock (carLock)
            {
                if (State == CarState.Driving)
                    return Task.FromResult($"Car is already driving to {DrivingDestination}.");
                if (State == CarState.Charging)
                    return Task.FromResult("Cannot drive while charging.");

                // Find closest known location
                string? closest = null;
                double minDist = double.MaxValue;
                foreach (var kvp in Environment.KnownLocations)
                {
                    double d = Math.Sqrt(Math.Pow(kvp.Value.Latitude - latitude, 2) + Math.Pow(kvp.Value.Longitude - longitude, 2));
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = kvp.Key;
                    }
                }

                // Get current coordinates
                double currLat, currLon;
                if (Environment.KnownLocations.TryGetValue(Location, out var currLoc))
                {
                    currLat = currLoc.Latitude;
                    currLon = currLoc.Longitude;
                }
                else if (Location.StartsWith("Custom:"))
                {
                    var parts = Location.Split(':');
                    var coords = parts[1].Split(',');
                    currLat = double.Parse(coords[0]);
                    currLon = double.Parse(coords[1]);
                }
                else
                {
                    // fallback to Home
                    currLat = Environment.KnownLocations["Home"].Latitude;
                    currLon = Environment.KnownLocations["Home"].Longitude;
                }

                // Calculate distance using Haversine formula
                double R = 6371.0; // Earth radius in km
                double dLat = (latitude - currLat) * Math.PI / 180.0;
                double dLon = (longitude - currLon) * Math.PI / 180.0;
                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(currLat * Math.PI / 180.0) * Math.Cos(latitude * Math.PI / 180.0) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                double distance = R * c;

                // If destination is a known location and within threshold, snap to it
                bool isKnown = minDist <= 0.02;
                string destLabel = isKnown ? closest! : $"Custom:{latitude},{longitude}";
                if (Location == destLabel)
                    return Task.FromResult($"Car is already at {(isKnown ? closest : $"({latitude}, {longitude})")}.");

                double requiredBattery = distance; // 1% per km for simplicity
                if (Battery < requiredBattery)
                    return Task.FromResult($"Not enough battery for the trip. Required: {requiredBattery:F1}%, Available: {Battery:F1}%");
                State = CarState.Driving;
                DrivingDestination = destLabel;
                drivingCts?.Cancel();
                drivingCts = new CancellationTokenSource();
                var token = drivingCts.Token;
                double tripMinutes = distance / SpeedKmh * 60.0;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(tripMinutes), token);
                        if (!token.IsCancellationRequested)
                        {
                            lock (carLock)
                            {
                                Battery -= requiredBattery;
                                Location = destLabel;
                                State = CarState.Parked;
                                DrivingDestination = null;
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                });
                return Task.FromResult($"Driving to {(isKnown ? closest : $"({latitude}, {longitude})")} (lat: {latitude}, lon: {longitude}). Estimated time: {tripMinutes:F1} minutes.");
            }
        }

        [McpServerTool(Name = "car_stop", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Stops the car if it is currently driving. Updates state to parked. If the car is not driving, return a message indicating it's already stopped.")]
        public static Task<string> Stop()
        {
            lock (carLock)
            {
                if (State != CarState.Driving)
                    return Task.FromResult("Car is already stopped.");
                drivingCts?.Cancel();
                State = CarState.Parked;
                DrivingDestination = null;
                return Task.FromResult("Car stopped and parked.");
            }
        }

        [McpServerTool(Name = "car_start_charging", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Begins charging the car if it's parked. Fails if it's currently driving or already charging. Simulate charging over time (e.g., 1% every minute), and allow cancellation through car_stop_charging.")]
        public static Task<string> StartCharging()
        {
            lock (carLock)
            {
                if (State == CarState.Driving)
                    return Task.FromResult("Cannot charge while driving.");
                if (State == CarState.Charging)
                    return Task.FromResult("Already charging.");
                if (Battery >= MaxBattery)
                    return Task.FromResult("Battery is already full.");
                State = CarState.Charging;
                chargingCts?.Cancel();
                chargingCts = new CancellationTokenSource();
                var token = chargingCts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1), token);
                            lock (carLock)
                            {
                                if (Battery < MaxBattery)
                                {
                                    Battery = Math.Min(MaxBattery, Battery + ChargeRatePerMinute);
                                }
                                if (Battery >= MaxBattery || token.IsCancellationRequested)
                                {
                                    State = CarState.Parked;
                                    chargingCts = null;
                                    break;
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                });
                return Task.FromResult("Charging started.");
            }
        }

        [McpServerTool(Name = "car_stop_charging", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Stops charging the car. Update the status to reflect the final battery percentage and change the state to parked.")]
        public static Task<string> StopCharging()
        {
            lock (carLock)
            {
                if (State != CarState.Charging)
                    return Task.FromResult("Car is not charging.");
                chargingCts?.Cancel();
                State = CarState.Parked;
                chargingCts = null;
                return Task.FromResult($"Charging stopped. Battery at {Battery:F1}%.");
            }
        }

        [McpServerTool(Name = "car_schedule_trip", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Schedules a future trip to a given destination at a specific DateTime. Store trip info and allow scheduling regardless of current state. If the car is not parked at the scheduled time, the schedule will be cancelled.")]
        public static Task<string> ScheduleTrip(string destination, DateTime time)
        {
            lock (carLock)
            {
                // Allow scheduling to any coordinates as well as known locations
                bool isKnown = Environment.KnownLocations.ContainsKey(destination);
                if (!isKnown && !destination.StartsWith("Custom:"))
                    return Task.FromResult($"Unknown destination: {destination}.");
                if (ScheduledTrip.HasValue)
                    return Task.FromResult($"A trip is already scheduled to {ScheduledTrip.Value.destination} at {ScheduledTrip.Value.time}.");
                ScheduledTrip = (destination, time);
                scheduleCts?.Cancel();
                StartScheduleMonitor();
                return Task.FromResult($"Trip scheduled to {destination} at {time}.");
            }
        }

        [McpServerTool(Name = "car_cancel_scheduled_trip", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Cancels the currently scheduled trip if one exists.")]
        public static Task<string> CancelScheduledTrip()
        {
            lock (carLock)
            {
                if (!ScheduledTrip.HasValue)
                    return Task.FromResult("No trip is currently scheduled.");
                ScheduledTrip = null;
                scheduleCts?.Cancel();
                return Task.FromResult("Scheduled trip cancelled.");
            }
        }
    }
}
