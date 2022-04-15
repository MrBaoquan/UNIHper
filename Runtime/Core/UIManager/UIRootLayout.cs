using UnityEngine;
namespace UNIHper {
    public enum UIType {
        Normal,
        Standalone,
        Popup
    }

    public enum UIDriver {
        UGUI,
        FGUI
    }

    internal abstract class UIRootLayout {
        protected Transform m_root;
        protected Transform m_normalUIRoot;
        public Transform NormalUIRoot { get => m_normalUIRoot; }
        protected Transform m_standaloneUIRoot;
        public Transform StandaloneUIRoot { get => m_standaloneUIRoot; }
        protected Transform m_popupUIRoot;
        public Transform PopupUIRoot { get => m_popupUIRoot; }
    }

    internal class UGUIRootLayout : UIRootLayout {
        public UGUIRootLayout (Canvas UIRoot) {
            m_root = UIRoot.transform;
            //MonoBehaviour.DontDestroyOnLoad (m_root.gameObject);

            m_normalUIRoot = newUIRoot ("NormalUIRoot");
            m_standaloneUIRoot = newUIRoot ("StandaloneUIRoot");
            m_popupUIRoot = newUIRoot ("PopupUIRoot");

            m_standaloneUIRoot.SetAsLastSibling ();
            m_normalUIRoot.SetAsLastSibling ();
            m_popupUIRoot.SetAsLastSibling ();
        }

        private RectTransform newUIRoot (string InName) {
            var _uiRoot = m_root.Find (InName);
            if (_uiRoot == null) {
                _uiRoot = new GameObject (InName).transform;
                var _rectTrans = _uiRoot.AddComponent<RectTransform> ();
                _rectTrans.SetParent (m_root);
                _rectTrans.offsetMin = Vector2.zero;
                _rectTrans.offsetMax = Vector2.zero;
                _rectTrans.anchorMin = Vector2.zero;
                _rectTrans.anchorMax = Vector2.one;
                _uiRoot = _rectTrans;
            }
            return _uiRoot as RectTransform;
        }
    }

    internal class FGUIRootLayout : UIRootLayout {

        public FGUIRootLayout (Transform CanvasRoot) {
            m_root = CanvasRoot;
            m_normalUIRoot = newUIRoot ("NormalUIRoot");
            m_standaloneUIRoot = newUIRoot ("StandaloneUIRoot");
            m_popupUIRoot = newUIRoot ("PopupUIRoot");
        }

        private Transform newUIRoot (string InName) {
            var _uiRoot = m_root.Find (InName);
            if (_uiRoot == null) {
                _uiRoot = new GameObject (InName).transform;
                _uiRoot.SetParent (m_root);
            }
            return _uiRoot;
        }
    }
}