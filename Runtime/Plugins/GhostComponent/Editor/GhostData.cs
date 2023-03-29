using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace UNIHper.GhostComponent {

    [Serializable]
    public class GhostMeta {
        [SerializeField]
        public string AssetID;

        [SerializeField]
        public string GUID;

        [SerializeField]
        public bool HasEntity = true;
    }

    public class GhostData : ScriptableObject {
        const string defaultConfigPath = "Assets/Resources/Ghost.asset";
        /// <summary>
        /// true if ghost mode now
        /// </summary>
        [HideInInspector]
        public bool bGhost = false;

        public List<string> excludeDirectories = new List<string> () { };

        [SerializeField, HideInInspector]
        private List<GhostMeta> ghostMetas = new List<GhostMeta> () { };

        /// <summary>
        /// 清除幽灵数据缓存
        /// </summary>
        public void ClearGhostMetas () {
            ghostMetas.Clear ();
        }

        public void MarkAsGhost () {
            ghostMetas.ForEach (_meta => _meta.HasEntity = false);
        }

        public GhostMeta GetGhostMeta (string assetID) {
            var _ghostMeta = ghostMetas.FirstOrDefault (_ghostMeta => _ghostMeta.AssetID == assetID);
            if (_ghostMeta is null) {
                _ghostMeta = new GhostMeta () { AssetID = assetID };
                ghostMetas.Add (_ghostMeta);
            }
            return _ghostMeta;
        }

        private static GhostData instance = null;
        public static GhostData Instance {
            get {
                if (instance) return instance;

                var _ghostData = AssetDatabase.FindAssets ("t:GhostData", new [] { "Assets" }).FirstOrDefault ();
                if (_ghostData is null) {
                    AssetDatabase.CreateAsset (ScriptableObject.CreateInstance<GhostData> (), defaultConfigPath);
                    Debug.Log ("create ghost config data successfully");
                }
                instance = AssetDatabase.LoadAssetAtPath<GhostData> (defaultConfigPath);
                return instance;
            }

        }
    }
}