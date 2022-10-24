#if NET_STANDARD_2_1
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace UNIHper {

    public abstract class USPMsgReceiver {
        protected SerialPort serialPort = null;
        private Queue<SPMessage> messages = null;
        public void Prepare (SerialPort InSerialPort, Queue<SPMessage> InMessages) {
            serialPort = InSerialPort;
            messages = InMessages;
            OnConnected ();
        }

        CancellationTokenSource cancellationToken = null;
        public void OnConnected () {
            cancellationToken = new CancellationTokenSource ();
            Task.Run (() => {
                while (!cancellationToken.Token.IsCancellationRequested) {
                    OnFlushMessage ();
                }
            }, cancellationToken.Token);
        }

        public virtual void OnFlushMessage () { }

        protected void PushMessage (SPMessage InMessage) {
            InMessage.PortName = serialPort.PortName;
            messages.Enqueue (InMessage);
        }

        public void Dispose () {
            if (cancellationToken != null) {
                cancellationToken.Cancel ();
                cancellationToken.Dispose ();
            }

        }

    }

}
#endif