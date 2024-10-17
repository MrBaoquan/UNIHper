using System;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using UniRx;

namespace UNIHper.Network
{
    public class UUdpClient : USocket
    {
        private Socket udpClient;

        public UUdpClient()
            : base(NetProtocol.Udp)
        {
            udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
        }

        public UUdpClient(string InLocalIP, int InLocalPort, UNetMsgReceiver messageReceiver)
            : this()
        {
            SetReceiver(messageReceiver);
            SetLocalEP(InLocalIP, InLocalPort);
        }

        public UUdpClient SetLocalEP(string InLocalIP, int InLocalPort)
        {
            setLocalEndPoint(InLocalIP, InLocalPort);
            return this;
        }

        public UUdpClient SetRemoteEP(string InRemoteIP, int InRemotePort)
        {
            setRemoteEndPoint(InRemoteIP, InRemotePort);
            return this;
        }

        public UUdpClient Listen()
        {
            startListener();
            return this;
        }

        public UUdpClient Connect()
        {
            startListener();
            return this;
        }

        public UUdpClient EnableBroadcast()
        {
            if (localEndPoint == null)
            {
                UnityEngine.Debug.LogWarning("local endpoint not set yet.");
                return this;
            }
            udpClient.EnableBroadcast = true;
            udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            return this;
        }

        public new UUdpClient SetReceiver(UNetMsgReceiver InMessageReceiver)
        {
            base.SetReceiver(InMessageReceiver);
            return this;
        }

        public override void Dispose()
        {
            base.Dispose();
            udpClientTokenSource.Cancel();
        }

        CancellationTokenSource udpClientTokenSource = null;

        protected override void onLocalEndPointChanged()
        {
            udpClient.Bind(localEndPoint);
        }

        private void startListener()
        {
            if (udpClientTokenSource != null)
            {
                udpClientTokenSource.Cancel();
            }
            udpClientTokenSource = new CancellationTokenSource();
            startNewReceiver(udpClient);
            dispatchMessages();
        }

        protected override void onRemoteEndPointChanged()
        {
            udpClient.Connect(remoteEndPoint);
        }

        public int SendTo(byte[] InData, string InIP, int InPort)
        {
            return SendTo(InData, new IPEndPoint(IPAddress.Parse(InIP), InPort));
        }

        public int SendTo(string InData, string InIP, int InPort)
        {
            return SendTo(
                System.Text.Encoding.UTF8.GetBytes(InData),
                new IPEndPoint(IPAddress.Parse(InIP), InPort)
            );
        }

        public int SendTo(byte[] InData, EndPoint InRemote)
        {
            if (udpClient == null || InData == null)
                return -1;
            return udpClient.SendTo(InData, InData.Length, SocketFlags.None, InRemote);
        }

        public void Send(byte[] InData)
        {
            udpClient.Send(InData);
        }

        public int Broadcast(byte[] InData, int InPort)
        {
            int _retCode = -1;
            if (udpClient == null || InData == null)
                return _retCode;
            if (!udpClient.EnableBroadcast)
            {
                EnableBroadcast();
            }
            try
            {
                _retCode = udpClient.SendTo(
                    InData,
                    InData.Length,
                    SocketFlags.None,
                    new IPEndPoint(IPAddress.Broadcast, InPort)
                );
            }
            catch (System.Exception e)
            {
                _retCode = -1;
                UnityEngine.Debug.LogWarning(e.Message);
            }
            return _retCode;
        }
    }
}
