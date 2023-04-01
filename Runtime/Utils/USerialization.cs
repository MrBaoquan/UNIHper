using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using YamlDotNet.Serialization;

namespace UNIHper
{
    public static class USerialization
    {
        // public static void SerializeXML (object item, string path) {
        //     XmlSerializer serializer = new XmlSerializer (item.GetType ());
        //     StreamWriter writer = new StreamWriter (path);
        //     serializer.Serialize (writer.BaseStream, item);
        //     writer.Close ();
        // }

        public static void SerializeYAML(object item, string path)
        {
            StreamWriter writer = new StreamWriter(path);
            var serializer = new SerializerBuilder().Build();
            serializer.Serialize(writer, item);
            writer.Close();
        }

        public static T DeserializeYAML<T>(string path)
            where T : class
        {
            if (!File.Exists(path))
                return default(T);

            var deserializer = new DeserializerBuilder().Build();

            using (StreamReader reader = new StreamReader(path))
            {
                try
                {
                    return deserializer.Deserialize<T>(reader);
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    throw;
                }
            }
        }
    }
}
