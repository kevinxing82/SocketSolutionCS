using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace org.kevinxing.socket
{
    class ChatAsyncClient
    {
        private Socket client;
        public event EventHandler<LogEventArgs> logHandler;

        private SocketConfiguration config;
        private ArraySegment<byte> receiveBuffer;
        private ArraySegment<byte> dataBuffer;
        private int dataBufferOffset = 0;
        public async Task StartClientAsync()
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 9527);

                config = new SocketConfiguration();
                receiveBuffer = new ArraySegment<byte>(new byte[config.ReceiveBufferSize]);
                dataBuffer = new ArraySegment<byte>(new byte[config.ReceiveBufferSize * 2]);
                client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                 await client.ConnectAsync(remoteEP);

                if(client.Connected)
                {
                    log(String.Format("Socket connected to {0}",
                    client.RemoteEndPoint.ToString()));

                    await Process();
                }
            }
            catch(Exception e)
            {
                log(e.ToString());
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

                while (client.Connected)
                {
                    //decode
                    consumLength = 0;
                    int receiveCount = await client.ReceiveAsync(receiveBuffer, SocketFlags.None);
                    if (receiveCount == 0)
                    {
                        break;
                    }

                    //copy buffer
                    Array.Copy(receiveBuffer.Array, receiveBuffer.Offset,
                        dataBuffer.Array, dataBufferOffset, receiveCount);
                    while (true)
                    {
                        frameLength = 0;
                        payload = null;
                        payloadOffset = 0;
                        payloadCount = 0;

                        if (config.FrameBuilder.Decoder.TryDecodeFrame(
                            dataBuffer.Array,
                            dataBuffer.Offset + consumLength,
                            dataBufferOffset + receiveCount - consumLength,
                            out frameLength, out payload, out payloadOffset, out payloadCount))
                        {
                            try
                            {
                                //dispatch payload
                                log(System.Text.Encoding.ASCII.GetString(payload));
                                //await _dispatcher.OnSessionReceive(this, payload, payloadOffset, payloadCount);
                            }
                            catch (Exception ex)
                            {
                                log(ex.ToString());
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
                    int lastLength = dataBufferOffset + receiveCount - consumLength;
                    Array.Copy(dataBuffer.Array, consumLength, dataBuffer.Array, 0, lastLength);
                    dataBufferOffset = lastLength;

                }
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
            finally
            {
                //close
            }
        }
        public async Task SendAsync(String data)
        {
            byte[] bArr = System.Text.Encoding.ASCII.GetBytes(data);
            byte[] frameArr;
            int frameOffset;
            int frameLength;
            config.FrameBuilder.Encoder.EncodeFrame(bArr, 0, bArr.Length,out  frameArr,out  frameOffset,out  frameLength);
            int sendCount = await client.SendAsync(new ArraySegment<byte>(frameArr), SocketFlags.None);
            log(String.Format("Socket send {0}",
                    sendCount));
        }
        private void log(String msg)
        {
            EventHandler<LogEventArgs> tmp = Volatile.Read(ref logHandler);
            if (tmp != null) tmp(this, new LogEventArgs(msg));
        }

        public static bool IsValidIP(string v)
        {
            if ((v.Length > 12) || (v.Length < 7))
            {
                return false;
            }
            try
            {
                string[] s = v.Split(new string[] { "." }, StringSplitOptions.None);
                if (s.Length != 4)
                {
                    return false;
                }
                foreach (string i in s)
                {
                    Convert.ToInt32(i);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }



    
}
