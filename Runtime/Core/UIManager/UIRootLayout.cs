using System.Runtime.Serialization;
using UnityEngine;

namespace UNIHper.UI
{
    public enum UIType
    {
        Normal,
        Standalone,
        Popup,
        None
    }

    internal class UIRootLayout
    {
        private Transform m_root;
        public RectTransform Root => m_root as RectTransform;
        private RectTransform m_normalUIRoot;
        public RectTransform NormalUIRoot
        {
            get => m_normalUIRoot;
        }
        private RectTransform m_standaloneUIRoot;
        public RectTransform StandaloneUIRoot
        {
            get => m_standaloneUIRoot;
        }
        private RectTransform m_popupUIRoot;
        public RectTransform PopupUIRoot
        {
            get => m_popupUIRoot;
        }

        public UIRootLayout(Canvas UIRoot)
        {
            m_root = UIRoot.transform;
            //MonoBehaviour.DontDestroyOnLoad (m_root.gameObject);

            m_normalUIRoot = newUIRoot("NormalUIRoot");
            m_standaloneUIRoot = newUIRoot("StandaloneUIRoot");
            m_popupUIRoot = newUIRoot("PopupUIRoot");

            m_standaloneUIRoot.SetAsLastSibling();
            m_normalUIRoot.SetAsLastSibling();
            m_popupUIRoot.SetAsLastSibling();
        }

        public void SetRenderMode(RenderMode renderMode)
        {
            var _canvas = m_root.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = m_root.gameObject.AddComponent<Canvas>();
            }
            _canvas.renderMode = renderMode;
        }

        private RectTransform newUIRoot(string InName)
        {
            var _uiRoot = m_root.Find(InName);
            if (_uiRoot == null)
            {
                _uiRoot = new GameObject(InName).transform;
                var _rectTrans = _uiRoot.AddComponent<RectTransform>();
                _rectTrans.SetParent(m_root);
                _rectTrans.offsetMin = Vector2.zero;
                _rectTrans.offsetMax = Vector2.zero;
                _rectTrans.anchorMin = Vector2.zero;
                _rectTrans.anchorMax = Vector2.one;

                _rectTrans.localScale = Vector3.one;
                _rectTrans.localPosition = Vector3.zero;

                _uiRoot = _rectTrans;
            }
            return _uiRoot as RectTransform;
        }
    }
}
