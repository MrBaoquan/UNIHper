using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.Net;
using UniRx;

namespace UNIHper
{
    
public class UNetConnectedEvent:UEvent{
    public string RemoteIP = string.Empty;
    public int RemotePort = -1;
    public string Key{
        get{
            return string.Format("{0}_{1}", RemoteIP, RemotePort);
        }
    }
}
public class UNetDisconnectedEvent:UEvent{
    public string RemoteIP = string.Empty;
    public int RemotePort = -1;
    public string Key{
        get{
            return string.Format("{0}_{1}", RemoteIP, RemotePort);
        }
    }
}

public abstract class USocket : IDisposable
{
    const string InValidIP = "Invalid IP";
    const int InValidPort = -1;

    public NetProtocol Protocol {
        get{return protocol;}
    }
    protected NetProtocol protocol = NetProtocol.Unknown;
    protected MessageQueeue messageDispatcher = new MessageQueeue();
    protected IPEndPoint localEndPoint;
    public IPEndPoint LocalEndPoint{
        get{return localEndPoint;}
        set{
            localEndPoint = value;
            onLocalEndPointChanged();
        }
    }
    public string LocalIP{
        get{
            if(localEndPoint==null) return InValidIP;
            return localEndPoint.Address.ToString();
        }
    }
    public int LocalPort{
        get{
            if(LocalEndPoint==null) return InValidPort;
            return localEndPoint.Port;
        }
    }

    public string RemoteIP{
        get{
            if(RemoteEndPoint==null) return InValidIP;
            return RemoteEndPoint.Address.ToString();
        }
    }
    public int RemotePort{
        get{
            if(RemoteEndPoint==null) return InValidPort;
            return RemoteEndPoint.Port;
        }
    }

    
    protected IPEndPoint remoteEndPoint;
    public IPEndPoint RemoteEndPoint{
        get{return remoteEndPoint;}
        set{
            remoteEndPoint = value;
            onRemoteEndPointChanged();
        }
   }

    protected USocket(NetProtocol InProtocol){
        protocol = InProtocol;
    }

    public string Key{
        get{
            if(remoteEndPoint==null) return string.Empty;
            return string.Format("{0}_{1}",remoteEndPoint.Address.ToString(),remoteEndPoint.Port);
        }
    }

    public virtual void Dispose()
    {
        if(messageDispatcherHandler!=null){
            messageDispatcherHandler.Dispose();
        }

        messageReceivers.ForEach(_receiver=>{
            if(_receiver!=null)
                _receiver.Dispose();
        });
    }

    public USocket SetReceiver(UNetMsgReceiver InMessageReceiver){
        MsgReceiver = InMessageReceiver;
        return this;
    }

    public USocket OnReceived(Action<NetMessage, USocket> InMessageHandler)
    {
        onMessageReceivedHandler = InMessageHandler;
        return this;
    }

    protected void setLocalEndPoint(string InIP, int InPort){
        LocalEndPoint = new IPEndPoint(IPAddress.Parse(InIP),InPort);
    }

    protected void setLocalEndPoint(IPEndPoint InLocalEndPoint){
        LocalEndPoint = InLocalEndPoint;
    }

    protected void setRemoteEndPoint(string InIP, int InPort){
        RemoteEndPoint = new IPEndPoint(IPAddress.Parse(InIP),InPort);
    }

    protected virtual void onLocalEndPointChanged(){

    }

    protected virtual void onRemoteEndPointChanged(){

    }

    IDisposable messageDispatcherHandler = null;

    Action<NetMessage,USocket> onMessageReceivedHandler = null;
    protected void dispatchMessages(){
        if(messageDispatcherHandler!=null) messageDispatcherHandler.Dispose();
        messageDispatcherHandler = Observable.EveryUpdate().Subscribe(_=>{
            var _message = messageDispatcher.PopMessage();
            while(_message!=null){
                var _netMessage = new NetMessage{Message = _message, Protocol=protocol};
                if(onMessageReceivedHandler!=null){
                    onMessageReceivedHandler(_netMessage,this);
                    //onMessageReceivedHandler.DynamicInvoke(new object[]{_netMessage, this});
                }
                Managements.Event.Fire(_netMessage);
                _message = messageDispatcher.PopMessage();
            }
        });
    }

    protected UNetMsgReceiver MsgReceiver = null;
    protected List<UNetMsgReceiver> messageReceivers = new List<UNetMsgReceiver>();
    protected void startNewReceiver(Socket InSocket){
        if(MsgReceiver==null) return;
        UNetMsgReceiver _receiver =  MsgReceiver.Clone(); //Activator.CreateInstance(MsgReceiver) as UMessageReceiver;
        messageReceivers.Add(_receiver);
        _receiver.Prepare(InSocket, messageDispatcher, this)
            .OnDisconnect(onReceiverDisconnected);
    }

    protected virtual void onReceiverDisconnected(string InKey, Socket InSocket){
        Managements.Event.Fire(new UNetDisconnectedEvent{RemoteIP=RemoteIP,RemotePort=RemotePort});
    }

    protected void destroySocket(Socket InSocket)
    {
        if(InSocket==null) return;
        try
        {
            if(InSocket.Connected){
                InSocket.Shutdown(SocketShutdown.Both);
                InSocket.Disconnect(false);
            }
            
            InSocket.Close();
            InSocket.Dispose();     
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning(e.Message);
        }
        InSocket = null;
    }

}


}