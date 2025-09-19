using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace UNIHper
{
    public enum ConfigDriver
    {
        XML,
        JSON,
        YAML
    }

    public enum AppPath
    {
        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        ProjectDir,

        /// <summary>
        /// Save the config file in the project [Application.dataPath] directory
        /// </summary>
        DataDir,

        /// <summary>
        /// Save the config file in the project [streaming assets] directory
        /// </summary>
        StreamingDir,

        /// <summary>
        /// Save the config file in the project [%userprofile%\AppData\LocalLow\companyname\productname] directory
        /// </summary>
        PersistentDir,
        None
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SerializedAt : Attribute
    {
        // 序列化主目录
        public AppPath RootDir = AppPath.PersistentDir;

        // 序列化子目录
        public string SubDir = string.Empty;

        // 序列化优先级
        public int Priority = 0;

        public string FileName = string.Empty;

        public bool RecoverOnError = true;

        public SerializedAt(AppPath RootDir, string SubDir = "Configs", int Priority = 0)
        {
            this.RootDir = RootDir;
            this.SubDir = SubDir;
            this.Priority = Priority;
            this.RecoverOnError = true;
        }

        public SerializedAt(AppPath RootDir, string SubDir, string FileName, int Priority = 0, bool RecoverOnError = true)
        {
            this.RootDir = RootDir;
            this.SubDir = SubDir;
            this.Priority = Priority;
            this.FileName = FileName;
            this.RecoverOnError = RecoverOnError;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SerializeWith : Attribute
    {
        // 序列化主目录
        public ConfigDriver Mode = ConfigDriver.XML;

        public SerializeWith(ConfigDriver InMode)
        {
            Mode = InMode;
        }
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    [SerializedAt(AppPath.StreamingDir, Priority = -1), SerializeWith(ConfigDriver.XML)]
#else
    [SerializedAt(AppPath.PersistentDir, Priority = -1)]
#endif
    public class UConfig
    {
        [XmlIgnore, JsonIgnore]
        internal string filePath;

        [XmlIgnore, JsonIgnore]
        public string FilePath
        {
            get { return filePath; }
        }

        [XmlIgnore, JsonIgnore]
        internal ConfigDriver driver;

        [XmlIgnore, JsonIgnore]
        public ConfigDriver Driver
        {
            get { return driver; }
        }

        public void Delete()
        {
            File.Delete(filePath);
        }

#if !UNITY_ANDROID && !ENABLE_IL2CPP
        [JsonIgnore]
        [XmlAnyElement("FileComment")]
        public XmlComment FileComment
        {
            get { return new XmlDocument().CreateComment(Comment()); }
            set { }
        }
#endif

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        internal void Loaded()
        {
            OnLoaded();
        }

        internal void Unloaded()
        {
            OnUnloaded();
        }

        internal void Serializing()
        {
            OnSerializing();
        }

        internal void Serialized()
        {
            OnSerialized();
        }

        internal void Deserialized()
        {
            OnDeserialized();
        }

        // This method is called after the object has been deserialized.
        // You can override this method to perform any additional initialization.
        protected virtual void OnDeserialized() { }

        /// <summary>
        /// Called once when config data is loaded
        /// </summary>
        protected virtual void OnLoaded() { }

        protected virtual void OnUnloaded() { }

        protected virtual void OnSerializing() { }

        protected virtual void OnSerialized() { }

        protected virtual string Comment()
        {
            return "Write your descriptions here";
        }
    }
}
