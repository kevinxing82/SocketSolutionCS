using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public sealed class SocketConfiguration
    {
        public SocketConfiguration()
        {
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            FrameBuilder = new LengthPrefixedFrameBuilder();
        }

        public int ReceiveBufferSize { get; set; }
        public int SendBufferSize { get; set; }

        public IFrameBuilder FrameBuilder{get;set;}
    } 
}
