using System;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper {

    /// <summary>
    /// 数组索引迭代器 迭代范围 [Min,Max)
    /// </summary>
    public class Indexer {
        private int maxIndex;
        public int Max { get => maxIndex; }
        private int minIndex = 0;
        public int Min { get => minIndex; }
        private int current = 0;
        public int Current {
            get { return current; }
        }

        private UnityEvent<int> onIndexChanged = new UnityEvent<int> ();
        public IObservable<int> OnIndexChangedAsObservable () {
            return onIndexChanged.AsObservable ();
        }

        public Indexer (int Max) {
            SetMax (Max);
        }

        public Indexer (int Min, int Max) {
            SetMin (Min);
            SetMax (Max);
        }

        /// <summary>
        /// 设置索引最大值
        /// </summary>
        /// <param name="Max">索引最大值</param>
        public void SetMax (int Max) {
            maxIndex = Max;
        }

        /// <summary>
        /// 设置索引最小值
        /// </summary>
        /// <param name="Min">索引最小值</param>
        public void SetMin (int Min) {
            minIndex = Min;
        }

        /// <summary>
        /// 设置当前索引值
        /// </summary>
        /// <param name="Current"></param>
        /// <returns></returns>
        public int Set (int Current) {
            if (Current < minIndex || Current > maxIndex) {
                onIndexChanged.Invoke (current);
                return current;
            }
            current = Current;
            onIndexChanged.Invoke (current);
            return current;
        }

        public int Next () {
            if (Max == 0) {
                onIndexChanged.Invoke (0);
                return 0;
            }
            current = (int) Mathf.Repeat (current + 1, Max + 1);
            onIndexChanged.Invoke (current);
            return current;
        }

        /// <summary>
        /// 将索引置为第一个
        /// </summary>
        /// <returns></returns>
        public int Step2First () {
            current = Min;
            onIndexChanged.Invoke (current);
            return current;
        }

        /// <summary>
        /// 将索引置位最后一个
        /// </summary>
        /// <returns></returns>
        public int Step2Last () {
            current = Max;
            onIndexChanged.Invoke (current);
            return current;
        }

        public int Prev () {
            if (Max == 0) {
                onIndexChanged.Invoke (0);
                return 0;
            }
            current = (int) Mathf.Repeat (current - 1, Max + 1);
            Debug.Log ("prev: " + current + " max:" + (Max + 1));
            onIndexChanged.Invoke (current);
            return current;
        }

        /// <summary>
        /// 下一个索引是否会越界
        /// </summary>
        /// <returns>越界则返回True  否则返回False</returns>
        public bool CheckNextOverflow () {
            var _next = current + 1;
            return _next > maxIndex;
        }

        public bool CheckPrevOverflow () {
            var _next = current - 1;
            return _next < 0;
        }

    }

}