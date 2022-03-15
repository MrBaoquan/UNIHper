using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace DNHper
{
    
public static class UReflection
{
    public static string GetPropertyValue(string InField, object InObject)
    {
        try
        {
            Type _class = InObject.GetType();
            object o = _class.GetProperty(InField).GetValue(InObject, null);
            string Value = Convert.ToString(o);
            if (string.IsNullOrEmpty(Value)) return null;
            return Value;
        }
        catch
        {
            return null;
        }
    }

    public static bool SetFieldValue<T>( object InObject, string InField,T Value)
    {
        try
        {
            Type _class = InObject.GetType();
            FieldInfo _fieldInfo = _class.GetField(InField);
            object _safeValue = Convert.ChangeType(Value, _fieldInfo.FieldType);
            _fieldInfo.SetValue(InObject,_safeValue);
            return true;
        }
        catch(Exception exc)
        {
            NLogger.Error(exc.Message);
            return false;
        }
    }

    public static void SetPrivateField<T>(object InTarget, string InField, T Value)
    {
        try
        {
            Type _class = InTarget.GetType();
            FieldInfo _fieldInfo = _class.GetField(InField,BindingFlags.NonPublic | BindingFlags.Instance);
            object _safeValue = Convert.ChangeType(Value,_fieldInfo.FieldType);
            _fieldInfo.SetValue(InTarget, _safeValue);
        }
        catch (System.Exception ex)
        {
            NLogger.Error(ex);
        }
    }

    public static void CallPrivateMethod(object InTarget, string InMethodName, params object[] InParams)
    {
        try
        {
            var _methodInfo = InTarget.GetType()
                .GetMethod(InMethodName, BindingFlags.NonPublic | BindingFlags.Instance);
            _methodInfo.Invoke(InTarget, InParams);
        }
        catch (System.Exception ex)
        {
            NLogger.Warn(InMethodName);
            NLogger.Error(ex);
        }
    }

    public static System.Type[] SubClasses(Type InBaseClass)
    {
        return Assembly.GetAssembly(InBaseClass).GetTypes().Where(_type=>_type.IsSubclassOf(InBaseClass)).ToArray();
    }

    public static List<System.Type> SubClasses(this Assembly InAssembly, Type InBaseClass){
        return InAssembly.GetTypes().Where(_type=>_type.IsSubclassOf(InBaseClass)).ToList();
    }


}

}