using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace UNIHper {

    public enum AppPath {
        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        ProjectDir,
        /// <summary>
        /// Save the config file in the project [streaming assets] directory
        /// </summary>
        StreamingDir,
        /// <summary>
        /// Save the config file in the project [%userprofile%\AppData\LocalLow\companyname\productname] directory
        /// </summary>
        PersistentDir
    }

    [AttributeUsage (AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SerializedAt : Attribute {
        // 序列化主目录
        public AppPath RootDir = AppPath.PersistentDir;
        // 序列化子目录
        public string SubDir = string.Empty;

        public SerializedAt (AppPath RootDir, string InSubDir = "Configs") {
            this.RootDir = RootDir;
        }
    }

    [SerializedAt (AppPath.PersistentDir)]
    public class UConfig {
        [XmlIgnore]
        protected string __path;

        [XmlIgnore]
        public string __Path {
            get {
                return __path;
            }
        }

        public void Delete () {
            File.Delete (__Path);
        }

        [XmlAnyElement ("FileComment")]
        public XmlComment FileComment { get { return new XmlDocument ().CreateComment (Comment ()); } set { } }

        /// <summary>
        /// Called once when config data is loaded
        /// </summary>
        protected virtual void OnLoaded () { }

        protected virtual void OnSerializing () { }

        protected virtual void OnSerialized () { }

        protected virtual string Comment () {
            return "Write your descriptions here";
        }
    }

}