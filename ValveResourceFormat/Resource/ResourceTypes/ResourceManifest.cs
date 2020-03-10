using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ValveResourceFormat.Blocks;
using ValveResourceFormat.Utils;

namespace ValveResourceFormat.ResourceTypes
{
    public class ResourceManifest : ResourceData
    {
        public List<string> Resources { get; private set; }

        public override void Read(BinaryReader reader, Resource resource)
        {
            reader.BaseStream.Position = Offset;

            var version = reader.ReadInt32();

            if (version != 8)
            {
                throw new UnexpectedMagicException("Unknown version", version, nameof(version));
            }

            Resources = new List<string>();

            var a = reader.ReadInt32();
            var b = reader.ReadInt32();

            var count = reader.ReadInt32();
            var offset = reader.BaseStream.Position;

            for (var i = 0; i < count; i++)
            {
                var offsetToValue = offset + reader.ReadInt32();
                offset += 4;

                reader.BaseStream.Position = offsetToValue;
                var value = reader.ReadNullTermString(Encoding.UTF8);

                reader.BaseStream.Position = offset;

                Resources.Add(value);
            }
        }

        public override string ToString()
        {
            using var writer = new IndentedTextWriter();
            foreach (var entry in Resources)
            {
                writer.WriteLine(entry);
            }

            return writer.ToString();
        }
    }
}
