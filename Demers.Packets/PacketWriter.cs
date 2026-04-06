using System;
using System.Text;

namespace Demers.Packets
{
    public class PacketWriter
    {
        private Packet _packet = null;
        private int _currentOffset = 0;
        private int _endOffset = 0;
        public bool AddLegacyByte { get; set; }

        public PacketWriter()
        {
            AddLegacyByte = false;
        }

        public PacketWriter(bool addLegacyByte)
        {
            AddLegacyByte = addLegacyByte;
        }

        public void WriteInt(int t)
        {
            if (_packet.Length < (_endOffset + 4))
            {
                _packet.Length = _packet.Length * 2 + 4;
                Array.Resize<byte>(ref _packet.Data, _packet.Length);
            }

            _currentOffset = _endOffset;
            _endOffset = _endOffset + 4;

            _packet.WriteInt(t, _currentOffset);
        }

        public void WriteDouble(double t)
        {
            if (_packet.Length < (_endOffset + 8))
            {
                _packet.Length = _packet.Length * 2 + 8;
                Array.Resize<byte>(ref _packet.Data, _packet.Length);
            }

            _currentOffset = _endOffset;
            _endOffset = _endOffset + 8;

            _packet.WriteDouble(t, _currentOffset);
        }

        public void WriteFloat(float t)
        {
            if (_packet.Length < (_endOffset + 4))
            {
                _packet.Length = _packet.Length * 2 + 4;
                Array.Resize<byte>(ref _packet.Data, _packet.Length);
            }

            _currentOffset = _endOffset;
            _endOffset = _endOffset + 4;

            _packet.WriteFloat(t, _currentOffset);
        }

        public void WriteBool(bool t)
        {
            if (_packet.Length < (_endOffset + 1))
            {
                _packet.Length = _packet.Length * 2 + 1;
                Array.Resize<byte>(ref _packet.Data, _packet.Length);
            }

            _currentOffset = _endOffset;
            _endOffset = _endOffset + 1;

            _packet.WriteBool(t, _currentOffset);
        }

        public void WriteString(string s)
        {
            int byteCount = Encoding.UTF8.GetByteCount(s);

            if (_packet.Length < (_endOffset + byteCount + 4))
            {
                _packet.Length = _packet.Length * 2 + (byteCount + 4);
                Array.Resize<byte>(ref _packet.Data, _packet.Length);
            }

            _currentOffset = _endOffset;
            _endOffset = _endOffset + byteCount + 4;

            _packet.WriteString(s, _currentOffset);
        }

        public Packet GetPacket()
        {
            if (_packet != null)
            {
                int finalLength = _endOffset + (AddLegacyByte ? 1 : 0);

                if (_packet.Length != finalLength)
                {
                    _packet.Length = finalLength;
                    Array.Resize<byte>(ref _packet.Data, _packet.Length);
                }
            }

            return _packet;
        }

        public void Clear()
        {
            _packet = null;
            _currentOffset = 0;
            _endOffset = 0;
        }

        public void NewPacket(int opcode)
        {
            Clear();
            _packet = new Packet(opcode, 0);
        }
    }
}
