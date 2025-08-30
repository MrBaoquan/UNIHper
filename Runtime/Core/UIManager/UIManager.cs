using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DNHper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace UNIHper.UI
{
    using System.Reflection;

    using UniRx;

    public class UIConfig
    {
        [JsonProperty("asset"), DefaultValue("")]
        public string Asset = string.Empty;

        [JsonProperty("type"), DefaultValue(UIType.None)]
        [JsonConverter(typeof(StringEnumConverter))]
        public UIType ShowType = UIType.Normal;

        [JsonProperty("canvas"), DefaultValue("")]
        public string RenderCanvasName = "CanvasDefault";

        [JsonProperty("order"), DefaultValue(-1)]
        public int Order = -1;

        [JsonProperty("id"), DefaultValue(0)]
        public int InstID = 0;

        [JsonIgnore]
        internal string __UIKey = string.Empty;

        [JsonIgnore]
        public Type classType => AssemblyConfig.GetUNIType(__UIKey);

        public GameObject GetAsset()
        {
            if (string.IsNullOrEmpty(Asset) == false)
            {
                return ResourceManager.Instance.Get<GameObject>(Asset);
            }

            return ResourceManager.Instance.Get<GameObject>(__UIKey)
                ?? ResourceManager.Instance.Get<GameObject>(__UIKey.Split('.').LastOrDefault());
        }
    }

    public class UIManager : Singleton<UIManager>
    {
        internal const string CANVAS_DEFAULT = "CanvasDefault";

        internal const string PERSISTENCE_SCENE = "Persistence";
        private Dictionary<string, UIRootLayout> m_uiRootLayoutDic = new Dictionary<string, UIRootLayout>();

        // 自定义的UI配置
        private Dictionary<string, Dictionary<string, UIConfig>> customUIConfigData = null;

        // 框架层持久化的UI配置
        private Dictionary<string, UIConfig> builtInConfigData = null;

        // 应用层 附加的配置
        private Dictionary<string, Dictionary<string, UIConfig>> additionalConfigData =
            new Dictionary<string, Dictionary<string, UIConfig>>();

        // 所有已实例化的UI集合
        private Dictionary<string, UIBase> allSpawnedUICaches = new Dictionary<string, UIBase>();
        private Dictionary<string, UIBase> allSpawnedPersistentUICaches = new Dictionary<string, UIBase>();

        // 当前管理中的normalUIs
        private Dictionary<string, UIBase> activatedNormalUIs = new Dictionary<string, UIBase>();

        // 当前管理中的standaloneUIs
        private Dictionary<string, UIBase> activatedStandaloneUIs = new Dictionary<string, UIBase>();

        // 当前管理中的popupUIs
        private List<UIBase> activatedPopupUIs = new List<UIBase>();

        internal async Task Initialize()
        {
            UNIHperLogger.Log("UIManager Initializing ...");
            ReadConfigData();
            spawnPersistUIs();
            await Task.CompletedTask;
        }

        internal void CleanUp()
        {
            HideAll();
            allSpawnedUICaches.Values
                .ToList()
                .ForEach(_ui =>
                {
                    GameObject.Destroy(_ui.gameObject);
                });
            this.allSpawnedPersistentUICaches.Clear();
            this.allSpawnedUICaches.Clear();
            this.activatedNormalUIs.Clear();
            this.activatedStandaloneUIs.Clear();
            this.activatedPopupUIs.Clear();
        }

        internal void OnEnterScene(string sceneName)
        {
            var _allKeys = allSpawnedUICaches.Keys.ToList();
            _allKeys.ForEach(_uiKey =>
            {
                if (isPersistUI(_uiKey))
                    return;

                if (allSpawnedUICaches.ContainsKey(_uiKey))
                {
                    var _ui = allSpawnedUICaches[_uiKey];
                    //UReflection.CallPrivateMethod(_ui, "HandleHide");
                    _ui.HandleHide();
                    GameObject.Destroy(_ui.gameObject);
                    allSpawnedUICaches.Remove(_uiKey);
                    if (activatedPopupUIs.Contains(_ui))
                        activatedPopupUIs.Remove(_ui);
                }
                if (activatedNormalUIs.ContainsKey(_uiKey))
                    activatedNormalUIs.Remove(_uiKey);
            });

            Dictionary<string, UIConfig> _uis = null;
            if (!customUIConfigData.TryGetValue(sceneName, out _uis))
            {
                //Debug.LogWarningFormat("Find nothing ui in scene {0}", sceneName);
                return;
            }
            SpawnUIS(_uis);
        }

        public UIBase Show(string uiKey, bool bForceNotify = false)
        {
            return Show<UIBase>(uiKey, bForceNotify);
        }

        public T Show<T>(bool bForceNotify = false)
            where T : UIBase
        {
            var _uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T));
            return Show<T>(_uiKey, bForceNotify);
        }

        public T Show<T>(int instanceID, bool bForceNotify = false)
            where T : UIBase
        {
            var _uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID);
            return Show<T>(_uiKey, bForceNotify);
        }

        private T Show<T>(string uiKey, bool bForceNotify = false)
            where T : UIBase
        {
            if (!allSpawnedUICaches.TryGetValue(uiKey, out UIBase _uiComponent))
            {
                Debug.LogWarningFormat($"Show ui {uiKey} failed. UI {uiKey} not exits.");
                return null;
            }

            if (_uiComponent.isShowing)
            {
                Debug.LogWarning($"UI {uiKey} is already showing.");
                if (bForceNotify)
                    _uiComponent.ForceInvokeOnShownEvent();
                return _uiComponent as T;
            }

            Show(uiKey, _uiComponent);
            return _uiComponent as T;
        }

        public T Create<T>(int instanceID = 0)
            where T : UIBase
        {
            var _uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID);

            if (Exists<T>(instanceID))
            {
                Debug.LogWarning($"UI {_uiKey} is already exists.");
                return Get<T>(instanceID);
            }

            var _config = findUIConfigs<T>().Where(x => x.__UIKey.Contains("#")).FirstOrDefault();
            if (_config == null)
            {
                _config = new UIConfig { };
            }
            _config.__UIKey = _uiKey;

            return Create<T>(instanceID, _config);
        }

        public T Create<T>(string assetName, int instanceID = 0)
            where T : UIBase
        {
            var _uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID);

            if (Exists<T>(instanceID))
            {
                Debug.LogWarning($"UI {_uiKey} is already exists.");
                return Get<T>(instanceID);
            }

            var _config = findUIConfigs<T>().Where(x => x.__UIKey.Contains("#")).FirstOrDefault();
            if (_config == null)
            {
                _config = new UIConfig { };
            }
            _config.__UIKey = _uiKey;
            _config.Asset = assetName;

            return Create<T>(instanceID, _config);
        }

        public T Create<T>(int instanceID, UIConfig uiConfig)
            where T : UIBase
        {
            var _uiKey = $"{AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID)}";
            SpawnUI(_uiKey, uiConfig);
            return Get<T>(instanceID);
        }

        public async void Destroy<T>(int instanceID = 0, bool immediate = false)
            where T : UIBase
        {
            if (!Exists<T>(instanceID))
            {
                Debug.LogWarning($"{typeof(T)} with instanceID {instanceID} does not exist");
                return;
            }
            var _uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID);
            var _uiComponent = Hide<T>(instanceID);
            allSpawnedUICaches.Remove(_uiKey);
            if (!immediate)
            {
                await _uiComponent.WaitForTransitionComplete(0);
            }

            GameObject.Destroy(_uiComponent.gameObject);
        }

        public T Get<T>(int instanceID = 0)
            where T : UIBase
        {
            string uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID);
            if (!allSpawnedUICaches.TryGetValue(uiKey, out UIBase _uiComponent))
            {
                return null;
            }

            return _uiComponent as T;
        }

        public UIBase Get(string uiKey)
        {
            if (!allSpawnedUICaches.TryGetValue(uiKey, out UIBase _uiComponent))
            {
                return null;
            }
            return _uiComponent;
        }

        public List<T> GetAll<T>()
            where T : UIBase
        {
            return allSpawnedUICaches.Values.Where(x => x is T).Cast<T>().ToList();
        }

        public bool Exists<T>(int instanceID = 0)
            where T : UIBase
        {
            return Get<T>(instanceID) != null;
        }

        public T Hide<T>(int instanceID, bool bForceNotify = false)
            where T : UIBase
        {
            if (!Exists<T>(instanceID))
            {
                Debug.LogError($"{typeof(T)} is not exist.");
                return null;
            }
            var _uiKey = AssemblyConfig.GetTypeUniqueID(typeof(T), instanceID);
            return Hide<T>(_uiKey, bForceNotify);
        }

        public T Hide<T>(bool bForceNotify = false)
            where T : UIBase
        {
            var _key = AssemblyConfig.GetTypeUniqueID(typeof(T));
            return Hide<T>(_key, bForceNotify);
        }

        public UIBase Hide(string uiKey, bool bForceNotify = false)
        {
            return Hide<UIBase>(uiKey, bForceNotify);
        }

        public bool IsShowing<T>(int instanceID = 0)
            where T : UIBase
        {
            return Get<T>(instanceID) != null && Get<T>(instanceID).isShowing;
        }

        private T Hide<T>(string uiKey, bool bForceNotify = false)
            where T : UIBase
        {
            if (uiKey == "")
            {
                uiKey = typeof(T).Name;
            }

            if (!allSpawnedUICaches.TryGetValue(uiKey, out UIBase _uiComponent))
            {
                Debug.LogWarningFormat("Hide ui {0} failed. UI {0} not exits.", uiKey);
                return null;
            }

            if (!_uiComponent.isShowing)
            {
                Debug.LogWarningFormat("UI {0} is already hidden.", uiKey);
                if (bForceNotify)
                    _uiComponent.ForceInvokeOnHiddenEvent();
                return _uiComponent as T;
            }

            UIType _uiType = _uiComponent.Type;
            switch (_uiType)
            {
                case UIType.Normal:
                    hideNormalUI(uiKey);
                    break;
                case UIType.Standalone:
                    hideStandaloneUI(uiKey);
                    break;
                case UIType.Popup:
                    hidePopupUI(uiKey);
                    break;
            }

            return _uiComponent as T;
        }

        public void HideAll<T>()
            where T : UIBase
        {
            GetAll<T>().ForEach(x => x.Hide());
        }

        public void HideAll()
        {
            allSpawnedUICaches.Where(_ui => _ui.Value.isShowing).ToList().ForEach(_ui => Hide(_ui.Key));
        }

        public void ShowAll<T>()
            where T : UIBase
        {
            GetAll<T>().ForEach(x => x.Show());
        }

        public void ShowAll()
        {
            allSpawnedUICaches.Where(_ui => !_ui.Value.isShowing).ToList().ForEach(_ui => Show(_ui.Key));
        }

        private bool isStashing = false;
        List<UIBase> stashedUIs = new List<UIBase>();

        public void StashActiveUI()
        {
            if (isStashing)
                return;
            isStashing = true;
            var _normalUIs = activatedNormalUIs.Values.Where(_ui => _ui.isShowing).ToList();
            var _standaloneUIs = activatedStandaloneUIs.Values.Where(_ui => _ui.isShowing).ToList();
            var _popupUIs = activatedPopupUIs.Where(_ui => _ui.isShowing).ToList();
            stashedUIs = _normalUIs.Concat(_standaloneUIs).Concat(_popupUIs).ToList();
            stashedUIs.ForEach(_ui => Hide(_ui.__UIKey));
        }

        public List<UIBase> ActiveUIs
        {
            get
            {
                var _normalUIs = activatedNormalUIs.Values.Where(_ui => _ui.isShowing).ToList();
                var _standaloneUIs = activatedStandaloneUIs.Values.Where(_ui => _ui.isShowing).ToList();
                var _popupUIs = activatedPopupUIs.Where(_ui => _ui.isShowing).ToList();
                return _normalUIs.Concat(_standaloneUIs).Concat(_popupUIs).ToList();
            }
        }

        public void PopStashedUI()
        {
            if (!isStashing)
                return;
            stashedUIs.ForEach(_ui => Show(_ui.__UIKey));
            isStashing = false;
        }

        public UIBase Hide(string InKey)
        {
            return Hide<UIBase>(InKey);
        }

        public T Toggle<T>()
            where T : UIBase
        {
            var _ui = Get<T>();
            if (_ui is null)
                return null;
            _ui.Toggle();
            return _ui;
        }

        // 对话框类
        public void ShowAlert(string InContent, Action OnConfirm = null)
        {
            var _uiComponent = Show<DialogUI>();

            _uiComponent.SetContent(InContent).ShowDialog();
            IDisposable _clear = null;
            _clear = _uiComponent
                .OnConfirmAsObservable()
                .Subscribe(_ =>
                {
                    Utility.Dispose(ref _clear);

                    Utility.CallFunction(OnConfirm);
                    Hide<DialogUI>();
                });
        }

        public void ShowConfirmPanel(string InContent, Action OnConfirm = null, Action OnCancel = null)
        {
            var _uiComponent = Show<DialogUI>();
            _uiComponent.SetContent(InContent).ShowConfirm();
            IDisposable _clearConfirm = null;
            IDisposable _clearCancel = null;
            _clearConfirm = _uiComponent
                .OnConfirmAsObservable()
                .Subscribe(_ =>
                {
                    Utility.Dispose(ref _clearConfirm);
                    Utility.Dispose(ref _clearCancel);

                    Utility.CallFunction(OnConfirm);
                    Hide<DialogUI>();
                });

            _clearCancel = _uiComponent
                .OnCancelAsObservable()
                .Subscribe(_ =>
                {
                    Utility.Dispose(ref _clearConfirm);
                    Utility.Dispose(ref _clearCancel);

                    Utility.CallFunction(OnCancel);
                    Hide<DialogUI>();
                });
        }

        public void HideConfirmPanel()
        {
            Hide<DialogUI>();
        }

        public void ShowSaveFileDialog(string BaseDir, Func<string, bool> OnSaved)
        {
            ShowSaveFileDialog(BaseDir, "*.*", OnSaved);
        }

        public void ShowSaveFileDialog(string BaseDir, string SearchPattern, Func<string, bool> OnSaved)
        {
            var _uiComponent = Show<FileDialog>();

            IDisposable _clear = null;
            _clear = _uiComponent
                .SaveFile(BaseDir, SearchPattern)
                .Subscribe(_value =>
                {
                    if (OnSaved(Path.Combine(BaseDir, _value)))
                    {
                        Utility.Dispose(ref _clear);
                        Hide<FileDialog>();
                    }
                });
        }

        public void HideSaveFileDialog()
        {
            Hide<FileDialog>();
        }

        public void ShowOpenFileDialog(string FileDir, Func<string, bool> OnOpened)
        {
            ShowOpenFileDialog(FileDir, "*.*", OnOpened);
        }

        public void ShowOpenFileDialog(string FileDir, string InSearchPattern, Func<string, bool> OnOpened)
        {
            var InUI = Show<FileDialog>();
            IDisposable _clear = null;
            _clear = InUI.ReadFile(FileDir, InSearchPattern)
                .Subscribe(_value =>
                {
                    Utility.Dispose(ref _clear);
                    if (OnOpened(Path.Combine(FileDir, _value)))
                    {
                        Hide<FileDialog>();
                    }
                    ;
                });
        }

        public void AddConfig(string configPath)
        {
            var _textAsset = Resources.Load<TextAsset>(configPath);
            if (_textAsset == null)
            {
                Debug.LogErrorFormat("Append config path {0} failed. Config file not exits.", configPath);
                return;
            }
            var _configData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, UIConfig>>>(_textAsset.text);
            mergeUIConfig(additionalConfigData, _configData);
        }

        public void SetRenderMode(RenderMode renderMode, string canvasKey = CANVAS_DEFAULT)
        {
            var _uiLayout = getUIRootLayout(canvasKey);
            if (_uiLayout == null)
            {
                Debug.LogWarning("UI Layout not found: " + canvasKey);
                return;
            }
            _uiLayout.SetRenderMode(renderMode);
        }

        public Canvas RootCanvas(string canvasKey = CANVAS_DEFAULT)
        {
            var _uiLayout = getUIRootLayout(canvasKey);
            if (_uiLayout == null)
            {
                Debug.LogWarning("UI Layout not found: " + canvasKey);
                return null;
            }
            return _uiLayout.Root.GetComponent<Canvas>();
        }

        /// <summary>
        /// Private Methods
        /// </summary>

        private void mergeUIConfig(
            Dictionary<string, Dictionary<string, UIConfig>> dstConfig,
            Dictionary<string, Dictionary<string, UIConfig>> srcConfig
        )
        {
            srcConfig
                .ToList()
                .ForEach(_sceneKey =>
                {
                    if (!dstConfig.ContainsKey(_sceneKey.Key))
                    {
                        dstConfig.Add(_sceneKey.Key, _sceneKey.Value);
                    }
                    else
                    {
                        _sceneKey.Value
                            .ToList()
                            .ForEach(__uiKey =>
                            {
                                if (!dstConfig[_sceneKey.Key].ContainsKey(__uiKey.Key))
                                {
                                    dstConfig[_sceneKey.Key].Add(__uiKey.Key, __uiKey.Value);
                                }
                                else
                                {
                                    dstConfig[_sceneKey.Key][__uiKey.Key] = __uiKey.Value;
                                }
                            });
                    }
                });
        }

        private List<UIConfig> findUIConfigs<T>()
            where T : UIBase
        {
            return customUIConfigData.Values.SelectMany(x => x.Values).Where(x => x.classType == typeof(T)).ToList();
        }

        // 加载代码注册UI
        private Dictionary<string, Dictionary<string, UIConfig>> loadCodeRegisterUI()
        {
            var _uis = AssemblyConfig.GetSubClasses(typeof(UIBase));
            return _uis.Select(_uiType =>
                {
                    var _attrs = _uiType.GetCustomAttributes<UIPage>(inherit: true);
                    var _uiAutoID = 0;
                    return _attrs.Select(_attr =>
                    {
                        _attr.InstID = _attr.InstID <= 0 ? _uiAutoID++ : _attr.InstID;
                        _attr.UIKey = AssemblyConfig.GetTypeUniqueID(_uiType, _attr.InstID);
                        return _attr;
                    });
                })
                .SelectMany(_attrs => _attrs)
                .Where(_uiAttr => _uiAttr != null)
                .GroupBy(_ => _.Scene)
                .ToDictionary(
                    _ => _.Key,
                    _ =>
                        _.Select(
                                _uiAttr =>
                                    new UIConfig
                                    {
                                        __UIKey = _uiAttr.UIKey,
                                        ShowType = _uiAttr.Type,
                                        Asset = _uiAttr.Asset,
                                        RenderCanvasName = _uiAttr.Canvas,
                                        Order = _uiAttr.Order,
                                        InstID = _uiAttr.InstID
                                    }
                            )
                            .ToDictionary(_uiConfig => _uiConfig.__UIKey, _uiConfig => _uiConfig)
                );
        }

        private void ReadConfigData()
        {
            string _uiPath = UNIHperSettings.UIConfigPath;
            TextAsset _uiAsset = Resources.Load<TextAsset>(_uiPath);
            customUIConfigData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, UIConfig>>>(_uiAsset.text);

            mergeUIConfig(customUIConfigData, additionalConfigData);

            customUIConfigData = customUIConfigData
                .Select(_ =>
                {
                    return new KeyValuePair<string, Dictionary<string, UIConfig>>(_.Key, fillUIKeys(_.Value));
                })
                .ToDictionary(_ => _.Key, _ => _.Value);

            var _codeRegisterUIs = loadCodeRegisterUI();
            mergeUIConfig(customUIConfigData, _codeRegisterUIs);

            var _persistUIAsset = Resources.Load<TextAsset>("__Configs/Persistence/ui");
            builtInConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, UIConfig>>(_persistUIAsset.text);
            builtInConfigData = fillUIKeys(builtInConfigData);

            // 对UI进行默认排序
            customUIConfigData = orderUIConfig(customUIConfigData);
            builtInConfigData = builtInConfigData.Values.OrderBy(_ui => _ui.Order).ToDictionary(_ui => _ui.__UIKey, _ui => _ui);
        }

        private Dictionary<string, Dictionary<string, UIConfig>> orderUIConfig(Dictionary<string, Dictionary<string, UIConfig>> uiConfig)
        {
            return uiConfig
                .Select(_sceneKV =>
                {
                    return new KeyValuePair<string, Dictionary<string, UIConfig>>(
                        _sceneKV.Key,
                        _sceneKV.Value.Values.OrderBy(_ui => _ui.Order).ToDictionary(_ui => _ui.__UIKey, _ui => _ui)
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        /// <summary>
        /// 自动补全UIKey的程序集名
        /// </summary>
        /// <param name="uiConfigs"></param>
        /// <returns></returns>
        private Dictionary<string, UIConfig> fillUIKeys(Dictionary<string, UIConfig> uiConfigs)
        {
            return uiConfigs
                .Select(_kv =>
                {
                    var _uiType = AssemblyConfig.GetUNIType(_kv.Key);
                    if (_uiType != null)
                    {
                        var _uiKey = AssemblyConfig.GetTypeUniqueID(_uiType);
                        _kv.Value.__UIKey = _uiKey;
                        return new KeyValuePair<string, UIConfig>(_uiKey, _kv.Value);
                    }
                    return _kv;
                })
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        private void spawnPersistUIs()
        {
            SpawnUIS(builtInConfigData);

            Dictionary<string, UIConfig> _uis = null;
            if (!customUIConfigData.TryGetValue(PERSISTENCE_SCENE, out _uis))
            {
                return;
            }
            SpawnUIS(_uis);
        }

        private void SpawnUIS(Dictionary<string, UIConfig> InUIConfigs)
        {
            foreach (var _uiConfig in InUIConfigs)
            {
                SpawnUI(_uiConfig.Key, _uiConfig.Value);
            }
        }

        private void SpawnUI(string uiKey, UIConfig uiConfig)
        {
            Type _T = uiConfig.classType;

            if (_T == null)
            {
                Debug.LogWarning($"Create UI {uiKey} failed. UI script not found.");
                return;
            }

            GameObject _uiPrefab = uiConfig.GetAsset();
            if (_uiPrefab == null)
            {
                Debug.LogWarning($"Create UI {uiKey} failed. UI asset not found.");
                return;
            }

            GameObject _newUI = GameObject.Instantiate(_uiPrefab, getUIRootLayout(uiConfig.RenderCanvasName).NormalUIRoot);

            _newUI.name = $"{_uiPrefab.name} [{uiKey}]";
            _newUI.SetActive(false);

            UIBase _uiComponent = _newUI.GetComponent(_T) as UIBase;
            if (!_uiComponent)
            {
                _uiComponent = _newUI.AddComponent(_T) as UIBase;
            }

            _uiComponent.__CanvasKey = uiConfig.RenderCanvasName;
            _uiComponent.__UIKey = uiKey;
            _uiComponent.__Type = uiConfig.ShowType;
            _uiComponent.__InstanceID = uiConfig.InstID;
            _newUI.transform.SetParent(getParentUIAttachTo(_uiComponent.Type, uiConfig.RenderCanvasName));

            allSpawnedUICaches.Add(uiKey, _uiComponent);
            _uiComponent.OnLoad();
        }

        private Transform getParentUIAttachTo(UIType InUIType, string InCanvasKey)
        {
            var _uiLayout = getUIRootLayout(InCanvasKey);
            switch (InUIType)
            {
                case UIType.Normal:
                    return _uiLayout.NormalUIRoot;
                case UIType.Standalone:
                    return _uiLayout.StandaloneUIRoot;
                case UIType.Popup:
                    return _uiLayout.PopupUIRoot;
            }
            return null;
        }

        private UIRootLayout getUIRootLayout(string canvasKey = CANVAS_DEFAULT)
        {
            if (!m_uiRootLayoutDic.ContainsKey(canvasKey))
            {
#if UNITY_2023_1_OR_NEWER
                var _canvas = GameObject
                    .FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(_ => _.gameObject.name == canvasKey)
                    .FirstOrDefault();
#else
                var _canvas = GameObject.FindObjectsOfType<Canvas>(true).Where(_ => _.gameObject.name == canvasKey).FirstOrDefault();
#endif
                if (_canvas == null)
                {
                    var _uiLayoutGO = GameObject.Instantiate(Resources.Load<GameObject>("__Prefabs/CanvasDefault"));
                    _uiLayoutGO.name = canvasKey;
                    _canvas = _uiLayoutGO.GetComponent<Canvas>();
                }
                m_uiRootLayoutDic.Add(canvasKey, new UIRootLayout(_canvas));
            }
            return m_uiRootLayoutDic[canvasKey];
        }

        private void Show(string InKey, UIBase InUIComponent)
        {
            UIType _uiType = InUIComponent.Type;
            switch (_uiType)
            {
                case UIType.Normal:
                    showNormalUI(InKey);
                    break;
                case UIType.Standalone:
                    showStandaloneUI(InKey);
                    break;
                case UIType.Popup:
                    showPopupUI(InKey);
                    break;
            }
        }

        private void showNormalUI(string InKey)
        {
            UIBase _uiComponent = allSpawnedUICaches[InKey];
            if (activatedNormalUIs.ContainsKey(InKey))
                return;
            activatedNormalUIs.Add(InKey, _uiComponent);
            _uiComponent.HandleShow();
        }

        private void hideNormalUI(string InKey)
        {
            UIBase _uiComponent;
            if (!activatedNormalUIs.TryGetValue(InKey, out _uiComponent))
            {
                return;
            }

            activatedNormalUIs.Remove(InKey);
            _uiComponent.HandleHide();
        }

        private void showStandaloneUI(string InKey)
        {
            UIBase _uiComponent = allSpawnedUICaches[InKey];
            foreach (
                var _uiItem in activatedStandaloneUIs.Values
                    .Where(_ui => _ui.__CanvasKey == _uiComponent.__CanvasKey && _ui != _uiComponent)
                    .ToList()
            )
            {
                _uiItem.HandleHide();
            }

            _uiComponent.HandleShow();

            if (!activatedStandaloneUIs.Keys.Contains(InKey))
            {
                activatedStandaloneUIs.Add(InKey, _uiComponent);
            }
        }

        private void hideStandaloneUI(string InKey)
        {
            UIBase _uiComponent;
            if (!activatedStandaloneUIs.TryGetValue(InKey, out _uiComponent))
            {
                return;
            }

            //UReflection.CallPrivateMethod(_uiComponent, "HandleHide");
            _uiComponent.HandleHide();

            activatedStandaloneUIs.Remove(InKey);
            var _last = activatedStandaloneUIs.Values
                .Where(_ui => _ui.__CanvasKey == _uiComponent.__CanvasKey && _ui != _uiComponent)
                .LastOrDefault();
            if (_last != default(UIBase))
            {
                //UReflection.CallPrivateMethod(_last, "HandleShow");
                _last.HandleShow();
            }
        }

        private void showPopupUI(string InKey)
        {
            UIBase _uiComponent = allSpawnedUICaches[InKey];
            if (_uiComponent == null)
            {
                return;
            }

            if (!activatedPopupUIs.Contains(_uiComponent))
            {
                activatedPopupUIs.Add(_uiComponent);
            }
            else
            {
                if (activatedPopupUIs.Remove(_uiComponent))
                {
                    activatedPopupUIs.Add(_uiComponent);
                }
            }

            //UReflection.CallPrivateMethod(_uiComponent, "HandleShow");
            _uiComponent.transform.SetAsLastSibling();
            _uiComponent.HandleShow();
        }

        private void hidePopupUI(string uiKey = "")
        {
            if (activatedPopupUIs.Count <= 0)
            {
                return;
            }
            UIBase _uiComponent = null;
            if (string.IsNullOrEmpty(uiKey))
            {
                _uiComponent = activatedPopupUIs.LastOrDefault();
            }
            else
            {
                _uiComponent = allSpawnedUICaches[uiKey];
            }

            if (!activatedPopupUIs.Contains(_uiComponent))
            {
                return;
            }

            //UReflection.CallPrivateMethod(_uiComponent, "HandleHide");
            _uiComponent.HandleHide();
            activatedPopupUIs.Remove(_uiComponent);
        }

        private bool isPersistUI(string uiKey)
        {
            if (builtInConfigData.ContainsKey(uiKey))
            {
                return true;
            }

            Dictionary<string, UIConfig> _cusPersistUIs = null;
            if (customUIConfigData.TryGetValue(PERSISTENCE_SCENE, out _cusPersistUIs))
            {
                if (_cusPersistUIs.ContainsKey(uiKey))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
