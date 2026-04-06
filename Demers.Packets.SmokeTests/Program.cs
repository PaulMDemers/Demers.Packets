using System.Net;
using System.Net.Sockets;
using System.Text;
using Demers.Packets;

var tests = new (string Name, Func<Task> Run)[]
{
    ("ExactLengthWithoutLegacyByte", ExactLengthWithoutLegacyByte),
    ("LegacyByteIsOptIn", LegacyByteIsOptIn),
    ("Utf8StringRoundTripMaintainsOffsets", Utf8StringRoundTripMaintainsOffsets),
    ("LoopbackClientServerExchange", LoopbackClientServerExchange),
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Smoke tests failed:");

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($" - {failure}");
    }

    return 1;
}

Console.WriteLine("All smoke tests passed.");
return 0;

static Task ExactLengthWithoutLegacyByte()
{
    var writer = new PacketWriter();
    writer.NewPacket(7);
    writer.WriteInt(123456);
    writer.WriteBool(true);

    var packet = writer.GetPacket();
    AssertEqual(5, packet.Length, "packet length should exactly match payload bytes");

    var reader = new PacketReader(packet);
    AssertEqual(123456, reader.ReadInt(), "int payload should round-trip");
    AssertEqual(true, reader.ReadBool(), "bool payload should round-trip");

    return Task.CompletedTask;
}

static Task LegacyByteIsOptIn()
{
    var writer = new PacketWriter(addLegacyByte: true);
    writer.NewPacket(8);
    writer.WriteInt(42);

    var packet = writer.GetPacket();
    AssertEqual(5, packet.Length, "legacy mode should keep a single trailing byte");
    AssertEqual(0, packet.Data[^1], "legacy byte should be zero-filled");

    var reader = new PacketReader(new Packet(packet.Opcode, 4) { Data = packet.Data[..4] });
    AssertEqual(42, reader.ReadInt(), "legacy compatibility packet should keep original payload bytes");

    return Task.CompletedTask;
}

static Task Utf8StringRoundTripMaintainsOffsets()
{
    const string message = "hé🚀";
    var expectedStringBytes = Encoding.UTF8.GetByteCount(message);

    var writer = new PacketWriter();
    writer.NewPacket(9);
    writer.WriteString(message);
    writer.WriteInt(99);

    var packet = writer.GetPacket();
    AssertEqual(expectedStringBytes + 8, packet.Length, "string payload should use UTF-8 byte count, not character count");

    var reader = new PacketReader(packet);
    AssertEqual(message, reader.ReadString(), "UTF-8 string should round-trip");
    AssertEqual(99, reader.ReadInt(), "reader offset should advance by UTF-8 byte count");

    return Task.CompletedTask;
}

static async Task LoopbackClientServerExchange()
{
    var port = GetFreeTcpPort();
    var server = new PacketServer(port);
    var client = new PacketClient(IPAddress.Loopback.ToString(), port);

    PacketClient? acceptedClient = null;
    for (var attempt = 0; attempt < 100 && acceptedClient == null; attempt++)
    {
        acceptedClient = server.Accept();
        if (acceptedClient == null)
        {
            await Task.Delay(10);
        }
    }

    AssertNotNull(acceptedClient, "server did not accept the loopback client");

    Packet? receivedPacket = null;
    acceptedClient!.OnReceive += (packet, _) => receivedPacket = packet;

    const string payload = "Hello from smoke test 🚀";
    var writer = new PacketWriter();
    writer.NewPacket(10);
    writer.WriteString(payload);
    var outgoingPacket = writer.GetPacket();

    AssertEqual(Encoding.UTF8.GetByteCount(payload) + 4, outgoingPacket.Length, "header length should match exact UTF-8 payload bytes");
    AssertEqual(true, await client.WriteAsync(outgoingPacket), "client write should succeed");

    for (var attempt = 0; attempt < 100 && receivedPacket == null; attempt++)
    {
        AssertEqual(true, await acceptedClient.ReadAsync(), "server read should remain connected");
        if (receivedPacket == null)
        {
            await Task.Delay(10);
        }
    }

    AssertNotNull(receivedPacket, "server never received the outgoing packet");
    AssertEqual(outgoingPacket.Length, receivedPacket!.Length, "received packet should not have a trailing compatibility byte by default");

    var reader = new PacketReader(receivedPacket);
    AssertEqual(payload, reader.ReadString(), "loopback payload should round-trip through PacketClient/PacketServer");

    client.Disconnect();
    acceptedClient.Disconnect();
}

static int GetFreeTcpPort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}. Expected '{expected}', got '{actual}'.");
    }
}

static void AssertNotNull(object? value, string message)
{
    if (value == null)
    {
        throw new InvalidOperationException(message);
    }
}
