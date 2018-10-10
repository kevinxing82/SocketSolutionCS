using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public interface ISessionEventDispatcher
    {
        Task OnSessionStarted(ChatSession session);
        Task OnSessionReceive(ChatSession session, byte[] data, int offset, int count);
        Task OnSessionClosed(ChatSession session);
    }
}
