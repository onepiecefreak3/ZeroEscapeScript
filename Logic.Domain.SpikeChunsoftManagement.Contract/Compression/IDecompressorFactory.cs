using Logic.Domain.SpikeChunsoftManagement.Contract.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Compression
{
    public interface IDecompressorFactory
    {
        IDecompressor Get(CompressionType type);
    }
}
