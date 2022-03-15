using System.Text.RegularExpressions;
using System;

namespace DNHper
{

public class Machine{
    public static string CPUID{
        get{
            var _res = WinAPI.CALLCMD("/c wmic cpu get ProcessorId");
            return Regex.Split(_res,"\r\n|\r|\n")[2];
        }
    }
}

}