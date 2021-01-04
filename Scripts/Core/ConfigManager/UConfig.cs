using System;
using System.Xml.Serialization;

namespace UHelper
{

public enum UAppPath
{
    ProjectDir,
    StreamingDir,
    PersistentDir
}

[AttributeUsage(AttributeTargets.Class,Inherited=true,AllowMultiple=false)]
public class SerializedAt : Attribute
{
    public UAppPath SaveTo = UAppPath.PersistentDir;
    public SerializedAt(UAppPath InPath){
        SaveTo = InPath;
    }
}

[SerializedAt(UAppPath.PersistentDir)]
public class UConfig
{
    [XmlIgnore]
    protected string __path;

    [XmlIgnore]
    public string __Path{
        get{
            return __path;
        }
    }
}


}
