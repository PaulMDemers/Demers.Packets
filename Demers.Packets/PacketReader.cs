namespace Demers.Packets
{
    public class PacketReader
    {
        private Packet _packet;
        private int _currentOffset = 0;

        public PacketReader(Packet p)
        {
            _packet = p;
        }

        public void Reset()
        {
            _currentOffset = 0;
        }

        public int ReadInt()
        {
            _currentOffset += 4;
            return _packet.ReadInt(_currentOffset - 4);
        }

        public float ReadFloat()
        {
            _currentOffset += 4;
            return _packet.ReadFloat(_currentOffset - 4);
        }

        public double ReadDouble()
        {
            _currentOffset += 8;
            return _packet.ReadDouble(_currentOffset - 8);
        }

        public bool ReadBool()
        {
            _currentOffset += 1;
            return _packet.ReadBool(_currentOffset - 1);
        }

        public string ReadString()
        {
            string s = _packet.ReadString(_currentOffset);
            _currentOffset += s.Length + 4;

            return s;
        }
    }
}
