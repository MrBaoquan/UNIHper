using Google.Protobuf;

namespace UNIHper.Network.ProtoBuf
{
    public static class NetworkExtension
    {
        public static void Send2TcpServer(
            this UNetManager netManager,
            IMessage InMessage,
            string InKey = ""
        )
        {
            byte[] _data = ProtoMessage.PackageMessage(InMessage);
            netManager.Send2TcpServer(_data, InKey);
        }
    }
}
