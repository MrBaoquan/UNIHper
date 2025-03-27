using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DNHper;
using UnityEngine;

namespace UNIHper.Network
{
    public class NetStringMessage : UMessage
    {
        public string Content = string.Empty;
    }

    public class StringMsgReceiver : UNetMsgReceiver
    {
        public int ReadBufferSize = 4096;
        CancellationTokenSource recvHandler = null;

        public override void OnConnected()
        {
            if (protocol == NetProtocol.Tcp)
            {
                startTcpReceiver();
            }
            else if (protocol == NetProtocol.Udp)
            {
                startUdpReceiver();
            }
        }

        private void startTcpReceiver()
        {
            if (recvHandler != null)
            {
                recvHandler.Cancel();
                recvHandler = null;
            }
            recvHandler = new CancellationTokenSource();
            byte[] _buffer = new byte[ReadBufferSize];
            Task.Factory.StartNew(
                () =>
                {
                    while (!recvHandler.Token.IsCancellationRequested)
                    {
                        try
                        {
                            bool _connected =
                                !socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0;
                            if (!_connected)
                            {
                                recvHandler.Cancel();
                                fireDisconnectedEvent();
                                return;
                            }
                            Array.Clear(_buffer, 0, _buffer.Length);
                            int _received = socket.Receive(_buffer);
                            pushMessage(
                                new NetStringMessage
                                {
                                    Content = Encoding.UTF8
                                        .GetString(_buffer.Slice(0, _received))
                                        .Trim(),
                                    RawData = _buffer.Slice(0, _received)
                                }
                            );
                        }
                        catch (System.Exception)
                        {
                            //UnityEngine.Debug.LogWarning (e.Message);
                        }
                    }
                },
                recvHandler.Token
            );
        }

        private void startUdpReceiver()
        {
            if (recvHandler != null)
            {
                recvHandler.Cancel();
                recvHandler = null;
            }
            recvHandler = new CancellationTokenSource();
            byte[] _buffer = new byte[ReadBufferSize];
            Task.Factory.StartNew(
                () =>
                {
                    while (!recvHandler.Token.IsCancellationRequested)
                    {
                        try
                        {
                            Array.Clear(_buffer, 0, _buffer.Length);
                            EndPoint _endPoint = new IPEndPoint(IPAddress.Any, 0);
                            int _received = socket.ReceiveFrom(_buffer, ref _endPoint);
                            IPEndPoint _ipEP = _endPoint as IPEndPoint;
                            RemoteIP = _ipEP.Address.ToString();
                            RemotePort = _ipEP.Port;

                            var _validBuf = _buffer.Slice(0, _received);
                            pushMessage(
                                new NetStringMessage
                                {
                                    Content = Encoding.UTF8.GetString(_validBuf).Trim(),
                                    RawData = _validBuf
                                }
                            );
                        }
                        catch (System.Exception e)
                        {
                            UnityEngine.Debug.LogWarning(e.Message);
                        }
                    }
                },
                recvHandler.Token
            );
        }

        public override void Dispose()
        {
            base.Dispose();
            if (recvHandler != null)
            {
                recvHandler.Cancel();
                recvHandler = null;
            }
        }
    }
}
