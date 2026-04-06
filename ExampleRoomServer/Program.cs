using Demers.Packets;
using Demers.Packets.Examples.RoomSample;

var port = ParseIntArg(args, "--port", RoomSampleDefaults.DefaultPort);

Console.WriteLine($"Room server listening on port {port}.");
Console.WriteLine("Clients join at (0, 0). Use Q to quit.");

var server = new PacketServer(port);
var trackedClients = new Dictionary<PacketClient, ConnectedPlayer>();
var nextPlayerId = 1;
var quit = false;

while (!quit)
{
    var newClient = server.Accept();
    if (newClient != null)
    {
        var trackedPlayer = new ConnectedPlayer(newClient);
        trackedClients.Add(newClient, trackedPlayer);
        newClient.OnReceive += (packet, client) => HandlePacket(packet, trackedPlayer);
        Console.WriteLine("Accepted a new TCP client.");
    }

    var disconnectedClients = new List<ConnectedPlayer>();

    foreach (var trackedPlayer in trackedClients.Values.ToList())
    {
        if (!await trackedPlayer.Client.ReadAsync())
        {
            disconnectedClients.Add(trackedPlayer);
        }
    }

    foreach (var disconnectedClient in disconnectedClients)
    {
        RemoveClient(disconnectedClient, "disconnected");
    }

    if (!Console.IsInputRedirected && Console.KeyAvailable)
    {
        var keyInfo = Console.ReadKey(true);
        if (keyInfo.Key == ConsoleKey.Q)
        {
            quit = true;
        }
    }

    await Task.Delay(RoomSampleDefaults.NetworkPollDelayMs);
}

Console.WriteLine("Shutting down room server.");

void HandlePacket(Packet packet, ConnectedPlayer trackedPlayer)
{
    var reader = new PacketReader(packet);

    switch ((RoomOpcode)packet.Opcode)
    {
        case RoomOpcode.JoinRoom:
            HandleJoinRoom(reader, trackedPlayer);
            break;
        case RoomOpcode.Move:
            HandleMove(reader, trackedPlayer);
            break;
        default:
            SendServerMessage(trackedPlayer.Client, $"Unhandled opcode {packet.Opcode}.");
            break;
    }
}

void HandleJoinRoom(PacketReader reader, ConnectedPlayer trackedPlayer)
{
    if (trackedPlayer.Joined)
    {
        SendServerMessage(trackedPlayer.Client, "You already joined the room.");
        return;
    }

    var requestedName = reader.ReadString().Trim();
    trackedPlayer.PlayerId = nextPlayerId++;
    trackedPlayer.Name = string.IsNullOrWhiteSpace(requestedName)
        ? $"Player{trackedPlayer.PlayerId}"
        : requestedName;
    trackedPlayer.X = 0;
    trackedPlayer.Y = 0;
    trackedPlayer.Joined = true;

    SendWelcome(trackedPlayer);
    BroadcastPlayerJoined(trackedPlayer, except: trackedPlayer.Client);
    BroadcastServerMessage($"{trackedPlayer.Name} entered the room at (0, 0).");

    Console.WriteLine($"Player {trackedPlayer.PlayerId} joined as '{trackedPlayer.Name}'.");
}

void HandleMove(PacketReader reader, ConnectedPlayer trackedPlayer)
{
    if (!trackedPlayer.Joined)
    {
        SendServerMessage(trackedPlayer.Client, "Join the room before moving.");
        return;
    }

    var dx = ClampStep(reader.ReadInt());
    var dy = ClampStep(reader.ReadInt());

    if (dx == 0 && dy == 0)
    {
        SendServerMessage(trackedPlayer.Client, "Move ignored because dx and dy were both zero.");
        return;
    }

    trackedPlayer.X += dx;
    trackedPlayer.Y += dy;

    BroadcastPlayerMoved(trackedPlayer);
    Console.WriteLine(
        $"Player {trackedPlayer.PlayerId} '{trackedPlayer.Name}' moved to ({trackedPlayer.X}, {trackedPlayer.Y})."
    );
}

void SendWelcome(ConnectedPlayer trackedPlayer)
{
    var joinedPlayers = trackedClients.Values
        .Where(player => player.Joined)
        .OrderBy(player => player.PlayerId)
        .ToList();

    var writer = new PacketWriter();
    writer.NewPacket((int)RoomOpcode.Welcome);
    writer.WriteInt(trackedPlayer.PlayerId);
    writer.WriteInt(joinedPlayers.Count);

    foreach (var player in joinedPlayers)
    {
        writer.WriteInt(player.PlayerId);
        writer.WriteString(player.Name);
        writer.WriteInt(player.X);
        writer.WriteInt(player.Y);
    }

    trackedPlayer.Client.Write(writer.GetPacket());
}

void BroadcastPlayerJoined(ConnectedPlayer trackedPlayer, PacketClient? except = null)
{
    foreach (var target in trackedClients.Values.Where(player => player.Joined && player.Client != except).ToList())
    {
        var writer = new PacketWriter();
        writer.NewPacket((int)RoomOpcode.PlayerJoined);
        writer.WriteInt(trackedPlayer.PlayerId);
        writer.WriteString(trackedPlayer.Name);
        writer.WriteInt(trackedPlayer.X);
        writer.WriteInt(trackedPlayer.Y);
        target.Client.Write(writer.GetPacket());
    }
}

void BroadcastPlayerMoved(ConnectedPlayer trackedPlayer)
{
    foreach (var target in trackedClients.Values.Where(player => player.Joined).ToList())
    {
        var writer = new PacketWriter();
        writer.NewPacket((int)RoomOpcode.PlayerMoved);
        writer.WriteInt(trackedPlayer.PlayerId);
        writer.WriteInt(trackedPlayer.X);
        writer.WriteInt(trackedPlayer.Y);
        target.Client.Write(writer.GetPacket());
    }
}

void BroadcastPlayerLeft(ConnectedPlayer trackedPlayer)
{
    foreach (var target in trackedClients.Values.Where(player => player.Joined && player.Client != trackedPlayer.Client).ToList())
    {
        var writer = new PacketWriter();
        writer.NewPacket((int)RoomOpcode.PlayerLeft);
        writer.WriteInt(trackedPlayer.PlayerId);
        target.Client.Write(writer.GetPacket());
    }
}

void BroadcastServerMessage(string message)
{
    foreach (var target in trackedClients.Values.Where(player => player.Joined).ToList())
    {
        SendServerMessage(target.Client, message);
    }
}

void SendServerMessage(PacketClient target, string message)
{
    var writer = new PacketWriter();
    writer.NewPacket((int)RoomOpcode.ServerMessage);
    writer.WriteString(message);
    target.Write(writer.GetPacket());
}

void RemoveClient(ConnectedPlayer trackedPlayer, string reason)
{
    if (!trackedClients.Remove(trackedPlayer.Client))
    {
        return;
    }

    if (trackedPlayer.Joined)
    {
        BroadcastPlayerLeft(trackedPlayer);
        BroadcastServerMessage($"{trackedPlayer.Name} left the room.");
        Console.WriteLine(
            $"Player {trackedPlayer.PlayerId} '{trackedPlayer.Name}' left because they {reason}."
        );
    }
    else
    {
        Console.WriteLine($"A TCP client {reason} before joining the room.");
    }

    trackedPlayer.Client.Disconnect();
}

static int ParseIntArg(string[] args, string key, int defaultValue)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(args[i + 1], out var parsed))
        {
            return parsed;
        }
    }

    return defaultValue;
}

static int ClampStep(int value)
{
    return Math.Clamp(value, -1, 1);
}

sealed class ConnectedPlayer
{
    public ConnectedPlayer(PacketClient client)
    {
        Client = client;
        Name = string.Empty;
    }

    public PacketClient Client { get; }
    public int PlayerId { get; set; }
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Joined { get; set; }
}
