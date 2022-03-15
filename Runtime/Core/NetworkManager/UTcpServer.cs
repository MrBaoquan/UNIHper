using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using UniRx;

namespace UNIHper
{
    
public class UTcpServer : USocket
{
    private Socket socket;
    public UTcpServer(UNetMsgReceiver InMessageReceiver=null):base(NetProtocol.Tcp){
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        SetReceiver(InMessageReceiver);
    }
    public UTcpServer(string InIP, int InPort, UNetMsgReceiver InMessageReceiver=null):this(InMessageReceiver){
        setLocalEndPoint(InIP,InPort);
    }

    public new UTcpServer SetReceiver(UNetMsgReceiver InMessageReceiver){
        base.SetReceiver(InMessageReceiver);
        return this;
    }

    int backLog = 10;
    public UTcpServer SetBacklog(int Backlog){
        backLog = Backlog;
        return this;
    }

    public override void Dispose(){
        base.Dispose();
        listenTaskHandler.Cancel();
        connections.Values.ToList()
            .ForEach(_socket=>{
                destroySocket(_socket);
            });
        destroySocket(socket);
    }

    public void Send2Client(byte[] InData, string InKey="")
    {
        if(InData==null) return;
        if(InKey==""){
            connections.Values.ToList()
                .ForEach(_connection=>{
                    _connection.Send(InData);
                });
            return;
        }
        if(!connections.ContainsKey(InKey))return;
        connections[InKey].Send(InData);
    }

    protected override void onLocalEndPointChanged(){
        //socket.Bind(localEndPoint);
    }

    CancellationTokenSource listenTaskHandler = new CancellationTokenSource();

    Dictionary<string,Socket> connections = new Dictionary<string, Socket>();
    public UTcpServer Listen()
    {
        socket.Bind(localEndPoint);
        socket.Listen(backLog);
        Task.Factory.StartNew(()=>{
            while(!listenTaskHandler.Token.IsCancellationRequested){
                try
                {
                    var _connection = socket.Accept();
                    var _rep = _connection.RemoteEndPoint as IPEndPoint;
                    var _key = string.Format("{0}_{1}",_rep.Address.ToString(),_rep.Port);
                    if(connections.ContainsKey(_key)) continue;
                    connections.Add(_key,_connection);
                    startNewReceiver(_connection);

                    Observable.Start(()=>{
                        onClientConnected(_connection);
                    },Scheduler.MainThread).Subscribe(_=>{

                    });   
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning(e.Message);
                }
            }
        },listenTaskHandler.Token);
        dispatchMessages();
        return this;
    }

    private void onClientConnected(Socket InSocket)
    {
        var _rep = InSocket.RemoteEndPoint as IPEndPoint;
        var _key = string.Format("{0}_{1}",_rep.Address.ToString(),_rep.Port);
        Managements.Event.Fire(new UNetConnectedEvent{RemoteIP=_rep.Address.ToString(),RemotePort=_rep.Port});
    }
    
    protected override void onReceiverDisconnected(string InKey, Socket InSocket)
    {
        if(!connections.ContainsKey(InKey)) return;
        base.onReceiverDisconnected(InKey,InSocket);
        connections.Remove(InKey);
    }

}


}