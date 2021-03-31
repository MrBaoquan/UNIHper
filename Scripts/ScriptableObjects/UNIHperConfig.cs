using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu (fileName = "UNIHperConfig", menuName = "UNIHper/Assets/UNIHperConfig", order = 1)]
public class UNIHperConfig : ScriptableObject {
    private static UNIHperConfig instance = null;
    private static UNIHperConfig Self () {
        if (instance == null)
            instance = Resources.Load<UNIHperConfig> ("UNIHperConfig");
        return instance;
    }

    public static string ResourceConfigPath {
        get {
            return Self ().resPath;
        }
    }

    public static string UIConfigPath {
        get {
            return Self ().uiPath;
        }
    }

    public static string AssemblyConfigPath {
        get {
            return Self ().assemblyPath;
        }
    }

    public string resPath = "UNIHper/resources";
    public string uiPath = "UNIHper/uis";
    public string assemblyPath = "UNIHper/assemblies";
}