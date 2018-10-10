using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace org.kevinxing.socket                                    
{
    public class ChatServer:IDisposable
    {
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;                 // the socket used to listen for incoming connection requests
                                                         // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SessionPool m_sessionPool;
        ISessionEventDispatcher _dipatcher;
        SocketConfiguration _configuration;


        private readonly ConcurrentDictionary<String, ChatSession> _sessions = new ConcurrentDictionary<string, ChatSession>();

        public event EventHandler<LogEventArgs> logHandler;

        public ChatServer(SocketConfiguration cnfg,ISessionEventDispatcher dispatcher)
        {
            _configuration = cnfg;
            _dipatcher = dispatcher;
        }

        public void Init()
        {
            m_sessionPool = new SessionPool(
                () =>
                {
                    ChatSession session= new ChatSession(_configuration,_dipatcher, this);
                    return session;
                },(session)=>
                {
                    try
                    {
                        session.Detach();
                    }
                    catch(Exception ex)
                    {
                        //log
                    }
                }).Initialize(512);

        }

        public void Start(IPEndPoint localEndPoint)
        {
            try
            {
                listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(100);
                log("====================================");
                log(String.Format("Server Start  ip:{0} port:{1}",
                    localEndPoint.Address.MapToIPv4().ToString(),
                    localEndPoint.Port));
                log("====================================");
                // post accepts on the listening socket
                Task.Factory.StartNew(async () =>
                {
                    await StartAccept();
                },
                TaskCreationOptions.LongRunning);
            }
            catch (Exception e) when (!ShouldThrow(e))
            {
                log(e.ToString());
            }
        }

        public void Shutdown()
        {
            try
            {
                listenSocket.Close(0);
                listenSocket = null;

                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        foreach(ChatSession session in _sessions.Values)
                        {
                            await session.Close();
                        }
                    }
                    catch (Exception ex) when (!ShouldThrow(ex)) { }
                }, TaskCreationOptions.PreferFairness).Wait();
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
        }

        private void SetSocketOptions()
        {
            listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        private async Task StartAccept()
        {
            try
            {
                while(true)
                {
                    Socket acceptSocket = await listenSocket.AcceptAsync();
                    await ProcessAccept(acceptSocket);
                }
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
            catch (Exception ex)
            {
                log(ex.Message);
            }
        }

        private async Task ProcessAccept(Socket socket)
        {
            ChatSession session = m_sessionPool.Take();
            session.Attach(socket);
            if(_sessions.TryAdd(session.SessionKey,session))
            {
                log(String.Format("New session[{0}]", session));
                try
                {
                    await session.Start();
                }
                finally
                {
                    ChatSession recycle;
                    if(_sessions.TryRemove(session.SessionKey,out recycle))
                    {
                        log(String.Format("Close session[{0}].", recycle));
                    }
                }
            }
            m_sessionPool.Return(session);
        }

        #region  Send
        public async Task SendToAsync(String sessionKey,byte[] data)
        {
            await SendToAsync(sessionKey, data, 0, data.Length);
        }

        public async Task SendToAsync(String sessionKey,byte[] data,int offset,int count)
        {
            ChatSession sessionFound;
            if(_sessions.TryGetValue(sessionKey,out sessionFound))
            {
                await sessionFound.SendAsync(data, offset, count);
            }
            else
            {
                log(String.Format("Cannot find session[{0}].", sessionKey));
            }
        }

        public async Task SendToAsync(ChatSession session,byte[] data)
        {
            await SendToAsync(session, data, 0, data.Length);
        }

        public async Task SendToAsync(ChatSession session,byte[] data,int offset,int count)
        {
            ChatSession sessionFound;
            if (_sessions.TryGetValue(session.SessionKey, out sessionFound))
            {
                await sessionFound.SendAsync(data, offset, count);
            }
            else
            {
                log(String.Format("Cannot find session[{0}].", session));
            }
        }

        public async Task BroadcastAsync(byte[] data)
        {
            await BroadcastAsync(data, 0, data.Length);
        }

        public async Task BroadcastAsync(byte[] data,int offset,int count)
        {
            foreach(ChatSession session in _sessions.Values)
            {
                await session.SendAsync(data, offset, count);
            }
        }
        #endregion

        #region Session
        public bool HasSession(String sessionKey)
        {
            return _sessions.ContainsKey(sessionKey);
        }

        public ChatSession GetSession(String sessionKey)
        {
            ChatSession session = null;
            _sessions.TryGetValue(sessionKey, out session);
            return session;
        }

        public async Task  CloseSession(String sessionKey)
        {
            ChatSession session = null;
            if(_sessions.TryGetValue(sessionKey,out session))
            {
                await session.Close();
            }
        }
        #endregion

        private bool ShouldThrow(Exception ex)
        {
            if (ex is ObjectDisposedException
               || ex is InvalidOperationException
               || ex is SocketException
               || ex is IOException)
            {
                return false;
            }
            return true;
        }
        private void log(String msg)
        {
            EventHandler<LogEventArgs> tmp = Volatile.Read(ref logHandler);
            if (tmp != null) tmp(this, new LogEventArgs(msg));
        }

        #region IDisposable Support
        private readonly object _disposeLock = new object();
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            lock(_disposeLock)
            {
                if(disposedValue)
                {
                    return;
                }

                if (disposing)
                {
                     try
                    {
                        if(listenSocket != null)
                        {
                            listenSocket.Dispose();
                        }
                        if(m_sessionPool!=null)
                        {
                            m_sessionPool.Dispose();
                        }
                    }
                    catch(Exception ex)
                    {
                        log(ex.Message);
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
             Dispose(true);
             GC.SuppressFinalize(this);
        }
        #endregion
    }
}
