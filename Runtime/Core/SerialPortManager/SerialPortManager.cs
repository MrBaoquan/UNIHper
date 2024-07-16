using System.Collections.Generic;
using System.Linq;
using DNHper;

namespace UNIHper
{
    using UniRx;

    public class SPMessage : UEvent
    {
        public byte[] RawData = null;
        public string PortName = string.Empty;
    }

    public class SerialPortManager : Singleton<SerialPortManager>
    {
        private Dictionary<string, USerialPort> serialPorts = new Dictionary<string, USerialPort>();

        public USerialPort BuildConnect(
            string InPortName,
            int InBaudRate = 9600,
            USPMsgReceiver InReceiver = null
        )
        {
            if (serialPorts.ContainsKey(InPortName))
            {
                return serialPorts[InPortName];
            }
            var _newSerialPort = new USerialPort(InPortName, InBaudRate, InReceiver);
            serialPorts.Add(InPortName, _newSerialPort);
            return _newSerialPort;
        }

        public void Send(string InPortName, byte[] InData)
        {
            if (!serialPorts.ContainsKey(InPortName))
            {
                return;
            }
            serialPorts[InPortName].Write(InData);
        }

        public void Dispose()
        {
            serialPorts.Values
                .ToList()
                .ForEach(_ =>
                {
                    _.Dispose();
                });
            serialPorts.Clear();
        }
    }
}
