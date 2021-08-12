using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using DNHper;
using UnityEngine;

namespace UNIHper {
    public class ScreenConfig {

        [XmlAttribute ()]
        public bool Activate = true;
        [XmlAttribute ()]
        public int PosX = 0;
        [XmlAttribute ()]
        public int PosY = 0;
        [XmlAttribute ()]
        public int Width = Screen.width;
        [XmlAttribute ()]
        public int Height = Screen.height;

        [XmlAttribute ()]
        public FullScreenMode Mode = FullScreenMode.FullScreenWindow;
    }

    public class SetWindowPos {
        public HWndInsertAfter HWndInsertAfter = HWndInsertAfter.HWND_TOPMOST;
        public Vector4 SWP_Rect = new Vector4 (0, 0, 1920, 1080);
        public List<SetWindowPosFlags> SWPFlags = new List<SetWindowPosFlags> () { SetWindowPosFlags.SWP_SHOWWINDOW };
    }

    public class SetWindowLong {
        public SetWindowLongIndex Index = SetWindowLongIndex.GWL_STYLE;
        public List<GWL_STYLE> GWL_Styles = new List<GWL_STYLE> { GWL_STYLE.WS_POPUP };
        public List<GWL_EXSTYLE> GWL_EXStyles = new List<GWL_EXSTYLE> ();

        [XmlIgnore]
        public long NewValue {
            get {
                if (Index == SetWindowLongIndex.GWL_STYLE) {
                    return (long) GWL_Styles.Aggregate ((_flags, _current) => _flags | _current);
                } else if (Index == SetWindowLongIndex.GWL_EXSTYLE) {
                    return (long) GWL_EXStyles.Aggregate ((_flags, _current) => _flags | _current);
                }
                return 0;
            }
        }
    }

    public class KeepWindowTop {
        [XmlAttribute]
        public float Interval = 5f;
        [XmlAttribute]
        public bool SetWindowPos = true;
        [XmlAttribute]
        public bool SetWindowLong = true;
        public SetWindowPos SetWindowPosFunction = new SetWindowPos ();
        public SetWindowLong SetWindowLongFunction = new SetWindowLong ();
    }

    public class AppConfig : UConfig {
        public KeepWindowTop KeepWindowTop = new KeepWindowTop ();
        public ScreenConfig PrimaryScreen = new ScreenConfig ();

        [XmlArray ("Displays")]
        [XmlArrayItem ("Display")]
        public List<ScreenConfig> Displays = new List<ScreenConfig> ();
    }
}