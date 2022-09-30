using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Demers.Packets
{
    public class PacketServer
    {
        TcpListener listener;

        public PacketServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
        }

        public PacketClient Accept()
        {
            if (listener.Pending())
            {
                return new PacketClient(listener.AcceptTcpClient());
            }

            return null;
        }
    }
}
