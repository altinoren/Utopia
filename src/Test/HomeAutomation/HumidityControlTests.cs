using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class HumidityControlTests
{
    private const string Room = "Kitchen";
    private const string InvalidRoom = "NonExistentRoom";

    public HumidityControlTests()
    {
        // Reset state before each test
        var monthly = Utopia.Environment.LondonMonthlyHumidity;
        var initial = monthly[DateTime.UtcNow.Month - 1];
        var dictHumidity = HumidityControl.RoomHumidity;
        var dictSetpoints = HumidityControl.RoomSetpoints;
        var dictStates = HumidityControl.RoomStates;
        foreach (var key in Utopia.Environment.RoomsWithArea.Keys)
        {
            dictHumidity[key] = initial;
            dictSetpoints[key] = null;
            dictStates[key] = HumidityControlState.Off;
        }
    }

    [Fact]
    public async Task GetStatus_ValidRoom_ReturnsCorrectStatus()
    {
        var result = await HumidityControl.GetStatus(Room);
        Assert.Contains($"Current humidity in {Room}", result);
    }

    [Fact]
    public async Task GetStatus_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await HumidityControl.GetStatus(InvalidRoom);
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public async Task SetPower_On_InitializesSetpointAndTurnsOn()
    {
        var result = await HumidityControl.SetPower(Room, true);
        Assert.Contains("now", result);
        Assert.Contains("Setpoint is 50.0%", result);
    }

    [Fact]
    public async Task SetPower_Off_TurnsOff()
    {
        await HumidityControl.SetPower(Room, true);
        var result = await HumidityControl.SetPower(Room, false);
        Assert.Contains("now off", result);
    }

    [Fact]
    public async Task SetPower_AlreadyOn_ReportsAlreadyOn()
    {
        await HumidityControl.SetPower(Room, true);
        var result = await HumidityControl.SetPower(Room, true);
        Assert.Contains("already on", result);
    }

    [Fact]
    public async Task SetPower_AlreadyOff_ReportsAlreadyOff()
    {
        var result = await HumidityControl.SetPower(Room, false);
        Assert.Contains("already off", result);
    }

    [Fact]
    public async Task SetHumidity_SetsSetpointAndTurnsOnIfOff()
    {
        var result = await HumidityControl.SetHumidity(Room, 45.5);
        Assert.Contains("Setpoint for Kitchen set to 45.5%", result);
    }

    [Fact]
    public async Task SetHumidity_UpdatesSetpointIfOn()
    {
        await HumidityControl.SetPower(Room, true);
        var result = await HumidityControl.SetHumidity(Room, 40.0);
        Assert.Contains("Setpoint for Kitchen set to 40.0%", result);
    }

    [Fact]
    public async Task GetCurrentHumidity_ValidRoom_ReturnsValue()
    {
        var value = await HumidityControl.GetCurrentHumidity(Room);
        Assert.NotNull(value);
        Assert.True(value > 0);
    }

    [Fact]
    public async Task GetCurrentHumidity_InvalidRoom_ReturnsNull()
    {
        var value = await HumidityControl.GetCurrentHumidity(InvalidRoom);
        Assert.Null(value);
    }

    [Fact]
    public async Task SetPower_CaseInsensitiveRoomNames()
    {
        var result = await HumidityControl.SetPower("kitchen", true);
        Assert.Contains("now", result);
        var status = await HumidityControl.GetStatus("KITCHEN");
        Assert.Contains("Humidity control is", status);
    }
}