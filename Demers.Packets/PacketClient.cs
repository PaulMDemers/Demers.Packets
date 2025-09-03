using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Demers.Packets
{
    static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
    }

    public delegate void ReceivedPacket(Packet packet, PacketClient client);

    public class PacketClient
    {
        private TcpClient _client;
        private NetworkStream _st;
        private Packet _nextPacket;

        public ReceivedPacket OnReceive = null;

        public PacketClient(TcpClient c)
        {
            _client = c;
            _st = _client.GetStream();
        }

        public PacketClient(string hostname, int port)
        {
            _client = new TcpClient(hostname, port);
            _st = _client.GetStream();
        }

        public bool Read()
        {
            if (_nextPacket == null)
            {
                if (_client.Available >= 8)
                {
                    byte[] buffer = new byte[8];
                    int bytesRead = _st.Read(buffer, 0, 8);
                    
                    if (bytesRead != 8) //Bad read, DC client
                        return false;

                    int length = BitConverter.ToInt32(buffer, 4);
                    int opcode = BitConverter.ToInt32(buffer, 0);

                    _nextPacket = new Packet(opcode, length);
                }
            }
            else
            {
                if (_client.Available >= _nextPacket.Length)
                {
                    int bytesRead = _st.Read(_nextPacket.Data, 0, _nextPacket.Length);
                    
                    if (bytesRead != _nextPacket.Length) //Bad read, DC client
                        return false;
                    
                    PacketReceived();
                    _nextPacket = null;
                }
            }

            if (!_client.Client.IsConnected())
                return false;

            return true;
        }
        
        public async Task<bool> ReadAsync()
        {
            if (_nextPacket == null)
            {
                if (_client.Available >= 8)
                {
                    byte[] buffer = new byte[8];
                    int bytesRead = await _st.ReadAsync(buffer, 0, 8);

                    if (bytesRead != 8) //Bad read, DC client
                        return false;

                    int length = BitConverter.ToInt32(buffer, 4);
                    int opcode = BitConverter.ToInt32(buffer, 0);

                    _nextPacket = new Packet(opcode, length);
                }
            }
            else
            {
                if (_client.Available >= _nextPacket.Length)
                {
                    int bytesRead = await _st.ReadAsync(_nextPacket.Data, 0, _nextPacket.Length);

                    if (bytesRead != _nextPacket.Length) //Bad read, DC client
                        return false;
                    
                    PacketReceived();
                    _nextPacket = null;
                }
            }

            if (!_client.Client.IsConnected())
                return false;

            return true;
        }

        public bool Write(Packet sendPacket)
        {
            if (_client.SendBufferSize < sendPacket.Length + 8)
                _client.SendBufferSize = sendPacket.Length + 50;

            byte[] buffer = new byte[sendPacket.Length + 8];

            Buffer.BlockCopy(BitConverter.GetBytes(sendPacket.Opcode), 0, buffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(sendPacket.Length), 0, buffer, 4, 4);
            Buffer.BlockCopy(sendPacket.Data, 0, buffer, 8, sendPacket.Length);

            _st.Write(buffer, 0, buffer.Length);
            
            if (!_client.Client.IsConnected())
                return false;

            return true;
        }
        
        public async Task<bool> WriteAsync(Packet sendPacket)
        {
            if (_client.SendBufferSize < sendPacket.Length + 8)
                _client.SendBufferSize = sendPacket.Length + 50;

            byte[] buffer = new byte[sendPacket.Length + 8];

            Buffer.BlockCopy(BitConverter.GetBytes(sendPacket.Opcode), 0, buffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(sendPacket.Length), 0, buffer, 4, 4);
            Buffer.BlockCopy(sendPacket.Data, 0, buffer, 8, sendPacket.Length);

            await _st.WriteAsync(buffer, 0, buffer.Length);
            
            if (!_client.Client.IsConnected())
                return false;

            return true;
        }

        public virtual void PacketReceived()
        {
            if (OnReceive != null)
                OnReceive(_nextPacket, this);
        }

        public void Disconnect()
        {
            _client.Close();
        }
    }
}
