using System.IO;
using YamlDotNet.Serialization;
using Newtonsoft.Json;

namespace UNIHper
{
    public static class USerialization
    {
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

        public static void SerializeJSON(object item, string path)
        {
            var _content = JsonConvert.SerializeObject(item, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.Write(_content);
            }
        }

        public static T DeserializeJSON<T>(string path)
            where T : class
        {
            if (!File.Exists(path))
                return default(T);

            using (StreamReader reader = new StreamReader(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
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
