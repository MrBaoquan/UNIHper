using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace UNIHper.GhostComponent {
    public class GhostData : ScriptableObject {
        const string defaultConfigPath = "Assets/Resources/Ghost.asset";
        /// <summary>
        /// true if ghost mode now
        /// </summary>
        [HideInInspector]
        public bool bGhost = false;

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