using System.Text;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Ports;
namespace UHelper
{

public class SPLineMessage : SPMessage
{
    public string Content = string.Empty;
}

public class SPStringLineReceiver : USPMsgReceiver
{
    public override void OnFlushMessage()
    {
        try
        {
            string _result =serialPort.ReadLine();
            PushMessage(new SPLineMessage{RawData=Encoding.UTF8.GetBytes(_result), Content=_result});
        }
        catch (System.Exception)
        {
            //UnityEngine.Debug.Log(_result.Length);
            //UnityEngine.Debug.Log(e.Message);
        }
    }


}



}