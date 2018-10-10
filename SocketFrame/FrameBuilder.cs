using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public class FrameBuilder : IFrameBuilder
    {
        public FrameBuilder(IFrameEncoder encoder,IFrameDecoder decoder)
        {
            if(encoder==null)
            {
                throw new ArgumentNullException("encoder");
            }
            if(decoder==null)
            {
                throw new ArgumentNullException("decoder");
            }

            this.Encoder = encoder;
            this.Decoder = decoder;
        }
        public IFrameDecoder Decoder
        {
            get; private set;
        }

        public IFrameEncoder Encoder
        {
            get;private set;
        }
    }
}
