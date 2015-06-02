using System.IO;

namespace Client.Authentication
{
    public class WorldServerInfo
    {
        public byte Type { get; private set; }
        private byte locked;
        public byte Flags { get; private set; }
        public string Name { get; private set; }
        public string Address { get; private set; }
        public int Port { get; private set; }
        public float Population { get; private set; }
        private byte load;
        private byte timezone;
        private byte version_major;
        private byte version_minor;
        private byte version_bugfix;
        private ushort build;
        public uint ID { get; private set; }
        //private ushort unk2;

        public WorldServerInfo(BinaryReader reader)
        {
            Type = reader.ReadByte();
            locked = reader.ReadByte();
            Flags = reader.ReadByte();
            Name = reader.ReadCString();
            string address = reader.ReadCString();
            string[] tokens = address.Split(':');
            Address = tokens[0];
            Port = tokens.Length > 1 ? int.Parse(tokens[1]) : 8085;
            Population = reader.ReadSingle();
            load = reader.ReadByte();
            timezone = reader.ReadByte();
            ID = reader.ReadByte();

            if ((Flags & 4) != 0)
            {
                version_major = reader.ReadByte();
                version_minor = reader.ReadByte();
                version_bugfix = reader.ReadByte();
                build = reader.ReadUInt16();
            }
        }
    }
}
