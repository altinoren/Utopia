using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class SmartBlindsTests
{
    private const string Room = "Kitchen";
    private const string InvalidRoom = "NonExistentRoom";

    public SmartBlindsTests()
    {
        // Reset static state before each test
        foreach (var key in SmartBlinds.RoomBlinds.Keys.ToList())
        {
            SmartBlinds.RoomBlinds[key] = 100;
            if (SmartBlinds.RoomTimers.TryGetValue(key, out var timer))
            {
                timer.Dispose();
                SmartBlinds.RoomTimers.Remove(key);
            }
            if (SmartBlinds.RoomTarget.ContainsKey(key))
                SmartBlinds.RoomTarget.Remove(key);
        }
    }

    [Fact]
    public async Task GetState_ValidRoom_ReturnsCorrectStatus()
    {
        SmartBlinds.RoomBlinds[Room] = 70;
        var result = await SmartBlinds.GetState(Room);
        Assert.Equal($"Blinds in {Room} are 70% open.", result);
    }

    [Fact]
    public async Task GetState_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await SmartBlinds.GetState(InvalidRoom);
        Assert.Equal("Room not found", result);
    }

    [Theory]
    [InlineData(85, 80)]
    [InlineData(0, 0)]
    [InlineData(101, 100)]
    [InlineData(-5, 0)]
    public async Task SetState_ClampsAndRoundsToNearest10(int input, int expected)
    {
        var result = await SmartBlinds.SetState(Room, input);
        Assert.Equal($"Blinds in {Room} set to {expected}% open.", result);
        Assert.Equal(expected, SmartBlinds.RoomBlinds[Room]);
    }

    [Fact]
    public async Task SetState_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await SmartBlinds.SetState(InvalidRoom, 50);
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public async Task SetStateWithTimer_ImmediateIfNotEnoughTime()
    {
        var now = DateTime.Now;
        var result = await SmartBlinds.SetStateWithTimer(Room, 0, now.AddMinutes(1));
        Assert.Contains("immediately", result);
        Assert.Equal(0, SmartBlinds.RoomBlinds[Room]);
    }

    [Fact]
    public async Task SetStateWithTimer_InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await SmartBlinds.SetStateWithTimer(InvalidRoom, 50, DateTime.Now.AddMinutes(10));
        Assert.Equal("Room not found", result);
    }

    [Fact]
    public void Dispose_ClearsTimersAndTargets()
    {
        // Add a timer and target
        SmartBlinds.RoomTimers[Room] = new Timer(_ => { });
        SmartBlinds.RoomTarget[Room] = 50;
        SmartBlinds.Dispose();
        Assert.Empty(SmartBlinds.RoomTimers);
        Assert.Empty(SmartBlinds.RoomTarget);
    }
}