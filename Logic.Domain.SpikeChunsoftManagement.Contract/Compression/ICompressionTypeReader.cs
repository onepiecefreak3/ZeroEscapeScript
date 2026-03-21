using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.SpikeChunsoftManagement.Contract.Enums;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Compression
{
    public interface ICompressionTypeReader
    {
        CompressionType Peek(Stream input);
    }
}
