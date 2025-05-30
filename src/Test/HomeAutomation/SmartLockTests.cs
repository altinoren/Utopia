using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class SmartLockTests
{
    public SmartLockTests()
    {
        // Reset lock state before each test (default: locked)
        SmartLock.IsLocked = true;
    }

    [Fact]
    public async Task GetState_InitiallyLocked()
    {
        var state = await SmartLock.GetState();
        Assert.Equal("Locked", state);
    }

    [Fact]
    public async Task SetState_UnlocksAndLocks()
    {
        var result1 = await SmartLock.SetState(false);
        Assert.Equal("Front door is now unlocked.", result1);
        var state1 = await SmartLock.GetState();
        Assert.Equal("Unlocked", state1);

        var result2 = await SmartLock.SetState(true);
        Assert.Equal("Front door is now locked.", result2);
        var state2 = await SmartLock.GetState();
        Assert.Equal("Locked", state2);
    }

    [Fact]
    public async Task SetState_AlreadyInState_ReturnsAlreadyMessage()
    {
        await SmartLock.SetState(true); // Ensure locked
        var result1 = await SmartLock.SetState(true);
        Assert.Equal("Front door is already locked.", result1);

        await SmartLock.SetState(false); // Unlock
        var result2 = await SmartLock.SetState(false);
        Assert.Equal("Front door is already unlocked.", result2);
    }
}