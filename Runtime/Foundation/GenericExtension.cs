using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UNIHper
{
    public static class GenericExtension
    {
        public static List<T> Clone<T>(this List<T> InList)
        {
            T[] _newArr = new T[InList.Count];
            InList.CopyTo(_newArr);
            return _newArr.ToList();
        }

        public static string ToLogString<T>(this List<T> InList)
        {
            if (InList.Count <= 0)
            {
                return "[]";
            }
            string _str = "[";
            InList.ForEach(_ =>
            {
                _str += (_.ToString() + ", ");
            });
            _str = _str.Substring(0, _str.Length - 2);
            return _str + "]";
        }
    }
}
