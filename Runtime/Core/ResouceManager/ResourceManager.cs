using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace UNIHper
{
    public class ResourceItem
    {
        public string driver = "Resources";
        public string type;
        public string path;
        public string label;
    }

    public class ResourceManager : Singleton<ResourceManager>
    {
        const string CUSTOM_RES_KEY = "__custom";

        // 自定义资源配置
        private Dictionary<string, List<ResourceItem>> resourcesConfigData;

        // 资源 AB包配置项
        private Dictionary<string, List<ResourceItem>> assetBundlesConfigData =
            new Dictionary<string, List<ResourceItem>>();

        // 可寻址资源配置项
        private Dictionary<string, List<ResourceItem>> addressableConfigData =
            new Dictionary<string, List<ResourceItem>>();

        // 框架层持久性资源配置项
        private List<ResourceItem> persistConfigData;

        // 额外附加的资源配置项
        private Dictionary<string, List<ResourceItem>> additionalConfigData =
            new Dictionary<string, List<ResourceItem>>();

        /// <summary>
        ///  所有已加载资源实例 [Persistence,CUSTOM_RES_KEY,SCENE_NAME]
        /// </summary>
        private Dictionary<string, Dictionary<string, UnityEngine.Object>> resources;

        /// <summary>
        /// Addressable 按标签分组的资源实例
        /// </summary>
        private readonly Dictionary<
            string,
            Dictionary<string, UnityEngine.Object>
        > addressableLabelAssets = new();

        // 所有AB包实例
        private readonly Dictionary<string, Dictionary<string, AssetBundle>> bundles = new();

        internal async Task Initialize()
        {
            UNIHperLogger.Log("ResourceManager Initializing ...");
            this.ReadConfigData();
            registerEvents();
            resources = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();

            resources = resourcesConfigData.Keys
                .Select(
                    _key =>
                        new KeyValuePair<string, Dictionary<string, UnityEngine.Object>>(
                            _key,
                            new Dictionary<string, UnityEngine.Object>()
                        )
                )
                .ToDictionary(_ => _.Key, _ => _.Value);

            // 框架层持久性资源
            await loadResourceAssets(persistConfigData, "Persistence");
            UNIHperLogger.Log("load framework persistence assets finished");
            // 应用层持久性资源
            await this.LoadAssetByKey("Persistence");
            UNIHperLogger.Log("load application persistence assets finished");
            // 加载场景资源
            await this.LoadSceneResources();
            UNIHperLogger.Log("load scene assets finished");
        }

        internal void CleanUp()
        {
            this.assetBundlesConfigData.Clear();
            this.addressableConfigData.Clear();
            this.resources.Clear();
        }

        private void registerEvents()
        {
            UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = (op, ex) => { };
        }

        /// <summary>
        /// 加载当前场景资源
        /// </summary>
        internal async Task LoadSceneResources()
        {
            string _sceneName = getCurrentSceneName();
            await this.LoadSceneResources(_sceneName);
        }

        /// <summary>
        /// 加载{InSceneName}场景资源
        /// </summary>
        /// <param name="sceneName"></param>
        internal async Task LoadSceneResources(string sceneName)
        {
            Debug.Log($"Load scene resources for [{sceneName}]");
            await this.LoadAssetByKey(sceneName);
        }

        /// <summary>
        /// 卸载{InSceneName}场景资源
        /// </summary>
        /// <param name="InSceneName"></param>
        internal void UnloadSceneResources(string InSceneName)
        {
            this.UnLoadAssetByKey(InSceneName);
        }

        /// <summary>
        /// 获取资源名为[InResName]的资源
        /// </summary>
        /// <param name="InResName"></param>
        /// <returns></returns>
        public List<UnityEngine.Object> Get(string InResName)
        {
            var _resources = resources.Keys
                .Where(
                    _key =>
                        new List<string>()
                        {
                            "Persistence",
                            getCurrentSceneName(),
                            CUSTOM_RES_KEY
                        }.Contains(_key)
                )
                .Select(_key => resources[_key])
                .SelectMany(_v => _v);
            return _resources
                .Where(_res => _res.Key.EndsWith($"_{InResName}"))
                .Select(_res => _res.Value)
                .ToList();
        }

        /// <summary>
        /// 获取资源名为{InResName},类型为{InResType}的资源
        /// </summary>
        /// <param name="InResName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(string InResName)
            where T : UnityEngine.Object
        {
            // 1. 从持久层资源查询
            T _resource = GetPersistRes<T>(InResName);
            if (_resource == null)
            {
                // 2. 从场景层资源查询
                _resource = GetSceneRes<T>(InResName);
            }

            // 3. 从自定义资源查询(用户通过ResourceManager主动添加的资源)
            if (_resource == null)
            {
                _resource = GetCustomRes<T>(InResName);
            }

            if (_resource == null)
            {
                Debug.LogWarning($"Resource not found: {InResName}");
                return null;
            }
            return _resource;
        }

        public IObservable<T> LoadAddessableAssetAsync<T>(string InResKey)
        {
            return Addressables
                .LoadAssetAsync<T>(InResKey)
                .Task.ToObservable()
                .ObserveOn(Scheduler.MainThread);
        }

        /// <summary>
        /// 加载addressable资源
        /// </summary>
        /// <param name="InResKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private IObservable<IList<T>> LoadAddressableAssetsAsync<T>(string InResKey)
        {
            return Addressables
                .LoadAssetsAsync<T>(InResKey, null)
                .Task.ToObservable()
                .ObserveOn(Scheduler.MainThread);
        }

        /// <summary>
        /// 资源转为基类Object
        /// </summary>
        /// <param name="InResKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private IObservable<IList<UnityEngine.Object>> loadAddressableAssetsAsync<T>(
            string InResKey
        )
        {
            return LoadAddressableAssetsAsync<T>(InResKey)
                .Select(_item => _item.Select(_ => _ as UnityEngine.Object).ToList());
        }

        /// <summary>
        /// 在指定的{InSceneName}场景中获取资源名为{InResName},类型为{InResType}的资源
        /// </summary>
        /// <param name="InName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSceneRes<T>(string InName)
            where T : UnityEngine.Object
        {
            string _sceneName = getCurrentSceneName();
            Dictionary<string, UnityEngine.Object> _resources;
            if (!resources.TryGetValue(_sceneName, out _resources))
            {
                return null;
            }

            return getResource<T>(_resources, InName);
        }

        /// <summary>
        /// 获取持久区资源名为{InName},类型为T的资源
        /// </summary>
        /// <param name="InName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPersistRes<T>(string InName)
            where T : UnityEngine.Object
        {
            Dictionary<string, UnityEngine.Object> _resources;
            if (!resources.TryGetValue("Persistence", out _resources))
            {
                Debug.LogWarning("There is no Persistence assets.");
                return null;
            }

            return getResource<T>(_resources, InName);
        }

        public T GetCustomRes<T>(string InName)
            where T : UnityEngine.Object
        {
            if (!resources.ContainsKey(CUSTOM_RES_KEY))
                return null;
            return getResource<T>(resources[CUSTOM_RES_KEY], InName);
        }

        public bool Exists<T>(string InResKey)
            where T : UnityEngine.Object
        {
            foreach (var _res in resources)
            {
                if (_res.Value.ContainsKey($"{typeof(T).FullName}_{InResKey}"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 加载AB包
        /// </summary>
        /// <param name="InPath"></param>
        /// <returns></returns>
        public AssetBundle AppendAssetBundle(string InPath)
        {
            loadABAssets(
                new List<ResourceItem>
                {
                    new ResourceItem { path = InPath, type = "AssetBundle" }
                },
                CUSTOM_RES_KEY
            );
            return bundles[CUSTOM_RES_KEY][getABFullPath(InPath)];
        }

        // 按标签加载Addressable资源
        public async Task<IEnumerable<UnityEngine.Object>> AppendAddressableLabelAssets<T>(
            string labelName
        )
            where T : UnityEngine.Object
        {
            var _labelKey = buildAALabelKey(labelName, typeof(T));
            if (addressableLabelAssets.ContainsKey(_labelKey))
                return addressableLabelAssets[_labelKey].Values;

            var _assets = await loadAddressableAssetsAsync<T>(labelName);
            appendResources(_assets, labelName);
            addressableLabelAssets.Add(
                _labelKey,
                _assets.Cast<UnityEngine.Object>().ToDictionary(_ => buildResKey(_), _ => _)
            );

            return _assets;
        }

        /// <summary>
        /// 获取按标签分组的资源
        /// </summary>
        /// <param name="labelName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetLabelAssets<T>(string labelName)
            where T : UnityEngine.Object
        {
            var _labelKey = buildAALabelKey(labelName, typeof(T));
            if (!addressableLabelAssets.ContainsKey(_labelKey))
            {
                UNIHperLogger.LogWarning($"can not find label assets with key: {labelName}");
                return null;
            }

            return addressableLabelAssets[_labelKey].Values.OfType<T>().ToList();
        }

        public T GetLabelAsset<T>(string labelName, string resName)
            where T : UnityEngine.Object
        {
            var _labelKey = buildAALabelKey(labelName, typeof(T));
            if (!addressableLabelAssets.ContainsKey(_labelKey))
            {
                Debug.LogWarning($"label not found: {_labelKey}");
                return null;
            }
            var _labelAssets = addressableLabelAssets[_labelKey];
            string _key = string.Format("{0}_{1}", typeof(T).FullName, resName);

            if (_labelAssets.ContainsKey(_key) == false)
            {
                Debug.LogWarning($"resource not found: {_key}");
                return null;
            }

            return _labelAssets[_key] as T;
        }

        public async Task<IEnumerable<AudioClip>> AppendAudioClips(IEnumerable<string> AudioPaths)
        {
            var _validPathes = AudioPaths.Where(_path => File.Exists(_path));
            if (_validPathes.Count() <= 0)
                return null;

            var _audioClips = await Observable.Zip(
                _validPathes.Select(_path => this.LoadAudioClip(_path))
            );
            appendResources(_audioClips, CUSTOM_RES_KEY);
            return _audioClips;
        }

        /// <summary>
        /// 加载指定目录下的音频文件
        /// </summary>
        /// <param name="audioDir">音频目录</param>
        /// <param name="searchPattern">匹配模式</param>
        /// <param name="searchOption">搜索模式</param>
        /// <returns></returns>
        public async Task<IEnumerable<AudioClip>> AppendAudioClips(
            string audioDir,
            string searchPattern = "*.wav|*.mp3",
            SearchOption searchOption = SearchOption.AllDirectories
        )
        {
            var _searchPatterns = searchPattern
                .Split('|')
                .Select(_pattern => _pattern.Replace("*", ""));
            ;
            return await AppendAudioClips(
                Directory
                    .GetFiles(audioDir, "*.*", searchOption)
                    .Where(_path => _searchPatterns.Contains(Path.GetExtension(_path).ToLower()))
            );
        }

        public async Task<AudioClip> AppendAudioClip(string InPath)
        {
            var _audioClip = await this.LoadAudioClip(InPath);
            appendResources(new List<AudioClip> { _audioClip }, CUSTOM_RES_KEY);
            return _audioClip;
        }

        /// <summary>
        /// 加载指定路径的图片
        /// </summary>
        /// <param name="TexturePathes">资源路径列表</param>
        /// <returns></returns>
        public async Task<IEnumerable<Texture2D>> AppendTexture2Ds(
            IEnumerable<string> TexturePathes
        )
        {
            var _validPathes = TexturePathes.Where(_path => File.Exists(_path));
            if (_validPathes.Count() <= 0)
                return null;

            var _textures = await Observable.Zip(
                _validPathes.Select(_path => this.LoadTexture2D(_path))
            );
            appendResources(_textures, CUSTOM_RES_KEY);
            return _textures;
        }

        /// <summary>
        /// 加载指定目录下的图片
        /// </summary>
        /// <param name="textureDir">资源目录</param>
        /// <param name="searchPattern">匹配方式</param>
        /// <param name="searchOption">搜索模式</param>
        /// <returns></returns>
        public async Task<IEnumerable<Texture2D>> AppendTexture2Ds(
            string textureDir,
            string searchPattern = "*.png|*.jpg|*.jpeg",
            SearchOption searchOption = SearchOption.AllDirectories
        )
        {
            var _searchPatterns = searchPattern
                .Split('|')
                .Select(_pattern => _pattern.Replace("*", ""));
            return await AppendTexture2Ds(
                Directory
                    .GetFiles(textureDir, "*.*", searchOption)
                    .Where(_path => _searchPatterns.Contains(Path.GetExtension(_path).ToLower()))
            );
        }

        public async Task<Texture2D> AppendTexture2D(string InPath)
        {
            try
            {
                var _texture = await this.LoadTexture2D(InPath);
                appendResources(new List<Texture2D> { _texture }, CUSTOM_RES_KEY);
                return _texture;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 卸载AB包
        /// </summary>
        /// <param name="InPath"></param>
        public void UnloadAssetBundle(string InPath)
        {
            var _path = getABFullPath(InPath);
            bundles[CUSTOM_RES_KEY][_path].Unload(true);
            bundles[CUSTOM_RES_KEY].Remove(_path);
            RefreshResources();
        }

        /// <summary>
        /// Private Methods Below
        /// </summary>

        private void RefreshResources()
        {
            resources = resources
                .Select(_ =>
                {
                    return new KeyValuePair<string, Dictionary<string, UnityEngine.Object>>(
                        _.Key,
                        _.Value
                            .Where(_1 => _1.Value != null)
                            .ToDictionary(_1 => _1.Key, _2 => _2.Value)
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        private T getResource<T>(Dictionary<string, UnityEngine.Object> InResources, string InName)
            where T : UnityEngine.Object
        {
            string _key = string.Format("{0}_{1}", typeof(T).FullName, InName);
            UNIHperLogger.Log($"Try to get resource with key: {_key}");
            if (!InResources.ContainsKey(_key))
                return default(T);
            return InResources[_key] as T;
        }

        public void AddConfig(string configPath)
        {
            var _resAsset = Resources.Load<TextAsset>(configPath);
            var _additionalConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<
                Dictionary<string, List<ResourceItem>>
            >(_resAsset.text);
            mergeResourceConfig(additionalConfigData, _additionalConfig);
        }

        private void mergeResourceConfig(
            Dictionary<string, List<ResourceItem>> dstConfig,
            Dictionary<string, List<ResourceItem>> srcConfig
        )
        {
            foreach (var _configNode in srcConfig)
            {
                if (!dstConfig.ContainsKey(_configNode.Key))
                {
                    dstConfig.Add(_configNode.Key, _configNode.Value);
                    continue;
                }
                dstConfig[_configNode.Key].AddRange(_configNode.Value);
            }
        }

        private void ReadConfigData()
        {
            string _resPath = UNIHperSettings.ResourceConfigPath;
            TextAsset _resAsset = Resources.Load<TextAsset>(_resPath);

            // 应用层自定义资源加载配置项
            var _appConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<
                Dictionary<string, List<ResourceItem>>
            >(_resAsset.text);

            mergeResourceConfig(_appConfigData, additionalConfigData);

            resourcesConfigData = _appConfigData
                .Select(_configNode =>
                {
                    return new KeyValuePair<string, List<ResourceItem>>(
                        _configNode.Key,
                        _configNode.Value.Where(_resItem => _resItem.driver == "Resources").ToList()
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);

            // 应用层自定义AB包
            var _appABDic = _appConfigData
                .Select(_configNode =>
                {
                    return new KeyValuePair<string, List<ResourceItem>>(
                        _configNode.Key,
                        _configNode.Value
                            .Where(_resItem => _resItem.driver == "AssetBundle")
                            .ToList()
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);

            addressableConfigData = _appConfigData
                .Select(_configNode =>
                {
                    return new KeyValuePair<string, List<ResourceItem>>(
                        _configNode.Key,
                        _configNode.Value
                            .Where(_resItem => _resItem.driver == "Addressable")
                            .ToList()
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);

            // 框架层资源加载配置项
            var _persistAsset = Resources.Load<TextAsset>("__Configs/Persistence/res");

            var _persistConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<
                List<ResourceItem>
            >(_persistAsset.text);
            persistConfigData = _persistConfigData.Where(_ => _.driver == "Resources").ToList();

            // 框架层AB包
            var _persistABList = _persistConfigData.Except(persistConfigData).ToList();

            // 初始化所有AB包
            assetBundlesConfigData.Add("Persistence", _persistABList);
            foreach (var _ab in _appABDic)
            {
                if (!assetBundlesConfigData.ContainsKey(_ab.Key))
                {
                    assetBundlesConfigData.Add(_ab.Key, _ab.Value);
                }
                else
                {
                    var _bundles = assetBundlesConfigData[_ab.Key];
                    assetBundlesConfigData[_ab.Key] = _bundles.Concat(_ab.Value).ToList();
                }
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="resKey"></param>
        private async Task LoadAssetByKey(string resKey)
        {
            if (assetBundlesConfigData.ContainsKey(resKey))
            {
                UNIHperLogger.Log($"Load ab assets: {resKey}");
                await loadABAssetsAsync(assetBundlesConfigData[resKey], resKey);
            }
            // Resources资源加载
            if (resourcesConfigData.ContainsKey(resKey))
            {
                UNIHperLogger.Log($"Load resources assets: {resKey}");
                await loadResourceAssets(resourcesConfigData[resKey], resKey);
            }

            if (addressableConfigData.ContainsKey(resKey))
            {
                UNIHperLogger.Log($"Load addressable assets: {resKey}");
                await loadAddressableAssets(addressableConfigData[resKey], resKey);
            }

            await Task.CompletedTask;
        }

        // 加载Resources文件夹下资源包
        private async Task loadResourceAssets(List<ResourceItem> InItems, string InResID)
        {
            foreach (var _item in InItems)
            {
                var _T = Type.GetType("UnityEngine." + _item.type + ",UnityEngine");
                try
                {
                    UnityEngine.Object[] _resources = await Task.FromResult(
                        Resources.LoadAll(_item.path, _T)
                    );
                    appendResources(_resources, InResID);
                }
                catch (Exception e)
                {
                    UNIHperLogger.LogError($"load assets {_item.path} failed, {e.Message}");
                }
            }
            UNIHperLogger.Log($"load resources {InResID} completed");
            await Task.CompletedTask;
        }

        // 加载Addressable资源包
        private async Task loadAddressableAssets(List<ResourceItem> InItems, string InResID)
        {
            if (InItems.Count <= 0)
            {
                await Task.CompletedTask;
                return;
            }

            Debug.Log($"loading addressable assets for [{InResID}]");
            foreach (var _resItem in InItems)
            {
                try
                {
                    var _T = Type.GetType("UnityEngine." + _resItem.type + ",UnityEngine");

                    var _labelKey = buildAALabelKey(_resItem.label, _T);
                    UNIHperLogger.Log($"loading addressable assets with label: {_labelKey}");
                    var _assets = await (
                        GetType()
                            .GetMethod(
                                "loadAddressableAssetsAsync",
                                BindingFlags.NonPublic | BindingFlags.Instance
                            )
                            .MakeGenericMethod(new Type[] { _T })
                            .Invoke(this, new object[] { _resItem.label })
                        as IObservable<List<UnityEngine.Object>>
                    );
                    if (!addressableLabelAssets.ContainsKey(_labelKey))
                    {
                        Dictionary<string, UnityEngine.Object> _assetsDict = new();
                        _assets.ForEach(_asset =>
                        {
                            string _key = buildResKey(_asset);
                            if (!_assetsDict.ContainsKey(_key))
                            {
                                _assetsDict.Add(_key, _asset);
                            }
                        });

                        addressableLabelAssets.Add(
                            buildAALabelKey(_resItem.label, _T),
                            _assetsDict
                        );
                    }

                    appendResources(_assets.ToArray(), InResID);
                    UNIHperLogger.Log($"load with label [{_resItem.label}] completed");
                }
                catch (Exception _ex)
                {
                    Debug.LogWarning(
                        $"try load addressable assets with label [{_resItem.label}] failed, {_ex.Message}"
                    );
                    continue;
                }
            }

            Debug.Log($"load addressable assets for [{InResID}] completed");
            await Task.CompletedTask;
        }

        // 加载AB包
        private async Task loadABAssetsAsync(List<ResourceItem> InItems, string InResID)
        {
            loadABAssets(InItems, InResID);
            await Task.CompletedTask;
        }

        private void loadABAssets(List<ResourceItem> InItems, string InResID)
        {
            if (!bundles.ContainsKey(InResID))
                bundles.Add(InResID, new Dictionary<string, AssetBundle>());

            foreach (var _item in InItems)
            {
                var _path = getABFullPath(_item.path);
                AssetBundle _bundle = AssetBundle.LoadFromFile(_path);
                bundles[InResID].Add(_path, _bundle);
                UnityEngine.Object[] _resources = _bundle.LoadAllAssets();
                appendResources(_resources, InResID);
            }
        }

        private string getABFullPath(string InPath)
        {
            return Path.Combine(Application.streamingAssetsPath, "AssetBundles", InPath);
        }

        private void appendResources(IEnumerable<UnityEngine.Object> InResources, string InResID)
        {
            if (!resources.ContainsKey(InResID))
                resources.Add(InResID, new Dictionary<string, UnityEngine.Object>());

            Action<UnityEngine.Object> _appendResource = (_resource) =>
            {
                string _key = buildResKey(_resource);
                if (resources[InResID].ContainsKey(_key))
                {
                    UNIHperLogger.LogWarning($"resource key can not duplicate, error key: {_key}");
                    return;
                }
                UNIHperLogger.Log($"{InResID} add asset {_key}");
                resources[InResID].Add(_key, _resource);
            };

            foreach (var _resource in InResources.Where(_ => _ != null))
            {
                // TODO: 子资源的加载处理
                _appendResource(_resource);
            }
        }

        private void UnLoadAssetByKey(string sceneKey)
        {
            if (resources.ContainsKey(sceneKey))
                resources[sceneKey].Clear();
            if (bundles.ContainsKey(sceneKey))
            {
                bundles[sceneKey].Values.ToList().ForEach(_ => _?.Unload(true));
                bundles[sceneKey].Clear();
            }
        }

        private string buildResKey(UnityEngine.Object resObj)
        {
            var _resTypeName = resObj.GetType().FullName;
#if UNITY_EDITOR
            if (resObj is UnityEditor.Animations.AnimatorController)
            {
                _resTypeName = typeof(UnityEngine.RuntimeAnimatorController).FullName;
            }
#endif
            return string.Format("{0}_{1}", _resTypeName, resObj.name);
        }

        private string buildAALabelKey(string label, Type resType)
        {
            return string.Format("{0}_{1}", resType.FullName, label);
        }

        private string getCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}
