using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.SpikeChunsoftManagement.Contract.Compression;
using Logic.Domain.SpikeChunsoftManagement.Contract.Enums;
using Logic.Domain.SpikeChunsoftManagement.InternalContract;

namespace Logic.Domain.SpikeChunsoftManagement.Compression
{
    internal class DecompressorFactory(ICoCoKernel kernel) : IDecompressorFactory
    {
        public IDecompressor Get(CompressionType type)
        {
            switch (type)
            {
                case CompressionType.Version6:
                    return kernel.Get<IAt6pDecompressor>();

                default:
                    throw new InvalidOperationException($"Unknown compression type {type}.");
            }
        }
    }
}
