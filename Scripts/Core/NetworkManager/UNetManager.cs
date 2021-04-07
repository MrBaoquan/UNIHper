using System.Net;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Google.Protobuf;

namespace UNIHper
{

public class UNetManager : Singleton<UNetManager>,Manageable
{
    private Dictionary<string, UTcpClient> allTcpClients = new Dictionary<string, UTcpClient>();
    private Dictionary<string, UTcpServer> allTcpServers = new Dictionary<string, UTcpServer>();
    private Dictionary<string, UUdpClient> allUdpClients = new Dictionary<string, UUdpClient>();
    private Dictionary<string, UUdpClient> allUdpServers = new Dictionary<string, UUdpClient>();

    public bool IsConnected(int Index){
        if(allTcpClients.Count<=0) return false;
        return allTcpClients.Values.ToList()[Index].Connected;
    }

    public bool TcpClientFullConnected{
        get{
            if(allTcpClients.Count<=0) return false;
            return allTcpClients.Values.ToList().TrueForAll(_socket=>_socket.Connected);
        }
    }

    public Dictionary<string,UTcpClient> AllTcpClients{
        get{return allTcpClients;}
    }

    public int TcpClientCount{
        get{
            return allTcpClients.Count;
        }
    }

    public List<IPAddress> LocalAddressList{
        get{
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.ToList();
        }
    }

    public void Initialize(){
        // Managements.Event.Register<UNetConnectedEvent>(_=>{
        //     if(!allClients.ContainsKey(_.Key)){
        //         allClients.Add(_.Key,_.Socket);
        //         UnityEngine.Debug.LogFormat("添加socket {0}",_.Key);
        //     }
        // });

        // Managements.Event.Register<UNetDisconnectedEvent>(_=>{
        //     if(allClients.ContainsKey(_.Key)){
        //         allClients.Remove(_.Key);
        //         UnityEngine.Debug.LogFormat("移除socket {0}",_.Key);
        //     }
        // });
    }
    public void Uninitialize(){

    }

    public UTcpClient BuildTcpClient(string InRemoteIP,int InRemotePort, UNetMsgReceiver MessageReceiver=null)
    {
        string _key = string.Format("{0}_{1}", InRemoteIP, InRemotePort);
        if(allTcpClients.ContainsKey(_key)) return allTcpClients[_key];
        var _socket = new UTcpClient();
        _socket.SetReceiver(MessageReceiver)
            .SetRemoteEP(InRemoteIP,InRemotePort);
        allTcpClients.Add(_key, _socket);
        return _socket;
    }

    public void CloseTcpClient(string InKey){
        if(!allTcpClients.ContainsKey(InKey)) return;
        allTcpClients[InKey].Disconnect();
        allTcpClients.Remove(InKey);
    }

    public void CloseTcpServer(string InKey){
        if(!allTcpServers.ContainsKey(InKey)) return;
        allTcpServers[InKey].Dispose();
        allTcpServers.Remove(InKey);
    }

    public UTcpServer BuildTcpListener(string InLocalIP="127.0.0.1",int InLocalPort=6666, UNetMsgReceiver MessageReceiver=null)
    {
        string _key = string.Format("{0}_{1}", InLocalIP, InLocalPort);
        if(allTcpServers.ContainsKey(_key)) return allTcpServers[_key];

        var _socket = new UTcpServer(InLocalIP, InLocalPort, MessageReceiver);
        allTcpServers.Add(_key, _socket);
        return _socket;
    }

    public UUdpClient BuildUdpListener(string InIP, int InPort, UNetMsgReceiver messageReceiver=null){
        string _key = string.Format("{0}_{1}", InIP, InPort);
        if(allUdpServers.ContainsKey(_key)) return allUdpServers[_key];
        var _udpServer = new UUdpClient(InIP,InPort, messageReceiver);
        allUdpServers.Add(_key,_udpServer);
        return _udpServer;
    }

    public UUdpClient BuildUdpClient(string InRemoteIP, int InRemotePort, UNetMsgReceiver messageReceiver=null)
    {
        string _key = string.Format("{0}_{1}",InRemoteIP,InRemotePort);
        if(allUdpClients.ContainsKey(_key)) return allUdpClients[_key];
        var _udpClient = new UUdpClient();
        _udpClient.SetRemoteEP(InRemoteIP, InRemotePort)
            .SetReceiver(messageReceiver);
        allUdpClients.Add(_key,_udpClient);
        return _udpClient;
    }

    public void SendUdpBroadcast(byte[] InData, int InPort, string InKey="")
    {
        if(InKey==""){
            if(allUdpServers.Count<=0){
                UnityEngine.Debug.LogFormat("No udp connection exists.");
                return;
            }
            allUdpServers.Values.ToList().ForEach(_udpClient=>{
                _udpClient.Broadcast(InData,InPort);
            });
            return;
        }
        if(!allUdpServers.ContainsKey(InKey)){
            UnityEngine.Debug.LogWarningFormat("Connection key {0} not exists", InKey);
            return;
        }
        allUdpServers[InKey].Broadcast(InData,InPort);
    }

    public int Send2UdpClient(byte[] InData, string InIP, int InPort, string InKey="")
    {
        if(InKey==""){
            int _retCode = -1;
            allUdpServers.Values.ToList()
                .ForEach(_udpServer=>{
                    _retCode =_udpServer.SendTo(InData,InIP,InPort);
                });
            return _retCode;
        }
        if(!allUdpServers.ContainsKey(InKey)) return -1;
        return allUdpServers[InKey].SendTo(InData, InIP, InPort);
    }

    public void Send2UdpServer(byte[] InData, string InKey=""){
        if(InKey==""){
            allUdpClients.Values.ToList()
                .ForEach(_udpClient=>{
                    _udpClient.Send(InData);
                });
            return;
        }
        if(!allUdpClients.ContainsKey(InKey)) return;
        allUdpClients[InKey].Send(InData);
    }

    public void Send2TcpServer(byte[] InData, string InKey="")
    {
        if(InKey==""){
            allTcpClients.Values.ToList().ForEach(_socket=>{
                _socket.Send2Server(InData);
            });
            return;
        }
        if(!allTcpClients.ContainsKey(InKey)){
            UnityEngine.Debug.LogWarningFormat("Connection key {0} not exists", InKey);
            return;
        }; 
        allTcpClients[InKey].Send2Server(InData);
    }

    public void Send2TcpServer(IMessage InMessage, string InKey="")
    {
        byte[] _data = ProtoMessage.PackageMessage(InMessage);
        Send2TcpServer(_data,InKey);
    }

    public void Send2TcpClient(byte[] InData, string InLocalKey="", string InRemoteKey="")
    {
        if(InLocalKey==""){
            allTcpServers.Values.ToList().ForEach(_socket=>{
                _socket.Send2Client(InData,InRemoteKey);
            });
            return;
        }
        if(!allTcpServers.ContainsKey(InLocalKey)) return;
        allTcpServers[InLocalKey].Send2Client(InData, InRemoteKey);
    }

    public void DisposeAllTCPClients()
    {
        allTcpClients.Values.ToList().ForEach(_socket=>{
            _socket.Disconnect();
        });
    }

    public void DisposeAllUDPServers()
    {
        allUdpServers.Values.ToList().ForEach(_socket=>{
            _socket.Dispose();
        });
    }

    public void DisposeAllTCPServers(){
        allTcpServers.Values.ToList().ForEach(_socket=>{
            _socket.Dispose();
        });
    }

    public void DisposeAllUDPClients(){
        allUdpClients.Values.ToList().ForEach(_udpClient=>{
            _udpClient.Dispose();
        });
    }

    public void Dispose()
    {
        DisposeAllTCPClients();
        DisposeAllUDPServers();
        DisposeAllTCPServers();
    }

}

}
