using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DNHper;
using Newtonsoft.Json;
using UnityEngine;

namespace UNIHper
{
    [System.Serializable]
    public class UType
    {
        public string Name;
        public string FullName;
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

        public static Type GetUType(string InTypeName)
        {
            return Self().getUType(InTypeName);
        }

        public static List<Type> GetSubClasses(Type InBaseType)
        {
            return Self().getSubClasses(InBaseType);
        }

        public List<string> Assemblies;
        public List<string> filterBaseTypes = new List<string>
        {
            typeof(UIBase).AssemblyQualifiedName,
            typeof(SceneScriptBase).AssemblyQualifiedName,
            typeof(UConfig).AssemblyQualifiedName
        };
        public List<UType> CachedTypes;
        private Dictionary<string, string> allTypes
        {
            get
            {
                return CachedTypes
                    .GroupBy(_type => _type.Name)
                    .Select(_group => _group.First())
                    .ToDictionary(_1 => _1.Name, _2 => _2.FullName);
            }
        }

        private Dictionary<string, List<Type>> allTypeMaps = new Dictionary<string, List<Type>>();

        private Type getUType(string InTypeName)
        {
            if (!allTypes.ContainsKey(InTypeName))
            {
                return null;
            }
            return Type.GetType(allTypes[InTypeName]);
        }

        public void refresh()
        {
            Assemblies.Clear();
            allTypeMaps.Clear();
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
            if (!allTypeMaps.ContainsKey(InBaseType.AssemblyQualifiedName))
                return new List<Type>();
            return allTypeMaps[InBaseType.AssemblyQualifiedName];
        }

        public void Awake() { }

        public void OnDestroy() { }

        public void LoadNewAssembly(string assemblyName)
        {
            var _allTypes = allTypes;
            var _assembly = Assembly.Load(assemblyName);
            //var _assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (_assembly == null)
            {
                UnityEngine.Debug.LogWarningFormat("can not load {0}", assemblyName);
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
                    _filterTypes.ForEach(_type =>
                    {
                        if (UNIHperSettings.ShowDebugLog)
                        {
                            UnityEngine.Debug.LogFormat(
                                $"Add {assemblyName} -> Type:{_type.FullName} ",
                                _type.Name,
                                _type.FullName
                            );
                        }
                        if (!_allTypes.ContainsKey(_type.Name))
                        {
                            CachedTypes.Add(
                                new UType
                                {
                                    Name = _type.Name,
                                    FullName = _type.AssemblyQualifiedName
                                }
                            );
                        }
                    });
                    if (!allTypeMaps.ContainsKey(_filterType.AssemblyQualifiedName))
                    {
                        allTypeMaps.Add(_filterType.AssemblyQualifiedName, new List<Type>());
                    }
                    allTypeMaps[_filterType.AssemblyQualifiedName].AddRange(_filterTypes);
                });
        }
    }
}
