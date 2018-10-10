using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public class LogEventArgs : EventArgs
    {
        private readonly String m_msg;
        public LogEventArgs(String msg)
        {
            m_msg = msg;
        }

        public String Message
        {
            get { return m_msg; }
        }
    }
}
