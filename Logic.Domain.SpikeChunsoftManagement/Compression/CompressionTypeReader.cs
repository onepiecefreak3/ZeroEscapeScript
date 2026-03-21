using Logic.Domain.SpikeChunsoftManagement.Contract.Compression;
using System.Buffers.Binary;
using Logic.Domain.SpikeChunsoftManagement.Contract.Enums;

namespace Logic.Domain.SpikeChunsoftManagement.Compression
{
    internal class CompressionTypeReader : ICompressionTypeReader
    {
        public CompressionType Peek(Stream input)
        {
            var buffer = new byte[4];
            _ = input.Read(buffer);

            input.Position -= 4;

            if (buffer[0] is (byte)'A' && buffer[1] is (byte)'T' && buffer[2] is (byte)'6' && buffer[3] is (byte)'P')
                return CompressionType.Version6;

            throw new InvalidOperationException($"Unknown compression type 0x{BinaryPrimitives.ReadInt32BigEndian(buffer):X8}.");
        }
    }
}
