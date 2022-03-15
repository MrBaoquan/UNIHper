#if NET_4_6
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;
using System.IO.Ports;
using UniRx;

using UNIHper;

namespace UNIHper
{

public class USerialPort
{
    SerialPort serialPort = new SerialPort();

    public int BytesToRead {
        get{
            return serialPort.BytesToRead;
        }
    }
    public string PortName{
        get{return serialPort.PortName;}
    }
    
    USPMsgReceiver msgReceiver = null;
    public USerialPort(string InPortName, int InBaudRate, USPMsgReceiver InReceiver=null){
        msgReceiver = InReceiver;
        serialPort.PortName = InPortName;
        serialPort.BaudRate = InBaudRate;

        serialPort.Parity = Parity.None;
        serialPort.ReadTimeout = 500;
        serialPort.WriteTimeout = 500;
        serialPort.DataBits = 8;
        serialPort.StopBits = StopBits.One;
    }

    public USerialPort SetReadTimeout(int ReadTimeout){
        serialPort.ReadTimeout = ReadTimeout;
        return this;
    }

    public USerialPort SetWriteTimeout(int WriteTimeout){
        serialPort.WriteTimeout = WriteTimeout;
        return this;
    }

    Action<SPMessage> OnReceiveHandler = null;
    public void OnReceive(Action<SPMessage> InHandler)
    {
        OnReceiveHandler = InHandler;
    }


    private Queue<SPMessage> messages = new Queue<SPMessage>();
    public USerialPort Open()
    {
        try
        {
            serialPort.Open();
            if(msgReceiver!=null){
                msgReceiver.Prepare(serialPort,messages);
                dispatchMessages();
            }
        }
        catch (System.Exception)
        {
            Debug.LogWarningFormat("open {0} failed", serialPort.PortName);
        }
        return this;
    }

    IDisposable messageDispatcherHandler = null;
    private void dispatchMessages(){
        messageDispatcherHandler = Observable.EveryUpdate().Subscribe(_=>{
            while(messages.Count>0){
                var _message = messages.Dequeue();
                if(OnReceiveHandler!=null)
                    OnReceiveHandler(_message);
                Managements.Event.Fire(_message);
            }
        });
    }

    private byte[] tempBuffer = new byte[4096];
    public byte[] Read(int count, int offset=0){
        int _readed = serialPort.Read(tempBuffer,offset,count);
        var _result = tempBuffer.Slice(offset,offset + _readed);
        return _result;
    }

    public void Write(byte[] InData){
        if(!serialPort.IsOpen){
            Debug.LogWarning("serial port has not opened yet.");
            return;
        }
        serialPort.Write(InData,0, InData.Length);
    }

    public void Dispose(){
        if(msgReceiver!=null)
            msgReceiver.Dispose();
        serialPort.Close();
    }

}


}

#endif