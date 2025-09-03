using Demers.Packets;

Console.WriteLine("Starting Server");
bool quit = false;

PacketServer server = new PacketServer(8888);
Dictionary<PacketClient, PacketClient> clients = new Dictionary<PacketClient, PacketClient>();

while (!quit)
{
    PacketClient newClient = CheckForNewClient();
    if (newClient != null)
        AddClientToMap(newClient);

    //CheckClients();
    await CheckClientsAsync();


    if (Console.KeyAvailable)
    {
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        
        if(keyInfo.Key == ConsoleKey.Q)
            quit = true;
    }
    
    await Task.Delay(10);
}

PacketClient CheckForNewClient()
{
    PacketClient newClient = server.Accept();

    if (newClient == null) return null;
    
    newClient.OnReceive += (packet, client) =>
    {
        PacketReader reader = new PacketReader(packet);
        Console.WriteLine($"Received packet (Opcode, Length, Data): {packet.Opcode}, {packet.Length}, " + reader.ReadString());
    };
    
    Console.WriteLine("New client connected");

    return newClient;
}

void AddClientToMap(PacketClient client)
{
    if(client != null)
        clients.Add(client, client);
}

void CheckClients()
{
    List<PacketClient> deadClients = new List<PacketClient>();
    
    foreach (PacketClient client in clients.Keys)
    {
        if(!client.Read())
            deadClients.Add(client);
    }

    foreach (PacketClient client in deadClients)
    {
        Console.WriteLine("Disconnected client removed");
        clients.Remove(client);
    }
}

async Task CheckClientsAsync()
{
    List<PacketClient> deadClients = new List<PacketClient>();
    
    foreach (PacketClient client in clients.Keys)
    {
        if(!(await client.ReadAsync()))
            deadClients.Add(client);
    }

    foreach (PacketClient client in deadClients)
    {
        Console.WriteLine("Disconnected client removed");
        clients.Remove(client);
    }
}