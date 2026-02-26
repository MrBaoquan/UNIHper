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

        /// <summary>
        /// 从集合中随机获取一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <returns>随机选中的元素，如果集合为空则返回 default(T)</returns>
        public static T RandomElement<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                Debug.LogWarning("[GenericExtension] RandomElement: source is null");
                return default(T);
            }

            var list = source as IList<T> ?? source.ToList();
            if (list.Count == 0)
            {
                Debug.LogWarning("[GenericExtension] RandomElement: collection is empty");
                return default(T);
            }

            var randomIndex = Random.Range(0, list.Count);
            return list[randomIndex];
        }

        /// <summary>
        /// 从集合中随机获取指定数量的不重复元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">要获取的元素数量</param>
        /// <returns>随机选中的元素列表</returns>
        public static List<T> RandomElements<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
            {
                Debug.LogWarning("[GenericExtension] RandomElements: source is null");
                return new List<T>();
            }

            var list = source as IList<T> ?? source.ToList();
            if (count > list.Count)
            {
                Debug.LogWarning($"[GenericExtension] RandomElements: requested {count} elements but only {list.Count} available");
                count = list.Count;
            }

            var result = new List<T>();
            var indices = Enumerable.Range(0, list.Count).ToList();

            for (int i = 0; i < count; i++)
            {
                var randomIndex = Random.Range(0, indices.Count);
                result.Add(list[indices[randomIndex]]);
                indices.RemoveAt(randomIndex);
            }

            return result;
        }
    }
}
