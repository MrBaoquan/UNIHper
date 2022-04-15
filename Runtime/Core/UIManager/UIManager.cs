using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DNHper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UniRx;
using UnityEngine;

namespace UNIHper {

    public class UIManager : Singleton<UIManager> {
        const string UGUICANVAS_DEFAULT = "UGUIRoot";
        const string FGUICANVAS_DEFAULT = "FGUIRoot";
        const int STANDALONE_ORDER = 0;
        const int NORMAL_ORDER = 100;
        const int POPUP_ORDER = 200;

        private Dictionary<string, UGUIRootLayout> m_uiRootLayoutDic = new Dictionary<string, UGUIRootLayout> ();
        private Dictionary<string, FGUIRootLayout> m_fuiRootLayoutDic = new Dictionary<string, FGUIRootLayout> ();

        internal class UIConfig {
            [JsonProperty ("asset")]
            public string Asset = string.Empty;
            [JsonProperty ("type")]
            [JsonConverter (typeof (StringEnumConverter))]
            public UIType Type = UIType.Normal;

            [JsonProperty ("driver")]
            [JsonConverter (typeof (StringEnumConverter))]
            public UIDriver Driver = UIDriver.UGUI;

            [JsonProperty ("script")]
            public string Script = string.Empty;

            [JsonProperty ("canvas")]
            public string Canvas = string.Empty;

            [JsonProperty ("package")]
            public string Package = string.Empty;

            [JsonProperty ("component")]
            public string Component = string.Empty;

            public string GetScript (string InDefault) {
                if (Script == string.Empty) {
                    return InDefault;
                }
                return Script;
            }

            public string GetAssetName (string InDefault) {
                if (Asset == string.Empty) {
                    return InDefault;
                }
                return Asset;
            }

            [JsonIgnore]
            public string CanvasName {
                get {
                    if (Canvas == string.Empty) {
                        return Driver == UIDriver.UGUI ? UGUICANVAS_DEFAULT : FGUICANVAS_DEFAULT;
                    }
                    return Canvas;
                }
            }
        }

        private Dictionary<string, Dictionary<string, UIConfig>> customUIConfigData = null;
        private Dictionary<string, UIConfig> persistConfigData = null;

        // 所有已实例化的UI集合
        private Dictionary<string, UIBase> allSpawnedUICaches = new Dictionary<string, UIBase> ();
        private Dictionary<string, UIBase> allSpawnedPersistentUICaches = new Dictionary<string, UIBase> ();
        private Dictionary<string, UIBase> normalUIs = new Dictionary<string, UIBase> ();
        private Dictionary<string, UIBase> standaloneUIs = new Dictionary<string, UIBase> ();
        private List<UIBase> popupUIs = new List<UIBase> ();
        internal async Task Initialize () {
            UNIHperLogger.Log ("UIManager Initializing ...");
            ReadConfigData ();
            spawnPersistUIs ();
            await Task.CompletedTask;
        }

        internal void OnEnterScene (string InSceneName) {
            var _allKeys = allSpawnedUICaches.Keys.ToList ();
            _allKeys.ForEach (_uiKey => {
                if (isPersistUI (_uiKey)) return;

                if (allSpawnedUICaches.ContainsKey (_uiKey)) {
                    var _ui = allSpawnedUICaches[_uiKey];
                    UReflection.CallPrivateMethod (_ui, "HandleHide");
                    GameObject.Destroy (_ui.gameObject);
                    allSpawnedUICaches.Remove (_uiKey);
                    if (popupUIs.Contains (_ui)) popupUIs.Remove (_ui);
                }
                if (normalUIs.ContainsKey (_uiKey)) normalUIs.Remove (_uiKey);
            });

            Dictionary<string, UIConfig> _uis = null;
            if (!customUIConfigData.TryGetValue (InSceneName, out _uis)) {
                Debug.LogWarningFormat ("Find nothing ui in scene {0}", InSceneName);
                return;
            }
            SpawnUIS (_uis);
        }

        public void Show (string InKey, Action<UIBase> InHandler = null) {
            UIBase _uiComponent = null;
            if (!allSpawnedUICaches.TryGetValue (InKey, out _uiComponent)) {
                Debug.LogWarningFormat ("Show ui {0} failed. UI {0} not exits.", InKey);
                return;
            }
            Show (InKey, _uiComponent);
            if (InHandler != null) {
                InHandler (_uiComponent);
            }
        }

        public T Show<T> (Action<T> InHandler = null) where T : UIBase {
            string _uiKey = typeof (T).Name;
            return Show<T> (_uiKey, InHandler);
        }

        public T Show<T> (string InKey, Action<T> InHandler = null) where T : UIBase {
            UIBase _uiComponent = null;
            if (!allSpawnedUICaches.TryGetValue (InKey, out _uiComponent)) {
                Debug.LogWarningFormat ("Show ui {0} failed. UI {0} not exits.", InKey);
                return null;
            }
            Show (InKey, _uiComponent);
            if (InHandler != null) {
                InHandler (_uiComponent as T);
            }
            return _uiComponent as T;
        }

        public T Get<T> (string InKey) where T : UIBase {
            if (InKey == "") {
                InKey = typeof (T).Name;
            }
            UIBase _uiComponent = null;
            if (!allSpawnedUICaches.TryGetValue (InKey, out _uiComponent)) {
                return null;
            }
            return _uiComponent as T;
        }

        public T Get<T> (Action<T> InHandler = null) where T : UIBase {

            string InKey = typeof (T).Name;
            UIBase _uiComponent = null;
            if (!allSpawnedUICaches.TryGetValue (InKey, out _uiComponent)) {
                return null;
            }
            if (InHandler != null) InHandler (_uiComponent as T);
            return _uiComponent as T;
        }

        public T Hide<T> (Action<T> InHandler = null) where T : UIBase {
            string _key = typeof (T).Name;
            return Hide<T> (_key, InHandler);
        }

        public T Hide<T> (string InKey, Action<T> InHandler = null) where T : UIBase {
            if (InKey == "") {
                InKey = typeof (T).Name;
            }
            UIBase _uiComponent;
            if (!allSpawnedUICaches.TryGetValue (InKey, out _uiComponent)) {
                Debug.LogWarningFormat ("Hide ui {0} failed. UI {0} not exits.", InKey);
                return null;
            }
            UIType _uiType = _uiComponent.Type;
            switch (_uiType) {
                case UIType.Normal:
                    hideNormalUI (InKey);
                    break;
                case UIType.Standalone:
                    hideStandaloneUI (InKey);
                    break;
                case UIType.Popup:
                    hidePopupUI (InKey);
                    break;
            }
            if (InHandler != null)
                InHandler (_uiComponent as T);
            return _uiComponent as T;
        }

        public void Hide (string InKey) {
            Hide<UIBase> (InKey);
        }

        // 对话框类
        public void ShowAlert (string InContent, Action OnConfirm = null) {
            Show<DialogUI> (InUI => {
                InUI.SetContent (InContent).ShowDialog ();
                IDisposable _clear = null;
                _clear = InUI.OnConfirmAsObservable ().Subscribe (_ => {
                    Utility.Dispose (ref _clear);

                    Utility.CallFunction (OnConfirm);
                    Hide<DialogUI> ();
                });
            });
        }

        public void ShowConfrim (string InContent, Action OnConfirm = null, Action OnCancel = null) {
            Show<DialogUI> (InUI => {
                InUI.SetContent (InContent).ShowConfirm ();
                IDisposable _clearConfirm = null;
                IDisposable _clearCancel = null;
                _clearConfirm = InUI.OnConfirmAsObservable ().Subscribe (_ => {
                    Utility.Dispose (ref _clearConfirm);
                    Utility.Dispose (ref _clearCancel);

                    Utility.CallFunction (OnConfirm);
                    Hide<DialogUI> ();
                });

                _clearCancel = InUI.OnCancelAsObservable ().Subscribe (_ => {
                    Utility.Dispose (ref _clearConfirm);
                    Utility.Dispose (ref _clearCancel);

                    Utility.CallFunction (OnCancel);
                    Hide<DialogUI> ();
                });
            });
        }

        public void ShowSaveFileDialog (string BaseDir, Func<string, bool> OnSaved) {
            ShowSaveFileDialog (BaseDir, "*.*", OnSaved);
        }
        public void ShowSaveFileDialog (string BaseDir, string SearchPattern, Func<string, bool> OnSaved) {
            Show<FileDialog> (InUI => {
                IDisposable _clear = null;
                _clear = InUI.SaveFile (BaseDir, SearchPattern)
                    .Subscribe (_value => {
                        if (OnSaved (Path.Combine (BaseDir, _value))) {
                            Utility.Dispose (ref _clear);
                            Hide<FileDialog> ();
                        };
                    });
            });
        }

        public void ShowOpenFileDialog (string FileDir, Func<string, bool> OnOpened) {
            ShowOpenFileDialog (FileDir, "*.*", OnOpened);
        }

        public void ShowOpenFileDialog (string FileDir, string InSearchPattern, Func<string, bool> OnOpened) {
            Show<FileDialog> (InUI => {
                IDisposable _clear = null;
                _clear = InUI.ReadFile (FileDir, InSearchPattern)
                    .Subscribe (_value => {
                        Utility.Dispose (ref _clear);
                        if (OnOpened (Path.Combine (FileDir, _value))) {
                            Hide<FileDialog> ();
                        };
                    });
            });
        }

        /// <summary>
        /// Private Methods
        /// </summary>

        private void ReadConfigData () {

            var _persistUIAsset = Resources.Load<TextAsset> ("Configs/Persistence/ui");
            persistConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, UIConfig>> (_persistUIAsset.text);

            string _uiPath = UNIHperConfig.UIConfigPath;
            TextAsset _uiAsset = Resources.Load<TextAsset> (_uiPath);
            customUIConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, UIConfig>>> (_uiAsset.text);

        }

        private void spawnPersistUIs () {
            SpawnUIS (persistConfigData);

            Dictionary<string, UIConfig> _uis = null;
            if (!customUIConfigData.TryGetValue ("Persistence", out _uis)) {
                return;
            }
            SpawnUIS (_uis);
        }

        private void SpawnUIS (Dictionary<string, UIConfig> InUIConfigs) {
            foreach (var _uiConfig in InUIConfigs) {
                SpawnUI (_uiConfig);
            }
        }

        private void SpawnUI (KeyValuePair<string, UIConfig> InUIConfig) {
            var _uiKey = InUIConfig.Key;
            var _uiConfig = InUIConfig.Value;
            //string _scriptName = InUIConfig.GetScript(InUIKey) + ", MainGame, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            Type _T = AssemblyConfig.GetUType (_uiKey);
            if (_T == null) {
                Debug.LogWarningFormat ("no class name match: {0}, spawn ui {0} failed", _uiKey);
                return;
            }
            var _newUI = createUIInstance (InUIConfig);
            if (_newUI == null) return;

            UIBase _uiComponent = _newUI.GetComponent (_T) as UIBase;
            if (!_uiComponent) {
                _uiComponent = _newUI.AddComponent (_T) as UIBase;
            }
            UReflection.SetPrivateField<string> (_uiComponent, "__CanvasKey", _uiConfig.CanvasName);
            UReflection.SetPrivateField<string> (_uiComponent, "__UIKey", _uiKey);
            UReflection.SetPrivateField<UIType> (_uiComponent, "__Type", _uiConfig.Type);
            UReflection.SetPrivateField<UIConfig> (_uiComponent, "__UIConfig", _uiConfig);

            _newUI.transform.SetParent (getParentUIAttachTo (_uiConfig));
            _newUI.layer = _newUI.transform.parent.gameObject.layer;
            allSpawnedUICaches.Add (_uiKey, _uiComponent);

            _newUI.SetActive (false);
            UReflection.CallPrivateMethod (_uiComponent, "OnInit");
        }

        private GameObject createUIInstance (KeyValuePair<string, UIConfig> InUIConfig) {
            var _uiConfig = InUIConfig.Value;
            if (_uiConfig.Driver == UIDriver.UGUI) {
                GameObject _uiPrefab = ResourceManager.Instance.Get<GameObject> (_uiConfig.GetAssetName (InUIConfig.Key));
                if (_uiPrefab is null) {
                    return null;
                }
                GameObject _newUI = GameObject.Instantiate (_uiPrefab, getUGUIRootLayout (_uiConfig.CanvasName).NormalUIRoot);
                return _newUI;
            } else if (_uiConfig.Driver == UIDriver.FGUI) {
                var _uiPrefab = ResourceManager.Instance.Get<GameObject> (_uiConfig.GetAssetName (InUIConfig.Key));
                if (_uiPrefab != null) {
                    return GameObject.Instantiate (_uiPrefab, getFGUIRootLayout (_uiConfig.CanvasName).NormalUIRoot);
                }

                var _newUI = new GameObject (InUIConfig.Key);
                return _newUI;
            }
            return null;
        }

        private Transform getParentUIAttachTo (UIConfig InUIConfig) {

            UIRootLayout _uiLayout = InUIConfig.Driver == UIDriver.UGUI? getUGUIRootLayout (InUIConfig.CanvasName) as UIRootLayout : getFGUIRootLayout (InUIConfig.CanvasName) as UIRootLayout;

            switch (InUIConfig.Type) {
                case UIType.Normal:
                    return _uiLayout.NormalUIRoot;
                case UIType.Standalone:
                    return _uiLayout.StandaloneUIRoot;
                case UIType.Popup:
                    return _uiLayout.PopupUIRoot;
            }
            return null;
        }

        private UGUIRootLayout getUGUIRootLayout (string InCanvasKey = UGUICANVAS_DEFAULT) {
            if (!m_uiRootLayoutDic.ContainsKey (InCanvasKey)) {
                var _canvas = GameObject.FindObjectsOfType<Canvas> (true).Where (_ => _.gameObject.name == InCanvasKey).FirstOrDefault ();
                if (_canvas == null) {
                    var _uiLayoutGO = GameObject.Instantiate (Resources.Load<GameObject> ($"Prefabs/{UGUICANVAS_DEFAULT}"));
                    _uiLayoutGO.name = InCanvasKey;
                    _canvas = _uiLayoutGO.GetComponent<Canvas> ();
                }
                m_uiRootLayoutDic.Add (InCanvasKey, new UGUIRootLayout (_canvas));
            }
            return m_uiRootLayoutDic[InCanvasKey];
        }

        private FGUIRootLayout getFGUIRootLayout (string InCanvasKey = UGUICANVAS_DEFAULT) {
            if (!m_fuiRootLayoutDic.ContainsKey (InCanvasKey)) {
                var _canvas = GameObject.Find (InCanvasKey);
                if (_canvas == null) {
                    _canvas = GameObject.Instantiate (Resources.Load<GameObject> ($"Prefabs/{FGUICANVAS_DEFAULT}"));
                }
                m_fuiRootLayoutDic.Add (InCanvasKey, new FGUIRootLayout (_canvas.transform));
            }
            return m_fuiRootLayoutDic[InCanvasKey];
        }

        private void Show (string InKey, UIBase InUIComponent) {
            UIType _uiType = InUIComponent.Type;
            switch (_uiType) {
                case UIType.Normal:
                    showNormalUI (InKey);
                    break;
                case UIType.Standalone:
                    showStandaloneUI (InKey);
                    break;
                case UIType.Popup:
                    showPopupUI (InKey);
                    break;
            }
        }

        private void showNormalUI (string InKey) {
            UIBase _uiComponent = allSpawnedUICaches[InKey];
            if (normalUIs.ContainsKey (InKey)) return;

            UReflection.CallPrivateMethod (_uiComponent, "HandleShow");
            normalUIs.Add (InKey, _uiComponent);

            syncFGUISortingOrders (normalUIs.Values.ToList (), NORMAL_ORDER);
        }

        private void hideNormalUI (string InKey) {
            UIBase _uiComponent;
            if (!normalUIs.TryGetValue (InKey, out _uiComponent)) {
                return;
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleHide");
            normalUIs.Remove (InKey);
        }

        private void showStandaloneUI (string InKey) {
            UIBase _uiComponent = allSpawnedUICaches[InKey];
            var _standaloneUIs = standaloneUIs.Values
                .Where (_ui => _ui.__CanvasKey == _uiComponent.__CanvasKey && _ui != _uiComponent)
                .ToList ();
            foreach (var _uiItem in _standaloneUIs) {
                UReflection.CallPrivateMethod (_uiItem, "HandleHide");
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleShow");

            if (!standaloneUIs.Keys.Contains (InKey)) {
                standaloneUIs.Add (InKey, _uiComponent);
            }

            syncFGUISortingOrders (standaloneUIs.Values.ToList (), STANDALONE_ORDER);
        }

        private void hideStandaloneUI (string InKey) {
            UIBase _uiComponent;
            if (!standaloneUIs.TryGetValue (InKey, out _uiComponent)) {
                return;
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleHide");

            standaloneUIs.Remove (InKey);
            var _last = standaloneUIs.Values
                .Where (_ui => _ui.__CanvasKey == _uiComponent.__CanvasKey && _ui != _uiComponent).LastOrDefault ();
            if (_last != default (UIBase)) {
                UReflection.CallPrivateMethod (_last, "HandleShow");
            }
        }

        private void showPopupUI (string InKey) {
            UIBase _uiComponent = allSpawnedUICaches[InKey];
            if (_uiComponent == null) { return; }

            if (!popupUIs.Contains (_uiComponent)) {
                popupUIs.Add (_uiComponent);
            } else {
                if (popupUIs.Remove (_uiComponent)) {
                    popupUIs.Add (_uiComponent);
                }
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleShow");
            _uiComponent.transform.SetAsLastSibling ();

            syncFGUISortingOrders (popupUIs, POPUP_ORDER);
        }

        private void hidePopupUI (string InKey = "") {
            if (popupUIs.Count <= 0) {
                return;
            }
            UIBase _uiComponent = null;
            if (InKey == "") {
                _uiComponent = popupUIs.FirstOrDefault ();
            } else {
                _uiComponent = allSpawnedUICaches[InKey];
            }

            if (!popupUIs.Contains (_uiComponent)) {
                return;
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleHide");
            popupUIs.Remove (_uiComponent);
        }

        private void syncFGUISortingOrders (List<UIBase> InUIs, int InStartOrder) {
            InUIs.Where (_ui => _ui is FGUIBase)
                .Select (_ => _ as FGUIBase)
                .WithIndex ().ToList ().ForEach (_item => {
                    _item.item.Panel.SetSortingOrder (InStartOrder + _item.index, true);
                });
        }

        private bool isPersistUI (string InUIKey) {

            if (persistConfigData.ContainsKey (InUIKey)) {
                return true;
            }

            Dictionary<string, UIConfig> _cusPersistUIs = null;
            if (customUIConfigData.TryGetValue ("Persistence", out _cusPersistUIs)) {
                if (_cusPersistUIs.ContainsKey (InUIKey)) {
                    return true;
                }
            }
            return false;
        }
    }
}