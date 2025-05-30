using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class SmartThermostatTests
{
    private const string Room = "Kitchen";
    private const string InvalidRoom = "NonExistentRoom";

    public SmartThermostatTests()
    {
        // Reset state before each test
        foreach (var room in SmartThermostat.RoomTemperatures.Keys.ToList())
        {
            SmartThermostat.RoomTemperatures[room] = 20.0;
            SmartThermostat.RoomSetpoints[room] = null;
            SmartThermostat.RoomThermostatStates[room] = false;
        }
    }

    [Fact]
    public async Task GetStatus_ValidRoom_ReturnsCorrectStatus()
    {
        var result = await SmartThermostat.GetStatus(Room);
        Assert.Contains($"Current temperature in {Room}", result);
    }

    [Fact]
    public async Task GetStatus_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await SmartThermostat.GetStatus(InvalidRoom);
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public async Task SetTemperature_TurnsOnIfOff()
    {
        var result = await SmartThermostat.SetTemperature(Room, 22.5);
        Assert.Contains("set to 22.5", result);
        Assert.True(SmartThermostat.RoomThermostatStates[Room]);
        Assert.Equal(22.5, SmartThermostat.RoomSetpoints[Room]);
    }

    [Fact]
    public async Task SetTemperature_UpdatesIfOn()
    {
        await SmartThermostat.SetTemperature(Room, 21.0);
        var result = await SmartThermostat.SetTemperature(Room, 19.0);
        Assert.Contains("set to 19.0", result);
        Assert.Equal(19.0, SmartThermostat.RoomSetpoints[Room]);
    }

    [Fact]
    public async Task SetTemperature_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await SmartThermostat.SetTemperature(InvalidRoom, 21.0);
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public async Task SetPower_On_InitializesSetpoint()
    {
        var result = await SmartThermostat.SetPower(Room, true);
        Assert.Contains("now on", result);
        Assert.True(SmartThermostat.RoomThermostatStates[Room]);
        Assert.Equal(20.0, SmartThermostat.RoomSetpoints[Room]);
    }

    [Fact]
    public async Task SetPower_Off_TurnsOff()
    {
        await SmartThermostat.SetPower(Room, true);
        var result = await SmartThermostat.SetPower(Room, false);
        Assert.Contains("now off", result);
        Assert.False(SmartThermostat.RoomThermostatStates[Room]);
    }

    [Fact]
    public async Task SetPower_AlreadyOn_ReportsAlreadyOn()
    {
        await SmartThermostat.SetPower(Room, true);
        var result = await SmartThermostat.SetPower(Room, true);
        Assert.Contains("already on", result);
    }

    [Fact]
    public async Task SetPower_AlreadyOff_ReportsAlreadyOff()
    {
        var result = await SmartThermostat.SetPower(Room, false);
        Assert.Contains("already off", result);
    }

    [Fact]
    public async Task SetPower_CaseInsensitiveRoomNames()
    {
        var result = await SmartThermostat.SetPower("kitchen", true);
        Assert.Contains("now on", result);
        Assert.True(SmartThermostat.RoomThermostatStates[Room]);
        var status = await SmartThermostat.GetStatus("KITCHEN");
        Assert.Contains("Thermostat is On", status);
    }

    [Fact]
    public void Dispose_CleansUpTimer()
    {
        SmartThermostat.Dispose();
        // After Dispose, timer should be null and isDisposed true (cannot check isDisposed directly)
        // But we can call Dispose again and ensure no exception is thrown
        SmartThermostat.Dispose();
    }
}