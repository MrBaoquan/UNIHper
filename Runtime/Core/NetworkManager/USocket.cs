using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using UnityEngine.Events;

namespace UNIHper.Network
{
    using UniRx;
    using UnityEngine;

    public class UNetConnectedEvent : UEvent
    {
        public string RemoteIP = string.Empty;
        public int RemotePort = -1;
        public string Key
        {
            get { return string.Format("{0}_{1}", RemoteIP, RemotePort); }
        }
    }

    public class UNetDisconnectedEvent : UEvent
    {
        public string RemoteIP = string.Empty;
        public int RemotePort = -1;
        public string Key
        {
            get { return string.Format("{0}_{1}", RemoteIP, RemotePort); }
        }
    }

    public abstract class USocket : IDisposable
    {
        const string InValidIP = "Invalid IP";
        const int InValidPort = -1;

        protected UnityEvent<UNetConnectedEvent> onConnected = new UnityEvent<UNetConnectedEvent>();
        public UnityEvent<UNetConnectedEvent> OnConnected
        {
            get => onConnected;
        }

        public IObservable<UNetConnectedEvent> OnConnectedAsObservable()
        {
            return onConnected.AsObservable();
        }

        protected UnityEvent<UNetDisconnectedEvent> onDisconnected =
            new UnityEvent<UNetDisconnectedEvent>();
        public UnityEvent<UNetDisconnectedEvent> OnDisconnected
        {
            get => onDisconnected;
        }

        public IObservable<UNetDisconnectedEvent> OnDisconnectedAsObservable()
        {
            return onDisconnected.AsObservable();
        }

        public NetProtocol Protocol
        {
            get { return protocol; }
        }
        protected NetProtocol protocol = NetProtocol.Unknown;
        protected MessageQueue messageDispatcher = new MessageQueue();
        protected IPEndPoint localEndPoint;
        public IPEndPoint LocalEndPoint
        {
            get { return localEndPoint; }
            set
            {
                localEndPoint = value;
                onLocalEndPointChanged();
            }
        }
        public string LocalIP
        {
            get
            {
                if (localEndPoint == null)
                    return InValidIP;
                return localEndPoint.Address.ToString();
            }
        }
        public int LocalPort
        {
            get
            {
                if (LocalEndPoint == null)
                    return InValidPort;
                return localEndPoint.Port;
            }
        }

        public string RemoteIP
        {
            get
            {
                if (RemoteEndPoint == null)
                    return InValidIP;
                return RemoteEndPoint.Address.ToString();
            }
        }
        public int RemotePort
        {
            get
            {
                if (RemoteEndPoint == null)
                    return InValidPort;
                return RemoteEndPoint.Port;
            }
        }

        protected IPEndPoint remoteEndPoint;
        public IPEndPoint RemoteEndPoint
        {
            get { return remoteEndPoint; }
            set
            {
                remoteEndPoint = value;
                onRemoteEndPointChanged();
            }
        }

        protected USocket(NetProtocol InProtocol)
        {
            protocol = InProtocol;
        }

        public string Key
        {
            get
            {
                if (remoteEndPoint == null)
                    return string.Empty;
                return string.Format(
                    "{0}_{1}",
                    remoteEndPoint.Address.ToString(),
                    remoteEndPoint.Port
                );
            }
        }

        public virtual void Dispose()
        {
            if (messageDispatcherHandler != null)
            {
                messageDispatcherHandler.Dispose();
            }

            messageReceivers.ForEach(_receiver =>
            {
                if (_receiver != null)
                    _receiver.Dispose();
            });
        }

        public USocket SetReceiver(UNetMsgReceiver InMessageReceiver)
        {
            MsgReceiver = InMessageReceiver;
            return this;
        }

        public USocket OnReceived(UnityAction<(UMessage Message, USocket Socket)> InMessageHandler)
        {
            onMessageReceived.AddListener(InMessageHandler);
            return this;
        }

        protected void setLocalEndPoint(string InIP, int InPort)
        {
            LocalEndPoint = new IPEndPoint(IPAddress.Parse(InIP), InPort);
        }

        protected void setLocalEndPoint(IPEndPoint InLocalEndPoint)
        {
            LocalEndPoint = InLocalEndPoint;
        }

        protected void setRemoteEndPoint(string InIP, int InPort)
        {
            RemoteEndPoint = new IPEndPoint(IPAddress.Parse(InIP), InPort);
        }

        protected virtual void onLocalEndPointChanged() { }

        protected virtual void onRemoteEndPointChanged() { }

        IDisposable messageDispatcherHandler = null;

        UnityEvent<(UMessage Message, USocket Socket)> onMessageReceived =
            new UnityEvent<(UMessage Message, USocket Socket)>();

        public IObservable<(UMessage Message, USocket Socket)> OnReceivedAsObservable()
        {
            return onMessageReceived.AsObservable();
        }

        protected void dispatchMessages()
        {
            if (messageDispatcherHandler != null)
                messageDispatcherHandler.Dispose();
            messageDispatcherHandler = Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
                    var _message = messageDispatcher.PopMessage();
                    while (_message != null)
                    {
                        onMessageReceived.Invoke((_message, this));
                        Managements.Event.Fire(_message);
                        _message = messageDispatcher.PopMessage();
                    }
                });
        }

        protected UNetMsgReceiver MsgReceiver = null;
        protected List<UNetMsgReceiver> messageReceivers = new List<UNetMsgReceiver>();

        protected void startNewReceiver(Socket InSocket)
        {
            if (MsgReceiver == null)
                return;
            UNetMsgReceiver _receiver = MsgReceiver.Clone(); //Activator.CreateInstance(MsgReceiver) as UMessageReceiver;
            messageReceivers.Add(_receiver);
            _receiver
                .Prepare(InSocket, messageDispatcher, this)
                .OnDisconnect(onReceiverDisconnected);
        }

        protected virtual void onReceiverDisconnected(string InKey, Socket InSocket)
        {
            var _infoArr = InKey.Split('_');
            var _ip = _infoArr[0];
            var _port = int.Parse(_infoArr[1]);
            // var _ip = (InSocket.RemoteEndPoint as IPEndPoint).Address.ToString ();
            // var _port = (InSocket.RemoteEndPoint as IPEndPoint).Port;
            var _disconnectedEvent = new UNetDisconnectedEvent
            {
                RemoteIP = _ip,
                RemotePort = _port
            };
            Managements.Event.Fire(_disconnectedEvent);
            onDisconnected.Invoke(_disconnectedEvent);
        }

        protected void destroySocket(ref Socket InSocket)
        {
            if (InSocket == null)
                return;

            try
            {
                if (InSocket.SocketType == SocketType.Stream)
                    InSocket.Shutdown(SocketShutdown.Both);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning(e.Message);
            }

            InSocket.Close();
            InSocket.Dispose();

            InSocket = null;
            UnityEngine.Debug.Log($"Socket {localEndPoint} closed.");
        }
    }
}
