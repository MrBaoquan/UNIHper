using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace UNIHper.Editor
{
    public static class CfgUtil<T>
        where T : new()
    {
        public static T loadJsonConfig(string filePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
            }
            catch
            {
                return new T();
            }
        }

        public static void saveJsonConfig(string filePath, T config)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        public static T LoadXmlConfig(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new T();
                }
                return DNHper.USerialization.DeserializeXML<T>(filePath);
            }
            catch
            {
                return new T();
            }
        }

        public static void SaveXmlConfig(string filePath, T config)
        {
            DNHper.USerialization.SerializeXML(config, filePath);
        }
    }

    public static class AssemblyCfgUtil
    {
        [System.Serializable]
        public class ConfigData : List<string> { }

        private static string ConfigFilePath =>
            "Assets/Resources/" + UNIHperSettings.AssemblyConfigPath + ".json";

        public static void AddAssembly(string assemblyName)
        {
            string filePath = ConfigFilePath;
            var config = CfgUtil<ConfigData>.loadJsonConfig(filePath);

            if (config.Contains(assemblyName))
                return;

            config.Add(assemblyName);
            CfgUtil<ConfigData>.saveJsonConfig(filePath, config);
        }

        public static void RemoveAssembly(string assemblyName)
        {
            string filePath = ConfigFilePath;
            var config = CfgUtil<ConfigData>.loadJsonConfig(filePath);

            if (!config.Contains(assemblyName))
                return;

            config.Remove(assemblyName);
            CfgUtil<ConfigData>.saveJsonConfig(filePath, config);
        }
    }

    /// <summary>
    /// 资源配置类实用脚本
    /// </summary>
    public static class ResCfgUtil
    {
        [System.Serializable]
        public class ConfigData : Dictionary<string, List<ResourceItem>> { }

        [System.Serializable]
        public class ResourceItem
        {
            public string driver = "Resources";
            public string type;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string path;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string label;

            public override bool Equals(object obj)
            {
                if (obj is ResourceItem other)
                {
                    return this.driver == other.driver
                        && this.type == other.type
                        && (this.label == other.label || this.path == other.path);
                }
                return false;
            }

            public override int GetHashCode()
            {
                int hashDriver = driver?.GetHashCode() ?? 0;
                int hashType = type?.GetHashCode() ?? 0;
                int hashLabel = label?.GetHashCode() ?? 0;
                int hashPath = path?.GetHashCode() ?? 0;

                return hashDriver ^ hashType ^ (hashLabel | hashPath);
            }
        }

        private static string ConfigFilePath =>
            "Assets/Resources/" + UNIHperSettings.ResourceConfigPath + ".json";

        public static void AddPersistenceItem(ResourceItem newItem)
        {
            AddItem("Persistence", newItem);
        }

        public static void AddItem(string sceneName, ResourceItem newItem)
        {
            string filePath = ConfigFilePath;
            var config = CfgUtil<ConfigData>.loadJsonConfig(filePath);

            if (!config.ContainsKey(sceneName))
            {
                config[sceneName] = new List<ResourceItem>();
            }

            if (!config[sceneName].Exists(item => item.Equals(newItem)))
            {
                config[sceneName].Add(newItem);
                CfgUtil<ConfigData>.saveJsonConfig(filePath, config);
            }
        }

        public static void RemoveItem(string sceneName, System.Predicate<ResourceItem> predicate)
        {
            string filePath = ConfigFilePath;
            var config = CfgUtil<ConfigData>.loadJsonConfig(filePath);

            if (config.ContainsKey(sceneName))
            {
                config[sceneName].RemoveAll(predicate);

                if (config[sceneName].Count == 0)
                {
                    config.Remove(sceneName);
                }

                CfgUtil<ConfigData>.saveJsonConfig(filePath, config);
            }
            else
            {
                Debug.LogWarning($"场景 {sceneName} 不存在于配置中。");
            }
        }
    }
}
