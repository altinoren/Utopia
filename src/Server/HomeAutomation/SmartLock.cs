using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class SmartLock
    {
        public static bool IsLocked = true; // Default: locked
        private static readonly Lock lockObj = new();

        [McpServerTool(Name = "lock_get_state", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the state of the front door lock.")]
        public static Task<string> GetState()
        {
            lock (lockObj)
            {
                return Task.FromResult(IsLocked ? "Locked" : "Unlocked");
            }
        }

        [McpServerTool(Name = "lock_set_state", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the state of the front door lock.")]
        public static Task<string> SetState(bool locked)
        {
            lock (lockObj)
            {
                if (IsLocked == locked)
                    return Task.FromResult($"Front door is already {(locked ? "locked" : "unlocked")}." );
                IsLocked = locked;
                return Task.FromResult($"Front door is now {(locked ? "locked" : "unlocked")}." );
            }
        }
    }
}
