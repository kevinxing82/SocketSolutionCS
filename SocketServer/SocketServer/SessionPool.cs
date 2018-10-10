using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    class SessionPool : ObjectPool<ChatSession>
    {
         public SessionPool(Func<ChatSession> createSession,Action<ChatSession> cleanSession)
        {
            _createSession = createSession;
            _cleanSession = cleanSession;
        }
        public SessionPool Initialize(int initialCount = 0)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "initialCount",
                    initialCount,
                    "Initial count must not be less then zero.");
            }
            for (int i = 0; i < initialCount; i++)
            {
                Add(Create());
            }
            return this;
        }

        protected override ChatSession Create()
        {
            return _createSession();
        }

        private void cleanSession(ChatSession session)
        {
            _cleanSession(session);
            Add(session);
        }

        public void Return(ChatSession session)
        {
            cleanSession(session);
            Add(session); 
        }

        private Func<ChatSession> _createSession;
        private Action<ChatSession> _cleanSession;
    }
}
