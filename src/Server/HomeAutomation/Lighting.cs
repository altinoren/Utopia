using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class Lighting
    {
        public static Dictionary<string, bool> LightsStatus { get; private set; }
        private static readonly Lock lightsLock = new();

        static Lighting()
        {
            LightsStatus = Environment.RoomsWithArea.Keys.ToDictionary(room => room, room => false, StringComparer.OrdinalIgnoreCase);
        }

        [McpServerTool(Name = "lights_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the status of lights in a room.")]
        public static Task<string> GetStatus(string room)
        {
            lock (lightsLock)
            {
                if (LightsStatus.ContainsKey(room))
                {
                    return Task.FromResult(LightsStatus[room] ? "On" : "Off");
                }
                return Task.FromResult("Room not found");
            }
        }

        [McpServerTool(Name = "lights_set_status", Destructive = false, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the status of lights in a room.")]
        public static Task<string> SetStatus(string room, bool status)
        {
            lock (lightsLock)
            {
                if (LightsStatus.ContainsKey(room))
                {
                    if (LightsStatus[room] == status)
                    {
                        return Task.FromResult($"Lights in {room} are already {(status ? "On" : "Off")}." );
                    }

                    LightsStatus[room] = status;
                    return Task.FromResult($"Lights in {room} are now {(status ? "On" : "Off")}." );
                }
                return Task.FromResult("Room not found");
            }
        }
    }
}
