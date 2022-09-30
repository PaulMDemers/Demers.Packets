namespace Demers.Packets
{
    public class PacketReader
    {
        Packet packet = null;
        int currentOffset = 0;

        public PacketReader(Packet p)
        {
            packet = p;
        }

        public void Reset()
        {
            currentOffset = 0;
        }

        public int ReadInt()
        {
            currentOffset += 4;
            return packet.ReadInt(currentOffset - 4);
        }

        public float ReadFloat()
        {
            currentOffset += 4;
            return packet.ReadFloat(currentOffset - 4);
        }

        public double ReadDouble()
        {
            currentOffset += 8;
            return packet.ReadDouble(currentOffset - 8);
        }

        public bool ReadBool()
        {
            currentOffset += 1;
            return packet.ReadBool(currentOffset - 1);
        }

        public string ReadString()
        {
            string s = packet.ReadString(currentOffset);
            currentOffset += s.Length + 4;

            return s;
        }
    }
}
