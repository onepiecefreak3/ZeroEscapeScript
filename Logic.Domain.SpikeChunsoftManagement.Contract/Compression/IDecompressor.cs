using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Compression
{
    public interface IDecompressor
    {
        byte[] Decompress(Stream input);
    }
}
