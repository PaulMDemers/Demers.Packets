using System;

namespace Demers.Packets
{
    public class PacketWriter
    {
        Packet packet = null;
        int currentOffset = 0;
        int endOffset = 0;

        public PacketWriter()
        {

        }

        public void WriteInt(int t)
        {
            if (packet.Length <= (endOffset + 4))
            {
                packet.Length = packet.Length * 2 + 4;
                Array.Resize<byte>(ref packet.Data, packet.Length);
            }

            currentOffset = endOffset;
            endOffset = endOffset + 4;

            packet.WriteInt(t, currentOffset);
        }

        public void WriteDouble(double t)
        {
            if (packet.Length <= (endOffset + 8))
            {
                packet.Length = packet.Length * 2 + 8;
                Array.Resize<byte>(ref packet.Data, packet.Length);
            }

            currentOffset = endOffset;
            endOffset = endOffset + 8;

            packet.WriteDouble(t, currentOffset);
        }

        public void WriteFloat(float t)
        {
            if (packet.Length <= (endOffset + 4))
            {
                packet.Length = packet.Length * 2 + 4;
                Array.Resize<byte>(ref packet.Data, packet.Length);
            }

            currentOffset = endOffset;
            endOffset = endOffset + 4;

            packet.WriteFloat(t, currentOffset);
        }

        public void WriteBool(bool t)
        {
            if (packet.Length <= (endOffset + 1))
            {
                packet.Length = packet.Length * 2 + 1;
                Array.Resize<byte>(ref packet.Data, packet.Length);
            }

            currentOffset = endOffset;
            endOffset = endOffset + 1;

            packet.WriteBool(t, currentOffset);
        }

        public void WriteString(string s)
        {
            if (packet.Length <= (endOffset + s.Length + 4))
            {
                packet.Length = packet.Length * 2 + (s.Length + 4);
                Array.Resize<byte>(ref packet.Data, packet.Length);
            }

            currentOffset = endOffset;
            endOffset = endOffset + s.Length + 4;

            packet.WriteString(s, currentOffset);
        }

        public Packet GetPacket()
        {
            if (packet != null)
            {
                if (endOffset < packet.Length - 1)
                {
                    packet.Length = endOffset + 1;
                    Array.Resize<byte>(ref packet.Data, packet.Length);
                }
            }

            return packet;
        }

        public void Clear()
        {
            packet = null;
            currentOffset = 0;
            endOffset = 0;
        }

        public void NewPacket(int opcode)
        {
            Clear();
            packet = new Packet(opcode, 1);
        }
    }
}
