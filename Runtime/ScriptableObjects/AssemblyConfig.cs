using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DNHper;
using Newtonsoft.Json;
using UNIHper.UI;
using UnityEngine;

namespace UNIHper
{
    [System.Serializable]
    public class UNIType
    {
        public string ID; // example: UNIHper.IdleUI
        public string FullName;
        public string Name => ID.Split(".").Last();
    }

    public class AssemblyConfig : ScriptableObject
    {
        private static AssemblyConfig instance = null;

        private static AssemblyConfig Self()
        {
            if (instance == null)
                instance = Resources.Load<AssemblyConfig>("Assemblies");
            return instance;
        }

        public static void Refresh()
        {
            Self().refresh();
        }

        public static Type GetUNIType(string InTypeName)
        {
            var _typeName = Regex.Replace(InTypeName, @"#[^#]+$", string.Empty);
            return Self().getUNIType(_typeName);
        }

        public static List<Type> GetSubClasses(Type InBaseType)
        {
            return Self().getSubClasses(InBaseType);
        }

        public List<string> Assemblies;
        public List<string> filterBaseTypes = new List<string>()
        {
            typeof(UIBase).AssemblyQualifiedName,
            typeof(SceneScriptBase).AssemblyQualifiedName,
            typeof(UConfig).AssemblyQualifiedName
        };

        public List<UNIType> CachedTypes;

        private Dictionary<string, List<Type>> allTypesMap = new Dictionary<string, List<Type>>();

        private Dictionary<string, Type> typeNamesMap { get; set; }

        private Dictionary<string, Type> typeNamesWithAssemblyMap { get; set; }

        private Type getUNIType(string typeName)
        {
            if (typeName.Contains("."))
            {
                if (typeNamesWithAssemblyMap.ContainsKey(typeName))
                    return typeNamesWithAssemblyMap[typeName];
                return null;
            }
            if (typeNamesMap.ContainsKey(typeName))
                return typeNamesMap[typeName];
            return null;
        }

        public void refresh()
        {
            filterBaseTypes.Clear();

            filterBaseTypes.Add(typeof(UIBase).AssemblyQualifiedName);
            filterBaseTypes.Add(typeof(SceneScriptBase).AssemblyQualifiedName);
            filterBaseTypes.Add(typeof(UConfig).AssemblyQualifiedName);

            Assemblies.Clear();
            allTypesMap.Clear();
            CachedTypes.Clear();
            var _internalAssemblies = getAssemblies("__Configs/assemblies");
            var _customAssemblies = getAssemblies(UNIHperSettings.AssemblyConfigPath);

            _internalAssemblies
                .Concat(_customAssemblies)
                .ToList()
                .ForEach(_assemblyName =>
                {
                    LoadNewAssembly(_assemblyName);
                });

            typeNamesMap = allTypesMap
                .SelectMany(_kv => _kv.Value)
                .GroupBy(_type => _type.Name)
                .Select(_group => _group.First())
                .ToDictionary(_type => _type.Name, _type => _type);

            typeNamesWithAssemblyMap = allTypesMap
                .SelectMany(_kv => _kv.Value)
                .ToDictionary(_type => GetTypeUniqueID(_type), _type => _type);
        }

        private List<string> getAssemblies(string InResPath)
        {
            var _asset = Resources.Load<TextAsset>(InResPath);
            if (_asset == null)
            {
                Debug.LogWarningFormat("can not load module: {0}", InResPath);
                return new List<string>();
            }
            return JsonConvert.DeserializeObject<List<string>>(_asset.text);
        }

        private List<Type> getSubClasses(Type InBaseType)
        {
            if (!allTypesMap.ContainsKey(InBaseType.AssemblyQualifiedName))
                return new List<Type>();
            return allTypesMap[InBaseType.AssemblyQualifiedName];
        }

        public static string GetTypeUniqueID(Type InType, int instanceID = 0)
        {
            var _instanceID = instanceID > 0 ? $"#{instanceID}" : "";
            return $"{InType.Assembly.GetName().Name}.{InType.Name}{_instanceID}";
        }

        /// <summary>
        /// 加载新的程序集
        /// </summary>
        /// <param name="assemblyName"></param>
        public void LoadNewAssembly(string assemblyName)
        {
            Assembly _assembly = null;
            try
            {
                _assembly = Assembly.Load(assemblyName);
            }
            catch (Exception)
            {
                UNIHperLogger.LogWarning($"Can not load assembly: {assemblyName}");
                return;
            }

            if (!Assemblies.Contains(assemblyName))
            {
                Assemblies.Add(assemblyName);
            }

            filterBaseTypes
                .Select(_filterTypeString => Type.GetType(_filterTypeString))
                .ToList()
                .ForEach(_filterType =>
                {
                    var _filterTypes = _assembly.SubClasses(_filterType).ToList();
                    if (!allTypesMap.ContainsKey(_filterType.AssemblyQualifiedName))
                    {
                        allTypesMap.Add(_filterType.AssemblyQualifiedName, new List<Type>());
                    }
                    allTypesMap[_filterType.AssemblyQualifiedName].AddRange(_filterTypes);
                });
        }
    }
}
