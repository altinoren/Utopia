using Utopia.HomeAutomation;

namespace UtopiaTest.HomeAutomation;

public class MultiRoomAudioTests
{
    private static readonly string Room1 = "Kitchen";
    private static readonly string Room2 = "Bedroom";
    private static readonly string InvalidRoom = "NonExistentRoom";

    public MultiRoomAudioTests()
    {
        // Reset state before each test
        var dict = MultiRoomAudio.RoomAudio;
        foreach (var key in dict.Keys)
        {
            dict[key].State = AudioState.Stopped;
            dict[key].SourceType = null;
            dict[key].SourceName = string.Empty;
            dict[key].Volume = 50;
        }
    }

    [Fact]
    public async Task PlaySong_SingleRoom_StatusShowsSongRepeat()
    {
        var result = await MultiRoomAudio.PlaySong("SongA", new[] { Room1 });
        Assert.Contains("Playing song 'SongA" , result);
        var status = await MultiRoomAudio.GetStatus(Room1);
        Assert.Contains("Playing song 'SongA" , status);
        Assert.Contains("repeat", status);
    }

    [Fact]
    public async Task PlaySong_MultipleRooms_StatusCorrect()
    {
        var result = await MultiRoomAudio.PlaySong("SongB", new[] { Room1, Room2 });
        Assert.Contains(Room1, result);
        Assert.Contains(Room2, result);
        var status1 = await MultiRoomAudio.GetStatus(Room1);
        var status2 = await MultiRoomAudio.GetStatus(Room2);
        Assert.Contains("SongB", status1);
        Assert.Contains("SongB", status2);
    }

    [Fact]
    public async Task PlayPlaylist_SingleRoom_StatusShowsPlaylist()
    {
        var result = await MultiRoomAudio.PlayPlaylist("ChillBeats", new[] { Room1 });
        Assert.Contains("Playing playlist 'ChillBeats" , result);
        var status = await MultiRoomAudio.GetStatus(Room1);
        Assert.Contains("Playing playlist 'ChillBeats" , status);
        Assert.DoesNotContain("repeat", status);
    }

    [Fact]
    public async Task PlayPlaylist_MultipleRooms_StatusCorrect()
    {
        var result = await MultiRoomAudio.PlayPlaylist("PartyMix", new[] { Room1, Room2 });
        Assert.Contains(Room1, result);
        Assert.Contains(Room2, result);
        var status1 = await MultiRoomAudio.GetStatus(Room1);
        var status2 = await MultiRoomAudio.GetStatus(Room2);
        Assert.Contains("PartyMix", status1);
        Assert.Contains("PartyMix", status2);
    }

    [Fact]
    public async Task SetVolume_ClampsAndSets()
    {
        var result = await MultiRoomAudio.SetVolume(Room1, 120);
        Assert.Contains("set to 100", result);
        var status = await MultiRoomAudio.GetStatus(Room1);
        Assert.Contains("Volume: 100", status);
        await MultiRoomAudio.SetVolume(Room1, -10);
        status = await MultiRoomAudio.GetStatus(Room1);
        Assert.Contains("Volume: 0", status);
    }

    [Fact]
    public async Task Stop_StopsAudioInRooms()
    {
        await MultiRoomAudio.PlaySong("SongC", new[] { Room1, Room2 });
        var result = await MultiRoomAudio.Stop(new[] { Room1 });
        Assert.Contains(Room1, result);
        var status1 = await MultiRoomAudio.GetStatus(Room1);
        var status2 = await MultiRoomAudio.GetStatus(Room2);
        Assert.Contains("stopped", status1);
        Assert.Contains("Playing song", status2);
    }

    [Fact]
    public async Task GetStatus_StoppedState()
    {
        var status = await MultiRoomAudio.GetStatus(Room1);
        Assert.Contains("stopped", status);
    }

    [Fact]
    public async Task InvalidRoom_ReturnsRoomNotFound()
    {
        var result = await MultiRoomAudio.PlaySong("SongD", new[] { InvalidRoom });
        Assert.Contains("Rooms not found", result);
        var status = await MultiRoomAudio.GetStatus(InvalidRoom);
        Assert.Equal("Room not found", status);
        var volResult = await MultiRoomAudio.SetVolume(InvalidRoom, 50);
        Assert.Equal("Room not found", volResult);
    }
}