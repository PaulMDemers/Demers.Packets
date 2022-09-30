using System;
using System.Net.Sockets;

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
        protected TcpClient client;
        protected NetworkStream st = null;
        protected Packet nextPacket = null;

        public ReceivedPacket OnReceive = null;

        public PacketClient(TcpClient c)
        {
            client = c;
            st = client.GetStream();
        }

        public PacketClient(string hostname, int port)
        {
            client = new TcpClient(hostname, port);
            st = client.GetStream();
        }

        public bool Read()
        {
            if (nextPacket == null)
            {
                if (client.Available >= 8)
                {
                    byte[] buffer = new byte[8];
                    st.Read(buffer, 0, 8);

                    int length = BitConverter.ToInt32(buffer, 4);
                    int opcode = BitConverter.ToInt32(buffer, 0);

                    nextPacket = new Packet(opcode, length);
                }
            }
            else
            {
                if (client.Available >= nextPacket.Length)
                {
                    st.Read(nextPacket.Data, 0, nextPacket.Length);

                    PacketReceived();
                    nextPacket = null;
                }
            }

            if (!client.Client.IsConnected())
                return false;

            return true;
        }

        public bool Write(Packet sendPacket)
        {
            if (client.SendBufferSize < sendPacket.Length + 8)
                client.SendBufferSize = sendPacket.Length + 50;

            byte[] buffer = new byte[sendPacket.Length + 8];

            Buffer.BlockCopy(BitConverter.GetBytes(sendPacket.Opcode), 0, buffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(sendPacket.Length), 0, buffer, 4, 4);
            Buffer.BlockCopy(sendPacket.Data, 0, buffer, 8, sendPacket.Length);

            st.Write(buffer, 0, buffer.Length);

            if (!client.Client.IsConnected())
                return false;

            return true;
        }

        public virtual void PacketReceived()
        {
            if (OnReceive != null)
                OnReceive(nextPacket, this);
        }

        public void Disconnect()
        {
            client.Close();
        }
    }
}
