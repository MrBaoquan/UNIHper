using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DNHper;
using UnityEngine;

namespace UNIHper {

    public class ConfigManager : Singleton<ConfigManager> {
        private Dictionary<string, UConfig> configs = new Dictionary<string, UConfig> ();
        private ConfigDriver driverMode = ConfigDriver.YAML;
        private const string configDir = "Configs";
        private string suffix {
            get {
                if (driverMode == ConfigDriver.YAML) {
                    return ".yaml";
                }
                return ".xml";
            }
        }
        internal async Task Initialize () {
            UNIHperLogger.Log ("ConfigManager Initializing ...");
            this.loadConfig ();

            Type[] _configClasses = AssemblyConfig.GetSubClasses (typeof (UConfig)).ToArray (); // UReflection.SubClasses(typeof(UConfig));

            foreach (var _configClass in _configClasses) {
                UConfig _configInstance = Activator.CreateInstance (_configClass) as UConfig;
                // 配置文件默认保存在 %userprofile%\AppData\LocalLow\<companyname>\<productname>
                string _configDir = Path.Combine (Application.persistentDataPath, configDir);

                var _attributes = Attribute.GetCustomAttributes (_configClass);
                var _attribute = _attributes.Where (_attr => _attr is SerializedAt).First ();

                if (_attribute != null) {
                    var _saveTo = (_attribute as SerializedAt).SaveTo;
                    if (_saveTo == AppPath.StreamingDir) {
                        _configDir = Path.Combine (Application.streamingAssetsPath, configDir);
                    }
                }

                if (!Directory.Exists (_configDir)) {
                    Directory.CreateDirectory (_configDir);
                }

                string _path = Path.Combine (_configDir, _configClass.Name + this.suffix);
                if (!File.Exists (_path)) {
                    this.serializeConfig (_configInstance, _path);
                } else {
                    //MethodInfo _method = typeof (USerialization).GetMethod ("DeserializeYAML").MakeGenericMethod (new Type[] { _configClass });
                    _configInstance = this.deserializeConfig (_configClass, _path);
                    // _method.Invoke (null, new object[] { _path }) as UConfig;
                }

                UReflection.SetPrivateField (_configInstance, "__path", _path);
                this.configs.Add (_configClass.Name, _configInstance);
            }
            await Task.CompletedTask;
        }

        private void loadConfig () {
            this.driverMode = UNIHperConfig.ConfigDriver;
        }

        public void SerializeAll () {
            this.configs.Values.ToList ().ForEach (_config => {
                this.serializeConfig (_config, _config.__Path);
                //USerialization.SerializeXML (_config, _config.__Path);
            });
        }

        private void serializeConfig (object target, string path) {
            if (this.driverMode == ConfigDriver.YAML) {
                UNIHper.USerialization.SerializeYAML (target, path);
                return;
            }
            DNHper.USerialization.SerializeXML (target, path);
        }

        private UConfig deserializeConfig (Type configClass, string path) {
            if (this.driverMode == ConfigDriver.YAML) {
                var _methodYAML = typeof (USerialization).GetMethod ("DeserializeYAML").MakeGenericMethod (new Type[] { configClass });
                return _methodYAML.Invoke (null, new object[] { path }) as UConfig;
            }
            MethodInfo _method = typeof (DNHper.USerialization).GetMethod ("DeserializeXML").MakeGenericMethod (new Type[] { configClass });
            return _method.Invoke (null, new object[] { path }) as UConfig;
        }

        public T Get<T> () where T : class {
            string _configName = typeof (T).Name;
            UConfig _config;
            if (this.configs.TryGetValue (_configName, out _config)) {
                return _config as T;
            }
            return null;
        }

        public bool Serialize<T> () {
            string _configName = typeof (T).Name;
            UConfig _config;
            if (this.configs.TryGetValue (_configName, out _config)) {
                DNHper.USerialization.SerializeXML (_config, _config.__Path);
                return true;
            }
            return false;
        }
    }

}