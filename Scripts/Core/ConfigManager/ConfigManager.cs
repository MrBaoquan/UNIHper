using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Xml.Serialization;
using System.IO;
using UnityEngine;

namespace UNIHper
{

public class ConfigManager : Singleton<ConfigManager>
{
    private Dictionary<string,UConfig> configs = new Dictionary<string, UConfig>();

    private const string configDir = "Configs";
    public void Initialize()
    {
        Type[] _configClasses =  AssemblyConfig.GetSubClasses(typeof(UConfig)).ToArray();// UReflection.SubClasses(typeof(UConfig));

        foreach (var _configClass in _configClasses)
        {
            UConfig _configInstance =  Activator.CreateInstance(_configClass) as UConfig;
            string _configDir = Path.Combine(Application.persistentDataPath, configDir);

            var _attributes = Attribute.GetCustomAttributes(_configClass);
            var _attribute = _attributes.Where(_attr=>_attr is SerializedAt).First();
            
            if(_attribute!=null){
                var _saveTo = (_attribute as SerializedAt).SaveTo;
                if(_saveTo==UAppPath.StreamingDir){
                    _configDir = Path.Combine(Application.streamingAssetsPath, configDir);
                }
            }

            if(!Directory.Exists(_configDir)){
                Directory.CreateDirectory(_configDir);
            }
            string _path = Path.Combine(_configDir,_configClass.Name+".xml");


            if(!File.Exists(_path)){
                USerialization.SerializeXML(_configInstance,_path);
            }else{
                MethodInfo _method = typeof(USerialization).GetMethod("DeserializeXML").MakeGenericMethod(new Type[]{_configClass});
                _configInstance = _method.Invoke(null,new object[]{_path}) as UConfig;
            }

            UReflection.SetPrivateField(_configInstance,"__path",_path);
            this.configs.Add(_configClass.Name, _configInstance);
        }
    }

    public void SerializeAll()
    {
        this.configs.Values.ToList().ForEach(_config=>{
            USerialization.SerializeXML(_config,_config.__Path);
        });
    }

    public T Get<T>() where T:class
    {
        string _configName = typeof(T).Name;
        UConfig _config;
        if(this.configs.TryGetValue(_configName,out _config)){
            return _config as T;
        }
        return null;
    }

    public bool Serialize<T>()
    {
        string _configName = typeof(T).Name;
        UConfig _config;
        if(this.configs.TryGetValue(_configName,out _config)){
            USerialization.SerializeXML(_config,_config.__Path);
            return true;
        }
        return false;
    }
}

}
