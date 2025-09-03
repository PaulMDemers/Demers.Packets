using Demers.Packets;

Console.WriteLine("Client Example Running");
Console.WriteLine("Example will spawn a new connection every 5 seconds and will randomly send packets to the server");

List<PacketClient> clients = new List<PacketClient>();
bool quit = false;
DateTime lastSpawnTime = DateTime.Now -  TimeSpan.FromSeconds(5);
Random r = new Random();

while (!quit)
{
    if (DateTime.Now - lastSpawnTime >= TimeSpan.FromSeconds(5))
    {
        PacketClient client = new PacketClient("127.0.0.1", 8888);
        clients.Add(client);
        lastSpawnTime = DateTime.Now;
        Console.WriteLine("Adding client");
    }

    List<PacketClient> deadClients = new List<PacketClient>();
    foreach (PacketClient client in clients)
    {
        if (!client.Read())
        {
            deadClients.Add(client);
            Console.WriteLine("Client DC'd");
        }
        else
        {
            //Randomly send packet
            if (r.Next(0, 1000) < 1)
            {
                Console.WriteLine("Sending Packet");
                PacketWriter writer = new PacketWriter();
                writer.NewPacket(1);
                writer.WriteString("Hello from client!");

                //client.Write(writer.GetPacket());
                await client.WriteAsync(writer.GetPacket());
            }
            //randomly close out connections
            else if (r.Next(0, 10000) < 25)
            {
                client.Disconnect();
                deadClients.Add(client);
            }
        }
    }
    
    foreach (PacketClient dc in deadClients)
        clients.Remove(dc);

    if (Console.KeyAvailable)
    {
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        if (keyInfo.Key == ConsoleKey.Q)
            quit = true;
    }

    await Task.Delay(5);
}