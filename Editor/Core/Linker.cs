using System.Collections.Generic;
using System.Xml.Serialization;
using DNHper;
using UNIHper;

namespace UNIHper.Editor
{
    public static class LinkerCfgUtil
    {
        public static void PreserveAssembly(string fullName)
        {
            var _linker = CfgUtil<Linker>.LoadXmlConfig("Assets/Resources/link.xml");
            if (_linker.Assemblies.Find(a => a.FullName == fullName) != null)
                return;
            _linker.Assemblies.Add(new Assembly { FullName = fullName, Preserve = "all" });
            CfgUtil<Linker>.SaveXmlConfig("Assets/Resources/link.xml", _linker);
        }
    }

    [XmlRoot("linker")]
    public class Linker
    {
        [XmlElement("assembly")]
        public List<Assembly> Assemblies { get; set; } = new List<Assembly>();
    }

    public class Assembly
    {
        [XmlAttribute("fullname")]
        public string FullName { get; set; }

        [XmlAttribute("preserve")]
        public string Preserve { get; set; } = "all";

        [XmlElement("type")]
        public List<LinkerType> Types { get; set; } = new List<LinkerType>();
    }

    public class LinkerType
    {
        [XmlAttribute("fullname")]
        public string FullName { get; set; }

        [XmlAttribute("preserve")]
        public string Preserve { get; set; } = "all";
    }
}
