# Demers.packets
A packet based abstraction for TCP sockets, servers, and client.  
Nuget: [Demers.Packets](https://www.nuget.org/packages/Demers.Packets)

## Installation
The package can be installed by adding the Nuget package Demers.Packets

## Usage
Demers.Packets can be used in both a client and a server mode, it also provides packet readers and writers.

## Packet Structure
All packets sent by this library are structured as follows:
* Bytes 0 - 3 - Int, Opcode - Use the Opcode to determine the meaning of the packet in your application
* Bytes 4 - 7 - Int, Length - The length of the packet's data
* Bytes 8 - 8+Length - The data sent in the packet. Using the PacketWriter.WriteX methods the data will automatically be appended in order. User the PacketReader.ReadX methods in the same order to get the data back out.

### Example Packets
An example packet might look like:
* Structure:
* Opcode = 1 - Enum in our application will determine Opcode 1 = MessageWithLocation
* String - message,
* Float - Latitude,
* Float - Longitude

This would result in the writing of the packet like:
```csharp
PacketWriter writer = new PacketWriter();
writer.NewPacket(Opcodes.MessageWithLocation); // 1
writer.WriteString($"Hello from Connecticut");
writer.WriteFloat(41.846);
writer.WriteFloat(-72.454)
```
And it would be read like:
```csharp
PacketReader reader = new PacketReader(packet);
switch(packet.Opcode)
{
    case Opcodes.MessageWithLocation:
        Console.WriteLine(reader.ReadString());
        Console.Writeline($"{reader.ReadFloat()},{reader.ReadFloat()}");
        break;
    
    default: break;
}
```
## Server Usage
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Demers.Packets;

Console.WriteLine("Server listening on 8888. Press Q to quit.");

var server  = new PacketServer(8888);
var clients = new List<PacketClient>();

while (true)
{
    // Accept new clients (non-blocking)
    var newClient = server.Accept();
    if (newClient != null)
    {
        Console.WriteLine("Client connected");
        newClient.OnReceive += (packet, client) =>
        {
            var reader = new PacketReader(packet);
            Console.WriteLine(
                $"Opcode={packet.Opcode}, Len={packet.Length}, Data='{reader.ReadString()}'"
            );
        };
        clients.Add(newClient);
    }

    // Poll clients to process incoming packets; remove any that disconnect
    for (int i = clients.Count - 1; i >= 0; i--)
    {
        if (!await clients[i].ReadAsync())
        {
            Console.WriteLine("Client disconnected");
            clients.RemoveAt(i);
        }
    }

    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) break;
    await Task.Delay(10);
}
```
## Client Usage
```csharp
using System;
using System.Threading.Tasks;
using Demers.Packets;

Console.WriteLine("Client connecting to 127.0.0.1:8888. Press Q to quit.");

var client = new PacketClient("127.0.0.1", 8888);

// Optional: handle any server packets
client.OnReceive += (packet, thisClient) =>
{
    var reader = new PacketReader(packet);
    Console.WriteLine($"Opcode={packet.Opcode}"");
    Console.WriteLine($"Server says: '{reader.ReadString()}'");
};

var lastSend = DateTime.UtcNow;

while (true)
{
    // Process incoming data; exit if connection drops
    if (!await client.ReadAsync())
    {
        Console.WriteLine("Disconnected.");
        break;
    }

    // Send a simple packet every 2 seconds
    if (DateTime.UtcNow - lastSend >= TimeSpan.FromSeconds(2))
    {
        var writer = new PacketWriter();
        writer.NewPacket(1);    //Mock Opcode
        writer.WriteString("Hello from client!");
        await client.WriteAsync(writer.GetPacket());
        lastSend = DateTime.UtcNow;
        Console.WriteLine("Sent packet.");
    }

    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) break;
    await Task.Delay(10);
}
```