using System.Collections.Generic;
using Newtonsoft.Json;

namespace UNIHper.Network
{
    public class UMessage : UEvent
    {
        [JsonIgnore]
        public string LocalIP = string.Empty;

        [JsonIgnore]
        public int LocalPort = -1;

        [JsonIgnore]
        public string RemoteIP = string.Empty;

        [JsonIgnore]
        public int RemotePort = -1;

        [JsonIgnore]
        public byte[] RawData = null;

        [JsonIgnore]
        public string RemoteKey
        {
            get { return string.Format("{0}_{1}", RemoteIP, RemotePort); }
        }

        [JsonIgnore]
        public string LocalKey
        {
            get { return string.Format("{0}_{1}", LocalIP, LocalPort); }
        }
    }

    public enum NetProtocol
    {
        Unknown = -1,
        Tcp = 6,
        Udp = 17
    }

    // public class NetMessage : UEvent
    // {
    //     public UMessage Message;
    //     // public NetProtocol Protocol = NetProtocol.Tcp;
    // }

    public class MessageQueue
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
            lock (this)
            {
                if (messages.Count <= 0)
                {
                    return null;
                }
                return messages.Dequeue();
            }
        }
    }
}
