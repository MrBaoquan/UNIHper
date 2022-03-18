using System;
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
        public AppPath SaveTo = AppPath.PersistentDir;
        public SerializedAt (AppPath InPath) {
            SaveTo = InPath;
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
    }

}