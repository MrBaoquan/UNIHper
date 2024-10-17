using System.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace UNIHper.Network
{
    using UniRx;

    public class UTcpClient : USocket
    {
        public bool Connected
        {
            get { return connected; }
        }

        private Socket socket;
        private bool clientReuse = true;
        private bool connected = false;

        public UTcpClient(UNetMsgReceiver messageReceiver = null)
            : base(NetProtocol.Tcp)
        {
            SetReceiver(messageReceiver);
        }

        public UTcpClient(
            string InLocalIP,
            int InLocalPort,
            UNetMsgReceiver InMessageReceiver = null
        )
            : this(InMessageReceiver)
        {
            setLocalEndPoint(InLocalIP, InLocalPort);
        }

        public new UTcpClient SetReceiver(UNetMsgReceiver InMessageReceiver)
        {
            base.SetReceiver(InMessageReceiver);
            return this;
        }

        public UTcpClient SetRemoteEP(string InRemoteIP, int InRemotePort)
        {
            setRemoteEndPoint(InRemoteIP, InRemotePort);
            return this;
        }

        public UTcpClient SetLocalEP(string InLocalIP, int InLocalPort)
        {
            setLocalEndPoint(InLocalIP, InLocalPort);
            return this;
        }

        IDisposable connectDisposable;

        public UTcpClient Connect()
        {
            if (connected)
            {
                UnityEngine.Debug.LogWarning("Already connected.");
                return this;
            }
            if (remoteEndPoint == null)
            {
                UnityEngine.Debug.LogWarning("Remote endpoint not set yet.");
                return null;
            }

            Connect(remoteEndPoint);
            connectDisposable = Observable
                .Interval(TimeSpan.FromMilliseconds(3000))
                .Where((_1, _2) => clientReuse && !Connected)
                .Subscribe(_3 =>
                {
                    UnityEngine.Debug.Log("Reconnecting...");
                    destroySocket(ref socket);
                    Connect(remoteEndPoint);
                });
            dispatchMessages();
            return this;
        }

        public void Disconnect()
        {
            doDisconnect(false);
        }

        public int Send2Server(byte[] InData)
        {
            if (socket == null || !Connected || InData == null)
            {
                UnityEngine.Debug.LogWarning("Send package failed");
                return 0;
            }
            try
            {
                if (!socket.Connected)
                {
                    UnityEngine.Debug.LogWarning("Socket not connect yet");
                    doDisconnect(true);
                    return 0;
                }

                return socket.Send(InData);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning(e.Message);
                doDisconnect(true);
            }
            return 0;
        }

        public int Send2Server(string InData)
        {
            return Send2Server(Encoding.UTF8.GetBytes(InData));
        }

        protected override void onReceiverDisconnected(string InKey, Socket InSocket)
        {
            doDisconnect(true);
        }

        private void doDisconnect(bool bReuse = false)
        {
            clientReuse = false;
            if (connected)
            {
                connected = false;
                var _disconnectedEvent = new UNetDisconnectedEvent
                {
                    RemoteIP = remoteEndPoint.Address.ToString(),
                    RemotePort = remoteEndPoint.Port
                };
                Managements.Event.Fire(_disconnectedEvent);
                onDisconnected.Invoke(_disconnectedEvent);
            }
            if (!bReuse)
            {
                destroySocket(ref socket);
                this.Dispose();
            }
            clientReuse = bReuse;
        }

        public override void Dispose()
        {
            if (socket != null)
            {
                destroySocket(ref socket);
            }

            base.Dispose();
            if (connectDisposable != null)
            {
                connectDisposable.Dispose();
                connectDisposable = null;
            }
        }

        private void Connect(IPEndPoint InRemoteEndPoint)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (localEndPoint != null)
                socket.Bind(localEndPoint);

            Observable
                .Start(() =>
                {
                    try
                    {
                        socket.Connect(InRemoteEndPoint);
                        if (localEndPoint == null)
                        {
                            setLocalEndPoint(socket.LocalEndPoint as IPEndPoint);
                        }
                        UnityEngine.Debug.LogFormat(
                            ("Try connect to: {0}:{1}"),
                            InRemoteEndPoint.Address.ToString(),
                            InRemoteEndPoint.Port
                        );
                        return true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log(e.Message);
                        UnityEngine.Debug.Log(e.StackTrace);
                        return false;
                    }
                })
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    if (!_)
                        return;
                    UnityEngine.Debug.LogFormat(
                        ("Connected to: {0}:{1}"),
                        InRemoteEndPoint.Address.ToString(),
                        InRemoteEndPoint.Port
                    );
                    connected = true;
                    var _event = new UNetConnectedEvent
                    {
                        RemoteIP = remoteEndPoint.Address.ToString(),
                        RemotePort = remoteEndPoint.Port
                    };
                    Managements.Event.Fire(_event);
                    onConnected.Invoke(_event);
                    startNewReceiver(socket);
                });
        }
    }
}
