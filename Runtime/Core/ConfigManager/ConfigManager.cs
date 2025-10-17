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
        private Dictionary<string, SerializedAt> CachedSerializedAtDict = new Dictionary<string, SerializedAt>();

        public void SetSerializedAt<T>(AppPath RootFolder)
            where T : UConfig
        {
            Managements.Config.SetSerializedAt<T>(new SerializedAt(RootFolder));
        }

        public void SetSerializedAt<T>(AppPath RootFolder, string SubDir)
            where T : UConfig
        {
            Managements.Config.SetSerializedAt<T>(new SerializedAt(RootFolder, SubDir));
        }

        public void SetSerializedAt<T>(AppPath RootFolder, string SubDir, string FileName)
            where T : UConfig
        {
            Managements.Config.SetSerializedAt<T>(new SerializedAt(RootFolder, SubDir, FileName));
        }

        public void SetSerializedAt<T>(SerializedAt serializedAt)
            where T : UConfig
        {
            var _configName = typeof(T).Name;
            if (this.CachedSerializedAtDict.ContainsKey(_configName))
            {
                this.CachedSerializedAtDict[_configName] = serializedAt;
            }
            else
            {
                this.CachedSerializedAtDict.Add(_configName, serializedAt);
            }
        }

        private Dictionary<string, UConfig> configs = new Dictionary<string, UConfig>();
        private string backupDir => Path.Combine(Application.persistentDataPath, "Backup/Configs");
        private string errorDir => Path.Combine(Application.persistentDataPath, "Error/Configs");

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

            var _configClasses = AssemblyConfig.GetSubClasses(typeof(UConfig)).ToList();
            UNIHperLogger.Log($"Config Classes Count: {_configClasses.Count}");
            // 根据优先级进行排序
            _configClasses.Sort(
                (a, b) =>
                {
                    var _aAttr = Attribute.GetCustomAttributes(a).Where(_attr => _attr is SerializedAt).FirstOrDefault();
                    var _bAttr = Attribute.GetCustomAttributes(b).Where(_attr => _attr is SerializedAt).FirstOrDefault();
                    var _aPriority = (_aAttr as SerializedAt)?.Priority;
                    var _bPriority = (_bAttr as SerializedAt)?.Priority;
                    if (_aPriority == null || _bPriority == null)
                    {
                        return _aPriority == null ? -1 : 1;
                    }
                    return _aPriority.Value.CompareTo(_bPriority.Value);
                }
            );

            foreach (var _configClass in _configClasses)
            {
                UConfig _configInstance = Activator.CreateInstance(_configClass) as UConfig;

                // 配置文件默认保存在 %userprofile%\AppData\LocalLow\<companyname>\<productname>
                var _attributes = Attribute.GetCustomAttributes(_configClass);
                var _serializeAtAttr = _attributes.Where(_attr => _attr is SerializedAt).FirstOrDefault();

                if (CachedSerializedAtDict.ContainsKey(_configClass.Name))
                {
                    _serializeAtAttr = CachedSerializedAtDict[_configClass.Name];
                }
                else if (_serializeAtAttr != null)
                {
                    CachedSerializedAtDict.Add(_configClass.Name, _serializeAtAttr as SerializedAt);
                }

                var _serializeWithAttr = _attributes.Where(_attr => _attr is SerializeWith).FirstOrDefault();

                string _configDir = Path.Combine(Application.persistentDataPath, configDir);
                string _fileName = string.Empty;

                // 计算配置文件保存目录
                if (_serializeAtAttr != null)
                {
                    var _serializedAttr = _serializeAtAttr as SerializedAt;
                    if (_serializedAttr.RootDir == AppPath.None)
                    {
                        UNIHperLogger.LogWarning($"Config {_configClass.FullName} skipped, because RootDir is None.");
                        continue;
                    }

                    _fileName = _serializedAttr.FileName;
                    if (_serializedAttr.RootDir == AppPath.StreamingDir)
                    {
                        _configDir = Path.Combine(Application.streamingAssetsPath, _serializedAttr.SubDir);
                    }
                    else if (_serializedAttr.RootDir == AppPath.PersistentDir)
                    {
                        _configDir = Path.Combine(Application.persistentDataPath, _serializedAttr.SubDir);
                    }
                    else if (_serializedAttr.RootDir == AppPath.DataDir)
                    {
                        _configDir = Path.Combine(Application.dataPath, _serializedAttr.SubDir);
                    }
                    else if (_serializedAttr.RootDir == AppPath.ProjectDir)
                    {
                        var _projectDir = Directory.GetParent(Application.dataPath).FullName;
                        _configDir = Path.Combine(_projectDir, _serializedAttr.SubDir);
                    }
                }

                // 配置文件驱动方式
                ConfigDriver _driverMode = ConfigDriver.XML;
                if (_serializeWithAttr is not null)
                {
                    var _serializeWith = (_serializeWithAttr as SerializeWith);
                    _driverMode = _serializeWith.Mode;
                }

                if (!Directory.Exists(_configDir))
                {
                    UNIHperLogger.Log($"Create config dir {_configDir} for : {_configClass.FullName}");
                    Directory.CreateDirectory(_configDir);
                }

                _fileName = string.IsNullOrEmpty(_fileName) ? _configClass.Name + this.suffix(_driverMode) : _fileName;

                string _path = Path.Combine(_configDir, _fileName);

                UNIHperLogger.Log($"Create config file {_path}");
                if (!File.Exists(_path))
                {
                    _configInstance.filePath = _path;
                    _configInstance.driver = _driverMode;
                    this.serializeConfig(_configInstance, _path, _driverMode);
                }
                else
                {
                    _configInstance = this.deserializeConfig(_configClass, _path, _driverMode);
                    _configInstance.filePath = _path;
                    _configInstance.driver = _driverMode;
                }

                this.configs.Add(_configClass.Name, _configInstance);
                _configInstance.Deserialized();
            }

            this.configs.Values
                .ToList()
                .ForEach(_config =>
                {
                    _config.Loaded();
                });
            BackupAll();
            await Task.CompletedTask;
        }

        internal void CleanUp()
        {
            configs.Values
                .ToList()
                .ForEach(_config =>
                {
                    _config.Unloaded();
                });
            this.configs.Clear();
        }

        public T Reload<T>()
            where T : UConfig
        {
            var _configKey = typeof(T).Name;
            if (!this.configs.ContainsKey(_configKey))
            {
                return null;
            }

            var _configInstance = this.configs[_configKey];
            _configInstance.Unloaded();

            var _path = _configInstance.FilePath;
            var _driver = _configInstance.Driver;
            if (!File.Exists(_path))
            {
                _configInstance.filePath = _path;
                _configInstance.driver = _driver;
                this.serializeConfig(_configInstance, _path, (ConfigDriver)_driver);
            }
            else
            {
                _configInstance = this.deserializeConfig(typeof(T), _path);
                _configInstance.filePath = _path;
                _configInstance.driver = _driver;
            }

            this.configs[_configKey] = _configInstance;
            _configInstance.Deserialized();
            _configInstance.Loaded();

            return _configInstance as T;
        }

        private void loadConfig() { }

        public void SaveAll()
        {
            this.configs.Values
                .ToList()
                .ForEach(_config =>
                {
                    this.serializeConfig(_config, _config.FilePath);
                });
        }

        public void BackupAll(bool force = false)
        {
            this.configs.Values
                .Where(_config => force || !this.hasBackup(_config))
                .ForEach(_config =>
                {
                    this.Backup(_config);
                });
        }

        private bool hasBackup(UConfig config)
        {
            var _backupFilePath = this.getBackupFilePath(config.FilePath);
            return File.Exists(_backupFilePath);
        }

        private void serializeConfig(UConfig target, string path, ConfigDriver driver = ConfigDriver.XML)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"serialize {target.GetType().Name} failed, path is null or empty.");
                return;
            }

            target.Serializing();
            if (driver == ConfigDriver.YAML)
            {
                USerialization.SerializeYAML(target, path);
                target.Serialized();

                return;
            }
            else if (driver == ConfigDriver.JSON)
            {
                USerialization.SerializeJSON(target, path);
                target.Serialized();

                return;
            }

            DNHper.USerialization.SerializeXML(target, path);
            Backup(target);
            target.Serialized();
        }

        private UConfig deserializeConfig(Type configClass, string path, ConfigDriver driver = ConfigDriver.XML)
        {
            var _cachedAttr = CachedSerializedAtDict.GetValueOrDefault(configClass.Name);
            if (_cachedAttr != null && _cachedAttr.RecoverOnError)
            {
                restoreIfConfigError(path);
            }

            if (driver == ConfigDriver.YAML)
            {
                var _methodYAML = typeof(USerialization).GetMethod("DeserializeYAML").MakeGenericMethod(new Type[] { configClass });
                return _methodYAML.Invoke(null, new object[] { path }) as UConfig;
            }
            else if (driver == ConfigDriver.JSON)
            {
                var _methodJSON = typeof(USerialization).GetMethod("DeserializeJSON").MakeGenericMethod(new Type[] { configClass });
                return _methodJSON.Invoke(null, new object[] { path }) as UConfig;
            }
            MethodInfo _method = typeof(DNHper.USerialization).GetMethod("DeserializeXML").MakeGenericMethod(new Type[] { configClass });
            return _method.Invoke(null, new object[] { path, null }) as UConfig;
        }

        public T Get<T>()
            where T : UConfig
        {
            string _configName = typeof(T).Name;
            UConfig _config;
            if (this.configs.TryGetValue(_configName, out _config))
            {
                return _config as T;
            }
            return null;
        }

        public bool Save<T>()
        {
            return Save(typeof(T).Name);
        }

        public bool Save(string _configKey)
        {
            if (!this.configs.TryGetValue(_configKey, out UConfig _config))
                return false;

            ConfigDriver _driver = _config.Driver;
            // UReflection.CallPrivateMethod(_config, "OnSerializing");
            _config.Serializing();
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
            // UReflection.CallPrivateMethod(_config, "OnSerialized");
            _config.Serialized();
            return true;
        }

        public void Backup(UConfig config)
        {
            var _srcFilePath = config.FilePath;

            if (checkIfXMLValid(_srcFilePath) == false)
            {
                Debug.LogWarning($"Config file {_srcFilePath} is invalid, skip backup.");
                return;
            }

            if (Directory.Exists(backupDir) == false)
            {
                UNIHperLogger.Log($"Create backup dir {backupDir}");
                Directory.CreateDirectory(backupDir);
            }
            var _backupFilePath = Path.Combine(backupDir, Path.GetFileName(_srcFilePath) + ".bak");
            File.Copy(_srcFilePath, _backupFilePath, true);
        }

        // public void Restore(UConfig config)
        // {
        //     var _srcFilePath = config.FilePath;
        //     var _backupFilePath = _srcFilePath + ".bak";
        //     File.Copy(_backupFilePath, _srcFilePath, true);
        // }

        private string getBackupFilePath(string sourceFilePath)
        {
            return Path.Combine(backupDir, Path.GetFileName(sourceFilePath) + ".bak");
        }

        private void restoreConfig(string sourceFilePath)
        {
            var _srcFilePath = sourceFilePath;
            var _backupFilePath = this.getBackupFilePath(_srcFilePath);
            if (File.Exists(_backupFilePath) == false)
            {
                Debug.LogWarning($"Backup file {_backupFilePath} not found.");
                return;
            }

            if (checkIfXMLValid(_backupFilePath) == false)
            {
                Debug.LogWarning($"Backup file {_backupFilePath} is invalid.");
                return;
            }

            // 将错误文件备份到error目录
            if (Directory.Exists(errorDir) == false)
            {
                UNIHperLogger.Log($"Create error dir {errorDir}");
                Directory.CreateDirectory(errorDir);
            }
            var _errorFilePath = Path.Combine(
                errorDir,
                Path.GetFileNameWithoutExtension(_srcFilePath) + " " + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".xml"
            );
            File.Copy(_srcFilePath, _errorFilePath, true);

            File.Copy(_backupFilePath, _srcFilePath, true);
            Debug.LogWarning($"Restored config file {_srcFilePath} from backup.");
        }

        private void restoreIfConfigError(string filePath)
        {
            // 仅针对 XML 文件进行检查
            if (Path.GetExtension(filePath) != ".xml")
            {
                return;
            }
            if (checkIfXMLValid(filePath))
            {
                return;
            }

            this.restoreConfig(filePath);
        }

        private bool checkIfXMLValid(string filePath)
        {
            try
            {
                var _xmlDoc = new System.Xml.XmlDocument();
                _xmlDoc.Load(filePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Config file {filePath} is invalid, {e.Message}");
                return false;
            }
        }
    }
}
