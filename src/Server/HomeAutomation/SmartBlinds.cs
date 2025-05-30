using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class SmartBlinds
    {
        public static Dictionary<string, int> RoomBlinds { get; private set; }
        public static Dictionary<string, Timer> RoomTimers { get; private set; }
        public static Dictionary<string, int> RoomTarget { get; private set; }
        private static readonly Lock blindsLock = new();
        private static bool isDisposed;

        static SmartBlinds()
        {
            RoomBlinds = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => 100, StringComparer.OrdinalIgnoreCase); // 100% open
            RoomTimers = new();
            RoomTarget = new();
        }

        [McpServerTool(Name = "blinds_get_state", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the current open percentage of the blinds in a room.")]
        public static Task<string> GetState(string room)
        {
            lock (blindsLock)
            {
                if (!RoomBlinds.ContainsKey(room))
                    return Task.FromResult("Room not found");
                return Task.FromResult($"Blinds in {room} are {RoomBlinds[room]}% open.");
            }
        }

        [McpServerTool(Name = "blinds_set_state", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the open percentage of the blinds in a room immediately (nearest 10%).")]
        public static Task<string> SetState(string room, int percent)
        {
            lock (blindsLock)
            {
                if (!RoomBlinds.ContainsKey(room))
                    return Task.FromResult("Room not found");
                int clamped = Math.Max(0, Math.Min(100, (int)Math.Round(percent / 10.0) * 10));
                RoomBlinds[room] = clamped;
                if (RoomTimers.TryGetValue(room, out var timer))
                {
                    RoomTarget[room] = clamped;
                    if (RoomBlinds[room] == clamped)
                    {
                        timer.Dispose();
                        RoomTimers.Remove(room);
                        RoomTarget.Remove(room);
                    }
                }
                else
                {
                    if (RoomTarget.ContainsKey(room))
                        RoomTarget.Remove(room);
                }
                return Task.FromResult($"Blinds in {room} set to {clamped}% open.");
            }
        }

        [McpServerTool(Name = "blinds_set_state_with_timer", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Schedules the blinds to reach the target open percentage at the specified time, changing one increment per minute.")]
        public static Task<string> SetStateWithTimer(string room, int percent, DateTime targetTime)
        {
            lock (blindsLock)
            {
                if (!RoomBlinds.ContainsKey(room))
                    return Task.FromResult("Room not found");
                int clamped = Math.Max(0, Math.Min(100, (int)Math.Round(percent / 10.0) * 10));
                if (RoomTimers.TryGetValue(room, out var timer))
                {
                    timer.Dispose();
                    RoomTimers.Remove(room);
                    RoomTarget.Remove(room);
                }
                int current = RoomBlinds[room];
                int steps = Math.Abs(clamped - current) / 10;
                if (steps == 0)
                {
                    RoomBlinds[room] = clamped;
                    return Task.FromResult($"Blinds in {room} already at {clamped}% open.");
                }
                DateTime now = DateTime.Now;
                TimeSpan totalTime = targetTime - now;
                if (totalTime.TotalMinutes < steps - 0.5)
                {
                    RoomBlinds[room] = clamped;
                    return Task.FromResult($"Not enough time to schedule gradual change. Blinds in {room} set to {clamped}% open immediately.");
                }
                int direction = clamped > current ? 1 : -1;
                int next = current + 10 * direction;
                DateTime firstChange = targetTime.AddMinutes(-steps + 1);
                TimeSpan due = firstChange - now;
                if (due < TimeSpan.Zero) due = TimeSpan.Zero;
                RoomTarget[room] = clamped;
                RoomTimers[room] = new Timer(_ => StepBlinds(room, direction), null, due, TimeSpan.FromMinutes(1));
                return Task.FromResult($"Blinds in {room} will be set to {clamped}% open at {targetTime:t}, changing one step per minute.");
            }
        }

        private static void StepBlinds(string room, int direction)
        {
            lock (blindsLock)
            {
                if (!RoomBlinds.ContainsKey(room) || !RoomTarget.ContainsKey(room)) return;
                int current = RoomBlinds[room];
                int target = RoomTarget[room];
                if (current == target)
                {
                    if (RoomTimers.TryGetValue(room, out var timer))
                    {
                        timer.Dispose();
                        RoomTimers.Remove(room);
                        RoomTarget.Remove(room);
                    }
                    return;
                }
                int next = current + 10 * Math.Sign(target - current);
                if ((direction > 0 && next > target) || (direction < 0 && next < target))
                    next = target;
                RoomBlinds[room] = next;
                if (next == target)
                {
                    if (RoomTimers.TryGetValue(room, out var timer))
                    {
                        timer.Dispose();
                        RoomTimers.Remove(room);
                        RoomTarget.Remove(room);
                    }
                }
            }
        }

        public static void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                foreach (var timer in RoomTimers.Values)
                {
                    timer.Dispose();
                }
                RoomTimers.Clear();
                RoomTarget.Clear();
            }
        }
    }
}
