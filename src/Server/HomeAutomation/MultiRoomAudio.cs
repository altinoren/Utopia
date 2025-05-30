using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Utopia.HomeAutomation
{
    public enum AudioState { Stopped, Playing, Paused }
    public enum AudioSourceType { Song, Playlist }

    [McpServerToolType]
    public static class MultiRoomAudio
    {
        public class RoomAudioState
        {
            public AudioState State { get; set; } = AudioState.Stopped;
            public AudioSourceType? SourceType { get; set; }
            public string SourceName { get; set; } = null;
            public int Volume { get; set; } = 50; // 0-100
        }

        public static Dictionary<string, RoomAudioState> RoomAudio { get; private set; }
        private static readonly Lock audioLock = new();

        static MultiRoomAudio()
        {
            RoomAudio = Environment.RoomsWithArea.Keys.ToDictionary(
                r => r, r => new RoomAudioState(), StringComparer.OrdinalIgnoreCase);
        }

        [McpServerTool(Name = "audio_play_song", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Plays a song in one or more rooms. Song will repeat.")]
        public static Task<string> PlaySong(string song, string[] rooms)
        {
            lock (audioLock)
            {
                var notFound = rooms.Where(r => !RoomAudio.ContainsKey(r)).ToList();
                foreach (var room in rooms.Except(notFound))
                {
                    var state = RoomAudio[room];
                    state.State = AudioState.Playing;
                    state.SourceType = AudioSourceType.Song;
                    state.SourceName = song;
                }
                if (notFound.Count > 0)
                    return Task.FromResult($"Rooms not found: {string.Join(", ", notFound)}");
                return Task.FromResult($"Playing song '{song}' in rooms: {string.Join(", ", rooms.Except(notFound))} (repeat mode).");
            }
        }

        [McpServerTool(Name = "audio_play_playlist", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Plays a playlist in one or more rooms. Status will show playlist name only.")]
        public static Task<string> PlayPlaylist(string playlist, string[] rooms)
        {
            lock (audioLock)
            {
                var notFound = rooms.Where(r => !RoomAudio.ContainsKey(r)).ToList();
                foreach (var room in rooms.Except(notFound))
                {
                    var state = RoomAudio[room];
                    state.State = AudioState.Playing;
                    state.SourceType = AudioSourceType.Playlist;
                    state.SourceName = playlist;
                }
                if (notFound.Count > 0)
                    return Task.FromResult($"Rooms not found: {string.Join(", ", notFound)}");
                return Task.FromResult($"Playing playlist '{playlist}' in rooms: {string.Join(", ", rooms.Except(notFound))}.");
            }
        }

        [McpServerTool(Name = "audio_stop", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Stops audio in one or more rooms.")]
        public static Task<string> Stop(string[] rooms)
        {
            lock (audioLock)
            {
                var notFound = rooms.Where(r => !RoomAudio.ContainsKey(r)).ToList();
                foreach (var room in rooms.Except(notFound))
                {
                    var state = RoomAudio[room];
                    state.State = AudioState.Stopped;
                    state.SourceType = null;
                    state.SourceName = null;
                }
                if (notFound.Count > 0)
                    return Task.FromResult($"Rooms not found: {string.Join(", ", notFound)}");
                return Task.FromResult($"Stopped audio in rooms: {string.Join(", ", rooms.Except(notFound))}.");
            }
        }

        [McpServerTool(Name = "audio_set_volume", Destructive = false, OpenWorld = false, ReadOnly = false, Idempotent = true),
            Description("Sets the volume in a room (0-100).")]
        public static Task<string> SetVolume(string room, int volume)
        {
            lock (audioLock)
            {
                if (!RoomAudio.ContainsKey(room))
                    return Task.FromResult("Room not found");
                volume = Math.Clamp(volume, 0, 100);
                RoomAudio[room].Volume = volume;
                return Task.FromResult($"Volume in {room} set to {volume}.");
            }
        }

        [McpServerTool(Name = "audio_get_status", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the audio status in a room.")]
        public static Task<string> GetStatus(string room)
        {
            lock (audioLock)
            {
                if (!RoomAudio.ContainsKey(room))
                    return Task.FromResult("Room not found");
                var state = RoomAudio[room];
                if (state.State == AudioState.Stopped)
                    return Task.FromResult($"Audio is stopped in {room}. Volume: {state.Volume}");
                if (state.SourceType == AudioSourceType.Song)
                    return Task.FromResult($"Playing song '{state.SourceName}' in {room} (repeat). Volume: {state.Volume}");
                if (state.SourceType == AudioSourceType.Playlist)
                    return Task.FromResult($"Playing playlist '{state.SourceName}' in {room}. Volume: {state.Volume}");
                return Task.FromResult($"Audio state unknown in {room}.");
            }
        }
    }
}
