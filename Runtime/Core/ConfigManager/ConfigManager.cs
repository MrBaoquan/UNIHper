using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DNHper;
using UnityEngine;

namespace UNIHper
{
    public class ConfigManager : Singleton<ConfigManager>
    {
        private Dictionary<string, UConfig> configs = new Dictionary<string, UConfig>();

        //private ConfigDriver driverMode = ConfigDriver.XML;
        private const string configDir = "Configs";

        private string suffix(ConfigDriver driverMode)
        {
            if (driverMode == ConfigDriver.YAML)
            {
                return ".yaml";
            }
            else if (driverMode == ConfigDriver.JSON)
            {
                return ".json";
            }
            return ".xml";
        }

        internal async Task Initialize()
        {
            UNIHperLogger.Log("ConfigManager Initializing ...");
            this.loadConfig();

            Type[] _configClasses = AssemblyConfig.GetSubClasses(typeof(UConfig)).ToArray(); // UReflection.SubClasses(typeof(UConfig));

            foreach (var _configClass in _configClasses)
            {
                UConfig _configInstance = Activator.CreateInstance(_configClass) as UConfig;

                // 配置文件默认保存在 %userprofile%\AppData\LocalLow\<companyname>\<productname>

                var _attributes = Attribute.GetCustomAttributes(_configClass);
                var _serializeAtAttr = _attributes.Where(_attr => _attr is SerializedAt).First();
                var _serializeWithAttr = _attributes.Where(_attr => _attr is SerializeWith).First();

                string _configDir = Path.Combine(Application.persistentDataPath, configDir);

                if (_serializeAtAttr != null)
                {
                    var _serializedAttr = (_serializeAtAttr as SerializedAt);
                    if (_serializedAttr.RootDir == AppPath.StreamingDir)
                    {
                        _configDir = Path.Combine(
                            Application.streamingAssetsPath,
                            _serializedAttr.SubDir
                        );
                    }
                    else if (_serializedAttr.RootDir == AppPath.PersistentDir)
                    {
                        _configDir = Path.Combine(
                            Application.persistentDataPath,
                            _serializedAttr.SubDir
                        );
                    }
                }

                ConfigDriver _driverMode = ConfigDriver.XML;
                if (_serializeWithAttr is not null)
                {
                    var _serializeWith = (_serializeWithAttr as SerializeWith);
                    _driverMode = _serializeWith.Mode;
                }

                if (!Directory.Exists(_configDir))
                {
                    Directory.CreateDirectory(_configDir);
                }

                string _path = Path.Combine(
                    _configDir,
                    _configClass.Name + this.suffix(_driverMode)
                );
                if (!File.Exists(_path))
                {
                    this.serializeConfig(_configInstance, _path, _driverMode);
                }
                else
                {
                    _configInstance = this.deserializeConfig(_configClass, _path, _driverMode);
                }

                UReflection.SetPrivateField(_configInstance, "__path", _path);
                UReflection.SetPrivateField(_configInstance, "__driver", (int)_driverMode);

                this.configs.Add(_configClass.Name, _configInstance);
            }
            this.configs.Values
                .ToList()
                .ForEach(_config =>
                {
                    UReflection.CallPrivateMethod(_config, "OnLoaded");
                });
            await Task.CompletedTask;
        }

        internal void CleanUp()
        {
            this.configs.Clear();
        }

        private void loadConfig() { }

        public void SerializeAll()
        {
            this.configs.Values
                .ToList()
                .ForEach(_config =>
                {
                    this.serializeConfig(_config, _config.FilePath);
                });
        }

        private void serializeConfig(
            object target,
            string path,
            ConfigDriver driver = ConfigDriver.XML
        )
        {
            UReflection.CallPrivateMethod(target, "OnSerializing");
            if (driver == ConfigDriver.YAML)
            {
                USerialization.SerializeYAML(target, path);
                UReflection.CallPrivateMethod(target, "OnSerialized");
                return;
            }
            else if (driver == ConfigDriver.JSON)
            {
                USerialization.SerializeJSON(target, path);
                UReflection.CallPrivateMethod(target, "OnSerialized");
                return;
            }
            UReflection.CallPrivateMethod(target, "OnSerialized");
            DNHper.USerialization.SerializeXML(target, path);
        }

        private UConfig deserializeConfig(
            Type configClass,
            string path,
            ConfigDriver driver = ConfigDriver.XML
        )
        {
            if (driver == ConfigDriver.YAML)
            {
                var _methodYAML = typeof(USerialization)
                    .GetMethod("DeserializeYAML")
                    .MakeGenericMethod(new Type[] { configClass });
                return _methodYAML.Invoke(null, new object[] { path }) as UConfig;
            }
            else if (driver == ConfigDriver.JSON)
            {
                var _methodJSON = typeof(USerialization)
                    .GetMethod("DeserializeJSON")
                    .MakeGenericMethod(new Type[] { configClass });
                return _methodJSON.Invoke(null, new object[] { path }) as UConfig;
            }
            MethodInfo _method = typeof(DNHper.USerialization)
                .GetMethod("DeserializeXML")
                .MakeGenericMethod(new Type[] { configClass });
            return _method.Invoke(null, new object[] { path }) as UConfig;
        }

        public T Get<T>()
            where T : class
        {
            string _configName = typeof(T).Name;
            UConfig _config;
            if (this.configs.TryGetValue(_configName, out _config))
            {
                return _config as T;
            }
            return null;
        }

        public bool Serialize<T>()
        {
            if (!this.configs.TryGetValue(typeof(T).Name, out UConfig _config))
                return false;

            ConfigDriver _driver = UReflection.GetPrivateField<ConfigDriver>(_config, "__driver");
            UReflection.CallPrivateMethod(_config, "OnSerializing");
            if (_driver == ConfigDriver.YAML)
            {
                USerialization.SerializeYAML(_config, _config.FilePath);
                return true;
            }
            else if (_driver == ConfigDriver.JSON)
            {
                USerialization.SerializeJSON(_config, _config.FilePath);
                return true;
            }

            DNHper.USerialization.SerializeXML(_config, _config.FilePath);
            UReflection.CallPrivateMethod(_config, "OnSerialized");
            return true;
        }
    }
}
