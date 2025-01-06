using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        const string ASSET_BUDDLE_DRIVER = "AssetBundle";
        const string ADDRESSABLE_DRIVER = "Addressable";
        const string RESOURCES_DRIVER = "Resources";
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
        private Dictionary<string, Dictionary<string, AssetItem>> resources;

        // 所有AB包实例
        private readonly Dictionary<string, Dictionary<string, AssetBundle>> bundles = new();

        // 加载资源入口
        internal async Task Initialize()
        {
            UNIHperLogger.Log("ResourceManager Initializing ...");
            this.readConfigData();
            registerEvents();

            resources = resourcesConfigData.Keys
                .Select(
                    _key =>
                        new KeyValuePair<string, Dictionary<string, AssetItem>>(
                            _key,
                            new Dictionary<string, AssetItem>()
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

        internal Dictionary<string, AssetItem> allAssets =>
            resources.Keys
                .Where(
                    _key =>
                        new List<string>
                        {
                            "Persistence",
                            getCurrentSceneName(),
                            CUSTOM_RES_KEY
                        }.Contains(_key)
                )
                .Select(_key => resources[_key])
                .SelectMany(_kv => _kv)
                .ToDictionary(_kv => _kv.Key, _kv => _kv.Value);

        // internal Dictionary<string, AssetItem> AllAssets => allAssets;

        /// <summary>
        /// 获取资源名为{assetName},类型为{T}的资源, assetName可以是资源路径(可以写进行部分匹配)
        /// </summary>
        /// <param name="assetName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(string assetName)
            where T : UnityEngine.Object
        {
            var _asset = getResource<T>(allAssets, assetName);
            if (_asset == null)
            {
                Debug.LogWarning($"Resource not found: {assetName}");
                return null;
            }
            return _asset;
        }

        // 筛选获取多个资源
        public List<T> GetMany<T>(string searchFilter)
            where T : UnityEngine.Object
        {
            var _type = typeof(T).FullName;
            string escapedSearchFilter = Regex.Escape(searchFilter.ToForwardSlash());
            string pattern = @$".*{escapedSearchFilter}.*_{_type}";
            return allAssets
                .Where(_kv => Regex.IsMatch(_kv.Key, pattern))
                .Select(_kv => _kv.Value.asset)
                .OfType<T>()
                .ToList();
        }

        // TODO: 编辑器下和打包后获取的资源路径列表不一致， 现在存在多个资源重复加载情况，后续考虑优化
        // private Task<T> ConvertToGeneric<T>(Task task, T result)
        // {
        //     var tcs = new TaskCompletionSource<T>();

        //     task.ContinueWith(t =>
        //     {
        //         if (t.IsFaulted)
        //         {
        //             tcs.TrySetException(t.Exception.InnerExceptions);
        //         }
        //         else if (t.IsCanceled)
        //         {
        //             tcs.TrySetCanceled();
        //         }
        //         else
        //         {
        //             tcs.TrySetResult(result);
        //         }
        //     });

        //     return tcs.Task;
        // }

        // private IObservable<object> LoadAssetAsync(IResourceLocation location, Type type)
        // {
        //     var _method = typeof(Addressables)
        //         .GetMethod("LoadAssetAsync", new Type[] { typeof(IResourceLocation) })
        //         .MakeGenericMethod(new Type[] { type });

        //     var _handle = _method.Invoke(null, new object[] { location });

        //     var genericHandleType = typeof(AsyncOperationHandle<>).MakeGenericType(type);
        //     Debug.LogWarning(genericHandleType);
        //     var taskProperty = genericHandleType.GetProperty("Task");
        //     Debug.LogWarning(taskProperty);

        //     var task = (System.Threading.Tasks.Task)taskProperty.GetValue(_handle);
        //     return ConvertToGeneric(task, type).ToObservable();
        // }

        /// <summary>
        /// 加载addressable资源
        /// </summary>
        /// <param name="labelName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private IObservable<IEnumerable<AssetItem>> LoadAddressableAssetsByLabel<T>(
            string labelName
        )
        {
            return Addressables
                .LoadResourceLocationsAsync(labelName, typeof(T))
                .Task.ToObservable()
                .SubscribeOnMainThread()
                .SelectMany(locations =>
                {
                    return locations
                        .ToObservable()
                        // 加载资源并返回资源和位置
                        .SelectMany(
                            location =>
                                Addressables.LoadAssetAsync<T>(location).Task.ToObservable(),
                            (location, asset) => new { location.PrimaryKey, asset }
                        )
                        .ToList()
                        .Select(
                            loadedAssets =>
                                loadedAssets
                                    .AsEnumerable()
                                    .Select(
                                        x =>
                                            new AssetItem
                                            {
                                                asset = x.asset as UnityEngine.Object,
                                                path = x.PrimaryKey,
                                                asssetDriver = AsssetDriver.Addressable,
                                                label = labelName
                                            }
                                    )
                        );
                })
                .SubscribeOnMainThread();
        }

        public bool Exists<T>(string assetName)
            where T : UnityEngine.Object
        {
            string _key = string.Format("{0}_{1}", assetName, typeof(T).FullName);
            return allAssets.Keys.ToList().Exists(_assetKey => _assetKey.EndsWith(_key));
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
                    new ResourceItem { path = InPath, type = ASSET_BUDDLE_DRIVER }
                },
                CUSTOM_RES_KEY
            );
            return bundles[CUSTOM_RES_KEY][getABFullPath(InPath)];
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
            return allAssets
                .Where(_ => _.Value.label == labelName)
                .Select(_ => _.Value.asset as T)
                .ToList();
        }

        public async Task<IEnumerable<AudioClip>> AppendAudioClips(IEnumerable<string> AudioPaths)
        {
            if (AudioPaths.Count() <= 0)
            {
                return new List<AudioClip>();
            }

            var _audioClips = await LoadAudioClips(AudioPaths);
            appendResources(_audioClips.OfType<AudioClip>(), CUSTOM_RES_KEY);
            return _audioClips;
        }

        public IObservable<IList<AudioClip>> LoadAudioClips(IEnumerable<string> AudioPaths)
        {
            return Observable.Zip(AudioPaths.Select(_path => this.LoadAudioClip(_path))).First();
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
            if (!Path.IsPathRooted(audioDir))
            {
                audioDir = PathUtils.GetExternalAbsolutePath(audioDir);
            }
            if (!Directory.Exists(audioDir))
            {
                Debug.LogWarning($"Directory not exists: {audioDir}");
                return null;
            }

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
            appendResources(new List<AudioClip> { _audioClip }.OfType<AudioClip>(), CUSTOM_RES_KEY);
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
            var _textures = await LoadTexture2Ds(TexturePathes);
            appendResources(_textures.OfType<Texture2D>(), CUSTOM_RES_KEY);
            return _textures;
        }

        public IObservable<IList<Texture2D>> LoadTexture2Ds(IEnumerable<string> TexturePathes)
        {
            return Observable.Zip(TexturePathes.Select(_path => this.LoadTexture2D(_path))).First();
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
            if (!Path.IsPathRooted(textureDir))
            {
                textureDir = PathUtils.GetExternalAbsolutePath(textureDir);
            }
            if (!Directory.Exists(textureDir))
            {
                Debug.LogWarning($"Directory not exists: {textureDir}");
                return null;
            }

            var _searchPatterns = searchPattern
                .Split('|')
                .Select(_pattern => _pattern.Replace("*", ""));
            return await AppendTexture2Ds(
                Directory
                    .GetFiles(textureDir, "*.*", searchOption)
                    .Where(_path => _searchPatterns.Contains(Path.GetExtension(_path).ToLower()))
            );
        }

        public IObservable<IList<Texture2D>> LoadTexture2Ds(
            string textureDir,
            string searchPattern = "*.png|*.jpg|*.jpeg",
            SearchOption searchOption = SearchOption.TopDirectoryOnly
        )
        {
            var _searchPatterns = searchPattern
                .Split('|')
                .Select(_pattern => _pattern.Replace("*", ""));

            return Observable
                .Zip(
                    Directory
                        .GetFiles(textureDir, "*.*", searchOption)
                        .Where(
                            _path => _searchPatterns.Contains(Path.GetExtension(_path).ToLower())
                        )
                        .Select(_path => this.LoadTexture2D(_path))
                )
                .First();
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
            // resources = resources
            //     .Select(_ =>
            //     {
            //         return new KeyValuePair<string, Dictionary<string, UnityEngine.Object>>(
            //             _.Key,
            //             _.Value
            //                 .Where(_1 => _1.Value != null)
            //                 .ToDictionary(_1 => _1.Key, _2 => _2.Value)
            //         );
            //     })
            //     .ToDictionary(_ => _.Key, _ => _.Value);
        }

        private T getResource<T>(Dictionary<string, AssetItem> assets, string assetPath)
            where T : UnityEngine.Object
        {
            assetPath = assetPath.ToForwardSlash();

            string _key = string.Format("{0}_{1}", assetPath, typeof(T).FullName);
            UNIHperLogger.Log($"Try to get resource with key: {_key}");

            // 先从当前资源实例中查找，如果存在直接返回
            if (assets.ContainsKey(_key))
            {
                return assets[_key].asset as T;
            }

            _key = $"/{_key}";
            string _findKey = assets.Keys.Where(_resKey => _resKey.EndsWith(_key)).FirstOrDefault();
            if (_findKey != null)
            {
                return assets[_findKey].asset as T;
            }

            _key = _key.TrimStart('/');
            // 根据InResources的Key值进行正则匹配，结尾匹配InName即可
            _findKey = assets.Keys.Where(_resKey => _resKey.EndsWith(_key)).FirstOrDefault();
            if (_findKey != null)
            {
                return assets[_findKey].asset as T;
            }

            return null;
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

        private void readConfigData()
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
                        _configNode.Value
                            .Where(_resItem => _resItem.driver == RESOURCES_DRIVER)
                            .ToList()
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
                            .Where(_resItem => _resItem.driver == ASSET_BUDDLE_DRIVER)
                            .ToList()
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);

            // 应用层Addressable资源加载配置项
            addressableConfigData = _appConfigData
                .Select(_configNode =>
                {
                    return new KeyValuePair<string, List<ResourceItem>>(
                        _configNode.Key,
                        _configNode.Value
                            .Where(_resItem => _resItem.driver == ADDRESSABLE_DRIVER)
                            .ToList()
                    );
                })
                .ToDictionary(_ => _.Key, _ => _.Value);

            // 框架层资源加载配置项
            var _persistAsset = Resources.Load<TextAsset>("__Configs/Persistence/res");

            var _persistConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<
                List<ResourceItem>
            >(_persistAsset.text);
            persistConfigData = _persistConfigData
                .Where(_ => _.driver == RESOURCES_DRIVER)
                .ToList();

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
                    var _assetItems = _resources.Select(
                        _res =>
                            new AssetItem
                            {
                                path = System.IO.Path
                                    .Combine("Resources", _item.path, _res.name)
                                    .ToForwardSlash(),
                                asset = _res,
                                asssetDriver = AsssetDriver.Resources
                            }
                    );
                    appendResources(_assetItems, InResID);
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

                    var _assets = await (
                        GetType()
                            .GetMethod(
                                "LoadAddressableAssetsByLabel",
                                BindingFlags.NonPublic | BindingFlags.Instance
                            )
                            .MakeGenericMethod(new Type[] { _T })
                            .Invoke(this, new object[] { _resItem.label })
                        as IObservable<IEnumerable<AssetItem>>
                    );
                    appendResources(_assets, InResID);
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

        internal enum AsssetDriver
        {
            Resources,
            AssetBundle,
            Addressable
        }

        internal class AssetItem
        {
            public AsssetDriver asssetDriver;
            public string label = string.Empty;
            public string path;
            public UnityEngine.Object asset;
        }

        private void appendResources(IEnumerable<UnityEngine.Object> InResources, string InResID)
        {
            appendResources(
                InResources.Select(_ => new AssetItem { path = _.name, asset = _ }),
                InResID
            );
        }

        private void appendResources(IEnumerable<AssetItem> assetItems, string InResID)
        {
            if (!resources.ContainsKey(InResID))
                resources.Add(InResID, new Dictionary<string, AssetItem>());

            Action<AssetItem> _appendResource = (_resItem) =>
            {
                string _key = buildResKey(_resItem.asset, _resItem.path);
                if (resources[InResID].ContainsKey(_key))
                {
                    UNIHperLogger.LogWarning($"resource key can not duplicate, error key: {_key}");
                    return;
                }
                UNIHperLogger.Log($"{InResID} add asset {_key}");
                resources[InResID].Add(_key, _resItem);
            };

            foreach (var _resource in assetItems.Where(_ => _.asset != null))
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

        private static string removeFileExtension(string path)
        {
            // 获取文件名和目录
            string directory = Path.GetDirectoryName(path);
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);

            // 如果没有目录信息，只返回去掉扩展名的文件名
            if (string.IsNullOrEmpty(directory))
            {
                return filenameWithoutExtension;
            }

            // 否则返回包含目录和去掉扩展名的文件名的完整路径
            return Path.Combine(directory, filenameWithoutExtension).ToForwardSlash();
        }

        private string buildResKey(UnityEngine.Object resObj, string path = "")
        {
            var _resTypeName = resObj.GetType().FullName;
#if UNITY_EDITOR
            if (resObj is UnityEditor.Animations.AnimatorController)
            {
                _resTypeName = typeof(UnityEngine.RuntimeAnimatorController).FullName;
            }
#endif

            return string.Format("{0}_{1}", removeFileExtension(path), _resTypeName);
        }

        private string getCurrentSceneName()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
    }
}
