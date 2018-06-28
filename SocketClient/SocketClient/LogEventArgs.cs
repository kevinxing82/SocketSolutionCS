using System;

namespace org.kevinxing.socket
{
    public class LogEventArgs
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