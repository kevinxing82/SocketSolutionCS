﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public enum TcpSocketConnectionState
    {
        None = 0,
        Connecting = 1,
        Connected = 2,
        Closed = 5,
    }
}
