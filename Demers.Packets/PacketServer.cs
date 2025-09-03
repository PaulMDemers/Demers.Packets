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
        private readonly TcpListener _listener;

        public PacketServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }

        public PacketClient Accept()
        {
            if (_listener.Pending())
            {
                return new PacketClient(_listener.AcceptTcpClient());
            }

            return null;
        }
    }
}
