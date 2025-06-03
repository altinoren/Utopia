using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class SmartBedTests
{
    public SmartBedTests()
    {
        // Reset SmartBed static state before each test
        SmartBed.EndSleepSessionTool().Wait();
        SmartBed.Dispose();
        // Reflection or internal method could be used for a more thorough reset if needed
    }

    [Fact]
    public async Task GetStatus_InitiallyOff()
    {
        var status = await SmartBed.GetStatus();
        Assert.Contains("Climate control OFF", status);
    }

    [Fact]
    public async Task SetForSleep_ActivatesClimateControl()
    {
        var result = await SmartBed.SetForSleep(19.5, 7);
        Assert.Contains("Bed set for sleep", result);
        var status = await SmartBed.GetStatus();
        Assert.Contains("Climate control ON", status);
        Assert.Contains("19.5", status);
        Assert.Contains("7h", status);
    }

    [Fact]
    public async Task SetForSleep_WhileActive_ReturnsAlreadySet()
    {
        await SmartBed.SetForSleep(19.0, 8);
        var result = await SmartBed.SetForSleep(20.0, 6);
        Assert.Contains("already set for sleep", result);
    }

    [Fact]
    public async Task EndSleepSessionTool_EndsSessionAndRecordsQuality()
    {
        await SmartBed.SetForSleep(18.5, 8);
        var result = await SmartBed.EndSleepSessionTool();
        Assert.Contains("Sleep session ended", result);
        var status = await SmartBed.GetStatus();
        Assert.Contains("Climate control OFF", status);
        var quality = await SmartBed.GetLastSleepQuality();
        Assert.Contains("Last sleep quality", quality);
    }

    [Fact]
    public async Task EndSleepSessionTool_WhenNotActive_ReturnsNoSession()
    {
        var result = await SmartBed.EndSleepSessionTool();
        Assert.Contains("No sleep session is currently active", result);
    }

    [Fact]
    public async Task GetLastSleepQuality_NoSession_ReturnsNoSession()
    {
        var result = await SmartBed.GetLastSleepQuality();
        Assert.Contains("No sleep session recorded yet", result);
    }
}
