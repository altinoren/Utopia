using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class Vacuum
    {
        private static readonly double cleaningTimePerSqmInMinutes = 1.5;
        private static readonly object vacuumLock = new();

        public static DateTime? LastStarted { get; set; } = null;
        public static DateTime? LastStopped { get; set; } = null;
        public static string LastRoom { get; set; } = string.Empty;
        public static int EstimatedRunningTime { get; set; } = 0;
        public static VacuumState State { get; set; } = VacuumState.Idle;
        public static CancellationTokenSource? CleaningCts { get; set; } = null;

        [McpServerTool(Name = "vacuum_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the status of the robot vacuum.")]
        public static Task<string> GetStatus()
        {
            lock (vacuumLock)
            {
                string status = $"Vacuum Status: {State}\n";
                if (State == VacuumState.Running)
                {
                    status += $"Started on: {LastStarted}\n";
                    status += $"Room: {LastRoom}\n";
                    status += $"Estimated running time: {EstimatedRunningTime} minutes\n";
                }
                else if (State == VacuumState.Idle)
                {
                    status += $"Stopped at: {LastStopped}\n";
                }
                return Task.FromResult(status);
            }
        }

        [McpServerTool(Name = "vacuum_start", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Starts the robot vacuum.")]
        public static Task<string> StartVacuum(string room)
        {
            lock (vacuumLock)
            {
                if (State == VacuumState.Running)
                {
                    return Task.FromResult($"Vacuum is already running in {LastRoom}.");
                }
                if (!Environment.RoomsWithArea.ContainsKey(room))
                {
                    return Task.FromResult($"Room '{room}' not found.");
                }
                LastStarted = DateTime.Now;
                LastRoom = room;
                EstimatedRunningTime = Convert.ToInt32(Environment.RoomsWithArea[room] * cleaningTimePerSqmInMinutes);
                State = VacuumState.Running;
                CleaningCts?.Cancel();
                CleaningCts = new CancellationTokenSource();
                var token = CleaningCts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(EstimatedRunningTime * 60 * 1000, token);
                        if (!token.IsCancellationRequested)
                        {
                            await StopVacuum();
                        }
                    }
                    catch (TaskCanceledException) { }
                });
                return Task.FromResult($"Vacuum started. Estimated running time: {EstimatedRunningTime} minutes.");
            }
        }

        [McpServerTool(Name = "vacuum_stop", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Stops the robot vacuum.")]
        public static Task<string> StopVacuum()
        {
            lock (vacuumLock)
            {
                if (State == VacuumState.Idle)
                {
                    return Task.FromResult($"Vacuum is already stopped.");
                }
                CleaningCts?.Cancel();
                LastStopped = DateTime.Now;
                State = VacuumState.Idle;
                return Task.FromResult("Vacuum stopped.");
            }
        }
    }
}

public enum VacuumState
{
    Idle,
    Running
}
