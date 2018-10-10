using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public class ChatSession
    {
        #region Properties
        public string SessionKey { get { return _sessionKey; } }
        public DateTime StartTime { get; private set; }
        public IPEndPoint RemoteEndPoint { get { return Connected ? (IPEndPoint)_socket.RemoteEndPoint : _remoteEndPoint; } }
        public IPEndPoint LocalEndPoint { get { return Connected ? (IPEndPoint)_socket.LocalEndPoint : _localEndPoint; } }

        public TcpSocketConnectionState State
        {
            get
            {
                switch(_state)
                {
                    case _None:
                        return TcpSocketConnectionState.None;
                    case _Connecting:
                        return TcpSocketConnectionState.Connecting;
                    case _Connected:
                        return TcpSocketConnectionState.Connected;
                    case _Disposed:
                        return TcpSocketConnectionState.Closed;
                    default:
                        return TcpSocketConnectionState.Closed;
                }
            }
        }
        #endregion

        public ChatSession(SocketConfiguration config,ISessionEventDispatcher  dispatcher, ChatServer chatServer)
        {
            if(config == null)
            {
                throw new ArgumentNullException("config");
            }
            if(dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }
            if(chatServer==null)
            {
                throw new ArgumentNullException("chatServer");
            }

            _dispatcher = dispatcher;
            _chatServer = chatServer;
            _configuration = config;
        }

        #region Attach
        public void Attach(Socket socket) {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            lock(_opsLock)
            {
                _socket = socket;
                SetSocketOption();

                _sessionKey = Guid.Empty.ToString();

                _remoteEndPoint = this.RemoteEndPoint;
                _localEndPoint     = this.LocalEndPoint;
                _state = _None;
            }
        }

        public void Detach() {
            lock (_opsLock)
            {
                _socket = null;
                _sessionKey = Guid.Empty.ToString();

                _remoteEndPoint = null;
                _localEndPoint = null;
                _state = _None;
            }
        }
        #endregion

        #region Process
        public async Task Start()
        {
            int origin = Interlocked.CompareExchange(ref _state, _Connecting, _None);
            if(origin == _Disposed)
            {
                throw new ObjectDisposedException("This tcp socket session has be disposed when connecting");
            }
            if(origin!=_None)
            {
                throw new InvalidOperationException("This tcp socket session is in invalid state when connecting");
            }

            if(_receiveBuffer==default(ArraySegment<byte>))
            {
                _receiveBuffer = new ArraySegment<byte>(new byte[_configuration.ReceiveBufferSize], 0, _configuration.ReceiveBufferSize);
            }
            if(_sessionBuffer==default(ArraySegment<byte>))
            {
                int doubleSize = _receiveBuffer.Count * 2;
                _sessionBuffer = new ArraySegment<byte>(new byte[doubleSize], 0, doubleSize);
            }
            _sessionBufferOffset = 0;

            try
            {
                if(Interlocked.CompareExchange(ref _state,_Connected,_Connecting)!=_Connecting)
                {
                    await Close(false);
                    throw new ObjectDisposedException("This tcp socket session has been disposed after connected");
                }

                //log
                bool isErrorOccurredInUserSide = false;
                try
                {
                    //onSessionStart
                    await _dispatcher.OnSessionStarted(this);
                }
                catch(Exception ex)
                {
                    isErrorOccurredInUserSide = true;
                    await HandleUserSideError(ex);
                }

                if(!isErrorOccurredInUserSide)
                {
                    await Process();
                }                          
                else
                {
                    await Close(true);
                }
            }
            catch(Exception ex)
            {
                //log
                await Close(true);
                throw;
            }
        }
        private async Task Process()
        {
            try
            {
                int frameLength;
                byte[] payload;
                int payloadOffset;
                int payloadCount;
                int consumLength = 0;

                while(State == TcpSocketConnectionState.Connected)
                {
                    consumLength = 0;
                    int receiveCount = await _socket.ReceiveAsync(_receiveBuffer, SocketFlags.None);
                    if (receiveCount == 0)
                    {
                        break;
                    }

                    //copy buffer
                    Array.Copy(_receiveBuffer.Array, _receiveBuffer.Offset,
                        _sessionBuffer.Array, _sessionBufferOffset, receiveCount);
                    while (true)
                    {
                        frameLength = 0;
                        payload = null;                                                       
                        payloadOffset = 0;
                        payloadCount = 0;

                        if (_configuration.FrameBuilder.Decoder.TryDecodeFrame(
                            _sessionBuffer.Array,
                            _sessionBuffer.Offset+consumLength,
                            _sessionBufferOffset+receiveCount - consumLength,
                            out frameLength, out payload,out payloadOffset,out payloadCount))
                        {
                            try
                            {
                                //dispatch payload
                                await _dispatcher.OnSessionReceive(this, payload,payloadOffset,payloadCount);
                            }
                            catch (Exception ex)
                            {
                                await HandleUserSideError(ex);
                            }
                            finally
                            {
                                consumLength += frameLength;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    //shift buffer
                    int lastLength = _sessionBufferOffset + receiveCount - consumLength;
                    Array.Copy(_sessionBuffer.Array, consumLength, _sessionBuffer.Array, 0, lastLength);
                    _sessionBufferOffset = lastLength;
                }
            }
            catch(Exception ex)
            {
                await HandleReceiveOperationException(ex);
            }
            finally
            {
                await Close(true);
            }
        }
        void SetSocketOption() { }
        #endregion

        #region Close
        public async Task Close()
        {
            await Close(true);
        }
        private async Task Close(bool shallNotifyUserSide)
        {
            if(Interlocked.Exchange(ref _state,_Disposed)==_Disposed)
            {
                return;
            }
            Shutdown();
            if(shallNotifyUserSide)
            {
                try
                {
                     //onSessionClosed
                }
                catch(Exception ex)
                {
                    await HandleUserSideError(ex);
                }
            }
            Clean();
        }
        public void Shutdown()
        {
            if(_socket!=null&&_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Send);
            }
        }
        private void Clean()
        {
            try
            {
                if (_socket != null)
                {
                    _socket.Dispose();
                }
            }
            catch { }
            finally
            {
                _socket = null;
            }
            //clean receiveBuffer
        }
        #endregion

        #region Exception Hanlder
        private async Task HandleSendOperationException(Exception ex)
        {
            if(IsSocketTimeOut(ex))
            {
                await CloseIfShould(ex);
            }
            await CloseIfShould(ex);
        }
        private async Task HandleReceiveOperationException(Exception ex)
        {
            if(IsSocketTimeOut(ex))
            {
                await CloseIfShould(ex);
            }
            await CloseIfShould(ex);
        }
        private bool IsSocketTimeOut(Exception ex)
        {
            return ex is IOException
                && ex.InnerException!=null
                && ex.InnerException is SocketException
                && (ex.InnerException as SocketException).SocketErrorCode == SocketError.TimedOut;
        }
        private async Task<bool> CloseIfShould(Exception ex)
        {
            if(ex is ObjectDisposedException
                || ex is InvalidOperationException
                || ex is SocketException
                || ex is IOException
                || ex is NullReferenceException
                || ex is ArgumentException)
            {
                //log
                await Close();
                return true;
            }
            return false;
        }
        private async Task HandleUserSideError(Exception ex)
        {
            //log
            await Task.CompletedTask;
        }
        #endregion

        #region Send
        public async Task SendAsync(byte[] data)
        {
            byte[] frameBuffer;
            int frameOffset;
            int frameLength;
            _configuration.FrameBuilder.Encoder.EncodeFrame(data, 0, data.Length, out frameBuffer, out frameOffset, out frameLength);
            await SendAsync(frameBuffer, frameOffset, frameLength);
        }
        public async Task SendAsync(byte[] data,int offset,int count)
        {
            // check buffer

            if(State!=TcpSocketConnectionState.Connected)
            {
                throw new InvalidOperationException("This session has not connected");
            }

            try
            {
                await _socket.SendAsync( new ArraySegment<byte>(data), SocketFlags.None);
            }
            catch(Exception ex)
            {
                await HandleSendOperationException(ex);
            }
        }
        #endregion

        private Socket _socket;
        private readonly SocketConfiguration _configuration;
        private readonly ISessionEventDispatcher _dispatcher;
        private readonly ChatServer _chatServer;
        private object _opsLock = new object();
        private String _sessionKey;
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        private ArraySegment<byte> _receiveBuffer = default(ArraySegment<byte>);
        private ArraySegment<byte> _sessionBuffer = default(ArraySegment<byte>);
        private ArraySegment<byte> _sendBuffer = default(ArraySegment<byte>);
        private int _state;
        private const int _None = 0;
        private const int _Connecting = 1;
        private const int _Connected = 2;
        private const int _Disposed = 5;
       

        private bool Connected { get { return _socket != null && _socket.Connected; } }

        public int _sessionBufferOffset { get; private set; }
    }
}
