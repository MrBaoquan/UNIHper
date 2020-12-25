using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO.Ports;

namespace UHelper{
    

public abstract class USPMsgReceiver
{
    protected SerialPort serialPort = null;
    private Queue<SPMessage> messages = null;
    public void Prepare(SerialPort InSerialPort, Queue<SPMessage> InMessages){
        serialPort = InSerialPort;
        messages = InMessages;
        OnConnected();
    }

    CancellationTokenSource cancellationToken = null;
    Task flushTask = null;
    public void OnConnected(){
        cancellationToken = new CancellationTokenSource();
        flushTask = Task.Factory.StartNew(()=>{
            while(!cancellationToken.Token.IsCancellationRequested){
                OnFlushMessage();
            }
        },cancellationToken.Token);
    }

    public virtual void OnFlushMessage(){}

    protected void PushMessage(SPMessage InMessage)
    {
        InMessage.PortName = serialPort.PortName;
        messages.Enqueue(InMessage);
    }

    public void Dispose(){
        if(cancellationToken!=null)
            cancellationToken.Cancel();
    }

}


}