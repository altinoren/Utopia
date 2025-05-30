using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class LightingTests
{
    public LightingTests()
    {
        // Reset all lights to off before each test
        foreach (var room in Lighting.LightsStatus.Keys.ToList())
        {
            Lighting.LightsStatus[room] = false;
        }
    }

    [Fact]
    public async Task GetStatus_ValidRoom_ReturnsCorrectStatus()
    {
        // Arrange
        const string room = "Kitchen";
        Lighting.LightsStatus[room] = true;

        // Act
        var result = await Lighting.GetStatus(room);

        // Assert
        Assert.Equal("On", result);
    }

    [Fact]
    public async Task GetStatus_InvalidRoom_ReturnsRoomNotFound()
    {
        // Arrange
        const string room = "NonExistentRoom";

        // Act
        var result = await Lighting.GetStatus(room);

        // Assert
        Assert.Equal("Room not found", result);
    }

    [Theory]
    [InlineData("Kitchen", true)]
    [InlineData("Bedroom", false)]
    [InlineData("Living Room", true)]
    public async Task SetStatus_ValidRoom_ChangesStatusAndReturnsCorrectMessage(string room, bool newStatus)
    {
        // Arrange
        Lighting.LightsStatus[room] = !newStatus; // Set to opposite of what we're going to change to

        // Act
        var result = await Lighting.SetStatus(room, newStatus);

        // Assert
        Assert.Equal($"Lights in {room} are now {(newStatus ? "On" : "Off")}.", result);
        Assert.Equal(newStatus, Lighting.LightsStatus[room]);
    }

    [Fact]
    public async Task SetStatus_SameStatus_ReturnsAlreadyInStateMessage()
    {
        // Arrange
        const string room = "Kitchen";
        const bool status = true;
        Lighting.LightsStatus[room] = status;

        // Act
        var result = await Lighting.SetStatus(room, status);

        // Assert
        Assert.Equal($"Lights in {room} are already {(status ? "On" : "Off")}.", result);
        Assert.Equal(status, Lighting.LightsStatus[room]);
    }

    [Fact]
    public async Task SetStatus_InvalidRoom_ReturnsRoomNotFound()
    {
        // Arrange
        const string room = "NonExistentRoom";

        // Act
        var result = await Lighting.SetStatus(room, true);

        // Assert
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public async Task AllRooms_HaveInitiallyOffStatus()
    {
        // Arrange - Constructor resets all lights to off

        // Act & Assert
        foreach (var room in Lighting.LightsStatus.Keys)
        {
            var status = await Lighting.GetStatus(room);
            Assert.Equal("Off", status);
        }
    }

    [Fact]
    public async Task SetStatus_CaseInsensitiveRoomNames()
    {
        // Arrange
        const string roomLower = "kitchen";
        const string roomProper = "Kitchen";

        // Act
        var result = await Lighting.SetStatus(roomLower, true);

        // Assert
        Assert.Equal($"Lights in {roomLower} are now On.", result);
        Assert.True(Lighting.LightsStatus[roomProper]);
        var status = await Lighting.GetStatus(roomLower);
        Assert.Equal("On", status);
    }
}