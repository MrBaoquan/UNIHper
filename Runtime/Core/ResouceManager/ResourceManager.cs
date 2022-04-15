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
using FairyGUI;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace UNIHper {
    public class ResourceItem {
        public string driver = "Resources";
        public string type;
        public string path;
        public string label;
    }

    public class ResourceManager : Singleton<ResourceManager> {
        const string CUSTOM_RES_KEY = "__custom";
        // 自定义资源配置
        private Dictionary<string, List<ResourceItem>> resourcesConfigData;

        // 资源 AB包配置项
        private Dictionary<string, List<ResourceItem>> assetBundlesConfigData = new Dictionary<string, List<ResourceItem>> ();

        // 可寻址资源配置项
        private Dictionary<string, List<ResourceItem>> addressableConfigData = new Dictionary<string, List<ResourceItem>> ();

        // 框架层持久性资源配置项
        private List<ResourceItem> persistConfigData;

        /// <summary>
        ///  所有已加载资源实例 [Persistence,CUSTOM_RES_KEY,SCENE_NAME]
        /// </summary>
        private Dictionary<string, Dictionary<string, UnityEngine.Object>> resources;
        // 所有AB包实例
        private Dictionary<string, Dictionary<string, AssetBundle>> bundles = new Dictionary<string, Dictionary<string, AssetBundle>> ();

        // 所有加载的uipackages
        private Dictionary<string, Dictionary<string, UIPackage>> packages = new Dictionary<string, Dictionary<string, UIPackage>> ();

        internal async Task Initialize () {
            UNIHperLogger.Log ("ResourceManager Initializing ...");
            this.ReadConfigData ();
            resources = new Dictionary<string, Dictionary<string, UnityEngine.Object>> ();

            resources = resourcesConfigData.Keys
                .Select (_key => new KeyValuePair<string, Dictionary<string, UnityEngine.Object>> (_key, new Dictionary<string, UnityEngine.Object> ()))
                .ToDictionary (_ => _.Key, _ => _.Value);

            // 框架层持久性资源
            await loadResourceAssets (persistConfigData, "Persistence");
            UNIHperLogger.Log ("load framework persistence assets finished");
            // 应用层持久性资源
            await this.LoadAssetByKey ("Persistence");
            UNIHperLogger.Log ("load application persistence assets finished");
            // 加载场景资源
            await this.LoadSceneResources ();
            UNIHperLogger.Log ("load scene assets finished");
        }

        /// <summary>
        /// 加载当前场景资源
        /// </summary>
        internal async Task LoadSceneResources () {
            string _sceneName = getCurrentSceneName ();
            await this.LoadSceneResources (_sceneName);
        }

        /// <summary>
        /// 加载{InSceneName}场景资源
        /// </summary>
        /// <param name="InSceneName"></param>
        internal async Task LoadSceneResources (string InSceneName) {
            Debug.Log ("Load scene assets " + InSceneName);
            await this.LoadAssetByKey (InSceneName);
        }

        /// <summary>
        /// 卸载{InSceneName}场景资源
        /// </summary>
        /// <param name="InSceneName"></param>
        internal void UnloadSceneResources (string InSceneName) {
            this.UnLoadAssetByKey (InSceneName);
        }

        /// <summary>
        /// 获取资源名为[InResName]的资源
        /// </summary>
        /// <param name="InResName"></param>
        /// <returns></returns>
        public List<UnityEngine.Object> Get (string InResName) {
            var _resources = resources.Keys
                .Where (_key => new List<string> () { "Persistence", getCurrentSceneName (), CUSTOM_RES_KEY }.Contains (_key))
                .Select (_key => resources[_key])
                .SelectMany (_v => _v);
            return _resources.Where (_res => _res.Key.EndsWith ($"@{InResName}"))
                .Select (_res => _res.Value)
                .ToList ();
        }

        /// <summary>
        /// 获取资源名为{InResName},类型为{InResType}的资源
        /// </summary>
        /// <param name="InResName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T> (string InResName) where T : UnityEngine.Object {
            // 1. 从持久层资源查询
            T _resource = GetPersistRes<T> (InResName);
            if (_resource == null) {
                // 2. 从场景层资源查询
                _resource = GetSceneRes<T> (InResName);
            }

            // 3. 从自定义资源查询(用户通过ResourceManager主动添加的资源)
            if (_resource == null) {
                _resource = GetCustomRes<T> (InResName);
            }

            if (_resource == null) {
                UNIHperLogger.LogWarning ($"can not find asset with name: {InResName}");
                return null;
            }
            return _resource;
        }

        public IObservable<T> LoadAddessableAssetAsync<T> (string InResKey) {
            return Addressables.LoadAssetAsync<T> (InResKey)
                .Task
                .ToObservable ()
                .ObserveOn (Scheduler.MainThread);
        }

        /// <summary>
        /// 加载addressable资源
        /// </summary>
        /// <param name="InResKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IObservable<IList<T>> LoadAddressableAssetsAsync<T> (string InResKey) {
            return Addressables.LoadAssetsAsync<T> (InResKey, null)
                .Task
                .ToObservable ()
                .ObserveOn (Scheduler.MainThread);
        }

        /// <summary>
        /// 资源转为基类Object
        /// </summary>
        /// <param name="InResKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private IObservable<IList<UnityEngine.Object>> loadAddressableAssetsAsync<T> (string InResKey) {
            return LoadAddressableAssetsAsync<T> (InResKey)
                .Select (_item => _item.Select (_ => _ as UnityEngine.Object).ToList ());
        }

        /// <summary>
        /// 在指定的{InSceneName}场景中获取资源名为{InResName},类型为{InResType}的资源
        /// </summary>
        /// <param name="InName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSceneRes<T> (string InName) where T : UnityEngine.Object {
            string _sceneName = getCurrentSceneName ();
            Dictionary<string, UnityEngine.Object> _resources;
            if (!resources.TryGetValue (_sceneName, out _resources)) {
                return null;
            }

            return getResource<T> (_resources, InName);
        }

        /// <summary>
        /// 获取持久区资源名为{InName},类型为T的资源
        /// </summary>
        /// <param name="InName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPersistRes<T> (string InName) where T : UnityEngine.Object {
            Dictionary<string, UnityEngine.Object> _resources;
            if (!resources.TryGetValue ("Persistence", out _resources)) {
                Debug.LogWarning ("There is no Persistence assets.");
                return null;
            }

            return getResource<T> (_resources, InName);
        }

        public T GetCustomRes<T> (string InName) where T : UnityEngine.Object {
            if (!resources.ContainsKey (CUSTOM_RES_KEY)) return null;
            return getResource<T> (resources[CUSTOM_RES_KEY], InName);
        }

        public bool Exists<T> (string InResKey) where T : UnityEngine.Object {
            foreach (var _res in resources) {
                if (_res.Value.ContainsKey ($"{typeof(T).FullName}_{InResKey}"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 加载AB包
        /// </summary>
        /// <param name="InPath"></param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundle (string InPath) {
            loadABAssets (new List<ResourceItem> { new ResourceItem { path = InPath, type = "AssetBundle" } }, CUSTOM_RES_KEY);
            return bundles[CUSTOM_RES_KEY][getABFullPath (InPath)];
        }

        /// <summary>
        /// 卸载AB包
        /// </summary>
        /// <param name="InPath"></param>
        public void UnloadAssetBundle (string InPath) {
            var _path = getABFullPath (InPath);
            bundles[CUSTOM_RES_KEY][_path].Unload (true);
            bundles[CUSTOM_RES_KEY].Remove (_path);
            RefreshResources ();
        }

        private void RefreshResources () {
            resources = resources.Select (_ => {
                return new KeyValuePair<string, Dictionary<string, UnityEngine.Object>> (_.Key, _.Value.Where (_1 => _1.Value != null).ToDictionary (_1 => _1.Key, _2 => _2.Value));
            }).ToDictionary (_ => _.Key, _ => _.Value);
        }

        /// <summary>
        /// Private Methods Below
        /// </summary>

        private T getResource<T> (Dictionary<string, UnityEngine.Object> InResources, string InName) where T : UnityEngine.Object {
            string _key = buildResKey (InName, typeof (T));
            UNIHperLogger.Log ($"try get asset: {_key}");
            if (!InResources.ContainsKey (_key)) return default (T);
            return InResources[_key] as T;
        }

        private string buildResKey (string InName, Type InType) {
            return string.Format ("{0}@{1}", InType.FullName, InName);
        }

        private void ReadConfigData () {
            string _resPath = UNIHperConfig.ResourceConfigPath;
            TextAsset _resAsset = Resources.Load<TextAsset> (_resPath);

            // 应用层自定义资源加载配置项
            var _appConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<ResourceItem>>> (_resAsset.text);

            resourcesConfigData = _appConfigData.Select (_configNode => {
                return new KeyValuePair<string, List<ResourceItem>> (_configNode.Key, _configNode.Value.Where (_resItem => _resItem.driver == "Resources").ToList ());
            }).ToDictionary (_ => _.Key, _ => _.Value);

            // 应用层自定义AB包
            var _appABDic = _appConfigData.Select (_configNode => {
                return new KeyValuePair<string, List<ResourceItem>> (_configNode.Key, _configNode.Value.Where (_resItem => _resItem.driver == "AssetBundle").ToList ());
            }).ToDictionary (_ => _.Key, _ => _.Value);

            addressableConfigData = _appConfigData.Select (_configNode => {
                return new KeyValuePair<string, List<ResourceItem>> (_configNode.Key, _configNode.Value.Where (_resItem => _resItem.driver == "Addressable").ToList ());
            }).ToDictionary (_ => _.Key, _ => _.Value);

            // 框架层资源加载配置项
            var _persistAsset = Resources.Load<TextAsset> ("Configs/Persistence/res");

            var _persistConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ResourceItem>> (_persistAsset.text);
            persistConfigData = _persistConfigData
                .Where (_ => _.driver == "Resources")
                .ToList ();

            // 框架层AB包
            var _persistABList = _persistConfigData.Except (persistConfigData).ToList ();

            // 初始化所有AB包
            assetBundlesConfigData.Add ("Persistence", _persistABList);
            foreach (var _ab in _appABDic) {
                if (!assetBundlesConfigData.ContainsKey (_ab.Key)) {
                    assetBundlesConfigData.Add (_ab.Key, _ab.Value);
                } else {
                    var _bundles = assetBundlesConfigData[_ab.Key];
                    assetBundlesConfigData[_ab.Key] = _bundles.Concat (_ab.Value).ToList ();
                }
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="InKey"></param>
        private async Task LoadAssetByKey (string InKey) {
            if (assetBundlesConfigData.ContainsKey (InKey)) {
                UNIHperLogger.Log ($"load ab assets {InKey}");
                await loadABAssetsAsync (assetBundlesConfigData[InKey], InKey);
            }
            // Resources资源加载
            if (resourcesConfigData.ContainsKey (InKey)) {
                UNIHperLogger.Log ($"load resources assets {InKey}");
                await loadResourceAssets (resourcesConfigData[InKey], InKey);
            }

            if (addressableConfigData.ContainsKey (InKey)) {
                UNIHperLogger.Log ($"load addressable assets {InKey}");
                await loadAddressableAssets (addressableConfigData[InKey], InKey);
            }

            await Task.CompletedTask;
        }

        // 加载Resources文件夹下资源包
        private async Task loadResourceAssets (List<ResourceItem> InItems, string InResID) {
            var _fairyGUIPackages = InItems.Where (_ => _.type == "FairyGUI").ToList ();

            // 加载FairyGUI资源包
            if (!packages.ContainsKey (InResID)) {
                packages.Add (InResID, new Dictionary<string, UIPackage> ());
            }
            _fairyGUIPackages.ForEach (_ => {
                packages[InResID].Add (_.path, UIPackage.AddPackage (_.path));
            });

            foreach (var _item in InItems.Except (_fairyGUIPackages)) {
                var _T = Type.GetType ("UnityEngine." + _item.type + ",UnityEngine");
                try {
                    UnityEngine.Object[] _resources = await Task.FromResult (Resources.LoadAll (_item.path, _T));
                    appendResources (_resources, InResID);
                } catch (Exception e) {
                    UNIHperLogger.LogError ($"load assets {_item.path} failed, {e.Message}");
                }
            }
            UNIHperLogger.Log ($"load resources {InResID} completed");
            await Task.CompletedTask;
        }

        // 加载Addressable资源包
        private async Task loadAddressableAssets (List<ResourceItem> InItems, string InResID) {
            if (InItems.Count <= 0) {
                await Task.CompletedTask;
                return;
            }

            var _fairyGUIPackages = InItems.Where (_ => _.type == "FairyGUI").ToList ();
            var _fairyGUIAssets = await Observable.Merge (
                _fairyGUIPackages.Select (_ => loadAddressableAssetsAsync<UnityEngine.Object> (_.label))
            ).ToTask ();

            appendResources (_fairyGUIAssets.ToArray (), InResID);

            _fairyGUIAssets.Where (_r => _r is TextAsset)
                .Select (_r => _r as TextAsset)
                .ToList ().ForEach (_obj => {
                    var _pack = UIPackage.AddPackage (_obj.bytes, _obj.name.Replace ("_fui", ""),
                        (string _name, string _extesion, Type _type, out DestroyMethod _destroyMethod) => {
                            _destroyMethod = DestroyMethod.None;
                            return Get (_name).FirstOrDefault () ?? null;
                        });
                });

            foreach (var _resItem in InItems.Except (_fairyGUIPackages)) {
                UNIHperLogger.Log ($"load addressable assets, label:{_resItem.label}");
                try {
                    var _T = Type.GetType ("UnityEngine." + _resItem.type + ",UnityEngine");
                    var _assets = await (GetType ().GetMethod ("loadAddressableAssetsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod (new Type[] { _T })
                        .Invoke (this, new object[] { _resItem.label }) as IObservable<List<UnityEngine.Object>>
                    );
                    appendResources (_assets.ToArray (), InResID);
                } catch (Exception /*_ex*/ ) {
                    UNIHperLogger.LogWarning ($"try load addressable assets {_resItem.label} failed");
                    continue;
                }
            }

            await Task.CompletedTask;
        }

        // 加载AB包
        private async Task loadABAssetsAsync (List<ResourceItem> InItems, string InResID) {
            loadABAssets (InItems, InResID);
            await Task.CompletedTask;
        }

        private void loadABAssets (List<ResourceItem> InItems, string InResID) {
            if (!bundles.ContainsKey (InResID)) bundles.Add (InResID, new Dictionary<string, AssetBundle> ());

            foreach (var _item in InItems) {
                var _path = getABFullPath (_item.path);
                AssetBundle _bundle = AssetBundle.LoadFromFile (_path);
                bundles[InResID].Add (_path, _bundle);
                UnityEngine.Object[] _resources = _bundle.LoadAllAssets ();
                appendResources (_resources, InResID);
            }
        }

        private string getABFullPath (string InPath) {
            return Path.Combine (Application.streamingAssetsPath, "AssetBundles", InPath);
        }

        private void appendResources (UnityEngine.Object[] InResources, string InResID) {
            if (!resources.ContainsKey (InResID)) resources.Add (InResID, new Dictionary<string, UnityEngine.Object> ());
            foreach (var _resource in InResources) {
                string _key = buildResKey (_resource.name, _resource.GetType ());
                if (resources[InResID].ContainsKey (_key)) {
                    UNIHperLogger.LogError ($"resource key can not duplicate, error key: {_key}");
                    continue;
                }
                UNIHperLogger.Log ($"{InResID} add asset {_key}");
                resources[InResID].Add (_key, _resource);
            }
        }

        private void UnLoadAssetByKey (string InKey) {
            if (resources.ContainsKey (InKey))
                resources[InKey].Clear ();
            if (bundles.ContainsKey (InKey)) {
                bundles[InKey].Values.ToList ().ForEach (_ => _?.Unload (true));
                bundles[InKey].Clear ();
            }
            if (packages.ContainsKey (InKey)) {
                packages[InKey].Values.ToList ().ForEach (_ => {
                    UIPackage.RemovePackage (_.id);
                });
                packages[InKey].Clear ();
            }
        }

        private string getCurrentSceneName () {
            return SceneManager.GetActiveScene ().name;
        }
    }

}