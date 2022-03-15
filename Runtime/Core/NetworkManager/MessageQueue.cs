using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Google.Protobuf;

namespace UNIHper
{


public class UMessage : UEvent
{
    public string LocalIP = string.Empty;
    public int LocalPort = -1;
    public string RemoteIP = string.Empty;
    public int RemotePort = -1;
    public byte[] RawData = null;

    public string RemoteKey{
        get{
            return string.Format("{0}_{1}", RemoteIP, RemotePort);
        }
    }

    public string LocalKey{
        get{
            return string.Format("{0}_{1}", LocalIP, LocalPort);
        }
    }
}

public enum NetProtocol
{
    Unknown = -1,
    Tcp = 6,
    Udp = 17
}

public class NetMessage : UEvent
{
    public UMessage Message;
    public NetProtocol Protocol = NetProtocol.Tcp;
}

public class MessageQueeue
{

    private Queue<UMessage> messages = new Queue<UMessage>();

    public void PushMessage(UMessage InMessage)
    {
        lock (this)
        {
            messages.Enqueue(InMessage);
        }
    }

    public UMessage PopMessage()
    {
        lock(this){
            if(messages.Count<=0){
                return null;
            }
            return messages.Dequeue();
        }
    }
}


}
