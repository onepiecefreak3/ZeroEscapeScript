using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.Contract.Enums;
using Komponent.IO;
using Logic.Domain.SpikeChunsoftManagement.Contract.Compression;
using Logic.Domain.SpikeChunsoftManagement.InternalContract;

namespace Logic.Domain.SpikeChunsoftManagement.Compression
{
    internal class At6pDecompressor : IAt6pDecompressor
    {
        public byte[] Decompress(Stream input)
        {
            var buffer = new byte[4];

            input.Position = 0x10;
            _ = input.Read(buffer, 0, 3);

            var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            var result = new byte[decompressedSize];

            input.Position = 0x14;
            result[0] = (byte)input.ReadByte();

            input.Position = 0x16;
            var reader = new BinaryBitReader(input, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.LittleEndian);

            DecompressInternal(result, reader);

            return result;
        }

        private void DecompressInternal(byte[] result, BinaryBitReader reader)
        {
            var position = 1;
            var currentByte = result[0];
            var previousByte = (byte)0;

            var count = 0;
            while (position < result.Length)
            {
                if (reader.ReadBit() is 0)
                {
                    count++;
                    continue;
                }

                var data = reader.ReadBits<int>(count);
                data += (1 << count) - 1;

                if (data is 1)
                {
                    (previousByte, currentByte) = (currentByte, previousByte);
                }
                else if (data is not 0)
                {
                    var delta = ((data & 1) is 0 ? 1 : -1) * (data >> 1);

                    previousByte = currentByte;
                    currentByte = (byte)(currentByte + delta);
                }

                result[position++] = currentByte;

                count = 0;
            }
        }
    }
}
