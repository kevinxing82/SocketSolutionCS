using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public class SimpleEventDispatcher : ISessionEventDispatcher
    {
        public event EventHandler<LogEventArgs> logHandler;
        public Task OnSessionClosed(ChatSession session)
        {
            log(string.Format("TCP session {0} has disconnected.", session));
            return  Task.CompletedTask;
        }

        public Task OnSessionReceive(ChatSession session, byte[] data, int offset, int count)
        {
            var text = Encoding.UTF8.GetString(data, offset, count);
            log(string.Format("Client : {0} --> ", session.RemoteEndPoint));
            if (count < 1024 * 1024 * 1)
            {
                log(text);
            }
            else
            {
                log(string.Format("{0} Bytes", count));
            }
            return Task.CompletedTask;
        }

        public Task OnSessionStarted(ChatSession session)
        {
            log(string.Format("TCP session {0} has connected {1}.", session.RemoteEndPoint, session));
            String helloWord = "Welcome to SAO~~~~!";
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(helloWord);
            session.SendAsync(buffer);
            return Task.CompletedTask;
        }

        private void log(String msg)
        {
            EventHandler<LogEventArgs> tmp = Volatile.Read(ref logHandler);
            if (tmp != null) tmp(this, new LogEventArgs(msg));
        }
    }

}
