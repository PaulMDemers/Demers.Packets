namespace Demers.Packets.Examples.RoomSample;

internal enum RoomOpcode
{
    JoinRoom = 1,
    Welcome = 2,
    PlayerJoined = 3,
    Move = 4,
    PlayerMoved = 5,
    PlayerLeft = 6,
    ServerMessage = 7
}

internal static class RoomSampleDefaults
{
    public const int DefaultPort = 8890;
    public const int NetworkPollDelayMs = 10;
}
