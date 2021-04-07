#if NET_4_6
using System.Linq;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

using UniRx;

namespace UNIHper
{


public class SPMessage : UEvent
{
    public byte[] RawData = null;
    public string PortName = string.Empty;
}

public class SerialPortManager : Singleton<SerialPortManager>, Manageable
{
    private Dictionary<string,USerialPort> serialPorts = new Dictionary<string, USerialPort>();
    public USerialPort BuildConnect(string InPortName, int InBaudRate=9600, USPMsgReceiver InReceiver=null)
    {
        if(serialPorts.ContainsKey(InPortName)){
            return serialPorts[InPortName];
        }
        var _newSerialPort = new USerialPort(InPortName, InBaudRate, InReceiver);
        serialPorts.Add(InPortName, _newSerialPort);
        return _newSerialPort;
    }


    public void Initialize(){}
    public void Uninitialize(){}

    public void Dispose(){
        serialPorts.Values.ToList()
            .ForEach(_=>{
                _.Dispose();
            });
    }

}


}
#endif