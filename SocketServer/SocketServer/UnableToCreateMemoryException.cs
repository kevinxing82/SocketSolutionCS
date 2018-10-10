using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    [Serializable]
    public class UnableToCreateMemoryException:Exception
    { 
        public UnableToCreateMemoryException()
            :base("All buffers were in use and acquiring more memory has been disabled")
        {

        }
    }
}
