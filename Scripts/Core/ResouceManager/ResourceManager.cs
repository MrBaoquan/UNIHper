using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UHelper {
    public struct ResourceItem {
        public string type;
        public string path;
    }

    public class ResourceManager : Singleton<ResourceManager>, Manageable {
        const string CUSTOM_RES_KEY = "__custom";
        // 自定义资源配置
        private Dictionary<string, List<ResourceItem>> customConfigData;
        private List<ResourceItem> persistConfigData;

        // 所有已加载资源实例
        private Dictionary<string, Dictionary<string, UnityEngine.Object>> resources;

        // 资源 AB包配置项
        private Dictionary<string, List<ResourceItem>> bundleConfigData = new Dictionary<string, List<ResourceItem>> ();
        // 所有AB包实例
        private Dictionary<string, Dictionary<string, AssetBundle>> bundles = new Dictionary<string, Dictionary<string, AssetBundle>> ();

        public void Initialize () {
            this.ReadConfigData ();
            resources = new Dictionary<string, Dictionary<string, UnityEngine.Object>> ();
            foreach (var _resource in customConfigData) {
                resources.Add (_resource.Key, new Dictionary<string, UnityEngine.Object> ());
            }
            LoadResourceAssets (persistConfigData, "Persistence");
            this.LoadAssetByKey ("Persistence");
            this.LoadSceneResources ();
        }

        public void Uninitialize () {

        }

        // 加载当前场景资源
        public void LoadSceneResources () {
            string _sceneName = getCurrrentSceneName ();
            this.LoadAssetByKey (_sceneName);
        }

        public void LoadSceneResources (string InSceneName) {
            Debug.Log ("Load scene assets " + InSceneName);
            this.LoadAssetByKey (InSceneName);
        }

        public void UnloadSceneResources (string InSceneName) {
            this.UnLoadAssetByKey (InSceneName);
        }

        public T Get<T> (string InResName) where T : UnityEngine.Object {
            // 1. 从持久层资源查询
            T _resource = GetPersistRes<T> (InResName);
            if (_resource == null) {
                // 2. 从场景层资源查询
                _resource = GetSceneRes<T> (InResName);
            }

            if (_resource == null) {
                _resource = GetCustomRes<T> (InResName);
            }

            if (_resource == null) {
                Debug.LogWarningFormat ("Can not find asset with name: {0}", InResName);
                return null;
            }
            return _resource;
        }

        public T GetSceneRes<T> (string InName) where T : UnityEngine.Object {
            string _sceneName = getCurrrentSceneName ();
            Dictionary<string, UnityEngine.Object> _resources;
            if (!resources.TryGetValue (_sceneName, out _resources)) {
                return null;
            }

            return getResource<T> (_resources, InName);
        }

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

        public bool Exists (string InResKey) {
            foreach (var _res in resources) {
                if (_res.Value.ContainsKey (InResKey))
                    return true;
            }
            return false;
        }

        public AssetBundle LoadAssetBundle (string InPath) {
            LoadABAssets (new List<ResourceItem> { new ResourceItem { path = InPath, type = "AssetBundle" } }, CUSTOM_RES_KEY);
            return bundles[CUSTOM_RES_KEY][getABFullPath (InPath)];
        }

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
            string _key = string.Format ("{0}_{1}", typeof (T).FullName, InName);
            if (!InResources.ContainsKey (_key)) return default (T);
            return InResources[_key] as T;
        }

        private void ReadConfigData () {
            string _resPath = UHelperConfig.ResourceConfigPath;
            TextAsset _resAsset = Resources.Load<TextAsset> (_resPath);
            // 逻辑层自定义资源加载配置项
            var _logicConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<ResourceItem>>> (_resAsset.text);

            customConfigData = _logicConfigData.Select (_ => {
                return new KeyValuePair<string, List<ResourceItem>> (_.Key, _.Value.Where (_1 => _1.type != "AssetBundle").ToList ());
            }).ToDictionary (_ => _.Key, _ => _.Value);

            // 逻辑层自定义AB包
            var _customABDic = _logicConfigData.Select (_ => {
                return new KeyValuePair<string, List<ResourceItem>> (_.Key, _.Value.Where (_1 => _1.type == "AssetBundle").ToList ());
            }).ToDictionary (_ => _.Key, _ => _.Value);

            // 框架层资源加载配置项
            var _persistAsset = Resources.Load<TextAsset> ("Configs/Persistence/res");
            var _coreConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ResourceItem>> (_persistAsset.text);
            persistConfigData = _coreConfigData.Where (_ => _.type != "AssetBundle").ToList ();

            // 框架层AB包
            var _coreABList = _coreConfigData.Except (persistConfigData).ToList ();

            // 初始化所有AB包
            bundleConfigData.Add ("Persistence", _coreABList);
            foreach (var _ab in _customABDic) {
                if (!bundleConfigData.ContainsKey (_ab.Key)) {
                    bundleConfigData.Add (_ab.Key, _ab.Value);
                } else {
                    var _bundles = bundleConfigData[_ab.Key];
                    bundleConfigData[_ab.Key] = _bundles.Concat (_ab.Value).ToList ();
                }
            }
        }

        private void LoadAssetByKey (string InKey) {
            if (bundleConfigData.ContainsKey (InKey)) {
                LoadABAssets (bundleConfigData[InKey], InKey);
            }

            if (customConfigData.ContainsKey (InKey)) {
                LoadResourceAssets (customConfigData[InKey], InKey);
            }

        }

        // 加载Resources文件夹下资源包
        private void LoadResourceAssets (List<ResourceItem> InItems, string InResID) {
            foreach (var _item in InItems) {
                var _T = Type.GetType ("UnityEngine." + _item.type + ",UnityEngine");
                UnityEngine.Object[] _resources = Resources.LoadAll (_item.path, _T);
                appendResources (_resources, InResID);
            }
        }

        // 加载AB包
        private void LoadABAssets (List<ResourceItem> InItems, string InResID) {
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
                //Debug.LogFormat("{0} Add resource {1}", InResID, _resource.name);
                if (resources[InResID].ContainsKey (_resource.name)) {
                    Debug.LogErrorFormat ("resource key can not duplicate, error key: {0}", _resource.name);
                    continue;
                }
                string _key = string.Format ("{0}_{1}", _resource.GetType ().FullName, _resource.name);
                resources[InResID].Add (_key, _resource);
            }
        }

        private void UnLoadAssetByKey (string InKey) {
            resources[InKey].Clear ();

        }

        private string getCurrrentSceneName () {
            return SceneManager.GetActiveScene ().name;
        }
    }

}