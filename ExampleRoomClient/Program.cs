using System.Collections.Concurrent;
using Demers.Packets;
using Demers.Packets.Examples.RoomSample;

var host = ParseStringArg(args, "--host", "127.0.0.1");
var port = ParseIntArg(args, "--port", RoomSampleDefaults.DefaultPort);
var playerName = ParseStringArg(args, "--name", $"Player-{Guid.NewGuid():N}"[..8]);
var script = ParseStringArg(args, "--script", string.Empty);
var commandDelayMs = ParseIntArg(args, "--delay-ms", 250);

Console.WriteLine($"Connecting to room server at {host}:{port} as '{playerName}'.");

var client = new PacketClient(host, port);
var players = new ConcurrentDictionary<int, RoomPlayer>();
var clientRunning = true;
var localPlayerId = 0;

client.OnReceive += (packet, _) =>
{
    var reader = new PacketReader(packet);

    switch ((RoomOpcode)packet.Opcode)
    {
        case RoomOpcode.Welcome:
            HandleWelcome(reader);
            break;
        case RoomOpcode.PlayerJoined:
            HandlePlayerJoined(reader);
            break;
        case RoomOpcode.PlayerMoved:
            HandlePlayerMoved(reader);
            break;
        case RoomOpcode.PlayerLeft:
            HandlePlayerLeft(reader);
            break;
        case RoomOpcode.ServerMessage:
            Console.WriteLine($"[server] {reader.ReadString()}");
            break;
        default:
            Console.WriteLine($"Received unhandled opcode {packet.Opcode}.");
            break;
    }
};

SendJoin(playerName);

var networkLoop = Task.Run(async () =>
{
    while (clientRunning)
    {
        if (!await client.ReadAsync())
        {
            Console.WriteLine("Disconnected from room server.");
            clientRunning = false;
            break;
        }

        await Task.Delay(RoomSampleDefaults.NetworkPollDelayMs);
    }
});

if (!string.IsNullOrWhiteSpace(script))
{
    await RunScriptAsync(script);
}
else
{
    PrintHelp();

    while (clientRunning)
    {
        var line = Console.ReadLine();
        if (line == null)
        {
            break;
        }

        await ExecuteCommandAsync(line);
    }
}

clientRunning = false;
client.Disconnect();
await networkLoop;

void HandleWelcome(PacketReader reader)
{
    localPlayerId = reader.ReadInt();
    var playerCount = reader.ReadInt();
    players.Clear();

    for (var i = 0; i < playerCount; i++)
    {
        var player = ReadPlayer(reader);
        players[player.PlayerId] = player;
    }

    Console.WriteLine($"Joined the room as player #{localPlayerId}.");
    PrintPlayers();
}

void HandlePlayerJoined(PacketReader reader)
{
    var player = ReadPlayer(reader);
    players[player.PlayerId] = player;
    Console.WriteLine($"[room] {player.Name} joined at ({player.X}, {player.Y}).");
}

void HandlePlayerMoved(PacketReader reader)
{
    var playerId = reader.ReadInt();
    var x = reader.ReadInt();
    var y = reader.ReadInt();

    if (players.TryGetValue(playerId, out var existingPlayer))
    {
        existingPlayer = existingPlayer with { X = x, Y = y };
        players[playerId] = existingPlayer;

        var marker = playerId == localPlayerId ? "you" : existingPlayer.Name;
        Console.WriteLine($"[move] {marker} now at ({x}, {y}).");
    }
    else
    {
        Console.WriteLine($"[move] Player #{playerId} now at ({x}, {y}).");
    }
}

void HandlePlayerLeft(PacketReader reader)
{
    var playerId = reader.ReadInt();

    if (players.TryRemove(playerId, out var player))
    {
        Console.WriteLine($"[room] {player.Name} left the room.");
    }
    else
    {
        Console.WriteLine($"[room] Player #{playerId} left the room.");
    }
}

async Task RunScriptAsync(string scriptText)
{
    var commands = scriptText
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var command in commands)
    {
        if (!clientRunning)
        {
            break;
        }

        Console.WriteLine($"> {command}");
        await ExecuteCommandAsync(command);

        if (clientRunning)
        {
            await Task.Delay(commandDelayMs);
        }
    }
}

async Task ExecuteCommandAsync(string input)
{
    var trimmed = input.Trim();
    if (string.IsNullOrWhiteSpace(trimmed))
    {
        return;
    }

    var lower = trimmed.ToLowerInvariant();

    switch (lower)
    {
        case "w":
        case "up":
            SendMove(0, -1);
            return;
        case "s":
        case "down":
            SendMove(0, 1);
            return;
        case "a":
        case "left":
            SendMove(-1, 0);
            return;
        case "d":
        case "right":
            SendMove(1, 0);
            return;
        case "look":
        case "list":
            PrintPlayers();
            return;
        case "where":
            PrintLocalPlayer();
            return;
        case "help":
            PrintHelp();
            return;
        case "quit":
        case "exit":
            clientRunning = false;
            return;
    }

    if (lower.StartsWith("move ", StringComparison.Ordinal))
    {
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 3
            && int.TryParse(parts[1], out var dx)
            && int.TryParse(parts[2], out var dy))
        {
            SendMove(dx, dy);
            return;
        }

        Console.WriteLine("Usage: move <dx> <dy>");
        return;
    }

    if (lower.StartsWith("wait ", StringComparison.Ordinal))
    {
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[1], out var waitMs))
        {
            await Task.Delay(waitMs);
            return;
        }

        Console.WriteLine("Usage: wait <milliseconds>");
        return;
    }

    Console.WriteLine($"Unknown command '{trimmed}'. Type 'help' for commands.");
}

void SendJoin(string name)
{
    var writer = new PacketWriter();
    writer.NewPacket((int)RoomOpcode.JoinRoom);
    writer.WriteString(name);
    client.Write(writer.GetPacket());
}

void SendMove(int dx, int dy)
{
    var writer = new PacketWriter();
    writer.NewPacket((int)RoomOpcode.Move);
    writer.WriteInt(dx);
    writer.WriteInt(dy);
    client.Write(writer.GetPacket());
}

RoomPlayer ReadPlayer(PacketReader reader)
{
    return new RoomPlayer(
        reader.ReadInt(),
        reader.ReadString(),
        reader.ReadInt(),
        reader.ReadInt()
    );
}

void PrintPlayers()
{
    Console.WriteLine("Room occupants:");

    foreach (var player in players.Values.OrderBy(player => player.PlayerId))
    {
        var marker = player.PlayerId == localPlayerId ? " (you)" : string.Empty;
        Console.WriteLine($" - #{player.PlayerId} {player.Name} at ({player.X}, {player.Y}){marker}");
    }
}

void PrintLocalPlayer()
{
    if (localPlayerId != 0 && players.TryGetValue(localPlayerId, out var player))
    {
        Console.WriteLine($"You are at ({player.X}, {player.Y}).");
        return;
    }

    Console.WriteLine("Your player state has not arrived yet.");
}

void PrintHelp()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  w/a/s/d or up/left/down/right - move by one tile");
    Console.WriteLine("  move <dx> <dy>                - move with explicit delta, clamped to -1..1");
    Console.WriteLine("  look                          - print the players currently in the room");
    Console.WriteLine("  where                         - print your current coordinates");
    Console.WriteLine("  wait <ms>                     - script helper to pause before the next command");
    Console.WriteLine("  quit                          - leave the sample client");
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

static string ParseStringArg(string[] args, string key, string defaultValue)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return defaultValue;
}

internal record RoomPlayer(int PlayerId, string Name, int X, int Y);
