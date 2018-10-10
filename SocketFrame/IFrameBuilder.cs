using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public interface IFrameBuilder
    {
        IFrameEncoder Encoder { get; }
        IFrameDecoder Decoder { get; }
    }
}
