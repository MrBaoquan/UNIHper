using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;

namespace DNHper
{
public static class USerialization
{
    public static void SerializeXML(object item, string path)
    {
        XmlSerializer serializer = new XmlSerializer(item.GetType());
        StreamWriter writer = new StreamWriter(path);
        serializer.Serialize(writer.BaseStream, item);
        writer.Close();
    }
    public static T DeserializeXML<T>(string path) where T : class
    {
        if(!File.Exists(path)) return default(T);
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        StreamReader reader = new StreamReader(path);
        try
        {
            T deserialized = (T)serializer.Deserialize(reader.BaseStream);    
            reader.Close();
            return deserialized;
        }
        catch (System.Exception)
        {
            reader.Close();
            throw;
        }
    }

    public static void SerializeObject(object Target, string InPath)
    {
        using(FileStream fileStream=new FileStream(InPath,FileMode.OpenOrCreate)){
            BinaryFormatter _formatter = new BinaryFormatter();
            try
            {
                _formatter.Serialize(fileStream, Target);
            }
            catch (SerializationException _exc)
            {
                NLogger.Error(_exc);
            }
        }
    }

    public static T DeserializeObject<T>(string InPath) where T :class
    {
        if(!File.Exists(InPath)){
            NLogger.Warn("File {0} not exists.", InPath);
            return default(T);
        }
        using(FileStream _fileStream = new FileStream(InPath,FileMode.Open)){
            try
            {
                BinaryFormatter _formatter = new BinaryFormatter();
                return _formatter.Deserialize(_fileStream) as T;
            }
            catch (SerializationException _exc)
            {
               NLogger.Error(_exc);
               return default(T);
            }
        }
    }
}

}
