using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class RobotVacuumTests
{
    private const string Room = "Kitchen";
    private const string InvalidRoom = "NonExistentRoom";

    public RobotVacuumTests()
    {
        // Reset static fields before each test
        Vacuum.LastStarted = null;
        Vacuum.LastStopped = null;
        Vacuum.LastRoom = string.Empty;
        Vacuum.EstimatedRunningTime = 0;
        Vacuum.State = VacuumState.Idle;
        Vacuum.CleaningCts?.Cancel();
        Vacuum.CleaningCts = null;
    }

    [Fact]
    public async Task GetStatus_InitiallyIdle()
    {
        var status = await Vacuum.GetStatus();
        Assert.Contains("Vacuum Status: Idle", status);
        Assert.Contains("Stopped at:", status);
    }

    [Fact]
    public async Task StartVacuum_ValidRoom_TransitionsToRunning()
    {
        var result = await Vacuum.StartVacuum(Room);
        Assert.Contains("Vacuum started. Estimated running time:", result);
        var status = await Vacuum.GetStatus();
        Assert.Contains("Vacuum Status: Running", status);
        Assert.Contains($"Room: {Room}", status);
    }

    [Fact]
    public async Task StartVacuum_InvalidRoom_ReturnsError()
    {
        var result = await Vacuum.StartVacuum(InvalidRoom);
        Assert.Contains("not found", result);
    }

    [Fact]
    public async Task StartVacuum_AlreadyRunning_ReturnsError()
    {
        await Vacuum.StartVacuum(Room);
        var result = await Vacuum.StartVacuum(Room);
        Assert.Contains("already running", result);
    }

    [Fact]
    public async Task StopVacuum_TransitionsToIdle()
    {
        await Vacuum.StartVacuum(Room);
        var result = await Vacuum.StopVacuum();
        Assert.Contains("Vacuum stopped", result);
        var status = await Vacuum.GetStatus();
        Assert.Contains("Vacuum Status: Idle", status);
    }

    [Fact]
    public async Task StopVacuum_AlreadyIdle_ReturnsError()
    {
        var result = await Vacuum.StopVacuum();
        Assert.Contains("already stopped", result);
    }
}