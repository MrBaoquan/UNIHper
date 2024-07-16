using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UNIHper
{
    public abstract class USPMsgReceiver
    {
        protected System.IO.Ports.SerialPort serialPort = null;
        private Queue<SPMessage> messages = null;

        public void Prepare(System.IO.Ports.SerialPort InSerialPort, Queue<SPMessage> InMessages)
        {
            serialPort = InSerialPort;
            messages = InMessages;
            OnConnected();
        }

        CancellationTokenSource cancellationToken = null;

        public void OnConnected()
        {
            cancellationToken = new CancellationTokenSource();
            Task.Run(
                () =>
                {
                    while (!cancellationToken.Token.IsCancellationRequested)
                    {
                        OnFlushMessage();
                    }
                },
                cancellationToken.Token
            );
        }

        public virtual void OnFlushMessage() { }

        protected void PushMessage(SPMessage InMessage)
        {
            InMessage.PortName = serialPort.PortName;
            messages.Enqueue(InMessage);
        }

        public void Dispose()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
                cancellationToken.Dispose();
            }
        }
    }
}
