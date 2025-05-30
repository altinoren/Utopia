using Utopia.HomeAutomation;
using System.Reflection;

namespace UtopiaTest.HomeAutomation;

public class AirQualityControlTests
{
    private const string Room = "Kitchen";
    private const string InvalidRoom = "NonExistentRoom";

    public AirQualityControlTests()
    {
        // Reset static state before each test
        foreach (var key in AirQualityControl.RoomAirQuality.Keys.ToList())
        {
            AirQualityControl.RoomAirQuality[key] = AirQuality.Good;
            AirQualityControl.RoomModes[key] = OperationMode.Normal;
            AirQualityControl.RoomPowerStates[key] = false;
        }
    }

    [Fact]
    public async Task GetStatus_ValidRoom_ReturnsCorrectStatus()
    {
        var result = await AirQualityControl.GetStatus(Room);
        Assert.Contains($"Air quality in {Room} is Good", result);
        Assert.Contains("Unit is Off", result);
        Assert.Contains("Normal mode", result);
    }

    [Fact]
    public async Task GetStatus_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await AirQualityControl.GetStatus(InvalidRoom);
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public async Task SetPower_TurnsOnAndOff()
    {
        var result1 = await AirQualityControl.SetPower(Room, true);
        Assert.Contains("is now on", result1);
        var status1 = await AirQualityControl.GetStatus(Room);
        Assert.Contains("Unit is On", status1);

        var result2 = await AirQualityControl.SetPower(Room, false);
        Assert.Contains("is now off", result2);
        var status2 = await AirQualityControl.GetStatus(Room);
        Assert.Contains("Unit is Off", status2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetPower_AlreadyInState_ReturnsAlreadyMessage(bool state)
    {
        await AirQualityControl.SetPower(Room, state);
        var result = await AirQualityControl.SetPower(Room, state);
        Assert.Contains($"already {(state ? "on" : "off")}", result);
    }

    [Theory]
    [InlineData(OperationMode.Quiet)]
    public async Task SetMode_ChangesMode(OperationMode mode)
    {
        var result = await AirQualityControl.SetMode(Room, mode);
        Assert.Contains($"now in {mode} mode", result);
        var status = await AirQualityControl.GetStatus(Room);
        Assert.Contains($"in {mode} mode", status);
    }

    [Theory]
    [InlineData(OperationMode.Normal)]
    [InlineData(OperationMode.Quiet)]
    public async Task SetMode_AlreadyInMode_ReturnsAlreadyMessage(OperationMode mode)
    {
        await AirQualityControl.SetMode(Room, mode);
        var result = await AirQualityControl.SetMode(Room, mode);
        Assert.Contains($"already in {mode} mode", result);
    }

    [Theory]
    [InlineData(AirQuality.Moderate)]
    [InlineData(AirQuality.VeryUnhealthy)]
    public async Task SimulateDegradation_DegradesAirQuality(AirQuality targetQuality)
    {
        var result = await AirQualityControl.SimulateDegradation(Room, targetQuality);
        Assert.Contains($"is now {targetQuality}", result);
        var status = await AirQualityControl.GetStatus(Room);
        Assert.Contains($"is {targetQuality}", status);
    }

    [Fact]
    public async Task SimulateDegradation_CannotDegradeToGood()
    {
        var result = await AirQualityControl.SimulateDegradation(Room, AirQuality.Good);
        Assert.Equal("Cannot degrade to Good quality", result);
    }

    [Fact]
    public async Task SetPower_CaseInsensitiveRoomNames()
    {
        var result = await AirQualityControl.SetPower("kitchen", true);
        Assert.Contains("is now on", result);
        var status = await AirQualityControl.GetStatus("KITCHEN");
        Assert.Contains("Unit is On", status);
    }
}