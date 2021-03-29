using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UniRx;
using UnityEngine;

namespace UHelper {

    public enum UIType {
        Normal,
        Standalone,
        Popup
    }

    public class UIManager : Singleton<UIManager>, Manageable {
        private Transform UIRoot = null;

        private Transform NormalUIRoot = null;
        private Transform StandaloneUIRoot = null;
        private Transform PopupUIRoot = null;

        private class UIConfig {
            [JsonProperty ("asset")]
            public string Asset = string.Empty;
            [JsonProperty ("type")]
            [JsonConverter (typeof (StringEnumConverter))]
            public UIType Type = UIType.Normal;

            [JsonProperty ("script")]
            public string script = string.Empty;

            public string GetScript (string InDefault) {
                if (script == string.Empty) {
                    return InDefault;
                }
                return script;
            }

            public string GetAssetName (string InDefault) {
                if (Asset == string.Empty) {
                    return InDefault;
                }
                return Asset;
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
        public void Initialize () {
            TargetUIRoot ();
            ReadConfigData ();
            spawnPersistUIs ();
        }

        public void OnEnterScene (string InSceneName) {
            var _allKeys = allSpawnedUICaches.Keys.ToList ();
            _allKeys.ForEach (_uiKey => {
                if (isPersistUI (_uiKey)) return;

                if (allSpawnedUICaches.ContainsKey (_uiKey)) {
                    var _ui = allSpawnedUICaches[_uiKey];
                    GameObject.Destroy (_ui.gameObject);
                    allSpawnedUICaches.Remove (_uiKey);
                    if (popupUIs.Contains (_ui)) popupUIs.Remove (_ui);
                }
                if (normalUIs.ContainsKey (_uiKey)) normalUIs.Remove (_uiKey);
            });
            // foreach(var _uiComponent in allSpawnedUICaches)
            // {
            //     if(persistConfigData.ContainsKey(_uiComponent.Key)) continue;
            //     GameObject.Destroy(_uiComponent.Value.gameObject);
            // }
            // allSpawnedUICaches.Clear();
            // popupUIs.Clear();
            // normalUIs.Clear();

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

        public void Uninitialize () {

        }

        /// <summary>
        /// Private Methods
        /// </summary>
        private void TargetUIRoot () {
            GameObject _uiRoot = GameObject.Find ("UIRoot");
            if (_uiRoot) {
                UIRoot = _uiRoot.transform;
            } else {
                Debug.LogWarning ("Cannot find UIRoot node, it will caused ui no effect.");
                return;
            }

            NormalUIRoot = UIRoot.Find ("NormalUIRoot");
            StandaloneUIRoot = UIRoot.Find ("StandaloneUIRoot");
            PopupUIRoot = UIRoot.Find ("PopupUIRoot");
        }

        private void ReadConfigData () {
            string _uiPath = UHelperConfig.UIConfigPath;
            TextAsset _uiAsset = Resources.Load<TextAsset> (_uiPath);
            customUIConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, UIConfig>>> (_uiAsset.text);

            var _persistUIAsset = Resources.Load<TextAsset> ("Configs/Persistence/ui");
            persistConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, UIConfig>> (_persistUIAsset.text);
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
                SpawnUI (_uiConfig.Key, _uiConfig.Value);
            }
        }

        private void SpawnUI (string InUIKey, UIConfig InUIConfig) {
            //string _scriptName = InUIConfig.GetScript(InUIKey) + ", MainGame, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            Type _T = AssemblyConfig.GetUType (InUIKey);
            if (_T == null) {
                Debug.LogWarningFormat ("no class name match: {0}, spawn ui {0} failed", InUIKey);
                return;
            }

            GameObject _uiPrefab = ResourceManager.Instance.Get<GameObject> (InUIConfig.GetAssetName (InUIKey));
            _uiPrefab.SetActive (false);
            GameObject _newUI = GameObject.Instantiate (_uiPrefab, NormalUIRoot);

            UIBase _uiComponent = _newUI.GetComponent (_T) as UIBase;
            if (!_uiComponent) {
                _uiComponent = _newUI.AddComponent (_T) as UIBase;
            }
            UReflection.SetPrivateField<string> (_uiComponent, "__UIKey", InUIKey);
            UReflection.SetPrivateField<UIType> (_uiComponent, "__Type", InUIConfig.Type);
            UReflection.CallPrivateMethod (_uiComponent, "OnLoad");

            _newUI.transform.SetParent (getParentUIAttachTo (_uiComponent.Type));
            allSpawnedUICaches.Add (InUIKey, _uiComponent);
        }

        private Transform getParentUIAttachTo (UIType InUIType) {
            switch (InUIType) {
                case UIType.Normal:
                    return NormalUIRoot;
                case UIType.Standalone:
                    return StandaloneUIRoot;
                case UIType.Popup:
                    return PopupUIRoot;
            }
            return null;
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
            foreach (var _uiItem in standaloneUIs) {
                UReflection.CallPrivateMethod (_uiItem.Value, "HandleHide");
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleShow");

            if (!standaloneUIs.Keys.Contains (InKey)) {
                standaloneUIs.Add (InKey, _uiComponent);
            }

        }

        private void hideStandaloneUI (string InKey) {
            UIBase _uiComponent;
            if (!standaloneUIs.TryGetValue (InKey, out _uiComponent)) {
                return;
            }

            UReflection.CallPrivateMethod (_uiComponent, "HandleHide");

            standaloneUIs.Remove (InKey);
            var _last = standaloneUIs.LastOrDefault ();
            if (!_last.Equals (default (KeyValuePair<string, UIBase>))) {
                UReflection.CallPrivateMethod (_last.Value, "HandleShow");
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