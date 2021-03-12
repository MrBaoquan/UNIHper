using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace UHelper {
    public class ScreenConfig {
        [XmlAttribute ()]
        public int Width = Screen.width;
        [XmlAttribute ()]
        public int Height = Screen.height;

        [XmlAttribute ()]
        public FullScreenMode Mode = FullScreenMode.FullScreenWindow;
    }

    public class SetWindowPos {
        public HWndInsertAfter HWndInsertAfter = HWndInsertAfter.HWND_TOPMOST;
        public Vector4 SWP_Rect = Vector4.zero;
        public List<SetWindowPosFlags> SWPFlags = new List<SetWindowPosFlags> () { SetWindowPosFlags.SWP_NOMOVE, SetWindowPosFlags.SWP_NOSIZE };
    }

    public class KeepWindowTop {
        [XmlAttribute]
        public float Interval = 0;
        [XmlAttribute]
        public bool ShowWindow = false;
        [XmlAttribute]
        public bool SetWindowPos = true;
        [XmlAttribute]
        public bool SetForegroundWindow = true;

        public SetWindowPos SetWindowPosFunction = new SetWindowPos ();
    }

    public class AppConfig : UConfig {
        public KeepWindowTop KeepWindowTop = new KeepWindowTop ();
        public ScreenConfig PrimaryScreen = new ScreenConfig ();

        [XmlArray ("Displays")]
        [XmlArrayItem ("Display")]
        public List<ScreenConfig> Displays = new List<ScreenConfig> ();
    }
}