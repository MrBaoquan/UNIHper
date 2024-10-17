using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UNIHper.Network
{
    using UniRx;

    public class UNetMsgReceiver
    {
        public bool Connected
        {
            get
            {
                if (socket == null)
                    return false;
                return !socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0;
            }
        }
        protected USocket uSocket;
        protected NetProtocol protocol
        {
            get
            {
                if (uSocket == null)
                    return NetProtocol.Unknown;
                return uSocket.Protocol;
            }
        }
        protected Socket socket;
        protected MessageQueue messageQueue;
        protected string RemoteIP = string.Empty;
        protected int RemotePort = -1;
        protected string LocalIP = string.Empty;
        protected int LocalPort = -1;
        protected string ConnectionKey = string.Empty;

        public UNetMsgReceiver Prepare(Socket InSocket, MessageQueue InQueue, USocket InUSocket)
        {
            socket = InSocket;
            var _ipEndPoint = socket.RemoteEndPoint as IPEndPoint;
            if (_ipEndPoint != null)
            {
                RemoteIP = _ipEndPoint.Address.ToString();
                RemotePort = _ipEndPoint.Port;
            }

            var _localEP = socket.LocalEndPoint as IPEndPoint;
            LocalIP = _localEP.Address.ToString();
            LocalPort = _localEP.Port;

            ConnectionKey = string.Format("{0}_{1}", RemoteIP, RemotePort);
            messageQueue = InQueue;
            uSocket = InUSocket;
            OnConnected();
            return this;
        }

        protected Action<string, Socket> onDisconnected = null;

        public void OnDisconnect(Action<string, Socket> InHandler)
        {
            onDisconnected = InHandler;
        }

        public virtual void OnConnected() { }

        protected void fireDisconnectedEvent()
        {
            if (onDisconnected != null)
            {
                Observable
                    .Start(
                        () =>
                        {
                            onDisconnected(ConnectionKey, socket);
                        },
                        Scheduler.MainThread
                    )
                    .Subscribe(_ =>
                    {
                        Dispose();
                    });
            }
        }

        protected void pushMessage(UMessage InMessage)
        {
            InMessage.LocalIP = LocalIP;
            InMessage.LocalPort = LocalPort;
            InMessage.RemoteIP = RemoteIP;
            InMessage.RemotePort = RemotePort;
            messageQueue.PushMessage(InMessage);
        }

        public UNetMsgReceiver Clone()
        {
            return this.MemberwiseClone() as UNetMsgReceiver;
        }

        public virtual void Dispose()
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Disconnect(false);
                    socket.Close();
                    socket.Dispose();
                    socket = null;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning(e.Message);
                }
            }
        }
    }
}
