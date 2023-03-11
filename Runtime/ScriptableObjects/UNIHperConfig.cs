using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UNIHper {

    public enum ConfigDriver {
        XML,
        YAML
    }

    public class UNIHperConfig : ScriptableObject {
        private static UNIHperConfig instance = null;
        private static UNIHperConfig Self () {
            if (instance == null) {
                instance = Resources.Load<UNIHperConfig> ("UNIHperConfig") ?? ScriptableObject.CreateInstance<UNIHperConfig> ();
            }

            return instance;
        }

        public static string ResourceConfigPath {
            get => Self ().resPath;
        }

        public static string UIConfigPath {
            get => Self ().uiPath;
        }

        public static string AssemblyConfigPath {
            get => Self ().assemblyPath;
        }

        public static ConfigDriver ConfigDriver {
            get => Self ().configDriver;
        }

        public static bool ShowDebugLog {
            get => Self ().showDebugMessage;
        }

        public string resPath = "UNIHper/resources";
        public string uiPath = "UNIHper/uis";
        public string assemblyPath = "UNIHper/assemblies";

        [Serializable]
        public class VariableHolder {
            public bool var1;
            public float var2 = 150f;
            public float var3 = 25f;
        }
        public bool showDebugMessage = false;
        public ConfigDriver configDriver = ConfigDriver.XML;
    }

}