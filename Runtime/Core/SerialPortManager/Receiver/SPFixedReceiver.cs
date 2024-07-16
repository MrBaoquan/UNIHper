using System.Threading.Tasks;

namespace UNIHper
{
    public class SPFixedReceiver : USPMsgReceiver
    {
        public int FixedLength = 0;
        public byte[] Header = new byte[] { };

        public override void OnFlushMessage()
        {
            if (FixedLength <= 0)
                return;
            if (serialPort.BytesToRead >= FixedLength)
            {
                try
                {
                    var _buffer = new byte[FixedLength];
                    var _rawData = serialPort.Read(_buffer, 0, FixedLength);
                    PushMessage(new SPMessage { RawData = _buffer });
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.Log(e.Message);
                }
            }
        }
    }
}
