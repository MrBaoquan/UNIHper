using System;
using System.Text;
using Google.Protobuf;
using System.Net.Sockets;

/// <summary>
/// Google ProtoBuf 消息封装类
/// </summary>
namespace UNIHper.Network.ProtoBuf
{
    public class ProtoMsgReceiver : UNetMsgReceiver
    {
        public class PBMessage : UMessage
        {
            public string From = string.Empty;
            public IMessage Data;
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public byte[] RawTypeSize = new byte[32];
            public byte[] RawDataSize = new byte[32];

            public byte[] RawTypeName = null;
            public string TypeName;
            public byte[] RawData = null;

            // 当前读取到第几步
            public int Step = -1;
        }

        public override void OnConnected()
        {
            StateObject state = new StateObject();
            socket.BeginReceive(
                state.RawTypeSize,
                0,
                32,
                0,
                new AsyncCallback(ReadCallback),
                state
            );
        }

        public void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            var handler = socket;

            int bytesRead = handler.EndReceive(ar);
            if (state.Step == -1 && bytesRead == 32)
            {
                state.Step = 0;
                var _typeSize = BitConverter.ToInt32(state.RawTypeSize, 0);
                state.RawTypeName = new byte[_typeSize];
                handler.BeginReceive(
                    state.RawDataSize,
                    0,
                    32,
                    0,
                    new AsyncCallback(ReadCallback),
                    state
                );
                return;
            }

            if (state.Step == 0 && bytesRead == 32)
            {
                state.Step = 1;
                int _dataSize = BitConverter.ToInt32(state.RawDataSize, 0);
                state.RawData = new byte[_dataSize];
                handler.BeginReceive(
                    state.RawTypeName,
                    0,
                    state.RawTypeName.Length,
                    0,
                    new AsyncCallback(ReadCallback),
                    state
                );
                return;
            }

            if (state.Step == 1)
            {
                state.Step = 2;
                var _typeName = System.Text.Encoding.Default
                    .GetString(state.RawTypeName)
                    .TrimEnd('\0');
                state.TypeName = _typeName;
                handler.BeginReceive(
                    state.RawData,
                    0,
                    state.RawData.Length,
                    0,
                    new AsyncCallback(ReadCallback),
                    state
                );
                return;
            }

            if (state.Step == 2)
            {
                state.Step = -1;
                IMessage _message = state.RawData.DeserializeFromTypeString(state.TypeName);
                PBMessage _recvMessage = new PBMessage();
                _recvMessage.From = string.Empty;
                _recvMessage.Data = _message;
                pushMessage(_recvMessage);
                handler.BeginReceive(
                    state.RawTypeSize,
                    0,
                    32,
                    0,
                    new AsyncCallback(ReadCallback),
                    state
                );
            }
        }
    }
}
