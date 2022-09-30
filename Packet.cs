using System;
using System.Text;

namespace Demers.Packets
{
    public class Packet
    {
        public int Opcode { get; set; }
        public int Length { get; set; }
        public byte[] Data = null;

        public Packet(int opcode, int length)
        {
            Opcode = opcode;
            Length = length;

            Data = new byte[length];
        }

        //Writers
        public void WriteBytes(Array src, int srcOffset, int dstOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, Data, dstOffset, count);
        }

        public void WriteBytes(Array src, int dstOffset, int count)
        {
            WriteBytes(src, 0, dstOffset, count);
        }

        public void WriteBytes(Array src, int dstOffset)
        {
            WriteBytes(src, 0, dstOffset, src.Length);
        }

        public void WriteBytes(Array src)
        {
            WriteBytes(src, 0, 0, src.Length);
        }

        //Specific writers
        public void WriteInt(int t, int offset)
        {
            WriteBytes(BitConverter.GetBytes(t), 0, offset, 4);
        }

        public void WriteDouble(double t, int offset)
        {
            WriteBytes(BitConverter.GetBytes(t), 0, offset, 8);
        }

        public void WriteFloat(float t, int offset)
        {
            WriteBytes(BitConverter.GetBytes(t), 0, offset, 4);
        }

        public void WriteBool(bool t, int offset)
        {
            WriteBytes(BitConverter.GetBytes(t), 0, offset, 1);
        }

        public void WriteString(string s, int offset)
        {
            byte[] b = Encoding.UTF8.GetBytes(s);

            WriteInt(b.Length, offset);
            WriteBytes(b, offset + 4, b.Length);
        }

        //Readers
        public int ReadInt(int offset)
        {
            return BitConverter.ToInt32(Data, offset);
        }

        public float ReadFloat(int offset)
        {
            return BitConverter.ToSingle(Data, offset);
        }

        public double ReadDouble(int offset)
        {
            return BitConverter.ToDouble(Data, offset);
        }

        public bool ReadBool(int offset)
        {
            return BitConverter.ToBoolean(Data, offset);
        }

        public string ReadString(int offset)
        {
            int length = ReadInt(offset);
            return Encoding.UTF8.GetString(Data, offset + 4, length);
        }
    }
}
